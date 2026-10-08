using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스2의 두뇌(FSM). 무엇을 할지만 정하고, 실제 이동·공격·스킬은 각 패턴 컴포넌트가 처리한다.
///
///   Think → (Move) → Attack → Recover → Think ...
///   Think → Skill → Recover → Think ...
///
/// 스킬 규칙
///   광역기: 체력이 _aoeHealthThresholds 선을 넘을 때마다 1회 예약 → 다음 Think 에서 가장 먼저 시전
///   즉사기: 큰 데미지(플레이어 스킬)를 맞을 때마다 누적 가중치 +1 → 일반 행동과 같은 풀에서 뽑힘, 사용 시 0
/// </summary>
public class Boss2Controller : MonoBehaviour
{
    private enum Boss2State { Idle, Think, Move, Attack, Skill, Recover, Dead }

    [Header("FSM")]
    [SerializeField] private bool _runOnStart = true;
    [SerializeField] private float _thinkDelay = 0.3f;
    [SerializeField] private float _recoverTime = 0.8f;
    [Tooltip("이동이 이 시간 안에 끝나지 않으면(플레이어가 계속 도망) 공격을 생략하고 다시 판단")]
    [SerializeField] private float _moveTimeout = 4f;
    [Tooltip("상태 전이와 선택 이유를 콘솔에 찍는다")]
    [SerializeField] private bool _logDecisions = false;

    [Header("행동 (이동 → 공격)")]
    [SerializeField] private List<Boss2Action> _actions = CreateDefaultActions();

    [Header("광역기: 체력 선마다 1회")]
    [SerializeField] private float[] _aoeHealthThresholds = { 0.8f, 0.6f, 0.4f, 0.2f };

    [Header("즉사기: 플레이어 스킬을 맞을수록 확률 상승")]
    [Tooltip("이 값 이상의 데미지를 플레이어 스킬로 본다")]
    [SerializeField] private int _skillHitThreshold = 100;
    [SerializeField] private float _instantKillWeightPerHit = 1f;
    [Tooltip("현재 누적 가중치 (플레이 중 확인용)")]
    [SerializeField] private float _instantKillWeight = 0f;
    [Tooltip("최대 누적 가중치 : 즉사기 발동 조건 횟수 ")]
    [SerializeField] private float _maxInstantKillWeight = 3f; 

    // 무적: 행동은 Boss2Action.IsInvincible 로 여기서, 스킬(광역기·즉사기)은 Boss2SkillPattern 이 직접 처리한다.

    [Tooltip("이 시간(초) 동안 한 대도 안 맞으면 즉사기")]
    [SerializeField] private float _noDamageKillTime = 10f;
    private float _lastDamagedTime;   // 마지막으로 맞은(또는 다시 잰) 시각
    
    
    private const string InstantKillName = "InstantKill";

    // 후보가 하나도 없을 때 거리를 좁히기 위한 기본 행동
    private readonly Boss2Action _fallbackAction = new Boss2Action
    {
        Name = "거리 좁히기(기본)",
        Move = Boss2MovePatternType.PressureWalk,
        Attack = Boss2AttackPatternType.None,
    };

    private Boss2 _boss;
    private Boss2MovePattern _move;
    private Boss2AttackPattern _attack;
    private Boss2SkillPattern _skill;
    // 없어도 된다. 있으면 GhostTrail 행동의 이동·공격 동안 켠다.
    private Boss2GhostTrail _ghostTrail;

    private Boss2State _state = Boss2State.Idle;
    private float _stateEnterTime;
    private Boss2Action _currentAction;
    private string _lastChoiceName;

    // 테스트로 실행한 행동인지. 끝나면 다음 판단(Think)으로 가지 않고 Idle 에서 멈춘다.
    private bool _isTestRun;

    // 일시정지: 상태·진행 중인 행동은 그대로 두고 FSM 판단만 멈춘다. (StopFsm 은 행동을 취소한다)
    private bool _isPaused;
    private float _pauseStartTime;

    // 연속 테스트에서 남은 행동들. 하나가 끝나면 Recover 없이 바로 다음 것을 꺼내 실행한다.
    private readonly Queue<System.Action> _testQueue = new Queue<System.Action>();

    private bool[] _aoeTriggered;
    private int _pendingAoeCount;

    private Transform Target => _move != null ? _move.Target : null;
    private float StateElapsed => Time.time - _stateEnterTime;

    private void Awake()
    {
        _boss = GetComponent<Boss2>();
        _move = GetComponent<Boss2MovePattern>();
        _attack = GetComponent<Boss2AttackPattern>();
        _skill = GetComponent<Boss2SkillPattern>();
        _ghostTrail = GetComponent<Boss2GhostTrail>();

        _aoeTriggered = new bool[_aoeHealthThresholds.Length];
    }

    private void OnEnable()
    {
        if (_boss != null)
        {
            _boss.Damaged += OnBossDamaged;
        }
    }

    private void OnDisable()
    {
        if (_boss != null)
        {
            _boss.Damaged -= OnBossDamaged;
        }
    }

    private void Start()
    {
        // 시작부터 이미 넘어 있는 선은 발동하지 않는다.
        for (int i = 0; i < _aoeHealthThresholds.Length; i++)
        {
            _aoeTriggered[i] = _boss.HealthRatio <= _aoeHealthThresholds[i];
        }

        if (Target == null)
        {
            TryFindTarget();
        }

        if (_runOnStart)
        {
            StartFsm();
        }
    }

    private void Update()
    {
        if (_isPaused || _state == Boss2State.Idle || _state == Boss2State.Dead)
            return;

        if (_boss.IsDead)
        {
            EnterDead();
            return;
        }

        switch (_state)
        {
            case Boss2State.Think:
            {
                if (StateElapsed >= _thinkDelay)
                {
                    Decide();
                }
                break;
            }
            case Boss2State.Move:
            {
                if (_move.IsPatternRunning == false)
                {
                    EnterAttack();
                }
                else if (StateElapsed >= _moveTimeout)
                {
                    Log($"이동 시간 초과 → 공격 생략 ({_currentAction.Name})");
                    _move.ApplyMovePattern(Boss2MovePatternType.None);
                    EnterState(Boss2State.Think);
                }
                break;
            }
            case Boss2State.Attack:
            {
                if (_attack.IsPatternRunning == false && _attack.IsAttacking == false)
                {
                    EnterState(Boss2State.Recover);
                }
                break;
            }
            case Boss2State.Skill:
            {
                if (_skill.IsSkillRunning == false)
                {
                    EnterState(Boss2State.Recover);
                }
                break;
            }
            case Boss2State.Recover:
            {
                if (StateElapsed >= _recoverTime)
                {
                    EnterState(Boss2State.Think);
                }
                break;
            }
        }
    }

    [ContextMenu("FSM/Start")]
    public void StartFsm()
    {
        if (_boss.IsDead)
            return;
        
        // 새로 시작하는 것이므로 일시정지 중이었다면 풀린 상태로 시작한다.
        _isPaused = false;
        _lastDamagedTime = Time.time;
        EnterState(Boss2State.Think);
    }

    [ContextMenu("FSM/Stop")]
    public void StopFsm()
    {
        _isPaused = false;
        _isTestRun = false;
        _testQueue.Clear();
        StopAllPatterns();
        EnterState(Boss2State.Idle);
    }

    public bool IsPaused => _isPaused;

    /// <summary>
    /// FSM 판단을 그 자리에서 멈춘다. 현재 상태와 진행 중인 행동은 취소하지 않으므로 Resume 하면 이어서 진행한다.
    /// 애니메이션·이동·공격 코루틴은 멈추지 않는다. (보스 전체 일시정지는 Boss2.PauseBoss 에서 함께 처리)
    /// </summary>
    [ContextMenu("FSM/Pause")]
    public void Pause()
    {
        if (_isPaused)
            return;

        _isPaused = true;
        _pauseStartTime = Time.time;
        Log($"일시정지 ({_state})");
    }

    /// <summary>
    /// 멈췄던 상태에서 이어서 진행한다. 멈춰 있던 시간은 상태 경과 시간(Think 대기·이동 시간 초과·Recover)에서 뺀다.
    /// </summary>
    [ContextMenu("FSM/Resume")]
    public void Resume()
    {
        if (_isPaused == false)
            return;

        _isPaused = false;
        _stateEnterTime += Time.time - _pauseStartTime;
        _lastDamagedTime += Time.time - _pauseStartTime;
        Log($"재개 ({_state})");
    }

    // ---- 테스트: 액션·스킬 하나를 실제 FSM 경로(무적·Recover 포함)로 실행하고 끝나면 Idle 로 멈춘다 ----
    public IReadOnlyList<Boss2Action> Actions => _actions;
    public bool IsTestRunning => _isTestRun;
    public string CurrentStateName => _state.ToString();
    public int TestQueueRemaining => _testQueue.Count;

    /// <summary>
    /// _actions[index] 를 조건(거리·체력·쿨다운)과 상관없이 바로 실행한다. 플레이 중에만 동작.
    /// </summary>
    public void TestAction(int index)
    {
        if (CanStartTest() == false || index < 0 || index >= _actions.Count)
            return;

        StartAction(_actions[index], "테스트");
    }

    /// <summary>
    /// _actions 를 순서대로 모두 실행한다. 하나가 끝나면 Recover 를 거치지 않고 바로 다음 행동으로 넘어간다.
    /// includeSkills 면 마지막에 즉사기·광역기도 이어서 실행한다. 플레이 중에만 동작.
    /// </summary>
    public void TestAllActions(bool includeSkills)
    {
        if (CanStartTest() == false)
            return;

        for (int i = 0; i < _actions.Count; i++)
        {
            Boss2Action action = _actions[i];
            string reason = $"연속 테스트 {i + 1}/{_actions.Count}";
            _testQueue.Enqueue(() => StartAction(action, reason));
        }

        if (includeSkills)
        {
            _testQueue.Enqueue(() => StartSkill(Boss2SkillPatternType.InstantKill, "연속 테스트"));
            _testQueue.Enqueue(() => StartSkill(Boss2SkillPatternType.AreaOfEffect, "연속 테스트"));
        }

        RunNextTest();
    }

    [ContextMenu("Test/All Actions")]
    private void TestAllActionsOnly() => TestAllActions(false);

    [ContextMenu("Test/All Actions + Skills")]
    private void TestAllActionsAndSkills() => TestAllActions(true);

    private bool RunNextTest()
    {
        if (_testQueue.Count == 0)
            return false;

        _testQueue.Dequeue().Invoke();
        return true;
    }

    /// <summary>
    /// 즉사기·광역기를 조건(가중치·체력 선)과 상관없이 바로 실행한다. 플레이 중에만 동작.
    /// </summary>
    public void TestSkill(Boss2SkillPatternType skillType)
    {
        if (CanStartTest() == false || skillType == Boss2SkillPatternType.None)
            return;

        StartSkill(skillType, "테스트");
    }

    [ContextMenu("Test/InstantKill")]
    private void TestInstantKill() => TestSkill(Boss2SkillPatternType.InstantKill);

    [ContextMenu("Test/AreaOfEffect")]
    private void TestAreaOfEffect() => TestSkill(Boss2SkillPatternType.AreaOfEffect);

    // 진행 중인 행동을 모두 멈추고 테스트 모드로 들어간다.
    private bool CanStartTest()
    {
        if (Application.isPlaying == false || _boss == null || _boss.IsDead)
            return false;

        _testQueue.Clear();
        _isPaused = false;
        StopAllPatterns();
        if (Target == null)
        {
            TryFindTarget();
        }

        _isTestRun = true;
        return true;
    }

    // ---- 판단 ----
    private void Decide()
    {
        // 1. 체력 선을 넘어 예약된 광역기가 있으면 가장 먼저
        if (_pendingAoeCount > 0)
        {
            _pendingAoeCount--;
            StartSkill(Boss2SkillPatternType.AreaOfEffect, $"광역기 (체력 선, 남은 예약 {_pendingAoeCount})");
            return;
        }

        if (Target == null && TryFindTarget() == false)
        {
            Log("대상이 없어 대기");
            EnterState(Boss2State.Think);
            return;
        }
        
        // 중간에 데미지 안입은 시간이 일정시간 이상 되면 즉사기 발동!
        if (Time.time - _lastDamagedTime >= _noDamageKillTime)
        {
            StartSkill(Boss2SkillPatternType.InstantKill, $"{_noDamageKillTime}초 무피격");
            return;
        }

        float distance = Vector2.Distance(transform.position, Target.position);
        float healthRatio = _boss.HealthRatio;
        float now = Time.time;

        // 2. 일반 행동 + 즉사기(누적 가중치)를 한 풀에 넣고 가중치로 뽑는다.
        List<Boss2Action> candidates = new List<Boss2Action>();
        List<float> weights = new List<float>();
        foreach (Boss2Action action in _actions)
        {
            if (action.IsAvailable(distance, healthRatio, now))
            {
                candidates.Add(action);
                weights.Add(AdjustForRepeat(action.Name, action.Weight));
            }
        }

        if (_instantKillWeight > _maxInstantKillWeight)
        {
            candidates.Add(null);   // null = 즉사기
            weights.Add(AdjustForRepeat(InstantKillName, _instantKillWeight));
        }

        if (candidates.Count == 0)
        {
            StartAction(_fallbackAction, $"후보 없음 (거리 {distance:F1})");
            return;
        }

        int picked = PickWeighted(weights);
        string reason = $"거리 {distance:F1}, 체력 {healthRatio:P0}, 후보 {candidates.Count}, 즉사기 가중치 {_instantKillWeight}";

        if (candidates[picked] == null)
        {
            StartSkill(Boss2SkillPatternType.InstantKill, reason);
        }
        else
        {
            StartAction(candidates[picked], reason);
        }
    }

    // 이동 패턴의 대상이 비어 있으면 씬의 Player 를 찾아 넣는다. (인스펙터 연결이 끊겨도 싸움이 멈추지 않도록)
    private bool TryFindTarget()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null)
            return false;

        _move.SetTarget(player.transform);
        Log($"대상 자동 연결: {player.name}");
        return true;
    }

    // 직전과 같은 선택은 가중치를 0으로 해 반복을 없앤다. 원래는 0.5f 절반
    private float AdjustForRepeat(string name, float weight)
    {
        return name == _lastChoiceName ? weight * 0f : weight;
    }

    private static int PickWeighted(List<float> weights)
    {
        float total = 0f;
        foreach (float w in weights)
        {
            total += w;
        }

        float roll = Random.value * total;
        for (int i = 0; i < weights.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return i;
        }

        return weights.Count - 1;
    }

    // ---- 실행 ----
    private void StartAction(Boss2Action action, string reason)
    {
        _currentAction = action;
        _lastChoiceName = action.Name;
        action.LastUsedTime = Time.time;
        Log($"행동: {action.Name} ({action.Move} → {action.Attack}) / {reason}");

        if (action.Move != Boss2MovePatternType.None)
        {
            _move.ApplyMovePattern(action.Move);
            EnterState(Boss2State.Move);
        }
        else
        {
            EnterAttack();
        }
    }

    private void EnterAttack()
    {
        // 공격하며 미끄러지지 않도록 이동을 먼저 끈다.
        _move.ApplyMovePattern(Boss2MovePatternType.None);

        if (_currentAction == null || _currentAction.Attack == Boss2AttackPatternType.None)
        {
            EnterState(Boss2State.Think);
            return;
        }

        // 제자리 공격은 속도로 방향이 정해지지 않으므로 플레이어를 바라보게 한다.
        if (Target != null)
        {
            _move.FaceTowards(Target.position);
        }

        _attack.ApplyAttackPattern(_currentAction.Attack, _currentAction.PickComboCount(), _currentAction.ProjectileType);
        EnterState(Boss2State.Attack);
    }

    private void StartSkill(Boss2SkillPatternType skillType, string reason)
    {
        _currentAction = null;
        _lastChoiceName = skillType == Boss2SkillPatternType.InstantKill ? InstantKillName : skillType.ToString();
        Log($"스킬: {skillType} / {reason}");

        _move.ApplyMovePattern(Boss2MovePatternType.None);
        _skill.ApplySkillPattern(skillType);

        // 다른 공격이 아직 끝나지 않아 거절됐으면 다시 판단한다.
        if (_skill.IsSkillRunning == false)
        {
            Log($"스킬 {skillType} 시작 실패 → 다시 판단");
            // 테스트로 실행한 광역기는 체력 선 예약이 아니므로 다시 예약하지 않는다.
            if (skillType == Boss2SkillPatternType.AreaOfEffect && _isTestRun == false)
            {
                _pendingAoeCount++;
            }
            EnterState(Boss2State.Think);
            return;
        }

        // 즉사기 누적 가중치는 실제로 시전됐을 때만 비운다. (거절되면 다음 판단에서 다시 후보)
        if (skillType == Boss2SkillPatternType.InstantKill)
        {
            _lastDamagedTime = Time.time;
            _instantKillWeight = 0f;
        }

        EnterState(Boss2State.Skill);
    }

    private void EnterDead()
    {
        _testQueue.Clear();
        StopAllPatterns();
        EnterState(Boss2State.Dead);
    }

    private void StopAllPatterns()
    {
        _move.ApplyMovePattern(Boss2MovePatternType.None);
        _attack.ApplyAttackPattern(Boss2AttackPatternType.None);
        _skill.ApplySkillPattern(Boss2SkillPatternType.None);
    }

    private void EnterState(Boss2State next)
    {
        // 연속 테스트 중이면 행동이 끝나는 즉시(Recover 생략) 다음 행동을 실행한다.
        if (_isTestRun && (next == Boss2State.Recover || next == Boss2State.Think) && RunNextTest())
            return;

        // 테스트 행동이 끝나 다음 판단으로 넘어가려 하면 Idle 에서 멈춘다. (Recover 후·이동 시간 초과·스킬 거절 모두)
        if (_isTestRun && next == Boss2State.Think)
        {
            _isTestRun = false;
            next = Boss2State.Idle;
            Log("테스트 종료 → Idle");
        }

        if (_state != next)
        {
            Log($"{_state} → {next}");
        }

        _state = next;
        _stateEnterTime = Time.time;

        // 행동 무적은 상태가 바뀔 때마다 여기서 정한다.
        // Skill 상태의 무적은 Boss2SkillPattern 이 시작·종료 때 직접 켜고 끄므로 덮어쓰지 않는다.
        if (_boss != null && next != Boss2State.Skill)
        {
            _boss.SetInvincible(IsInvincibleIn(next));
        }

        // 고스트 트레일도 무적과 같은 규칙으로 행동의 이동·공격 동안만 켠다.
        if (_ghostTrail != null)
        {
            _ghostTrail.SetFsmTrail(IsActionPhase(next) && _currentAction.GhostTrail);
        }
    }

    // 무적 행동은 이동부터 공격이 끝날 때까지 무적. Think · Recover · Idle · Dead 에서는 해제된다.
    private bool IsInvincibleIn(Boss2State state)
    {
        return IsActionPhase(state) && _currentAction.IsInvincible;
    }

    // 행동(이동 → 공격)이 진행 중인 상태인지
    private bool IsActionPhase(Boss2State state)
    {
        return _currentAction != null && (state == Boss2State.Move || state == Boss2State.Attack);
    }

    // ---- 피격 ----
    private void OnBossDamaged(int damage)
    {
        _lastDamagedTime = Time.time;
        
        if (damage >= _skillHitThreshold)
        {
            _instantKillWeight += _instantKillWeightPerHit;
            Log($"큰 피격 {damage} → 즉사기 가중치 {_instantKillWeight}");
        }

        // 한 번에 선을 여러 개 넘으면 넘은 만큼 예약한다.
        float healthRatio = _boss.HealthRatio;
        for (int i = 0; i < _aoeHealthThresholds.Length; i++)
        {
            if (_aoeTriggered[i] == false && healthRatio <= _aoeHealthThresholds[i])
            {
                _aoeTriggered[i] = true;
                _pendingAoeCount++;
                Log($"체력 {healthRatio:P0} → {_aoeHealthThresholds[i]:P0} 선 통과, 광역기 예약 {_pendingAoeCount}");
            }
        }
    }

    private void Log(string message)
    {
        if (_logDecisions)
        {
            Debug.Log($"[Boss2FSM] {message}");
        }
    }

    private static List<Boss2Action> CreateDefaultActions()
    {
        return new List<Boss2Action>
        {
            new Boss2Action
            {
                Name = "근접 난타", 
                Move = Boss2MovePatternType.None,
                Attack = Boss2AttackPatternType.ComboAttack,
                ComboMin = 2,
                ComboMax = 3,
                DistanceMin = 0f,
                DistanceMax = 1.5f,
                Cooldown = 0f,
                IsInvincible = false,
                Weight = 5f,
            },
            new Boss2Action
            {
                Name = "압박 콤보",
                Move = Boss2MovePatternType.PressureWalk,
                Attack = Boss2AttackPatternType.ComboAttack,
                ComboMin = 3, 
                ComboMax = 4, 
                DistanceMin = 1.5f, 
                DistanceMax = 5f, 
                Cooldown = 3f, 
                IsInvincible = false,
                Weight = 2f,
            },
            new Boss2Action
            {
                Name = "추격 강타", 
                Move = Boss2MovePatternType.ChaseRun, 
                Attack = Boss2AttackPatternType.SmashAttack,
                DistanceMin = 1.5f,
                DistanceMax = 100f,
                Cooldown = 6f,
                IsInvincible = true,
                GhostTrail = true,
                Weight = 4f,
            },
            new Boss2Action
            {
                Name = "기습 회전베기", 
                Move = Boss2MovePatternType.ChaseBlink, 
                Attack = Boss2AttackPatternType.SpinningSlash,
                DistanceMin = 1.5f, 
                DistanceMax = 100f, 
                HealthMax = 0.7f,
                Cooldown = 8f,
                IsInvincible = true,
                GhostTrail = true,
                Weight = 3f,
            },
            new Boss2Action
            {
                Name = "근접 회전 반격", 
                Move = Boss2MovePatternType.None,
                Attack = Boss2AttackPatternType.SpinningSlashInPlace,
                DistanceMin = 0f, 
                DistanceMax = 1.5f, 
                HealthMax = 0.4f, 
                Cooldown = 10f,
                IsInvincible = true,
                Weight = 3f,
            },
            new Boss2Action
            {
                Name = "후퇴 회전 반격", 
                Move = Boss2MovePatternType.BackStep, 
                Attack = Boss2AttackPatternType.BladeAuraSlash,
                DistanceMin = 0f, 
                DistanceMax = 1.5f, 
                Cooldown = 5f, 
                IsInvincible = true,
                Weight = 3f,
            }
        };
    }
}
