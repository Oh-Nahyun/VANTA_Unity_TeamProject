using UnityEngine;

public class Boss1RangedPattern : MonoBehaviour, IBoss1Attack
{
    [SerializeField] private float _weight = 20f;

    [SerializeField] private Transform _headPivot;
    [SerializeField] private Transform _playerTransform;
    
    [SerializeField] private Boss1Bullet _bulletPrefab;
    
    public bool IsFinished { get; private set; }
    
    public bool CanUse()
    {
        return true;
    }

    public float GetWeight()
    {
        return _weight;
    }

    public void EnterAttack()
    {
        IsFinished = false;

        Debug.Log("원거리 패턴 시작");
    }

    public void UpdateAttack()
    {
        FireBullet();
    }

    public void ExitAttack()
    {
        Debug.Log("원거리 패턴 종료");
    }

    private void FireBullet()
    {
        Vector2 direction = (_playerTransform.position - _headPivot.position).normalized;
        Boss1Bullet bullet = Instantiate(_bulletPrefab, _headPivot.position, Quaternion.identity);
        bullet.Initialize(direction);
    }
}
