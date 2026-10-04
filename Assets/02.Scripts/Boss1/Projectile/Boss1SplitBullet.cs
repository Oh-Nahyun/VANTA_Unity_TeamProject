using UnityEngine;

public class Boss1SplitBullet : MonoBehaviour
{
    // commit 체크용 주석
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
    
    private float _timer;

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
    }

    private void Update()
    {
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
        _timer = 0f;
        _currentState = SplitBulletState.Warning;
    }

    private void UpdateWarning()
    {
        if (_timer < _warningDuration)
        {
            return;
        } 
        _currentState = SplitBulletState.Split;

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

        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        if (_projectileManager != null)
        {
            _projectileManager.UnregisterProjectile(gameObject);
        }
    }
    
    private void ChangeState(SplitBulletState newState)
    {
        _currentState = newState;
        _timer = 0f;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                return;
            }
            player.TakeDamage();
            Destroy(gameObject);
        }
    }
}
