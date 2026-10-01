using UnityEngine;

public class Boss1SplitBulletAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Transform _splitHeadPivot;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Boss1SplitBullet _splitBulletPrefab;

    [SerializeField] private float _splitFireInteval = 1f;
    [SerializeField] private int _splitFireCount = 5;
    
    private Boss1ProjectileManager _projectileManager;
    
    private float _timer;
    private int _currentFireCount;
    
    public bool IsFinished { get; private set; }

    private void Awake()
    {
        _projectileManager = GetComponent<Boss1ProjectileManager>();
    }
    
    public void EnterAttack()
    {
        _timer = 0f;
        _currentFireCount = 0;
        IsFinished = false;
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }
        
        _timer += Time.deltaTime;
        if (_timer < _splitFireInteval)
        {
            return;
        }

        FireSplitBullet();
        _timer = 0f;
        _currentFireCount++;
        if (_currentFireCount >= _splitFireCount)
        {
            IsFinished = true;
        }
    }

    public void ExitAttack()
    {

    }

    
    private void FireSplitBullet()
    {
        Vector2 direction = (_playerTransform.position - _splitHeadPivot.position).normalized;
        Boss1SplitBullet splitBullet = Instantiate(_splitBulletPrefab, _splitHeadPivot.position, Quaternion.identity);
        splitBullet.Initialize(direction, _projectileManager);
    }
}
