using UnityEngine;

public class Boss1ClawAttack : MonoBehaviour, IBoss1MeleeAttack
{
    [SerializeField] private Boss1Movement _boss1Movement;
    [SerializeField] private Transform _playerTransform;

    [Header("공격전 빠르게 위치 조정")]
    [SerializeField] private float _moveSpeed = 7f; // 근거리 공격전 위치 조절이라서 빠르게 설정
    [SerializeField] private float _minAttackDistance = 2f;
    [SerializeField] private float _maxAttackDistance = 4f;
    [SerializeField] private float _yTolerance = 0.3f;

    [Header("공격전 전조 시간")]
    [SerializeField] private float _telegraphDuration = 0.5f;

    [SerializeField] private Boss1ClawHitBox _clawHitBox;

    [Header("공격후 후딜레이 시간")]
    [SerializeField] private float _recoveryDuration = 0.6f;

    [SerializeField] private Animator _animator;
    
    private float _timer;

    private ClawState _currentState;
    
    private enum ClawState
    {
        Positioning,
        Telegraph,
        ClawAttack,
        Recovery
    }
    
    public bool IsFinished { get; private set; }

    public void EnterAttack()
    {
        IsFinished = false;
        _timer = 0f;
        _clawHitBox.DisableHitBox();
        _currentState = ClawState.Positioning;
        Debug.Log("발톱 공격 시작");
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }

        switch (_currentState)
        {
            case ClawState.Positioning:
                UpdatePositioning();
                break;
            case ClawState.Telegraph:
                UpdateTelegraph();
                break;
            case ClawState.ClawAttack:
                UpdateClawAttack();
                break;
            case ClawState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public void ExitAttack()
    {
        _clawHitBox.DisableHitBox();
        _boss1Movement.StopMove();   
        
        Debug.Log("발톱 공격 종료");
    }

    private void UpdatePositioning()
    {
        float xDifference = _playerTransform.position.x - transform.position.x;
        float yDifference = _playerTransform.position.y - transform.position.y;
        
        float xDistance = Mathf.Abs(xDifference);
        float yDistance = Mathf.Abs(yDifference);

        if (xDistance < _minAttackDistance)
        {
            Vector2 direction = new Vector2(-Mathf.Sign(xDifference), 0f);
            _boss1Movement.MoveInDirection(direction, _moveSpeed);
            return;
        }

        if (xDistance > _maxAttackDistance)
        {
            Vector2 direction = new Vector2(xDifference, yDifference);
            _boss1Movement.MoveInDirection(direction, _moveSpeed);
            return;
        }

        if (yDistance > _yTolerance)
        {
            Vector2 direction = new Vector2(0f, Mathf.Sign(yDifference));
            _boss1Movement.MoveInDirection(direction, _moveSpeed);
            return;
        }
        
        _boss1Movement.StopMove();
        _timer = 0f;
        _currentState = ClawState.Telegraph;
    }

    private void UpdateTelegraph()
    {
        _boss1Movement.StopMove();
        _timer += Time.deltaTime;

        if (_timer >= _telegraphDuration)
        {
            _timer = 0f;
            _currentState = ClawState.ClawAttack;
            _animator.SetTrigger("ClawAttack");
        }
    }

    private void UpdateClawAttack()
    {
        _boss1Movement.StopMove();
        
    }

    private void UpdateRecovery()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= _recoveryDuration)
        {
            _timer = 0f;
            IsFinished = true;
        }
    }

    public void EnableClawHitBox()
    {
        _clawHitBox.EnableHitBox();
    }

    public void DisableClawHitBox()
    {
        _clawHitBox.DisableHitBox();
    }

    public void EndClawAttack()
    {
        _clawHitBox.DisableHitBox();
        
        _timer = 0f;
        _currentState = ClawState.Recovery;
    }
}
