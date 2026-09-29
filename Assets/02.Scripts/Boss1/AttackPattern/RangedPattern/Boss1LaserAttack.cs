using UnityEngine;

public class Boss1LaserAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Transform _laserHeadPivot;
    [SerializeField] private Transform _playerTransform;

    
    [SerializeField] private float _aimedFireInteval = 0.5f;
    [SerializeField] private int _aimedFireCount = 8;
    
    private float _timer;
    private int _currentFireCount;
    
    public bool IsFinished { get; private set; }

    public void EnterAttack()
    {
        _timer = 0f;
        _currentFireCount = 0;
        IsFinished = false;

        Debug.Log("AimedBulletAttack 패턴 시작");
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }
        
        _timer += Time.deltaTime;
        if (_timer < _aimedFireInteval)
        {
            return;
        }

        FireAimedBullet();
        _timer = 0f;
        _currentFireCount++;
        if (_currentFireCount >= _aimedFireCount)
        {
            IsFinished = true;
        }
    }

    public void ExitAttack()
    {
        Debug.Log("AimedBulletAttack 패턴 종료");
    }

    private void FireAimedBullet()
    {
        Vector2 direction = (_playerTransform.position - _aimedHeadPivot.position).normalized;
        Boss1Bullet bullet = Instantiate(_bulletPrefab, _aimedHeadPivot.position, Quaternion.identity);
        bullet.Initialize(direction);
    }
}
