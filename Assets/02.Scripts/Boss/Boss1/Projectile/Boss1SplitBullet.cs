using UnityEngine;

public class Boss1SplitBullet : MonoBehaviour
{
    [Header("확산탄 이동 속도")]
    [SerializeField] private float _speed = 2f;
    
    [Header("확산탄 분열 시작 전 이동 시간")]
    [SerializeField] private float _flyDuration = 2f;
    
    [Header("멈춘 후에 분열 전 대기 시간")]
    [SerializeField] private float _warningDuration = 0.7f;
    
    [Header("분열되는 탄환 개수")]
    [SerializeField] private int _splitBulletCount = 8;
    
    [SerializeField] private Boss1Bullet _bulletPrefab;
    
    private Boss1ProjectileManager _projectileManager;
    
    private Rigidbody2D _rigidBody;
    private Collider2D _collider;
    private Animator _animator;
    
    private float _timer;
    
    private bool _isDestroying;

    private SplitBulletState _currentState;

    private enum SplitBulletState
    {
        Fly,
        Warning,
        Split
    }

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_isDestroying)
        {
            return;
        }
        
        _timer += Time.deltaTime;
        
        switch(_currentState)
        {
            case SplitBulletState.Fly:
                UpdateFly();
                break;
            case SplitBulletState.Warning:
                UpdateWarning();
                break;
            case SplitBulletState.Split:
                UpdateSplit();
                break;
        }
    }

    public void Initialize(Vector2 direction, Boss1ProjectileManager projectileManager)
    {
        _projectileManager = projectileManager;
        _projectileManager.RegisterProjectile(gameObject);
        
        _timer = 0f;
        _isDestroying = false;
        
        _currentState = SplitBulletState.Fly;
        _rigidBody.linearVelocity = direction.normalized * _speed;
    }

    private void UpdateFly()
    {
        if (_timer < _flyDuration)
        {
            return;
        }
        
        _rigidBody.linearVelocity = Vector2.zero;
        _animator.SetTrigger("Warning");
        ChangeState(SplitBulletState.Warning);
    }

    private void UpdateWarning()
    {
        if (_timer < _warningDuration)
        {
            return;
        } 
        ChangeState(SplitBulletState.Split);
    }

    private void UpdateSplit()
    {
        float angleStep = 360f / _splitBulletCount;

        for (int i = 0; i < _splitBulletCount; i++)
        {
            float angle = angleStep * i;

            Vector2 direction = new Vector2( Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            Boss1Bullet bullet = Instantiate( _bulletPrefab, transform.position, Quaternion.identity );

            bullet.Initialize(direction, _projectileManager);
        }

        StartDisappear();
    }
    
    private void ChangeState(SplitBulletState newState)
    {
        _currentState = newState;
        _timer = 0f;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDestroying)
        {
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            
            if (player == null)
            {
                return;
            }
            
            player.TakeDamage();
            StartImpact();
        }
    }
    private void StartImpact()
    {
        _isDestroying = true;

        _rigidBody.linearVelocity = Vector2.zero;
        _collider.enabled = false;

        _animator.SetTrigger("Impact");
    }

    private void StartDisappear()
    {
        _isDestroying = true;

        _rigidBody.linearVelocity = Vector2.zero;
        _collider.enabled = false;

        _animator.SetTrigger("Disappear");
    }
    
    public void ForceDisappear()
    {
        if (_isDestroying)
        {
            return;
        }

        StartDisappear();
    }

    public void DestroyBullet()
    {
        Destroy(gameObject);
    }
    
    
    private void OnDestroy()
    {
        if (_projectileManager != null)
        {
            _projectileManager.UnregisterProjectile(gameObject);
        }
    }
}
