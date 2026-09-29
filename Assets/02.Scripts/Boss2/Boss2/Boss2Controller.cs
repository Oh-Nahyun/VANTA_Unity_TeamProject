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
    [SerializeField] private float _skillHitThreshold = 100f;
    [SerializeField] private float _instantKillWeightPerHit = 1f;
    [Tooltip("현재 누적 가중치 (플레이 중 확인용)")]
    [SerializeField] private float _instantKillWeight = 0f;

    private const string InstantKillName = "즉사기";

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

    private Boss2State _state = Boss2State.Idle;
    private float _stateEnterTime;
    private Boss2Action _currentAction;
    private string _lastChoiceName;

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
        if (_state == Boss2State.Idle || _state == Boss2State.Dead)
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

        EnterState(Boss2State.Think);
    }

    [ContextMenu("FSM/Stop")]
    public void StopFsm()
    {
        StopAllPatterns();
        EnterState(Boss2State.Idle);
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

        if (_instantKillWeight > 0f)
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
            _instantKillWeight = 0f;
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

    // 직전과 같은 선택은 가중치를 절반으로 해 반복을 줄인다.
    private float AdjustForRepeat(string name, float weight)
    {
        return name == _lastChoiceName ? weight * 0.5f : weight;
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

        _attack.ApplyAttackPattern(_currentAction.Attack, _currentAction.PickComboCount());
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
            if (skillType == Boss2SkillPatternType.AreaOfEffect)
            {
                _pendingAoeCount++;
            }
            EnterState(Boss2State.Think);
            return;
        }

        EnterState(Boss2State.Skill);
    }

    private void EnterDead()
    {
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
        if (_state != next)
        {
            Log($"{_state} → {next}");
        }

        _state = next;
        _stateEnterTime = Time.time;
    }

    // ---- 피격 ----
    private void OnBossDamaged(float damage)
    {
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
                Weight = 4f,
            },
            new Boss2Action
            {
                Name = "추격 강타", 
                Move = Boss2MovePatternType.ChaseRun, 
                Attack = Boss2AttackPatternType.SmashAttack,
                DistanceMin = 4f, 
                DistanceMax = 999f, 
                Cooldown = 6f, 
                Weight = 3f,
            },
            new Boss2Action
            {
                Name = "기습 회전베기", 
                Move = Boss2MovePatternType.ChaseBlink, 
                Attack = Boss2AttackPatternType.SpinningSlash,
                DistanceMin = 3f, 
                DistanceMax = 999f, 
                HealthMax = 0.7f, 
                Cooldown = 8f, 
                Weight = 3f,
            },
            new Boss2Action
            {
                Name = "근접 회전 반격", 
                Move = Boss2MovePatternType.None, 
                Attack = Boss2AttackPatternType.SpinningSlash,
                DistanceMin = 0f, 
                DistanceMax = 1.5f, 
                HealthMax = 0.4f, 
                Cooldown = 10f, 
                Weight = 3f,
            },
        };
    }
}
