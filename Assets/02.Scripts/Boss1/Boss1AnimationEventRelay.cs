using UnityEngine;

public class Boss1AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private Boss1ClawAttack _boss1ClawAttack;
    
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
}
