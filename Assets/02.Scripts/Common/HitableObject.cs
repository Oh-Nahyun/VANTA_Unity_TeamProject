using UnityEngine;

public abstract class HitableObject : MonoBehaviour
{
    public void ExecuteHitEvent(float damage, DamageIndicatorType damageType)
    {
        DamageIndicatorFactory.Instance.IndicateDamage(gameObject.transform.position, damage, damageType);
        HitEvent((int)damage);
    }
    protected abstract void HitEvent(int damage); //피격시 일어날 행동들 
}
