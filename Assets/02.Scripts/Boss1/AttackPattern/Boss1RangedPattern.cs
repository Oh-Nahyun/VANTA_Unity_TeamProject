using UnityEngine;

public class Boss1RangedPattern : MonoBehaviour, IBoss1Attack
{
    [SerializeField] private Boss1BulletHellAttack _bulletHellAttack;
    [SerializeField] private float _weight = 20f;
    
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

        _bulletHellAttack.EnterAttack();
        
        Debug.Log("원거리 패턴 시작");
    }

    public void UpdateAttack()
    {
        if (_bulletHellAttack.IsFinished)
        {
            _bulletHellAttack.ExitAttack();
            IsFinished = true;
            return;
        }

        _bulletHellAttack.UpdateAttack();
    }

    public void ExitAttack()
    {
        Debug.Log("원거리 패턴 종료");
    }
}
