using DG.Tweening;
using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float _rotationDuration = 8f;

    private Tween _rotationTween;

    private void OnEnable()
    {
        StartRotation();
    }

    private void StartRotation()
    {
        _rotationTween?.Kill();

        _rotationTween = transform
            .DORotate(
                new Vector3(0f, 0f, -360f),
                _rotationDuration,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetRelative()
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        _rotationTween?.Kill();
        _rotationTween = null;
    }
}
