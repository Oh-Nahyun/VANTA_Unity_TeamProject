using DG.Tweening;
using UnityEngine;

public class Fire : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _fireSprite;

    private bool _isActive = true;
    private Tween _deactivateTween;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fireSprite = GetComponent<SpriteRenderer>();
    }

    public void ActivateFire()
    {
        _isActive = true;

        SpriteStatusChange();

        _animator.SetTrigger("ActivateFire");
        
    }

    public void DeactivateFire()
    {
        _deactivateTween?.Kill();
        _isActive = false;
        _animator.SetTrigger("DeactivateFire");

        _deactivateTween = DOVirtual.DelayedCall(
            2f,
            () =>
            {
                SpriteStatusChange();

                ChangeColor();
                _deactivateTween = null;
            }
        );
    }

    private void ChangeColor()
    {
        _fireSprite.color = Color.red;
    }

    private void OnDestroy()
    {
        _deactivateTween?.Kill();
    }
    
    public void SpriteStatusChange()
    {
        _fireSprite.enabled = _isActive;

    }
 
}
