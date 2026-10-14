using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Boss2SkillPattern : Boss2AttackBase
{
    // 주의: 기반 클래스가 private Start / Update 를 갖고 있다.
    // 여기서 Start, Update, Awake 를 선언하면 Unity 가 이쪽만 호출해 기반 쪽 갱신이 멈춘다.
    // 그래서 이 클래스는 OnEnable / OnDisable 만 쓴다.
    //
    // 주의: 이 컴포넌트의 Attack Box Collider 는 비워 둔다.
    // 채우면 일반 콤보 때 Boss2AttackPattern 과 함께 두 번 데미지를 준다.
    // 같은 이유로 스킬 클립에는 StartDamage 같은 공용 이벤트 대신 전용 이벤트(InstantKillHit, SpawnAreaStrikes)를 쓴다.

    [Header("즉사기")]
    [SerializeField] private string _instantKillStartState = "StartInstantKillSkill";

    [SerializeField] private string _instantKillEndState = "EndInstantKillSkill";

    // 점프(Start)가 끝나면 플레이어 위 이 높이에 나타나 공격(End)한다.
    [SerializeField] private float _instantKillAppearHeight = 1f;

    // 나타난 순간의 플레이어 자리(보스 바로 아래)에 놓이는 판정 원
    [SerializeField] private float _instantKillRadius = 1f;

    [Header("광역기")]
    [SerializeField] private string _aoeStateName = "Boss2AOESkill";

    [SerializeField] private Boss2AreaStrike _areaStrikePrefab;
    [SerializeField] private int _strikeCountMin = 10;
    [SerializeField] private int _strikeCountMax = 15;

    // 더미끼리 이 거리보다 가까이 생기지 않게 한다. 자리가 부족하면 조건을 포기하고 배치한다.
    [SerializeField] private float _minStrikeSpacing = 1f;

    [Header("광역기 플레이어 주위")]
    [Tooltip("전체 더미 중 시전 순간의 플레이어 주위에 놓을 개수. 범위 안에서 매번 랜덤, 나머지는 맵 전체에 놓는다")]
    [SerializeField] private int _nearPlayerCountMin = 4;
    [SerializeField] private int _nearPlayerCountMax = 6;

    [Tooltip("플레이어 주위 더미를 놓을 반경 (플레이어 중심)")]
    [SerializeField] private float _nearPlayerRadius = 2f;

    [Header("광역기 순차 생성")]
    [Tooltip("다음 묶음까지 대기 시간(초). 범위 안에서 매번 랜덤")]
    [SerializeField] private float _spawnIntervalMin = 0.08f;
    [SerializeField] private float _spawnIntervalMax = 0.15f;

    [Tooltip("한 번에 동시에 나타나는 개수. 범위 안에서 매번 랜덤")]
    [SerializeField] private int _strikesPerWaveMin = 1;
    [SerializeField] private int _strikesPerWaveMax = 3;

    [Tooltip("더미가 끝나길 기다리는 최대 시간(초). 이상이 생겨도 스킬이 영원히 안 끝나는 일을 막는다")]
    [SerializeField] private float _aoeMaxDuration = 15f;

    [Header("맵 범위 (타원)")]
    [Tooltip("켜면 씬의 맵 벽(EdgeColliderGenerator) 타원을 그대로 쓴다. 씬에 없으면 아래 값을 쓴다")]
    [SerializeField] private bool _useSceneMapWall = true;

    [Tooltip("맵 벽에서 이만큼 안쪽에만 더미를 놓는다 (벽에 걸치지 않게)")]
    [SerializeField] private float _arenaInset = 0.5f;

    [SerializeField] private Vector2 _arenaCenter = Vector2.zero;

    // 타원의 가로·세로 전체 길이. EdgeColliderGenerator 와 같은 규격.
    [SerializeField] private Vector2 _arenaSize = new Vector2(10f, 6f);

    private EdgeColliderGenerator _mapWall;

    [Header("판정 대상")]
    // 기본은 모든 레이어. 대상은 태그(Player)로 한 번 더 거른다.
    [SerializeField] private LayerMask _targetLayers = ~0;

    [Header("효과")]
    [SerializeField] private ParticleSystem _areaOfEffectCircle;

    [SerializeField] private GameObject _fogEffect;
    [SerializeField] private GameObject _moonLightEffect;
    
    
    [Header("사운드")]
    [SerializeField] private AudioClip _instantKillVoice;
    [SerializeField] private AudioClip _areaOfEffectVoice;

    
    [Header("광역기 어둠 연출")]
    [Tooltip("씬의 Boss2DarknessEffect. 비워 두면 어둠 연출 없이 진행한다")]
    [SerializeField] private Boss2DarknessEffect _darknessEffect;

    [Tooltip("어둠 동안 보스를 약하게 비추는 Point Light 2D (Boss2 자식)")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D _bossDarknessLight;

    // 이번 스킬이 어둠을 켰는지. 끝날 때(정상·취소 공통) 이 값으로 되돌린다.
    private bool _darkenedBySkill;


    // 회피(QTE) 판정. 대기 시간 등 설정은 Boss2AvoidJudge 컴포넌트에 있다.
    private Boss2AvoidJudge _avoidJudge;

    // 이번 즉사기에서 이미 플레이어 위로 순간이동했는지
    private bool _hasTeleported;

    [Header("즉사기 잠식 연출")]
    [Tooltip("검은 입자가 빨려들며 몸을 덮는 효과 (SuckIn + Cover 를 자식으로 묶은 부모)")]
    [SerializeField] private ParticleSystem _engulfEffect;

    [Tooltip("순간이동 직후 몸에서 검은 입자가 흩어져 나가는 효과")]
    [SerializeField] private ParticleSystem _releaseEffect;

    private const int MaxSpacingAttempts = 30;

    private Boss2SkillPatternType _currentSkillPatternType = Boss2SkillPatternType.None;
    private Rigidbody2D _rigidbody2D;
    private Boss2MoveBase _mover;
    private Boss2MovePattern _movePattern;
    private Boss2AttackPattern _attackPattern;

    private Coroutine _skillRoutine;

    // 광역기: 위치는 한 번에 정해 섞어 두고, 이 큐에서 꺼내 순서대로 생성한다.
    private readonly Queue<Vector2> _strikeQueue = new Queue<Vector2>();
    // 생성된 더미. 모두 사라져야 광역기가 끝난다. (더미는 Finish 이벤트나 수명으로 스스로 파괴됨)
    private readonly List<Boss2AreaStrike> _activeStrikes = new List<Boss2AreaStrike>();
    private Coroutine _spawnRoutine;

    // 이번 스킬이 직접 이동을 막았는지. 다른 효과(크리스탈 프리즌 등)가 걸어둔 봉쇄는 풀지 않기 위해 기억한다.
    private bool _blockedMoveBySkill;

    [Header("스킬 무적")]
    [Tooltip("광역기를 시전하는 동안 무적")]
    [SerializeField] private bool _aoeInvincible = true;

    [Tooltip("즉사기를 시전하는 동안 무적")]
    [SerializeField] private bool _instantKillInvincible = true;

    private Boss2 _boss;

    // 이번 스킬이 무적을 켰는지. 끝날 때(정상·취소 공통) 이 값으로 되돌린다.
    private bool _invincibleBySkill;

    public bool IsSkillRunning => _currentSkillPatternType != Boss2SkillPatternType.None;
    public Boss2SkillPatternType CurrentSkillPatternType => _currentSkillPatternType;

    // 보스는 나타난 뒤 제자리에 묶이므로, 보스 바로 아래가 곧 나타난 순간의 플레이어 자리다.
    private Vector2 InstantKillCenter => (Vector2)transform.position + Vector2.down * _instantKillAppearHeight;

    private void OnEnable()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _mover = GetComponent<Boss2MoveBase>();
        _movePattern = GetComponent<Boss2MovePattern>();
        _attackPattern = GetComponent<Boss2AttackPattern>();
        _boss = GetComponent<Boss2>();
        _avoidJudge = Boss2AvoidJudge.GetOrAdd(gameObject);
        FindMapWall();
        _moonLightEffect.SetActive(false);
        _fogEffect.GetComponent<ParticleSystem>().Play();
    }

    private void FindMapWall()
    {
        if (_useSceneMapWall && _mapWall == null)
        {
            _mapWall = FindAnyObjectByType<EdgeColliderGenerator>();
        }
    }

    // 광역기 더미를 놓는 타원. 씬의 맵 벽이 있으면 그 타원을, 없으면 인스펙터 값을 쓴다.
    private Vector2 ArenaCenter => _useSceneMapWall && _mapWall != null ? _mapWall.Center : _arenaCenter;

    private Vector2 ArenaSize
    {
        get
        {
            Vector2 size = _useSceneMapWall && _mapWall != null ? _mapWall.Size : _arenaSize;
            float inset = _arenaInset * 2f;
            return new Vector2(Mathf.Max(size.x - inset, 0.1f), Mathf.Max(size.y - inset, 0.1f));
        }
    }

    private void OnDisable()
    {
        StopSkill();
    }

    /// <summary>
    /// 스킬 명령. None 을 주면 진행 중인 스킬을 멈춘다.
    /// </summary>
    public void ApplySkillPattern(Boss2SkillPatternType skillPatternType)
    {
        switch (skillPatternType)
        {
            case Boss2SkillPatternType.None:
            {
                StopSkill();
                EndSkill();
                break;
            }
            case Boss2SkillPatternType.InstantKill:
            {
                InstantKillSkill();
                break;
            }
            case Boss2SkillPatternType.AreaOfEffect:
            {
                AreaOfEffectSkill();
                break;
            }
        }
    }

    /// <summary>
    /// 즉사기. 제자리에서 점프(Start)한 뒤, 플레이어 위(_instantKillAppearHeight)에 나타나 내려벤다(End).
    /// 판정은 나타난 순간의 플레이어 자리에 놓이므로, 공격 모션 동안 벗어나면 피할 수 있다.
    /// 판정 순간은 EndInstantKillSkill 클립의 InstantKillHit 이벤트가 정한다.
    /// </summary>
    public void InstantKillSkill()
    {
        if (CanStartSkill(Boss2SkillPatternType.InstantKill) == false)
            return;

        _currentSkillPatternType = Boss2SkillPatternType.InstantKill;
        // 이전 공격의 회피 결과가 남아 있으면 이번 즉사기가 데미지를 주지 못한다.
        _avoidJudge.ResetResult();
        _hasTeleported = false;
        _skillRoutine = StartCoroutine(InstantKillRoutine());
    }

    /// <summary>
    /// 광역기. 제자리에서 시전하고, Boss2AOESkill 클립의 SpawnAreaStrikes 이벤트에서 맵 곳곳에 공격 더미를 뿌린다.
    /// </summary>
    public void AreaOfEffectSkill()
    {
        if (CanStartSkill(Boss2SkillPatternType.AreaOfEffect) == false)
            return;

        _currentSkillPatternType = Boss2SkillPatternType.AreaOfEffect;
        _skillRoutine = StartCoroutine(AreaOfEffectRoutine());
    }

    // 애니메이션 이벤트: StartInstantKillSkill 에서 Dissolve 로 완전히 사라진 순간.
    // 안 보일 때 플레이어 위로 옮기고, 이어지는 곡선에서 다시 나타난다.
    public void InstantKillTeleport()
    {
        if (_currentSkillPatternType != Boss2SkillPatternType.InstantKill || _hasTeleported)
            return;

        AppearAboveTarget();
        _hasTeleported = true;

        // 이전 자리에서 덮던 입자는 따라오지 않게 정리하고, 새 자리에서 흩어지며 나타난다.
        StopEffect(_engulfEffect);
        if (_releaseEffect != null)
        {
            _releaseEffect.Play(true);
        }
    }

    // 애니메이션 이벤트: StartInstantKillSkill 에서 사라지기 전. 검은 입자가 빨려들며 몸을 덮는다.
    public void InstantKillEngulf()
    {
        if (_currentSkillPatternType != Boss2SkillPatternType.InstantKill || _engulfEffect == null)
            return;

        _engulfEffect.Play(true);
    }

    private static void StopEffect(ParticleSystem effect)
    {
        if (effect != null)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // 애니메이션 이벤트: EndInstantKillSkill 의 베는 프레임
    public void InstantKillHit()
    {
        // 회피 실패 시 데미지
        if (_avoidJudge.LastResult == false)
        {
            foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
            {
                Player player = playerObject.GetComponent<Player>();
                // Player 태그만 있고 Player 컴포넌트가 없는 오브젝트(테스트 더미 등)는 건너뛴다.
                if (player == null)
                    continue;

                // TakeDamage가 아니라 
                player.TakeDamage(); // 인자로 키보드 키값 KeyCode
                // StartAvoid
            }
        }
    }

    // 애니메이션 이벤트: EndInstantKillSkill 의 내려베기 직전.
    // 플레이어 회피 입력(QTE)을 시작하고, 결과가 올 때까지 보스 모션을 멈춘다. (Boss2AvoidJudge)
    // 즉사기는 거리와 상관없이 모든 플레이어가 대상이고, 피한 뒤 보스에게 반격하는 회피(Counter)다.
    public void AvoidPoint()
    {
        if (_currentSkillPatternType != Boss2SkillPatternType.InstantKill)
            return;

        _avoidJudge.Begin(InstantKillCenter, 0f, Boss2AvoidType.Counter);
    }

    // 애니메이션 이벤트: Boss2AOESkill 의 참격 프레임
    public void SpawnAreaStrikes()
    {
        if (_areaStrikePrefab == null)
        {
            Debug.LogWarning("Area Strike Prefab 이 비어 있어 광역기 더미를 만들 수 없습니다.");
            return;
        }

        // 1. 위치는 처음에 한 번에 정한다. (간격·맵 범위 규칙 유지)
        //    일부는 시전 순간의 플레이어 주위에, 나머지는 맵 전체에 놓는다.
        FindMapWall();
        int count = Random.Range(_strikeCountMin, _strikeCountMax + 1);
        List<Vector2> positions = new List<Vector2>(count);

        Transform target = FindAttackTarget();
        if (target != null)
        {
            Vector2 playerPosition = target.position;
            int nearCount = Mathf.Min(count, Random.Range(_nearPlayerCountMin, Mathf.Max(_nearPlayerCountMin, _nearPlayerCountMax) + 1));
            for (int i = 0; i < nearCount; i++)
            {
                positions.Add(PickStrikePosition(positions, () => RandomPointNear(playerPosition)));
            }
        }

        while (positions.Count < count)
        {
            positions.Add(PickStrikePosition(positions, RandomPointInArena));
        }

        // 2. 나타나는 순서만 섞는다. (Fisher–Yates)
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // 3. 큐에 담아 순서대로 생성한다. 이전 광역기의 남은 큐는 버린다.
        StopSpawningStrikes();
        foreach (Vector2 position in positions)
        {
            _strikeQueue.Enqueue(position);
        }

        _spawnRoutine = StartCoroutine(SpawnStrikesRoutine());
    }

    // 큐에서 묶음 단위로 꺼내 생성하고, 묶음 사이에 랜덤 간격만큼 기다린다.
    private IEnumerator SpawnStrikesRoutine()
    {
        while (_strikeQueue.Count > 0)
        {
            int wave = Random.Range(_strikesPerWaveMin, Mathf.Max(_strikesPerWaveMin, _strikesPerWaveMax) + 1);
            for (int i = 0; i < wave && _strikeQueue.Count > 0; i++)
            {
                // 판정 반경·데미지는 더미 프리팹이 갖고, 터지는 순간 더미가 직접 Player 를 때린다.
                Boss2AreaStrike strike = Instantiate(_areaStrikePrefab, _strikeQueue.Dequeue(), Quaternion.identity);
                _activeStrikes.Add(strike);
            }

            if (_strikeQueue.Count > 0)
            {
                float interval = Random.Range(_spawnIntervalMin, Mathf.Max(_spawnIntervalMin, _spawnIntervalMax));
                if (interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        _spawnRoutine = null;
    }

    // 생성이 끝났고, 생성된 더미가 모두 사라졌는지
    private bool AreAreaStrikesFinished()
    {
        _activeStrikes.RemoveAll(strike => strike == null);
        return _spawnRoutine == null && _strikeQueue.Count == 0 && _activeStrikes.Count == 0;
    }

    // 남은 생성을 멈춘다. 이미 생성된 더미는 그대로 터지고 사라진다.
    private void StopSpawningStrikes()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        _strikeQueue.Clear();
        _activeStrikes.Clear();
    }

    private bool CanStartSkill(Boss2SkillPatternType skillPatternType)
    {
        bool isAttackBusy = _attackPattern != null && (_attackPattern.IsPatternRunning || _attackPattern.IsAttacking);
        if (IsSkillRunning || isAttackBusy)
        {
            Debug.LogWarning($"다른 공격이나 스킬이 진행 중이라 {skillPatternType} 을 시작할 수 없습니다.");
            return false;
        }

        return true;
    }

    private IEnumerator InstantKillRoutine()
    {
        BeginSkill();
        _animator.CrossFade(_instantKillStartState, 0.05f, 0, 0f);

        // 사운드 부분
        SoundManager.Instance.PlayEffectSound(_instantKillVoice);

        // Start → End 는 컨트롤러 전이가 넘긴다. 두 상태를 거치므로 시간을 더하지 않고,
        // End 상태가 끝까지 재생되거나 End 를 벗어날 때까지 상태를 보고 기다린다.
        float timeout = GetClipLength(_instantKillStartState) + GetClipLength(_instantKillEndState) + 1f;
        float elapsed = 0f;
        bool hasEnteredEnd = false;
        while (elapsed < timeout)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(_instantKillEndState))
            {
                if (hasEnteredEnd == false && _hasTeleported == false)
                {
                    // 보통은 Start 클립의 InstantKillTeleport 이벤트에서 이미 이동한다.
                    // 이벤트가 빠졌을 때를 대비해 공격 시작 순간에라도 플레이어 위로 옮긴다.
                    AppearAboveTarget();
                    _hasTeleported = true;
                }

                hasEnteredEnd = true;
                if (state.normalizedTime >= 1f)
                    break;
            }
            else if (hasEnteredEnd)
            {
                break;
            }

            // 회피 결과를 기다리는 동안은 시간 초과 카운트를 멈춘다. (대기 시간 초과는 Boss2AvoidJudge 가 처리)
            if (_avoidJudge.IsWaiting == false)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        FinishSkill();
        EndSkill();
    }

    private void AppearAboveTarget()
    {
        // 순간이동은 Boss2AttackBase 의 공용 기능을 쓴다. (회전베기와 공유)
        if (AppearAbove(FindInstantKillTarget(), _instantKillAppearHeight) == false)
        {
            Debug.LogWarning("즉사기 대상을 찾지 못해 제자리에서 공격합니다.");
        }
    }

    private Transform FindInstantKillTarget()
    {
        return FindAttackTarget();
    }

    // 보스 발밑 = 몸통(피격) 콜라이더 아래 끝의 가운데. 콜라이더가 없으면 피벗 위치.
    private Vector3 GetFeetPosition()
    {
        Collider2D body = GetComponent<Collider2D>();
        if (body == null || body.enabled == false)
            return transform.position;

        Bounds bounds = body.bounds;
        return new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
    }

    private IEnumerator AreaOfEffectRoutine()
    {
        BeginSkill();
        _animator.CrossFade(_aoeStateName, 0.05f, 0, 0f);

        
        
        // 맵을 어둡게 하고 플레이어 주변·보스만 밝힌다. FinishSkill 에서 되돌린다.
        if (_darknessEffect != null)
        {
            _darknessEffect.Begin(FindInstantKillTarget(), _bossDarknessLight);
            _darkenedBySkill = true;
        }

        // 효과 부분
        // 마법진 중심이 보스 발밑에 오도록 한다. (피벗은 스프라이트 가운데라 그대로 쓰면 배 높이에 생긴다)
        Instantiate(_areaOfEffectCircle, GetFeetPosition(), transform.rotation);
        _moonLightEffect.SetActive(true);
        
        // 사운드 부분
        SoundManager.Instance.PlayEffectSound(_areaOfEffectVoice);
        
        // 이 사이 클립의 SpawnAreaStrikes 이벤트에서 더미 순차 생성이 시작된다.
        float clipLength = GetClipLength(_aoeStateName);
        yield return new WaitForSeconds(clipLength);

        // 마지막 더미가 터지고 사라질 때까지가 광역기다. 그동안 보스는 제자리에서 시전 상태를 유지한다.
        float waited = clipLength;
        while (AreAreaStrikesFinished() == false && waited < _aoeMaxDuration)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        FinishSkill();
        EndSkill();
    }

    private void BeginSkill()
    {
        // 시작 직전에 맞아 남아 있던 피격 트리거가 스킬 애니메이션을 끊지 않게 지운다.
        ClearPendingHit();

        // 스킬 중 미끄러지지 않게 위치를 고정한다. 외부 얼음과는 따로 관리되므로 끝날 때 풀어도 얼음은 유지된다.
        _blockedMoveBySkill = _mover != null;
        if (_blockedMoveBySkill)
        {
            _mover.HoldForPattern();
        }

        // 스킬 무적은 스킬이 직접 켠다. FSM·테스트 메뉴 등 어떤 경로로 시작해도 같게 동작한다.
        _invincibleBySkill = (_boss != null && IsInvincibleSkill(_currentSkillPatternType));
        if (_invincibleBySkill)
        {
            _boss.SetInvincible(true);
        }
    }

    private bool IsInvincibleSkill(Boss2SkillPatternType skillType)
    {
        return (skillType == Boss2SkillPatternType.AreaOfEffect && _aoeInvincible)
            || (skillType == Boss2SkillPatternType.InstantKill && _instantKillInvincible);
    }

    private void StopSkill()
    {
        // 광역기 생성은 스킬 코루틴과 따로 돌므로 먼저 멈춘다.
        StopSpawningStrikes();

        if (_skillRoutine == null)
            return;

        StopCoroutine(_skillRoutine);
        FinishSkill();
    }

    // 정상 종료든 취소든 반드시 거친다. 여기서 풀지 않으면 취소된 스킬 때문에 보스가 계속 묶여 있게 된다.
    private void FinishSkill()
    {
        // 회피 결과 대기 중에 스킬이 끊겨도 보스가 멈춘 채로 남지 않게 한다.
        _avoidJudge.Cancel();
        _animator.speed = 1f;

        // 즉사기가 끊겨도 잠식 입자가 남지 않게 한다.
        StopEffect(_engulfEffect);
        StopEffect(_releaseEffect);

        if (_darkenedBySkill)
        {
            _darknessEffect.End();
            _darkenedBySkill = false;
        }

        if (_blockedMoveBySkill)
        {
            _mover.ReleaseFromPattern();
            _blockedMoveBySkill = false;
        }

        // 이 스킬이 켠 무적만 끈다.
        if (_invincibleBySkill)
        {
            _boss.SetInvincible(false);
            _invincibleBySkill = false;
        }

        _skillRoutine = null;
    }

    private void EndSkill()
    {
        _currentSkillPatternType = Boss2SkillPatternType.None;
    }

    // sampler 로 뽑고, 이미 놓인 더미와 최소 간격을 지키도록 몇 번 다시 뽑는다.
    private Vector2 PickStrikePosition(List<Vector2> placed, System.Func<Vector2> sampler)
    {
        Vector2 candidate = sampler();
        for (int attempt = 0; attempt < MaxSpacingAttempts; attempt++)
        {
            if (IsFarEnough(candidate, placed))
                return candidate;

            candidate = sampler();
        }

        return candidate;
    }

    // center 중심 _nearPlayerRadius 원 안 균일 분포. 맵 타원 밖이면 타원 안쪽으로 끌어당긴다.
    private Vector2 RandomPointNear(Vector2 center)
    {
        Vector2 point = center + Random.insideUnitCircle * _nearPlayerRadius;
        return ClampToArena(point);
    }

    // 맵 타원 밖의 점을 같은 방향의 타원 경계 조금 안쪽으로 옮긴다.
    private Vector2 ClampToArena(Vector2 point)
    {
        Vector2 center = ArenaCenter;
        Vector2 halfSize = ArenaSize * 0.5f;
        Vector2 local = point - center;
        float nx = local.x / Mathf.Max(halfSize.x, 0.0001f);
        float ny = local.y / Mathf.Max(halfSize.y, 0.0001f);
        float distance = Mathf.Sqrt(nx * nx + ny * ny);
        if (distance <= 1f)
            return point;

        const float InsideRatio = 0.95f;
        return center + local / distance * InsideRatio;
    }

    private bool IsFarEnough(Vector2 candidate, List<Vector2> placed)
    {
        float minSqr = _minStrikeSpacing * _minStrikeSpacing;
        foreach (Vector2 other in placed)
        {
            if ((candidate - other).sqrMagnitude < minSqr)
                return false;
        }

        return true;
    }

    private Vector2 RandomPointInArena()
    {
        // 반지름에 제곱근을 씌워야 가운데로 몰리지 않고 넓이 기준으로 고르게 퍼진다.
        float radius = Mathf.Sqrt(Random.value);
        float angle = Random.value * Mathf.PI * 2f;
        Vector2 size = ArenaSize;
        return ArenaCenter + new Vector2(
            Mathf.Cos(angle) * radius * size.x * 0.5f,
            Mathf.Sin(angle) * radius * size.y * 0.5f);
    }

    private float GetClipLength(string clipName)
    {
        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning($"'{clipName}' 클립을 찾지 못해 1초로 계산합니다.");
        return 1f;
    }

    private void OnDrawGizmosSelected()
    {
        // 맵 범위 타원 (편집 중에도 씬의 맵 벽 기준으로 보이도록 찾는다)
        FindMapWall();
        Vector2 center = ArenaCenter;
        Vector2 size = ArenaSize;
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        const int Segments = 48;
        Vector3 previous = center + new Vector2(size.x * 0.5f, 0f);
        for (int i = 1; i <= Segments; i++)
        {
            float angle = (float)i / Segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector2(Mathf.Cos(angle) * size.x * 0.5f, Mathf.Sin(angle) * size.y * 0.5f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        // 즉사기 판정 원: 나타난 뒤 보스 바로 아래
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(InstantKillCenter, _instantKillRadius);
    }

}
