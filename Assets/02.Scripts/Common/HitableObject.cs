using UnityEngine;

public abstract class HitableObject : MonoBehaviour
{
    public void ExecuteHitEvent(float damage,DamageIndicatorType damageType)
    {
        DamageIndicatorFactory.Instance.IndicateDamage(gameObject.transform.position,damage,damageType);
        HitEvent();
    }
    protected abstract void HitEvent(); //피격시 일어날 행동들 
}
