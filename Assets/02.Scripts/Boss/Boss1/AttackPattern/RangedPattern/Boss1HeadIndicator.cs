using DG.Tweening;
using UnityEngine;

public class Boss1HeadIndicator : MonoBehaviour
{
    [SerializeField] private float _popDuration = 0.15f;
    [SerializeField] private float _pulseScale = 1.08f;
    [SerializeField] private float _pulseDuration = 0.25f;

    private Vector3 _baseScale = Vector3.one;
    private bool _hasBaseScale;
    private Tween _popTween;
    private Tween _pulseTween;

    public void ShowHeadIndicator()
    {
        if (!_hasBaseScale)
        {
            _baseScale = transform.localScale;
            _hasBaseScale = true;
        }

        KillTweens();
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        _popTween = transform.DOScale(_baseScale, _popDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject)
            .OnComplete(StartPulse);
    }

    public void HideHeadIndicator()
    {
        KillTweens();
        if (_hasBaseScale)
        {
            transform.localScale = _baseScale;
        }
        gameObject.SetActive(false);
    }

    private void StartPulse()
    {
        _pulseTween = transform.DOScale(_baseScale * _pulseScale, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void KillTweens()
    {
        _popTween?.Kill();
        _pulseTween?.Kill();
    }
}
