using System;
using DG.Tweening;
using UnityEngine;

public class UIAttentionShake : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField] private float _shakeDistance = 2f;
    [SerializeField] private float _moveDuration = 0.04f;
    [SerializeField] private int _shakeCount = 3;

    [Header("Interval")]
    [SerializeField] private float _interval = 1.2f;

    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

 

    private void Start()
    {
        
        _originalPosition = _rectTransform.anchoredPosition;

        PlayLoop();
    }

    private void PlayLoop()
    {
        _sequence?.Kill();

        _rectTransform.anchoredPosition =
            _originalPosition;

        _sequence = DOTween.Sequence()
            .SetUpdate(true);

        for (int i = 0; i < _shakeCount; i++)
        {
            _sequence.Append(
                _rectTransform.DOAnchorPosX(
                    _originalPosition.x - _shakeDistance,
                    _moveDuration
                )
                .SetEase(Ease.Linear)
            );

            _sequence.Append(
                _rectTransform.DOAnchorPosX(
                    _originalPosition.x + _shakeDistance,
                    _moveDuration * 2f
                )
                .SetEase(Ease.Linear)
            );

            _sequence.Append(
                _rectTransform.DOAnchorPosX(
                    _originalPosition.x,
                    _moveDuration
                )
                .SetEase(Ease.Linear)
            );
        }

        _sequence.AppendInterval(_interval);

        _sequence.SetLoops(-1);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition =
                _originalPosition;
        }
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
    }
}
