using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Boss1MuzzleFlash : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private SpriteRenderer _flashSprite;
    [SerializeField] private Light2D _flashLight; // 비워도 됨

    [Header("조준탄·확산탄")]
    [SerializeField] private float _flashDuration = 0.12f;
    [SerializeField] private float _startScale = 0.6f;
    [SerializeField] private float _peakScale = 1.2f;
    [SerializeField] private float _peakLightIntensity = 2.5f;

    [Header("레이저 발사 중")]
    [SerializeField] private float _holdScale = 0.8f;
    [SerializeField] private float _holdLightIntensity = 1.5f;
    [SerializeField] private float _flickerAmount = 0.15f;
    [SerializeField] private float _flickerSpeed = 30f;
    [SerializeField] private float _holdFadeOutDuration = 0.1f;

    private Vector3 _baseScale;
    private bool _isHolding;
    private Sequence _sequence;

    private void Awake()
    {
        _baseScale = _flashSprite.transform.localScale;
        SetAlpha(0f);
        SetLight(0f);
    }

    private void Update()
    {
        if (!_isHolding)
        {
            return;
        }

        float flicker = 1f + Mathf.Sin(Time.time * _flickerSpeed) * _flickerAmount;
        _flashSprite.transform.localScale = _baseScale * (_holdScale * flicker);
        SetLight(_holdLightIntensity * flicker);
    }
    
    public void Play()
    {
        _isHolding = false;
        _sequence?.Kill();

        _flashSprite.transform.localScale = _baseScale * _startScale;
        SetAlpha(1f);
        SetLight(_peakLightIntensity);

        float growTime = _flashDuration * 0.4f;
        float fadeTime = _flashDuration * 0.6f;

        _sequence = DOTween.Sequence()
            .Append(_flashSprite.transform.DOScale(_baseScale * _peakScale, growTime).SetEase(Ease.OutQuad))
            .Append(_flashSprite.DOFade(0f, fadeTime))
            .Join(DOTween.To(GetLight, SetLight, 0f, fadeTime))
            .SetLink(gameObject);
    }
    
    public void StartHold()
    {
        Play();
        _sequence.OnComplete(() =>
        {
            SetAlpha(1f);
            _isHolding = true;
        });
    }
    
    public void StopHold()
    {
        if (!_isHolding && (_sequence == null || !_sequence.IsActive()))
        {
            return;
        }

        _isHolding = false;
        _sequence?.Kill();

        _sequence = DOTween.Sequence()
            .Append(_flashSprite.DOFade(0f, _holdFadeOutDuration))
            .Join(DOTween.To(GetLight, SetLight, 0f, _holdFadeOutDuration))
            .SetLink(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        Color color = _flashSprite.color;
        color.a = alpha;
        _flashSprite.color = color;
    }

    private float GetLight()
    {
        return _flashLight != null ? _flashLight.intensity : 0f;
    }

    private void SetLight(float intensity)
    {
        if (_flashLight != null)
        {
            _flashLight.intensity = intensity;
        }
    }
}
