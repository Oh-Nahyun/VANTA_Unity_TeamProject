using UnityEngine;

public class Boss1AttackBulletHell : MonoBehaviour, IBoss1Attack
{
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

        Debug.Log("원거리 패턴 시작");
    }

    public void UpdateAttack()
    {
        
    }

    public void ExitAttack()
    {
        Debug.Log("원거리 패턴 종료");
    }
}
