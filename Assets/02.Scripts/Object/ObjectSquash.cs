using UnityEngine;
using DG.Tweening;

public class ObjectSquash : MonoBehaviour
{
    [Header("스쿼시 세팅")]
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private float _squashAmount = 0.7f;
    [SerializeField] private float _stretchAmount = 1.2f;

    private Vector3 _originalScale;
    private Sequence _squashSequence;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlaySquash();
    }

    public void PlaySquash()
    {
        _squashSequence?.Kill();

        transform.localScale = new Vector3(
            _originalScale.x * _squashAmount,
            _originalScale.y * _stretchAmount,
            _originalScale.z * _squashAmount
        );

        _squashSequence = DOTween.Sequence();

        _squashSequence.Append(
            transform.DOScale(
                new Vector3(
                    _originalScale.x * _stretchAmount,
                    _originalScale.y * _squashAmount,
                    _originalScale.z * _stretchAmount
                ),
                _duration
            ).SetEase(Ease.OutQuad)
        );

        _squashSequence.Append(
            transform.DOScale(_originalScale, _duration)
                .SetEase(Ease.OutBack)
        );

        _squashSequence.SetUpdate(true);
    }

    private void OnDisable()
    {
        _squashSequence?.Kill();
    }
}
