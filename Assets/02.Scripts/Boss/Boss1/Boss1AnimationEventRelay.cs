using UnityEngine;

public class Boss1AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private Boss1IntroController _boss1IntroController;
    
    [SerializeField] private Boss1ClawAttack _boss1ClawAttack;
    [SerializeField] private Boss1SpitAttack _boss1SpitAttack;
    [SerializeField] private Boss1JumpAttack _boss1JumpAttack;

    [SerializeField] private Boss1DeathController _boss1DeathController;
    
    public void RemoveWarningZone()
    {
        _boss1IntroController.RemoveWarningZone();
    }
 
    public void EndIntroJumpLand()
    {
        _boss1IntroController.EndIntroJumpLand();
    }
    
    public void EndIntroClawStart()
    {
        _boss1IntroController.EndIntroClawStart();
    }
    
    public void EndIntroClawEnd()
    {
        _boss1IntroController.EndIntroClawEnd();
    }

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

    public void EndSpitAttack()
    {
        _boss1SpitAttack.EndSpitAttack();
    }

    public void JumpLandImpact()
    {
        _boss1JumpAttack.JumpLandImpact();
    }

    public void EndJumpLand()
    {
        _boss1JumpAttack.EndJumpLand();
    }

    public void EndDeathCollapse()
    {
        _boss1DeathController.EndDeathCollapse();
    }
}
