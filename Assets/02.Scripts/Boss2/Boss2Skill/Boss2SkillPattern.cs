using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Boss2SkillPattern : Boss2AttackBase, IAvoidInteraction
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

    [Header("맵 범위 (타원)")]
    [SerializeField] private Vector2 _arenaCenter = Vector2.zero;

    // 타원의 가로·세로 전체 길이. EdgeColliderGenerator 와 같은 규격.
    [SerializeField] private Vector2 _arenaSize = new Vector2(10f, 6f);

    [Header("판정 대상")]
    // 기본은 모든 레이어. 대상은 태그(Player)로 한 번 더 거른다.
    [SerializeField] private LayerMask _targetLayers = ~0;

    [Header("효과")]
    [SerializeField] private ParticleSystem _areaOfEffectCircle;
    
    
    private bool _isAvoided = false;

    private const int MaxSpacingAttempts = 30;

    private Boss2SkillPatternType _currentSkillPatternType = Boss2SkillPatternType.None;
    private Rigidbody2D _rigidbody2D;
    private Boss2MoveBase _mover;
    private Boss2MovePattern _movePattern;
    private Boss2AttackPattern _attackPattern;

    private Coroutine _skillRoutine;

    // 이번 스킬이 직접 이동을 막았는지. 다른 효과(크리스탈 프리즌 등)가 걸어둔 봉쇄는 풀지 않기 위해 기억한다.
    private bool _blockedMoveBySkill;

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
        // 이전 즉사기의 회피 결과가 남아 있으면 이번 즉사기가 데미지를 주지 못한다.
        _isAvoided = false;
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

    // 애니메이션 이벤트: EndInstantKillSkill 의 베는 프레임
    public void InstantKillHit()
    {
        // 회피 실패 시 데미지
        if (_isAvoided == false)
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

    public void AvoidPoint()
    {
        //DealAreaDamage(InstantKillCenter, _instantKillRadius, _targetLayers, InstantKillDamage);
        // 만약 플레이어가
        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerAvoid playerAvoid = playerObject.GetComponent<PlayerAvoid>();
            // Player 태그만 있고 Player 컴포넌트가 없는 오브젝트(테스트 더미 등)는 건너뛴다.
            if (playerAvoid == null)
                continue;

            // TakeDamage가 아니라 
            playerAvoid.StartAvoidAttackInteraction(gameObject); // 인자로 키보드 키값 KeyCode
            // StartAvoid
        }
    }

    // 애니메이션 이벤트: Boss2AOESkill 의 참격 프레임
    public void SpawnAreaStrikes()
    {
        if (_areaStrikePrefab == null)
        {
            Debug.LogWarning("Area Strike Prefab 이 비어 있어 광역기 더미를 만들 수 없습니다.");
            return;
        }

        int count = Random.Range(_strikeCountMin, _strikeCountMax + 1);
        List<Vector2> positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 position = PickStrikePosition(positions);
            positions.Add(position);

            // 판정 반경·데미지는 더미 프리팹이 갖고, 터지는 순간 더미가 직접 Player 를 때린다.
            Instantiate(_areaStrikePrefab, position, Quaternion.identity);
        }
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
                if (hasEnteredEnd == false)
                {
                    // 점프가 끝나고 공격이 시작되는 순간 플레이어 위로 나타난다.
                    AppearAboveTarget();
                }

                hasEnteredEnd = true;
                if (state.normalizedTime >= 1f)
                    break;
            }
            else if (hasEnteredEnd)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        FinishSkill();
        EndSkill();
    }

    private void AppearAboveTarget()
    {
        Transform target = FindInstantKillTarget();
        if (target == null)
        {
            Debug.LogWarning("즉사기 대상을 찾지 못해 제자리에서 공격합니다.");
            return;
        }

        Vector2 appearPosition = (Vector2)target.position + Vector2.up * _instantKillAppearHeight;
        // 이동 봉쇄(위치 고정) 중이어도 위치를 직접 정하는 것은 막히지 않는다. 물리 쪽 위치도 함께 옮긴다.
        if (_rigidbody2D != null)
        {
            _rigidbody2D.position = appearPosition;
        }

        transform.position = new Vector3(appearPosition.x, appearPosition.y, transform.position.z);
    }

    // 이동 패턴이 쫓는 대상을 우선 쓰고, 없으면 Player 태그 중 가장 가까운 대상(자기 자신 제외)을 쓴다.
    private Transform FindInstantKillTarget()
    {
        if (_movePattern != null && _movePattern.Target != null)
            return _movePattern.Target;

        Transform nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (candidate.transform.IsChildOf(transform))
                continue;

            float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = candidate.transform;
            }
        }

        return nearest;
    }

    private IEnumerator AreaOfEffectRoutine()
    {
        BeginSkill();
        _animator.CrossFade(_aoeStateName, 0.05f, 0, 0f);
        
        Instantiate(_areaOfEffectCircle, transform.position,transform.rotation);
        yield return new WaitForSeconds(GetClipLength(_aoeStateName));

        FinishSkill();
        EndSkill();
    }

    private void BeginSkill()
    {
        // 이미 다른 효과로 묶여 있으면 손대지 않는다. 막지 않았으니 끝날 때 풀지도 않는다.
        _blockedMoveBySkill = _mover != null && _mover.IsMoveBlocked == false;
        if (_blockedMoveBySkill)
        {
            _mover.MoveBlock();
        }
    }

    private void StopSkill()
    {
        if (_skillRoutine == null)
            return;

        StopCoroutine(_skillRoutine);
        FinishSkill();
    }

    // 정상 종료든 취소든 반드시 거친다. 여기서 풀지 않으면 취소된 스킬 때문에 보스가 계속 묶여 있게 된다.
    private void FinishSkill()
    {
        if (_blockedMoveBySkill)
        {
            _mover.MoveUnblock();
            _blockedMoveBySkill = false;
        }

        _skillRoutine = null;
    }

    private void EndSkill()
    {
        _currentSkillPatternType = Boss2SkillPatternType.None;
    }

    public void PlayTestSkill(Boss2SkillPatternType type)
    {
        if (_attackPattern != null)
        {
            _attackPattern.ApplyAttackPattern(Boss2AttackPatternType.None);
        }
        if (_movePattern != null)
        {
            _movePattern.ApplyMovePattern(Boss2MovePatternType.None);
        }
        ApplySkillPattern(Boss2SkillPatternType.None);
        ApplySkillPattern(type);
    }


// 타원 안 균일 분포로 뽑고, 이미 놓인 더미와 최소 간격을 지키도록 몇 번 다시 뽑는다.
    private Vector2 PickStrikePosition(List<Vector2> placed)
    {
        Vector2 candidate = RandomPointInArena();
        for (int attempt = 0; attempt < MaxSpacingAttempts; attempt++)
        {
            if (IsFarEnough(candidate, placed))
                return candidate;

            candidate = RandomPointInArena();
        }

        return candidate;
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
        return _arenaCenter + new Vector2(
            Mathf.Cos(angle) * radius * _arenaSize.x * 0.5f,
            Mathf.Sin(angle) * radius * _arenaSize.y * 0.5f);
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
        // 맵 범위 타원
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        const int Segments = 48;
        Vector3 previous = _arenaCenter + new Vector2(_arenaSize.x * 0.5f, 0f);
        for (int i = 1; i <= Segments; i++)
        {
            float angle = (float)i / Segments * Mathf.PI * 2f;
            Vector3 next = _arenaCenter + new Vector2(Mathf.Cos(angle) * _arenaSize.x * 0.5f, Mathf.Sin(angle) * _arenaSize.y * 0.5f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        // 즉사기 판정 원: 나타난 뒤 보스 바로 아래
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(InstantKillCenter, _instantKillRadius);
    }

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 ----
    [ContextMenu("Test/InstantKill")]
    private void TestInstantKill() => PlayTestSkill(Boss2SkillPatternType.InstantKill);

    [ContextMenu("Test/AreaOfEffect")]
    private void TestAreaOfEffect() => PlayTestSkill(Boss2SkillPatternType.AreaOfEffect);

    public void AvoidResultBehavior(bool result)
    {
        _isAvoided = result;
    }
}
