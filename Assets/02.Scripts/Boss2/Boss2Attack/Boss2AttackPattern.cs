using System.Collections;

using UnityEngine;

public class Boss2AttackPattern : Boss2AttackBase
{
    // 주의: 기반 클래스가 private Start / Update 를 갖고 있다.
    // 여기서 Start, Update, Awake 를 선언하면 Unity 가 이쪽만 호출해 기반의 입력·콤보 갱신이 멈춘다.
    // 그래서 이 클래스는 OnEnable / OnDisable 만 쓴다.

    [Header("스매시 공격")]
    // Animator 의 상태 이름이자 클립 이름. 클립 길이만큼 기다렸다가 패턴을 끝낸다.
    [SerializeField] private string _smashStateName = "Boss2SmashAttack";

    [Header("스피닝 슬래시")]
    [SerializeField] private string _spinStateName = "Boss2SpecialAttack";
    // 범위 반경. Boss2 를 선택하면 Scene 뷰에 원으로 보인다.
    [SerializeField] private float _spinRadius = 1.5f;
    // 보스 피벗 기준 원 중심 위치
    [SerializeField] private Vector2 _spinCenterOffset = Vector2.zero;
    // 기본은 모든 레이어. 대상은 태그(Player)로 한 번 더 거른다.
    [SerializeField] private LayerMask _spinTargetLayers = ~0;
    [Tooltip("켜면 반경 안의 플레이어에게 회피 입력(QTE)을 띄우고, 성공하면 데미지를 주지 않는다")]
    [SerializeField] private bool _spinAvoidable = true;
    [Tooltip("사라졌다 나타날 때(SpinningSlashAppear 이벤트) 보이는 보스 그림의 중심이 대상 위로 얼마나 떨어질지")]
    [SerializeField] private float _spinAppearHeight = 1f;
    [Tooltip("회전베기 프레임(다시 보일 때)의 보스 그림 중심이 피벗에서 떨어진 거리. 그림이 칸 위쪽에 그려져 있다")]
    [SerializeField] private Vector2 _spinVisualOffset = new Vector2(0f, 1.83f);
    [Tooltip("회전베기 프레임의 보스 그림 크기. 나타날 자리가 맵 안인지 이 크기로 판정한다")]
    [SerializeField] private Vector2 _spinVisualSize = new Vector2(2.6f, 2.6f);

    [Header("검기 공통")]
    [Tooltip("발사 중심에서 발사체가 생기는 위치까지 거리")]
    [SerializeField] private float _bladeAuraSpawnDistance = 0.8f;

    [Tooltip("보스 피벗 기준 발사 중심 보정 (검 높이 등)")]
    [SerializeField] private Vector2 _bladeAuraSpawnOffset = new Vector2(0f, 0.3f);

    [Header("검기 1·3방향 (BladeAuraSlash)")]
    [Tooltip("날릴 발사체 (Dark / Blood / 기본 SlashProjectile 교체 가능)")]
    [SerializeField] private ProjectileBase _bladeAuraProjectile;

    [SerializeField] private float _bladeAuraSpeed = 6f;

    [Tooltip("재생할 애니메이션 (상태 이름 = 클립 이름). 클립의 BladeAuraSlashHit 이벤트에서 발사")]
    [SerializeField] private string _bladeAuraStateName = "BladeAuraSlash";

    [Tooltip("행동에 발사 수가 지정되지 않았을 때 쓰는 값 (Single / Triple)")]
    [SerializeField] private SlashProjectileType _bladeAuraDefaultType = SlashProjectileType.Triple;

    [Header("검기 8방향 (BladeAuraSlashAll)")]
    [Tooltip("비워 두면 1·3방향 발사체를 쓴다")]
    [SerializeField] private ProjectileBase _bladeAuraAllProjectile;

    [Tooltip("0 이하면 1·3방향 속도를 쓴다")]
    [SerializeField] private float _bladeAuraAllSpeed = 0f;

    [Tooltip("재생할 애니메이션 (상태 이름 = 클립 이름). 클립의 BladeAuraSlashAllHit 이벤트에서 발사")]
    [SerializeField] private string _bladeAuraAllStateName = "BladeAuraSlashAll";

    private Vector2 _pendingAuraDirection = Vector2.right;
    private SlashProjectileType _pendingAuraType = SlashProjectileType.None;
    private bool _hasFiredBladeAura;

    private Vector2 BladeAuraCenter => (Vector2)transform.position + _bladeAuraSpawnOffset;

    private Boss2AttackPatternType _currentAttackPatternType = Boss2AttackPatternType.None;
    private Animator _patternAnimator;
    private Boss2MoveBase _mover;
    private Boss2SkillPattern _skillPattern;
    private Boss2AvoidJudge _avoidJudge;
    // 콤보가 아닌 단발 공격(Smash, SpinningSlash)을 재생하는 코루틴
    private Coroutine _singleAttackRoutine;
    // 이번 단발 공격이 직접 이동을 막았는지. 다른 효과가 걸어둔 봉쇄는 풀지 않기 위해 기억한다.
    private bool _blockedMoveBySkill;

    [SerializeField]private GameObject _spinningSlashEffect;


    public bool IsPatternRunning => _currentAttackPatternType != Boss2AttackPatternType.None;
    public Boss2AttackPatternType CurrentAttackPatternType => _currentAttackPatternType;

    private Vector2 SpinCenter => (Vector2)transform.position + _spinCenterOffset;

    private void OnEnable()
    {
        _patternAnimator = GetComponent<Animator>();
        _mover = GetComponent<Boss2MoveBase>();
        _skillPattern = GetComponent<Boss2SkillPattern>();
        _avoidJudge = Boss2AvoidJudge.GetOrAdd(gameObject);
        ComboEnded += EndPattern;
    }

    private void OnDisable()
    {
        ComboEnded -= EndPattern;
        StopSingleAttack();
    }

    /// <summary>
    /// 공격 명령. comboCount 는 ComboAttack 에서만 쓰며, 스텝을 번갈아 가며 그 타수만큼 연속으로 친다.
    /// projectileType 은 BladeAuraSlash 에서만 쓰며 Single / Triple 을 고른다. (None 이면 인스펙터 기본값)
    /// </summary>
    public void ApplyAttackPattern(Boss2AttackPatternType attackPatternType, int comboCount = 1,
        SlashProjectileType projectileType = SlashProjectileType.None)
    {
        switch (attackPatternType)
        {
            case Boss2AttackPatternType.None:
            {
                CancelCombo();
                StopSingleAttack();
                EndPattern();
                break;
            }
            case Boss2AttackPatternType.ComboAttack:
            {
                ComboAttack(comboCount);
                break;
            }
            case Boss2AttackPatternType.SmashAttack:
            {
                SmashAttack();
                break;
            }
            case Boss2AttackPatternType.BladeAuraSlash:
            {
                SlashProjectileType type = projectileType != SlashProjectileType.None ? projectileType : _bladeAuraDefaultType;
                BladeAuraSlash(GetTargetDirection(), type);
                break;
            }
            case Boss2AttackPatternType.SpinningSlash:
            {
                SpinningSlash();
                break;
            }
            case Boss2AttackPatternType.BladeAuraSlashAll:
            {
                BladeAuraSlashAll();
                break;
            }
        }
    }

    // 스텝(Attack, DoubleAttack)을 번갈아 가며 comboCount 타를 연속으로 친다.
    public void ComboAttack(int comboCount)
    {
        // 단발 공격 도중에 콤보가 끼어들면 그 애니메이션이 덮이고, 단발 공격이 끝나며 콤보 패턴까지 종료시킨다.
        if (_singleAttackRoutine != null)
        {
            Debug.LogWarning($"{_currentAttackPatternType} 진행 중에는 ComboAttack 을 시작할 수 없습니다.");
            return;
        }

        if (IsSkillRunning())
            return;

        _currentAttackPatternType = Boss2AttackPatternType.ComboAttack;
        RequestCombo(comboCount);
    }

    /// <summary>
    /// 스매시 공격. 판정(StartDamage / EndDamage)과 검날 발광은 클립의 이벤트·곡선이 처리한다.
    /// </summary>
    public void SmashAttack()
    {
        TryStartSingleAttack(Boss2AttackPatternType.SmashAttack, _smashStateName, false);
    }

    /// <summary>
    /// 검기 1·3방향. direction 에 가장 가까운 8방위를 기준으로
    /// Single 은 그 방향 1발, Triple 은 앞뒤 인접 방향까지 3발을 쏜다. (8방향은 BladeAuraSlashAll)
    /// 실제 발사는 BladeAuraSlash 클립의 BladeAuraSlashHit 이벤트에서 한다.
    /// </summary>
    public void BladeAuraSlash(Vector2 direction, SlashProjectileType type)
    {
        if (type == SlashProjectileType.None)
            return;

        if (type == SlashProjectileType.All)
        {
            Debug.LogWarning("8방향은 BladeAuraSlashAll 패턴으로 쏩니다. Triple 로 대신 실행합니다.");
            type = SlashProjectileType.Triple;
        }

        if (_bladeAuraProjectile == null)
        {
            Debug.LogWarning("Blade Aura Projectile 이 비어 있어 검기를 발사할 수 없습니다.");
            return;
        }

        _pendingAuraDirection = direction.sqrMagnitude > 0.0001f ? direction : Vector2.right;
        _pendingAuraType = type;
        _hasFiredBladeAura = false;
        TryStartSingleAttack(Boss2AttackPatternType.BladeAuraSlash, _bladeAuraStateName, true);
    }

    /// <summary>
    /// 검기 8방향. 방향과 상관없이 보스 주위 8방위 모두로 쏜다.
    /// 실제 발사는 BladeAuraSlashAll 클립의 BladeAuraSlashAllHit 이벤트에서 한다.
    /// </summary>
    public void BladeAuraSlashAll()
    {
        if (AllProjectile == null)
        {
            Debug.LogWarning("Blade Aura 발사체가 비어 있어 8방향 검기를 발사할 수 없습니다.");
            return;
        }

        _pendingAuraType = SlashProjectileType.All;
        _hasFiredBladeAura = false;
        TryStartSingleAttack(Boss2AttackPatternType.BladeAuraSlashAll, _bladeAuraAllStateName, true);
    }

    // 8방향 전용 값이 비어 있으면 1·3방향 값을 쓴다.
    private ProjectileBase AllProjectile => _bladeAuraAllProjectile != null ? _bladeAuraAllProjectile : _bladeAuraProjectile;
    private float AllSpeed => _bladeAuraAllSpeed > 0f ? _bladeAuraAllSpeed : _bladeAuraSpeed;

    // 대상(플레이어) 방향. 대상이 없으면 오른쪽.
    private Vector2 GetTargetDirection()
    {
        if (_mover is Boss2MovePattern movePattern && movePattern.Target != null)
            return movePattern.Target.position - transform.position;

        return Vector2.right;
    }

    // 발사할 _projectileDirections 인덱스 목록
    private int[] GetBladeAuraIndices(Vector2 direction, SlashProjectileType type)
    {
        int center = DirectionToIndex(direction);
        switch (type)
        {
            case SlashProjectileType.Single:
            {
                return new[] { center };
            }
            case SlashProjectileType.Triple:
            {
                // 배열이 시계 방향 순서라 인덱스 ±1 이 곧 양옆 인접 방향이다.
                return new[] { WrapDirectionIndex(center - 1), center, WrapDirectionIndex(center + 1) };
            }
            case SlashProjectileType.All:
            {
                int[] all = new int[_projectileDirections.Length];
                for (int i = 0; i < all.Length; i++)
                {
                    all[i] = i;
                }
                return all;
            }
            default:
            {
                return new int[0];
            }
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: BladeAuraSlash 클립의 휘두르는 프레임. 1·3방향으로 발사한다.
    /// </summary>
    public void BladeAuraSlashHit()
    {
        if (_currentAttackPatternType != Boss2AttackPatternType.BladeAuraSlash || _hasFiredBladeAura)
            return;

        FireBladeAura(_bladeAuraProjectile, _bladeAuraSpeed);
    }

    /// <summary>
    /// 애니메이션 이벤트: BladeAuraSlashAll 클립의 발사 프레임. 8방향으로 발사한다.
    /// </summary>
    public void BladeAuraSlashAllHit()
    {
        if (_currentAttackPatternType != Boss2AttackPatternType.BladeAuraSlashAll || _hasFiredBladeAura)
            return;

        FireBladeAura(AllProjectile, AllSpeed);
    }

    // 정해 둔 방향들로 발사체를 생성한다. 한 번의 공격에서 한 번만 쏜다.
    private void FireBladeAura(ProjectileBase prefab, float speed)
    {
        _hasFiredBladeAura = true;
        foreach (int index in GetBladeAuraIndices(_pendingAuraDirection, _pendingAuraType))
        {
            // 대각선 (1,1) 은 길이가 1.41 이라 정규화해야 발사 거리·속도가 방향마다 같다.
            Vector2 direction = _projectileDirections[index].normalized;
            Vector2 spawnPosition = BladeAuraCenter + direction * _bladeAuraSpawnDistance;

            ProjectileBase projectile = Instantiate(prefab, spawnPosition, Quaternion.identity);
            projectile.Launch(direction, speed);
        }
    }

    /// <summary>
    /// 제자리에서 회전하며 반경 안의 대상 전부에게 한 번 범위 데미지를 준다.
    /// 판정 순간은 클립의 SpinningSlashHit 이벤트가 정한다.
    /// </summary>
    public void SpinningSlash()
    {
        // 이전 공격의 회피 결과가 남아 있으면 이번 공격이 데미지를 주지 못한다.
        _avoidJudge.ResetResult();
        TryStartSingleAttack(Boss2AttackPatternType.SpinningSlash, _spinStateName, true);
    }

    /// <summary>
    /// Boss2SpecialAttack 클립에서 사라졌던 보스가 다시 보이기 직전의 애니메이션 이벤트에서 호출된다.
    /// 사라져 있는 동안 플레이어가 움직였어도, 나타나는 순간의 플레이어 위로 순간이동한다.
    /// 회피 판정(SpinningSlashAvoidPoint)이 새 위치를 기준으로 하도록 그보다 먼저 와야 한다.
    /// </summary>
    public void SpinningSlashAppear()
    {
        if (_currentAttackPatternType != Boss2AttackPatternType.SpinningSlash)
            return;

        Transform target = FindAttackTarget();
        if (AppearAbove(target, _spinAppearHeight, _spinVisualOffset, _spinVisualSize) == false)
        {
            Debug.LogWarning("회전베기 대상을 찾지 못해 제자리에서 나타납니다.");
            return;
        }

        if (_mover != null)
        {
            _mover.FaceTowards(target.position);
        }
    }

    /// <summary>
    /// Boss2SpecialAttack 클립의 타격 직전 애니메이션 이벤트에서 호출된다.
    /// 반경 안의 플레이어에게 피하기만 하는 회피 입력(Dodge QTE)을 띄우고, 결과가 올 때까지 보스 모션을 멈춘다.
    /// 즉사기의 AvoidPoint 와 이름이 같으면 두 패턴이 함께 반응하므로 이름을 다르게 둔다.
    /// </summary>
    public void SpinningSlashAvoidPoint()
    {
        if (_spinAvoidable == false || _currentAttackPatternType != Boss2AttackPatternType.SpinningSlash)
            return;

        _avoidJudge.Begin(SpinCenter, _spinRadius, Boss2AvoidType.Dodge);
    }

    /// <summary>
    /// Boss2SpecialAttack 클립의 애니메이션 이벤트에서 호출된다.
    /// </summary>
    public void SpinningSlashHit()
    {
        // Effect
        //Instantiate(_spinningSlashEffect);
        _spinningSlashEffect.GetComponent<ParticleSystem>().Play();

        // Damage: 회피에 성공했으면 주지 않는다.
        if (_spinAvoidable && _avoidJudge.LastResult)
            return;

        DealAreaDamage(SpinCenter, _spinRadius, _spinTargetLayers);
    }

    private void TryStartSingleAttack(Boss2AttackPatternType patternType, string stateName, bool blockMove)
    {
        if (IsAttacking || _singleAttackRoutine != null)
        {
            Debug.LogWarning($"다른 공격이 진행 중이라 {patternType} 을 시작할 수 없습니다.");
            return;
        }

        if (IsSkillRunning())
            return;

        _currentAttackPatternType = patternType;
        _singleAttackRoutine = StartCoroutine(SingleAttackRoutine(stateName, blockMove));
    }

    // 스킬 애니메이션이 공격 애니메이션에 덮이지 않도록, 스킬 중에는 공격을 시작하지 않는다.
    private bool IsSkillRunning()
    {
        if (_skillPattern == null || _skillPattern.IsSkillRunning == false)
            return false;

        Debug.LogWarning($"{_skillPattern.CurrentSkillPatternType} 스킬 진행 중에는 공격을 시작할 수 없습니다.");
        return true;
    }

    private IEnumerator SingleAttackRoutine(string stateName, bool blockMove)
    {
        EndDamage();
        ClearPendingHit();

        // 이미 다른 효과(크리스탈 프리즌 등)로 묶여 있으면 손대지 않는다.
        // 여기서 막지 않았으니 끝날 때 풀지도 않아, 그 효과의 봉쇄를 대신 풀어버리지 않는다.
        _blockedMoveBySkill = blockMove && _mover != null && _mover.IsMoveBlocked == false;
        if (_blockedMoveBySkill)
        {
            _mover.MoveBlock();
        }

        // layer 0, 시간 오프셋 0 을 명시해 매번 처음부터 재생한다. (콤보의 StartHit 과 같은 방식)
        _patternAnimator.CrossFade(stateName, 0.05f, 0, 0f);

        // 회피 결과를 기다리며 애니메이터가 멈춘 동안은 시간을 세지 않아야 공격이 중간에 끝나지 않는다.
        float clipLength = GetClipLength(stateName);
        float elapsed = 0f;
        while (elapsed < clipLength)
        {
            if (_avoidJudge.IsWaiting == false)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        // 클립에 발사 이벤트가 빠져 있어도 검기는 나가도록, 끝날 때까지 안 쐈으면 여기서 쏜다.
        BladeAuraSlashHit();
        BladeAuraSlashAllHit();

        FinishSingleAttack();
        EndPattern();
    }

    private void StopSingleAttack()
    {
        if (_singleAttackRoutine == null)
            return;

        StopCoroutine(_singleAttackRoutine);
        FinishSingleAttack();
    }

    // 정상 종료든 취소든 반드시 거친다. 여기서 풀지 않으면 취소된 스킬 때문에 보스가 계속 묶여 있게 된다.
    private void FinishSingleAttack()
    {
        // EndDamage 이벤트가 누락됐을 때를 대비해 히트박스를 확실히 끈다.
        EndDamage();

        // 회피 결과 대기 중에 공격이 끊겨도 보스가 멈춘 채로 남지 않게 한다.
        _avoidJudge.Cancel();

        if (_blockedMoveBySkill)
        {
            _mover.MoveUnblock();
            _blockedMoveBySkill = false;
        }

        _singleAttackRoutine = null;
    }

    public void PlayTestAttack(Boss2AttackPatternType type, int count =1)
    {
        if (_skillPattern != null)
        {
            _skillPattern.ApplySkillPattern(Boss2SkillPatternType.None);
        }

        if (_mover is Boss2MovePattern movePattern)
        {
            movePattern.ApplyMovePattern(Boss2MovePatternType.None);
        }

        ApplyAttackPattern(Boss2AttackPatternType.None);
        ApplyAttackPattern(type, count);
    }

    private float GetClipLength(string clipName)
    {
        foreach (AnimationClip clip in _patternAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning($"'{clipName}' 클립을 찾지 못해 1초 뒤에 패턴을 끝냅니다.");
        return 1f;
    }

    private void EndPattern()
    {
        _currentAttackPatternType = Boss2AttackPatternType.None;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(SpinCenter, _spinRadius);

        // 검기 발사 위치: 원 위의 8방위 점 (플레이 전에도 보이도록 방향을 직접 계산)
        Gizmos.color = new Color(0.6f, 0.4f, 1f);
        Gizmos.DrawWireSphere(BladeAuraCenter, _bladeAuraSpawnDistance);
        for (int i = 0; i < 8; i++)
        {
            float clockwise = i * 45f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(clockwise), Mathf.Cos(clockwise));
            Gizmos.DrawSphere(BladeAuraCenter + direction * _bladeAuraSpawnDistance, 0.06f);
        }
    }

    // 테스트: 진행 중인 행동을 정리하고 플레이어 방향으로 검기
    private void TestBladeAura(SlashProjectileType type)
    {
        PlayTestAttack(Boss2AttackPatternType.None);
        ApplyAttackPattern(Boss2AttackPatternType.BladeAuraSlash, 1, type);
    }

    [ContextMenu("Test/BladeAura Single")]
    private void TestBladeAuraSingle() => TestBladeAura(SlashProjectileType.Single);

    [ContextMenu("Test/BladeAura Triple")]
    private void TestBladeAuraTriple() => TestBladeAura(SlashProjectileType.Triple);

    [ContextMenu("Test/BladeAura All (8방향)")]
    private void TestBladeAuraAll() => PlayTestAttack(Boss2AttackPatternType.BladeAuraSlashAll);

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 ----
    [ContextMenu("Test/Combo 1")]
    private void TestCombo1() => PlayTestAttack(Boss2AttackPatternType.ComboAttack);
    
    [ContextMenu("Test/Combo 5")]
    private void TestCombo5() => PlayTestAttack(Boss2AttackPatternType.ComboAttack,5);

    [ContextMenu("Test/Smash")]
    private void TestSmash() => PlayTestAttack(Boss2AttackPatternType.SmashAttack);

    [ContextMenu("Test/Spinning Slash")]
    private void TestSpinningSlash() => PlayTestAttack(Boss2AttackPatternType.SpinningSlash);
}
