using DG.Tweening;
using UnityEngine;

public class Boss1HeadIndicator : MonoBehaviour
{
    [Header("등장 / 퇴장")]
    [SerializeField] private float _fadeInDuration = 0.12f;
    [SerializeField] private float _fadeOutDuration = 0.08f;

    [Header("등장 순간 움찔")]
    [SerializeField] private float _punchStrength = 0.08f;
    [SerializeField] private float _punchDuration = 0.15f;

    private SpriteRenderer _spriteRenderer;
    private Vector3 _baseScale;
    private float _baseAlpha;
    private bool _isInitialized;

    private Tween _fadeTween;
    private Tween _punchTween;
    
    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
        _baseAlpha = _spriteRenderer.color.a;
        _isInitialized = true;
    }

    public void ShowHeadIndicator()
    {
        Initialize();
        KillTweens();

        gameObject.SetActive(true);
        transform.localScale = _baseScale;
        SetAlpha(0f);

        _fadeTween = _spriteRenderer.DOFade(_baseAlpha, _fadeInDuration)
            .SetLink(gameObject);

        if (_punchStrength > 0f)
        {
            _punchTween = transform.DOPunchScale(_baseScale * _punchStrength, _punchDuration, 1, 0f)
                .SetLink(gameObject);
        }
    }

    public void HideHeadIndicator()
    {
        if (!_isInitialized || !gameObject.activeSelf)
        {
            return;
        }

        KillTweens();
        
        transform.localScale = _baseScale;

        _fadeTween = _spriteRenderer.DOFade(0f, _fadeOutDuration)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                SetAlpha(_baseAlpha);
                gameObject.SetActive(false);
            });
    }
    
    private void SetAlpha(float alpha)
    {
        Color color = _spriteRenderer.color;
        color.a = alpha;
        _spriteRenderer.color = color;
    }

    private void KillTweens()
    {
        _fadeTween?.Kill();
        _punchTween?.Kill();
    }
}
