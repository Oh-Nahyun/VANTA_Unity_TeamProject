using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Boss1MeleeTelegraph : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private SpriteRenderer _glintSprite;
    [SerializeField] private Light2D _glintLight;

    [Header("연출")]
    [SerializeField] private float _popDuration = 0.1f;
    [SerializeField] private float _holdDuration = 0.08f;
    [SerializeField] private float _fadeDuration = 0.15f;
    [SerializeField] private float _peakScale = 1.3f;
    [SerializeField] private float _spinAngle = 90f;
    [SerializeField] private float _peakLightIntensity = 3f;

    private Vector3 _baseScale;
    private Sequence _sequence;

    private void Awake()
    {
        _baseScale = _glintSprite.transform.localScale;
        SetAlpha(0f);
        SetLight(0f);
    }

    public void Play()
    {
        _sequence?.Kill(); // 이전 꺼 죽임

        Transform glint = _glintSprite.transform; // 시작값 세팅
        glint.localScale = Vector3.zero; 
        glint.localRotation = Quaternion.identity;
        SetAlpha(1f);
        SetLight(_peakLightIntensity);

        float total = _popDuration + _holdDuration + _fadeDuration;

        _sequence = DOTween.Sequence() // 두트윈 여러개 실행
            .Append(glint.DOScale(_baseScale * _peakScale, _popDuration).SetEase(Ease.OutBack))
            .Join(glint.DOLocalRotate(new Vector3(0f, 0f, _spinAngle), total, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
            .Append(glint.DOScale(_baseScale, _holdDuration))
            .Append(_glintSprite.DOFade(0f, _fadeDuration))
            .Join(glint.DOScale(Vector3.zero, _fadeDuration).SetEase(Ease.InBack))
            .Join(DOTween.To(GetLight, SetLight, 0f, _fadeDuration))
            .SetLink(gameObject);
    }

    public void Stop()
    {
        _sequence?.Kill();
        SetAlpha(0f);
        SetLight(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color color = _glintSprite.color;
        color.a = alpha;
        _glintSprite.color = color;
    }

    private float GetLight() => _glintLight != null ? _glintLight.intensity : 0f;

    private void SetLight(float intensity)
    {
        if (_glintLight != null)
        {
            _glintLight.intensity = intensity;
        }
    }
}
