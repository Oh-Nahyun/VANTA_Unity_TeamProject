public enum Boss2AttackPatternType
{
    None,
    ComboAttack,
    SmashAttack,
    BladeAuraSlash,
    // 순보 회전참: 플레이어 위로 순간이동 → 회피 입력, 실패하면 직접 데미지
    SpinningSlash,
    // 프리팹·씬에 숫자로 저장되므로 새 값은 항상 맨 뒤에 추가한다.
    BladeAuraSlashAll,
    // 제자리 회전참: 순간이동·회피 입력 없이 회전 반경 안에 범위 데미지
    SpinningSlashInPlace
}
