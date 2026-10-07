using System;
using DG.Tweening;
using UnityEngine;

public class Fire : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _fireSprite;

    private Tween _changeTween;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _fireSprite = GetComponent<SpriteRenderer>();
    }

    private void ActivateFire()
    {
        _animator.SetTrigger("ActivateFire");
    }

    private void DeactivateFire()
    {
        _animator.SetTrigger("DeactivateFire");
    }

    private void ChangeColor()
    {
        _fireSprite.color = Color.red;
    }

    public void ChangeFire(Action onActivate = null)
    {
        _changeTween?.Kill();

        DeactivateFire();

        _changeTween = DOVirtual.DelayedCall(
            2f,
            () =>
            {
                ChangeColor();

                ActivateFire();

                onActivate?.Invoke();

                _changeTween = null;
            }
        );
    }

    private void OnDestroy()
    {
        _changeTween?.Kill();
    }
}
