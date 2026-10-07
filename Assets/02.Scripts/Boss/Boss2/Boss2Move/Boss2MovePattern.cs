using UnityEngine;

public class Boss2MovePattern : Boss2MoveBase
{
    [Header("이동 패턴")]
    [SerializeField] private Transform _target;
    // 스킬(즉사기 등)도 같은 대상을 쓰도록 연다.
    public Transform Target => _target;

    /// <summary>
    /// 인스펙터에서 대상을 비워 둔 경우 컨트롤러가 찾아서 넣어 준다.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
    }
    // 접근 패턴은 공격 박스(AttackZone)가 타깃 위에 오는 지점까지 간다. 그 지점과 이 거리 안이면 도착.
    [SerializeField] private float _arriveTolerance = 0.15f;
    // ChaseBlink 는 남은 거리가 이보다 멀 때만 구른다. 가까이서 구르면 공격 위치를 지나쳐 버린다.
    [SerializeField] private float _blinkMinDistance = 2.5f;
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

    /// <summary>
    /// 공격 박스(AttackZone)가 타깃 위에 오도록 보스가 서야 할 지점.
    /// 보스가 타깃의 왼쪽에서 오면 타깃 왼쪽에, 오른쪽에서 오면 오른쪽에 선다.
    /// </summary>
    private Vector2 GetAttackStandPoint()
    {
        Vector2 target = _target.position;
        float side = transform.position.x <= target.x ? -1f : 1f;
        return target + new Vector2(side * AttackZoneOffset, 0f);
    }

    // 접근 패턴 공통 도착 처리. 공격 위치에 도착했으면 미끄러지지 않게 바로 멈추고 타깃을 바라본다.
    private bool TryArrive(Vector2 toStandPoint)
    {
        if (toStandPoint.magnitude > _arriveTolerance)
            return false;

        StopImmediately();
        FaceTowards(_target.position);
        return true;
    }

    // 걸어서 압박하며 접근
    private bool PressureWalk(Vector2 toTarget)
    {
        Vector2 toStandPoint = GetAttackStandPoint() - (Vector2)transform.position;
        if (TryArrive(toStandPoint)) return true;

        Walk(toStandPoint);
        return false;
    }

    // 달려서 추격
    private bool ChaseRun(Vector2 toTarget)
    {
        Vector2 toStandPoint = GetAttackStandPoint() - (Vector2)transform.position;
        if (TryArrive(toStandPoint)) return true;

        Run(toStandPoint);
        return false;
    }

    // 구르기로 순간이동하듯 접근. 가까워졌거나 쿨타임 동안은 걸어서 따라간다.
    private bool ChaseBlink(Vector2 toTarget)
    {
        Vector2 toStandPoint = GetAttackStandPoint() - (Vector2)transform.position;
        if (TryArrive(toStandPoint)) return true;

        if (toStandPoint.magnitude < _blinkMinDistance || TryDodge(toStandPoint) == false)
        {
            Walk(toStandPoint);
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

    /// <summary>
    /// 테스트용. 진행 중인 공격·스킬을 모두 정리한 뒤 바로 이동한다.
    /// 공격·스킬이 걸어 둔 이동 봉쇄도 그 정리 과정에서 풀린다.
    /// (Boss2Controller 가 켜져 있으면 FSM 이 곧바로 덮어쓰므로 FSM/Stop 후 사용)
    /// </summary>
    public void PlayTestMove(Boss2MovePatternType type)
    {
        Boss2AttackPattern attackPattern = GetComponent<Boss2AttackPattern>();
        if (attackPattern != null)
        {
            attackPattern.ApplyAttackPattern(Boss2AttackPatternType.None);
        }

        Boss2SkillPattern skillPattern = GetComponent<Boss2SkillPattern>();
        if (skillPattern != null)
        {
            skillPattern.ApplySkillPattern(Boss2SkillPatternType.None);
        }

        ApplyMovePattern(Boss2MovePatternType.None);
        ApplyMovePattern(type);
    }

    // ---- 테스트용: 플레이 중 컴포넌트 ⋮ 메뉴에서 실행 (Use Keyboard Input 은 꺼야 동작) ----
    [ContextMenu("Test/PressureWalk")]
    private void TestPressureWalk() => PlayTestMove(Boss2MovePatternType.PressureWalk);

    [ContextMenu("Test/ChaseRun")]
    private void TestChaseRun() => PlayTestMove(Boss2MovePatternType.ChaseRun);

    [ContextMenu("Test/ChaseBlink")]
    private void TestChaseBlink() => PlayTestMove(Boss2MovePatternType.ChaseBlink);

    [ContextMenu("Test/RoundMove")]
    private void TestRoundMove() => PlayTestMove(Boss2MovePatternType.RoundMove);

    [ContextMenu("Test/BackStep")]
    private void TestBackStep() => PlayTestMove(Boss2MovePatternType.BackStep);

    
}
