using UnityEngine;

public class Boss2AttackPattern : Boss2AttackBase
{
    // 주의: 기반 클래스가 private Start / Update 를 갖고 있다.
    // 여기서 Start, Update, Awake 를 선언하면 Unity 가 이쪽만 호출해 기반의 입력·콤보 갱신이 멈춘다.
    // 그래서 이 클래스는 OnEnable / OnDisable 만 쓴다.

    private Boss2AttackPatternType _currentAttackPatternType = Boss2AttackPatternType.None;

    public bool IsPatternRunning => _currentAttackPatternType != Boss2AttackPatternType.None;
    public Boss2AttackPatternType CurrentAttackPatternType => _currentAttackPatternType;

    private void OnEnable()
    {
        ComboEnded += EndPattern;
    }

    private void OnDisable()
    {
        ComboEnded -= EndPattern;
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
        _currentAttackPatternType = Boss2AttackPatternType.ComboAttack;
        RequestCombo(comboCount);
    }

    // TODO: 미구현 패턴. 구현 시 _currentAttackPatternType 을 세팅하고 끝날 때 EndPattern 을 부른다.
    public void SmashAttack()
    {
        Debug.LogWarning("SmashAttack 패턴은 아직 구현되지 않았습니다.");
    }

    public void BladeAuraSlash()
    {
        Debug.LogWarning("BladeAuraSlash 패턴은 아직 구현되지 않았습니다.");
    }

    public void SpinningSlash()
    {
        Debug.LogWarning("SpinningSlash 패턴은 아직 구현되지 않았습니다.");
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
}
