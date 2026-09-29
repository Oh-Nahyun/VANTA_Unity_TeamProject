using UnityEngine;

public class Boss2MovePattern : Boss2MoveBase
{
    [Header("이동 패턴")]
    [SerializeField] private Transform _target;
    // (임시) 공격 가능 범위. 접근 패턴은 타깃과 이 거리 안으로 들어오면 끝난다.
    [SerializeField] private float _attackRange = 1f;
    // BackStep 은 타깃과 이 거리 이상 벌어지면 끝난다.
    [SerializeField] private float _backStepDistance = 3f;
    
    private Boss2MovePatternType _currentMovePatternType = Boss2MovePatternType.None;

    public bool IsPatternRunning => _currentMovePatternType != Boss2MovePatternType.None;
    public Boss2MovePatternType CurrentMovePatternType => _currentMovePatternType;

    /// <summary>
    /// 이동 명령. 한 번 내리면 도착할 때까지 매 프레임 타깃을 쫓아간다.
    /// </summary>
    public void ApplyMovePattern(Boss2MovePatternType movePatternType)
    {
        _currentMovePatternType = movePatternType;
    }

    protected override void TickPattern()
    {
        if (_target == null || IsPatternRunning == false)
        {
            StopMove();
            return;
        }

        Vector2 toTarget = _target.position - transform.position;

        // 패턴마다 종료 조건이 달라서 각 패턴이 끝났는지를 직접 돌려준다.
        bool isFinished = false;
        switch (_currentMovePatternType)
        {
            case Boss2MovePatternType.None:
            {
                isFinished = true;
                break;
            }
            case Boss2MovePatternType.PressureWalk:
            {
                isFinished = PressureWalk(toTarget);
                break;
            }
            case Boss2MovePatternType.ChaseRun:
            {
                isFinished = ChaseRun(toTarget);
                break;
            }
            case Boss2MovePatternType.ChaseBlink:
            {
                isFinished = ChaseBlink(toTarget);
                break;
            }
            case Boss2MovePatternType.RoundMove:
            {
                isFinished = RoundMove(toTarget);
                break;
            }
            case Boss2MovePatternType.BackStep:
            {
                isFinished = BackStep(toTarget);
                break;
            }
        }

        if (isFinished)
        {
            EndPattern();
        }
    }

    private void EndPattern()
    {
        StopMove();
        _currentMovePatternType = Boss2MovePatternType.None;
    }

    // 접근 패턴 공통 종료 조건: 공격 가능 범위 안에 들어옴
    private bool IsInAttackRange(Vector2 toTarget)
    {
        return toTarget.magnitude <= _attackRange;
    }

    // 걸어서 압박하며 접근
    private bool PressureWalk(Vector2 toTarget)
    {
        if (IsInAttackRange(toTarget)) return true;

        Walk(toTarget);
        return false;
    }

    // 달려서 추격
    private bool ChaseRun(Vector2 toTarget)
    {
        if (IsInAttackRange(toTarget)) return true;

        Run(toTarget);
        return false;
    }

    // 구르기로 순간이동하듯 접근. 쿨타임 동안은 걸어서 따라간다.
    private bool ChaseBlink(Vector2 toTarget)
    {
        if (IsInAttackRange(toTarget)) return true;

        if (TryDodge(toTarget) == false)
        {
            Walk(toTarget);
        }
        return false;
    }

    // TODO: 타깃 주위를 도는 이동. 구현 전까지는 바로 끝낸다.
    private bool RoundMove(Vector2 toTarget)
    {
        return true;
    }

    // 타깃 반대 방향으로 물러난다. 타깃과 _backStepDistance 이상 벌어지면 끝.
    private bool BackStep(Vector2 toTarget)
    {
        if (toTarget.magnitude >= _backStepDistance) return true;

        // Walk / TryDodge 는 위치가 아니라 방향을 받는다. 타깃 반대 방향 = -toTarget
        Vector2 awayFromTarget = -toTarget;
        if (TryDodge(awayFromTarget) == false)
        {
            Walk(awayFromTarget);
        }
        return false;
    }

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 (Use Keyboard Input 은 꺼야 동작) ----
    [ContextMenu("Test/PressureWalk")]
    private void TestPressureWalk() => ApplyMovePattern(Boss2MovePatternType.PressureWalk);

    [ContextMenu("Test/ChaseRun")]
    private void TestChaseRun() => ApplyMovePattern(Boss2MovePatternType.ChaseRun);

    [ContextMenu("Test/ChaseBlink")]
    private void TestChaseBlink() => ApplyMovePattern(Boss2MovePatternType.ChaseBlink);

    [ContextMenu("Test/RoundMove")]
    private void TestRoundMove() => ApplyMovePattern(Boss2MovePatternType.RoundMove);

    
    [ContextMenu("Test/BackStep")]
    private void TestBackStep() => ApplyMovePattern(Boss2MovePatternType.BackStep);

    
}
