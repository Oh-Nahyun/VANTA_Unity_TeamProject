using DG.Tweening;
using UnityEngine;

public class Fire : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField]
    private AudioClip _fireSound;

    [Header("Scale Animation")]
    [SerializeField, Min(0.01f)]
    private float _activateDuration = 0.4f;

    [SerializeField, Min(0.01f)]
    private float _deactivateDuration = 0.4f;

    [SerializeField]
    private Ease _activateEase = Ease.OutBack;

    [SerializeField]
    private Ease _deactivateEase = Ease.InBack;

    [Header("Red Fire")]
    [SerializeField]
    private Color _changedColor = Color.red;

    [SerializeField, Min(1f)]
    private float _changedScaleMultiplier = 1.5f;

    [Header("Position Correction")]
    [SerializeField]
    private bool _enablePositionCorrection = true;

    [SerializeField, Range(0f, 1f)]
    private float _pivotCorrection = 0.5f;

    private SpriteRenderer _fireSprite;

    private Tween _scaleTween;

    private Vector3 _originalScale;
    private Vector3 _originalLocalPosition;

    private float _spriteHeight;

    private bool _isActive = true;
    private bool _isColorChanged = false;

    private void Awake()
    {
        _fireSprite = GetComponent<SpriteRenderer>();

        _originalScale = transform.localScale;
        _originalLocalPosition = transform.localPosition;

        if (_fireSprite.sprite != null)
        {
            _spriteHeight = _fireSprite.sprite.bounds.size.y;
        }
    }

    public void ActivateFire()
    {
        _scaleTween?.Kill();

        _isActive = true;

        _fireSprite.enabled = true;

        Vector3 targetScale = _isColorChanged
            ? _originalScale * _changedScaleMultiplier
            : _originalScale;

        _scaleTween = transform
            .DOScale(
                targetScale,
                _activateDuration
            )
            .SetEase(_activateEase)
            .OnUpdate(UpdatePositionCorrection)
            .OnComplete(() =>
            {
                UpdatePositionCorrection();
                _scaleTween = null;
            });

        if (_fireSound != null)
        {
            SoundManager.Instance.PlayEffectSound(
                _fireSound
            );
        }
    }

    public void DeactivateFire()
    {
        _scaleTween?.Kill();

        _isActive = false;

        _scaleTween = transform
            .DOScale(
                Vector3.zero,
                _deactivateDuration
            )
            .SetEase(_deactivateEase)
            .OnUpdate(UpdatePositionCorrection)
            .OnComplete(() =>
            {
                UpdatePositionCorrection();

                SpriteStatusChange();

                ChangeColor();

                _scaleTween = null;
            });
    }

    private void UpdatePositionCorrection()
    {
        if (!_enablePositionCorrection)
            return;

        float scaleDifference =
            transform.localScale.y - _originalScale.y;

        float correctionY =
            scaleDifference *
            _spriteHeight *
            _pivotCorrection;

        transform.localPosition =
            _originalLocalPosition +
            Vector3.up * correctionY;
    }

    private void ChangeColor()
    {
        _fireSprite.color = _changedColor;

        _isColorChanged = true;
    }

    public void SpriteStatusChange()
    {
        _fireSprite.enabled = _isActive;
    }

    private void OnDestroy()
    {
        _scaleTween?.Kill();
    }
}
