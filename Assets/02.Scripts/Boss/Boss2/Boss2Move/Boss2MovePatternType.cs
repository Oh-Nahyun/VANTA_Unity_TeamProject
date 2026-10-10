public enum Boss2MovePatternType
{
    None,
    ChaseRun,
    ChaseBlink,
    PressureWalk,
    RoundMove,
    BackStep,
    // 직선 돌진: 시작할 때 공격 위치를 고정하고 직선 인디케이터로 예고한 뒤, 그 방향으로 곧게 달린다.
    // 프리팹·씬에 숫자로 저장되므로 새 값은 항상 맨 뒤에 추가한다.
    DashLine
}
