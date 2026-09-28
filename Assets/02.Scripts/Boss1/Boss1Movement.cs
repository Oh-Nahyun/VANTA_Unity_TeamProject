using UnityEngine;
using UnityEngine.UIElements;

public class Boss1Movement : MonoBehaviour, IBossMovable
{
    [SerializeField] private float _normalChaseSpeed= 4f;
    [SerializeField] private float _aggressiveChaseSpeed = 7f;
    [SerializeField] private float _stopDistance = 1f;
    [SerializeField] private Animator _animator;

    [SerializeField] private Transform _visualPivot;
    [SerializeField] private float _horizontalFlip = 0.42f;
    
    
    private Rigidbody2D _rigidBody;
    private Transform _playerTransform;
    
    private MoveMode _moveMode;

    private bool _isMoveBlocked = false; // IBossMovable 관련, true면 이동 속박됨, false면 이동 제약 없음

    private enum MoveMode
    {
        NormalChase,
        AggressiveChase,
        Stop
    }
    
    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _playerTransform = GameObject.FindWithTag("Player").transform;
        StartNormalChase();
    }

    private void FixedUpdate()
    {
        FacePlayer();

        if (_isMoveBlocked)
        {
            _rigidBody.linearVelocity = Vector2.zero;
            _animator.SetBool("IsMoving", false);
            return;
        }
        
        switch (_moveMode)
        {
            case MoveMode.NormalChase:
                ChasePlayer(_normalChaseSpeed);
                break;
            case MoveMode.AggressiveChase:
                ChasePlayer(_aggressiveChaseSpeed);
                break;
            case MoveMode.Stop:
                break;
        }
        
        _animator.SetBool("IsMoving", _rigidBody.linearVelocity.sqrMagnitude > 0.01f);
    }

    public void MoveBlock()
    {
        _isMoveBlocked = true;
        _rigidBody.linearVelocity = Vector2.zero;
    }

    public void MoveUnblock()
    {
        _isMoveBlocked = false;
    }
    

    private void ChasePlayer(float chaseSpeed)
    {
        float distance = Vector2.Distance(_playerTransform.position, transform.position);

        if (distance <= _stopDistance)
        {
            _rigidBody.linearVelocity = Vector2.zero;
            return;
        }
        
        Vector2 direction = ((Vector2)_playerTransform.position - _rigidBody.position).normalized;
        _rigidBody.linearVelocity = direction * chaseSpeed;
    }

    public void StartNormalChase()
    {
        _moveMode = MoveMode.NormalChase;
    }
    
    public void StartAggressiveChase()
    {
        _moveMode = MoveMode.AggressiveChase;
    }
    
    public void Stop()
    {
        _moveMode = MoveMode.Stop;
        _rigidBody.linearVelocity = Vector2.zero;
    }
    
    private void FacePlayer()
    {
        Vector3 scale = _visualPivot.localScale;
        float rightPivotx = _rigidBody.position.x + _horizontalFlip;
        float leftPivotx = _rigidBody.position.x - _horizontalFlip;

        if (scale.x > 0f)
        {
            if (_playerTransform.position.x < leftPivotx)
            {
                scale.x = -1f;
            }
        }

        else
        {
            if (_playerTransform.position.x > rightPivotx)
            {
                scale.x = 1f;
            }
        }
        
        _visualPivot.localScale = scale;
    }
}
