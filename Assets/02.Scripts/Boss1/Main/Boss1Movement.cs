using UnityEngine;
using UnityEngine.UIElements;

public class Boss1Movement : MonoBehaviour, IBossMovable
{
    [SerializeField] private float _normalChaseSpeed= 4f;
    [SerializeField] private float _aggressiveChaseSpeed = 7f;
    [SerializeField] private float _stopDistance = 2.5f;
    [SerializeField] private float _yTolerance = 0.1f;
    
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
        Stop,
        Controlled
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
            case MoveMode.Controlled:
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
        float xDistance = Mathf.Abs(_playerTransform.position.x - _rigidBody.position.x);
        float yDistance = Mathf.Abs(_playerTransform.position.y - _rigidBody.position.y);

        if (xDistance <= _stopDistance && yDistance <= _yTolerance)
        {
            _rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        if (xDistance <= _stopDistance)
        {
            float yDirection = Mathf.Sign(_playerTransform.position.y - _rigidBody.position.y);
            _rigidBody.linearVelocity = new Vector2(0f, yDirection * chaseSpeed);
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

    public void StartControlled()
    {
        _moveMode = MoveMode.Controlled;
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
    
    public void MoveInDirection(Vector2 direction, float speed) //외부용
    {
        _rigidBody.linearVelocity = direction.normalized * speed;
    }

    public void StopMove() //외부용, 근거리 패턴용도, Stop()이랑 헷갈리면 안됨!
    {
        _rigidBody.linearVelocity = Vector2.zero;
    }
}
