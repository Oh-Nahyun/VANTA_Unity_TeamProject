using DG.Tweening;
using UnityEngine;

public class Boss1WarningZoneVFX : MonoBehaviour
{
    [Header("원 3개")]
    [SerializeField] private SpriteRenderer _area;   // WarningCircle
    [SerializeField] private SpriteRenderer _fill;   // WarningCircleMini
    [SerializeField] private SpriteRenderer _ring;   // WarningRing

    [Header("경고존 생성")]
    [SerializeField] private float _appearDuration = 0.2f;
    [SerializeField] private float _appearOvershoot = 1.7f;

    [Header("경고존 사라짐")]
    [SerializeField] private float _removeDuration = 0.12f;
    [SerializeField] private float _removeScale = 1.15f;
    [SerializeField] private float _removeFlashAlpha = 0.7f;

    private Vector3 _rootScale;
    private Vector3 _fillFullScale;
    private float _areaAlpha;
    private float _fillAlpha;
    private float _ringAlpha;

    private Sequence _sequence;
    private bool _isRemoving;

    private void Awake()
    {
        _rootScale = transform.localScale;
        _fillFullScale = _area.transform.localScale;
        _areaAlpha = _area.color.a;
        _fillAlpha = _fill.color.a;
        if (_ring != null)
        {
            _ringAlpha = _ring.color.a;
        }
    }

    public void Play(float duration)
    {
        _sequence?.Kill();
        _isRemoving = false;

        SetAlpha(_area, _areaAlpha);
        SetAlpha(_fill, _fillAlpha);
        SetAlpha(_ring, _ringAlpha);

        transform.localScale = Vector3.zero;
        _fill.transform.localScale = Vector3.zero;

        _sequence = DOTween.Sequence().SetLink(gameObject);
        _sequence.Append(transform.DOScale(_rootScale, _appearDuration)
            .SetEase(Ease.OutBack, _appearOvershoot));
        _sequence.Insert(0f, _fill.transform.DOScale(_fillFullScale, duration)
            .SetEase(Ease.Linear));
    }

    public void Remove()
    {
        if (_isRemoving) return;
        _isRemoving = true;

        _sequence?.Kill();

        // 번쩍: 채움을 꽉 채우고 한순간 진하게
        _fill.transform.localScale = _fillFullScale;
        SetAlpha(_fill, _removeFlashAlpha);

        _sequence = DOTween.Sequence().SetLink(gameObject);
        _sequence.Append(transform.DOScale(_rootScale * _removeScale, _removeDuration)
            .SetEase(Ease.OutQuad));
        _sequence.Join(_area.DOFade(0f, _removeDuration));
        _sequence.Join(_fill.DOFade(0f, _removeDuration));
        
        if (_ring != null)
        {
            _sequence.Join(_ring.DOFade(0f, _removeDuration));
        }
        _sequence.OnComplete(() => Destroy(gameObject));
    }

    private static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}
