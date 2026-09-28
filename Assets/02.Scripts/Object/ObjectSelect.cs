using DG.Tweening;
using UnityEngine;

public class ObjectSelect : MonoBehaviour
{
    [Header("Select Settings")]
    [SerializeField] private float _selectScale = 1.3f;
    [SerializeField] private float _duration = 0.15f;

    private Vector3 _originalScale;
    private Tween _scaleTween;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void StartSelect()
    {
        _scaleTween?.Kill();

        Vector3 targetScale = _originalScale * _selectScale;

        _scaleTween = transform
            .DOScale(targetScale, _duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void EndSelect()
    {
        _scaleTween?.Kill();

        _scaleTween = transform
            .DOScale(_originalScale, _duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        transform.localScale = _originalScale;
    }
}
