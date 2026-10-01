using UnityEngine;

public class Boss1AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private Boss1ClawAttack _boss1ClawAttack;
    [SerializeField] private Boss1SpitAttack _boss1SpitAttack;
    
    public void EnableClawHitBox()
    {
        _boss1ClawAttack.EnableClawHitBox();
    }

    public void DisableClawHitBox()
    {
        _boss1ClawAttack.DisableClawHitBox();
    }

    public void EndClawAttack()
    {
        _boss1ClawAttack.EndClawAttack();
    }

    public void EnableSpitHitBox()
    {
        _boss1SpitAttack.EnableSpitHitBox();
    }

    public void DisableSpitHitBox()
    {
        _boss1SpitAttack.DisableSpitHitBox();
    }

    public void EndSpitHitAttack()
    {
        _boss1SpitAttack.EndSpitAttack();
    }
    
}
