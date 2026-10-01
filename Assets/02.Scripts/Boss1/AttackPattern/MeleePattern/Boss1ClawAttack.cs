using UnityEngine;

public class Boss1ClawAttack : MonoBehaviour, IBoss1MeleeAttack
{
    [SerializeField] private Boss1Movement _boss1Movement;
    private Transform _playerTransform;

    [Header("공격전 빠르게 위치 조정")]
    [SerializeField] private float _moveSpeed = 7f; // 근거리 공격전 위치 조절이라서 빠르게 설정
    [SerializeField] private float _minAttackDistance = 2f;
    [SerializeField] private float _maxAttackDistance = 4f;
    [SerializeField] private float _yTolerance = 0.3f;

    [Header("공격전 전조 시간")]
    [SerializeField] private float _telegraphDuration = 0.2f;

    [SerializeField] private Boss1ClawHitBox _clawHitBox;

    [Header("공격후 후딜레이 시간")]
    [SerializeField] private float _recoveryDuration = 0.4f;

    [SerializeField] private Animator _animator;
    
    private float _timer;

    private ClawState _currentState;

    private bool _isAttackActive;
    
    private enum ClawState
    {
        Positioning,
        Telegraph,
        ClawAttack,
        Recovery
    }
    
    public bool IsFinished { get; private set; }

    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void EnterAttack()
    {
        _isAttackActive = true;
        IsFinished = false;
        _timer = 0f;
        _clawHitBox.DisableHitBox();
        _boss1Movement.LockFacing();
        _currentState = ClawState.Positioning;
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
        _isAttackActive = false;
        
        _clawHitBox.DisableHitBox();
        _boss1Movement.UnlockFacing();
        _boss1Movement.StopMove();   
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
            _animator.SetTrigger("ClawAttack");
            _currentState = ClawState.ClawAttack;
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
        if (!_isAttackActive)
        {
            return;
        }
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
