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

    private Boss2AttackPatternType _currentAttackPatternType = Boss2AttackPatternType.None;
    private Animator _patternAnimator;
    private Coroutine _smashRoutine;

    public bool IsPatternRunning => _currentAttackPatternType != Boss2AttackPatternType.None;
    public Boss2AttackPatternType CurrentAttackPatternType => _currentAttackPatternType;

    private void OnEnable()
    {
        _patternAnimator = GetComponent<Animator>();
        ComboEnded += EndPattern;
    }

    private void OnDisable()
    {
        ComboEnded -= EndPattern;
        StopSmash();
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
                StopSmash();
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
        // 스매시 도중에 콤보가 끼어들면 스매시 애니메이션이 덮이고, 스매시가 끝나며 콤보 패턴까지 종료시킨다.
        if (_smashRoutine != null)
        {
            Debug.LogWarning("SmashAttack 진행 중에는 ComboAttack 을 시작할 수 없습니다.");
            return;
        }

        _currentAttackPatternType = Boss2AttackPatternType.ComboAttack;
        RequestCombo(comboCount);
    }

    /// <summary>
    /// 스매시 공격. 판정(StartDamage / EndDamage)과 검날 발광은 클립의 이벤트·곡선이 처리하고,
    /// 여기서는 재생을 시작하고 클립이 끝나면 패턴을 종료한다.
    /// </summary>
    public void SmashAttack()
    {
        if (IsAttacking || _smashRoutine != null)
        {
            Debug.LogWarning("다른 공격이 진행 중이라 SmashAttack 을 시작할 수 없습니다.");
            return;
        }

        _currentAttackPatternType = Boss2AttackPatternType.SmashAttack;
        _smashRoutine = StartCoroutine(SmashRoutine());
    }

    public void BladeAuraSlash()
    {
        Debug.LogWarning("BladeAuraSlash 패턴은 아직 구현되지 않았습니다.");
    }

    public void SpinningSlash()
    {
        Debug.LogWarning("SpinningSlash 패턴은 아직 구현되지 않았습니다.");
    }

    private IEnumerator SmashRoutine()
    {
        EndDamage();
        // layer 0, 시간 오프셋 0 을 명시해 매번 처음부터 재생한다. (콤보의 StartHit 과 같은 방식)
        _patternAnimator.CrossFade(_smashStateName, 0.05f, 0, 0f);

        yield return new WaitForSeconds(GetClipLength(_smashStateName));

        // EndDamage 이벤트가 누락됐을 때를 대비해 히트박스를 확실히 끈다.
        EndDamage();
        _smashRoutine = null;
        EndPattern();
    }

    private void StopSmash()
    {
        if (_smashRoutine == null)
            return;

        StopCoroutine(_smashRoutine);
        _smashRoutine = null;
        EndDamage();
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

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 ----
    [ContextMenu("Test/Combo 1")]
    private void TestCombo1() => ComboAttack(1);

    [ContextMenu("Test/Combo 3")]
    private void TestCombo3() => ComboAttack(3);

    [ContextMenu("Test/Combo 5")]
    private void TestCombo5() => ComboAttack(5);

    [ContextMenu("Test/Smash")]
    private void TestSmash() => SmashAttack();
}
