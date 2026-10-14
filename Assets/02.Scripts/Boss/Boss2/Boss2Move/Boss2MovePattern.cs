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
    
    [Header("직선 돌진 (DashLine)")]
    [Tooltip("돌진 전에 직선 인디케이터를 띄우고 기다리는 시간")]
    [SerializeField] private float _dashTelegraphTime = 0.35f;
    [Tooltip("돌진 인디케이터 두께")]
    [SerializeField] private float _dashIndicatorWidth = 1.2f;
    [Tooltip("인디케이터를 도착 지점보다 이만큼 더 길게 그린다 (공격 박스까지 덮도록)")]
    [SerializeField] private float _dashIndicatorExtra = 0.8f;

    [Header("막힘 감지")]
    [Tooltip("움직이려는데 이 시간 동안 실제로 거의 못 움직였으면(벽·구석) 그 자리에서 이동을 끝낸다 (초)")]
    [SerializeField] private float _stuckCheckTime = 0.25f;
    [Tooltip("그 시간 동안 이 거리보다 적게 움직였으면 막힌 것으로 본다")]
    [SerializeField] private float _stuckDistance = 0.05f;

    // 막힘 감지: 지금 재고 있는 구간의 시작 위치와 흐른 시간
    private Vector2 _stuckCheckStart;
    private float _stuckCheckTimer;

    private Boss2MovePatternType _currentMovePatternType = Boss2MovePatternType.None;

    // 직선 돌진: 시작할 때 고정한 도착 지점과 예고 진행 상태
    private Vector2 _dashDestination;
    private Vector2 _dashDirection;
    private float _dashTelegraphTimer;
    private bool _isDashTelegraphing;
    private Boss2IndicatorSpawner _indicatorSpawner;

    public bool IsPatternRunning => _currentMovePatternType != Boss2MovePatternType.None;
    public Boss2MovePatternType CurrentMovePatternType => _currentMovePatternType;

    /// <summary>
    /// 이동 명령. 한 번 내리면 도착할 때까지 매 프레임 타깃을 쫓아간다.
    /// (DashLine 은 시작 순간의 공격 위치로 직선 돌진)
    /// </summary>
    public void ApplyMovePattern(Boss2MovePatternType movePatternType)
    {
        // 돌진 예고 중에 다른 명령(취소 포함)이 오면 인디케이터를 지운다.
        if (_currentMovePatternType == Boss2MovePatternType.DashLine && movePatternType != Boss2MovePatternType.DashLine)
        {
            ClearIndicator();
        }

        _currentMovePatternType = movePatternType;
        ResetStuckCheck();

        if (movePatternType == Boss2MovePatternType.DashLine)
        {
            BeginDash();
        }
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
            case Boss2MovePatternType.DashLine:
            {
                isFinished = DashLine();
                break;
            }
        }

        // 목표가 벽 너머이거나 물러날 곳이 벽이면 패턴이 끝나지 않으므로, 막혔으면 그 자리에서 끝낸다.
        if (isFinished == false && IsStuck())
        {
            StopImmediately();
            FaceTowards(_target.position);
            isFinished = true;
        }

        if (isFinished)
        {
            EndPattern();
        }
    }

    // 움직이려는 동안 _stuckCheckTime 마다 실제 이동 거리를 재서, _stuckDistance 보다 적으면 막힌 것으로 본다.
    // 제자리에서 기다리는 돌진 예고처럼 움직일 생각이 없는 동안은 재지 않는다.
    private bool IsStuck()
    {
        bool wantsToMove = MoveDirection.sqrMagnitude > 0.0001f || IsDodging;
        if (wantsToMove == false || _isDashTelegraphing)
        {
            ResetStuckCheck();
            return false;
        }

        _stuckCheckTimer += Time.deltaTime;
        if (_stuckCheckTimer < _stuckCheckTime)
            return false;

        bool isStuck = Vector2.Distance(transform.position, _stuckCheckStart) < _stuckDistance;
        ResetStuckCheck();
        return isStuck;
    }

    private void ResetStuckCheck()
    {
        _stuckCheckStart = transform.position;
        _stuckCheckTimer = 0f;
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

    // 직선 돌진 시작: 지금 공격 위치를 도착 지점으로 고정하고, 그 경로를 직선 인디케이터로 예고한다.
    private void BeginDash()
    {
        if (_target == null)
        {
            _isDashTelegraphing = false;
            _dashDestination = transform.position;
            return;
        }

        _dashDestination = GetAttackStandPoint();
        Vector2 toDestination = _dashDestination - (Vector2)transform.position;
        _dashDirection = toDestination.sqrMagnitude > 0.0001f ? toDestination.normalized : Vector2.right;
        _dashTelegraphTimer = 0f;
        _isDashTelegraphing = true;

        StopImmediately();
        FaceTowards(_target.position);

        if (_indicatorSpawner == null)
        {
            _indicatorSpawner = GetComponent<Boss2IndicatorSpawner>();
        }
        if (_indicatorSpawner != null && _dashTelegraphTime > 0f)
        {
            float length = toDestination.magnitude + _dashIndicatorExtra;
            _indicatorSpawner.ShowLine(transform.position, _dashDirection, length, _dashIndicatorWidth, _dashTelegraphTime);
        }
    }

    // 예고 동안 제자리에서 기다린 뒤, 고정한 도착 지점까지 방향을 바꾸지 않고 곧게 달린다.
    private bool DashLine()
    {
        if (_isDashTelegraphing)
        {
            StopMove();
            _dashTelegraphTimer += Time.deltaTime;
            if (_dashTelegraphTimer >= _dashTelegraphTime)
            {
                _isDashTelegraphing = false;
            }
            return false;
        }

        Vector2 toDestination = _dashDestination - (Vector2)transform.position;
        // 도착했거나 지나쳤으면(벽 등으로 비켜 간 경우 포함) 멈추고 타깃을 바라본다.
        if (toDestination.magnitude <= _arriveTolerance || Vector2.Dot(toDestination, _dashDirection) <= 0f)
        {
            StopImmediately();
            FaceTowards(_target.position);
            return true;
        }

        Run(_dashDirection);
        return false;
    }

    private void ClearIndicator()
    {
        _isDashTelegraphing = false;
        if (_indicatorSpawner != null)
        {
            _indicatorSpawner.Clear();
        }
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

    [ContextMenu("Test/DashLine")]
    private void TestDashLine() => PlayTestMove(Boss2MovePatternType.DashLine);

    [ContextMenu("Test/RoundMove")]
    private void TestRoundMove() => PlayTestMove(Boss2MovePatternType.RoundMove);

    [ContextMenu("Test/BackStep")]
    private void TestBackStep() => PlayTestMove(Boss2MovePatternType.BackStep);

    
}
