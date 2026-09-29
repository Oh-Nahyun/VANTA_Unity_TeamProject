using UnityEngine;

public class Boss1SplitBullet : MonoBehaviour
{
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _flyDuration = 2f;
    [SerializeField] private float _warningDuration = 0.7f;
    
    [SerializeField] private int _splitBulletCount = 8;
    
    [SerializeField] private Boss1Bullet _bulletPrefab;
    
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

    public void Initialize(Vector2 direction)
    {
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
        if (_timer < _flyDuration)
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

            bullet.Initialize(direction);
        }

        Destroy(gameObject);
    }
    
    private void ChangeState(SplitBulletState newState)
    {
        _currentState = newState;
        _timer = 0f;
    }
}
