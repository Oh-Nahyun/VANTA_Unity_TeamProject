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

    private Boss2AttackPatternType _currentAttackPatternType = Boss2AttackPatternType.None;
    private Animator _patternAnimator;
    private Boss2MoveBase _mover;
    private Boss2SkillPattern _skillPattern;
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
        ComboEnded += EndPattern;
    }

    private void OnDisable()
    {
        ComboEnded -= EndPattern;
        StopSingleAttack();
    }

    /// <summary>
    /// 공격 명령. comboCount 는 ComboAttack 에서만 쓰며, 스텝을 번갈아 가며 그 타수만큼 연속으로 친다.
    /// </summary>
    public void ApplyAttackPattern(Boss2AttackPatternType attackPatternType, int comboCount = 1)
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
                BladeAuraSlash();
                break;
            }
            case Boss2AttackPatternType.SpinningSlash:
            {
                SpinningSlash();
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

    public void BladeAuraSlash()
    {
        Debug.LogWarning("BladeAuraSlash 패턴은 아직 구현되지 않았습니다.");
    }

    /// <summary>
    /// 제자리에서 회전하며 반경 안의 대상 전부에게 한 번 범위 데미지를 준다.
    /// 판정 순간은 클립의 SpinningSlashHit 이벤트가 정한다.
    /// </summary>
    public void SpinningSlash()
    {
        TryStartSingleAttack(Boss2AttackPatternType.SpinningSlash, _spinStateName, true);
    }

    /// <summary>
    /// Boss2SpecialAttack 클립의 애니메이션 이벤트에서 호출된다.
    /// </summary>
    public void SpinningSlashHit()
    {
        // Effect 
        //Instantiate(_spinningSlashEffect);
        _spinningSlashEffect.GetComponent<ParticleSystem>().Play();
        
        // Damage 
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

        // 이미 다른 효과(크리스탈 프리즌 등)로 묶여 있으면 손대지 않는다.
        // 여기서 막지 않았으니 끝날 때 풀지도 않아, 그 효과의 봉쇄를 대신 풀어버리지 않는다.
        _blockedMoveBySkill = blockMove && _mover != null && _mover.IsMoveBlocked == false;
        if (_blockedMoveBySkill)
        {
            _mover.MoveBlock();
        }

        // layer 0, 시간 오프셋 0 을 명시해 매번 처음부터 재생한다. (콤보의 StartHit 과 같은 방식)
        _patternAnimator.CrossFade(stateName, 0.05f, 0, 0f);

        yield return new WaitForSeconds(GetClipLength(stateName));

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

        if (_blockedMoveBySkill)
        {
            _mover.MoveUnblock();
            _blockedMoveBySkill = false;
        }

        _singleAttackRoutine = null;
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
    }

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 ----
    [ContextMenu("Test/Combo 1")]
    private void TestCombo1() => ComboAttack(1);

    [ContextMenu("Test/Combo 3")]
    private void TestCombo3() => ComboAttack(3);

    [ContextMenu("Test/Combo 5")]
    private void TestCombo5() => ComboAttack(5);

    [ContextMenu("Test/Smash")]
    private void TestSmash() => SmashAttack();

    [ContextMenu("Test/Spinning Slash")]
    private void TestSpinningSlash() => SpinningSlash();
}
