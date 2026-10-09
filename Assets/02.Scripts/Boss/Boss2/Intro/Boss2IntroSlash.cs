using System.Collections;
using UnityEngine;

/// <summary>
/// 보스2 인트로의 X 베기 한 획. 선명한 본체(_core)가 한쪽 끝에서 반대쪽으로 순식간에 그어지고,
/// 같은 모양을 두껍게 만든 가산 글로우(_glow)가 외곽에서 반짝였다가 사라진다.
/// 스프라이트 피벗은 왼쪽 끝(0, 0.5)이어야 한다. 오브젝트의 회전으로 베는 방향을 정한다.
/// </summary>
public class Boss2IntroSlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _core;
    [SerializeField] private SpriteRenderer _glow;

    [Header("타이밍 (초, 시간 배율 무시)")]
    [SerializeField] private float _drawTime = 0.06f;
    [SerializeField] private float _holdTime = 0.08f;
    [SerializeField] private float _fadeTime = 0.3f;
    [SerializeField] private float _glowFadeTime = 0.4f;

    [Header("글로우")]
    [SerializeField] private float _glowStartThickness = 1f;
    [SerializeField] private float _glowEndThickness = 1.8f;

    private Color _coreColor;
    private Color _glowColor;
    private Vector3 _coreScale;
    private Vector3 _glowScale;
    private Coroutine _routine;

    private void Awake()
    {
        _coreColor = _core.color;
        _glowColor = _glow.color;
        _coreScale = _core.transform.localScale;
        _glowScale = _glow.transform.localScale;
        SetVisible(false);
    }

    public void Play()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetVisible(true);

        // 1) 긋기: 길이 0 → 1 (끝으로 갈수록 빨라졌다 멈추는 느낌)
        float t = 0f;
        while (t < _drawTime)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / _drawTime), 3f);
            SetLength(k);
            _core.color = _coreColor;
            _glow.color = WithAlpha(_glowColor, _glowColor.a * k);
            yield return null;
        }
        SetLength(1f);

        // 2) 유지 + 3) 사라짐: 본체는 얇아지며 사라지고, 글로우는 바깥으로 번지며 사라진다.
        float total = _holdTime + Mathf.Max(_fadeTime, _glowFadeTime);
        t = 0f;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            float coreK = Mathf.Clamp01((t - _holdTime) / _fadeTime);
            Vector3 coreScale = _coreScale;
            coreScale.y *= 1f - coreK;
            _core.transform.localScale = coreScale;
            _core.color = WithAlpha(_coreColor, _coreColor.a * (1f - coreK));

            float glowK = Mathf.Clamp01(t / (_holdTime + _glowFadeTime));
            Vector3 glowScale = _glowScale;
            glowScale.y *= Mathf.Lerp(_glowStartThickness, _glowEndThickness, glowK);
            _glow.transform.localScale = glowScale;
            _glow.color = WithAlpha(_glowColor, _glowColor.a * (1f - glowK) * (1f - glowK));
            yield return null;
        }

        SetVisible(false);
        _routine = null;
    }

    private void SetLength(float k)
    {
        Vector3 coreScale = _coreScale;
        coreScale.x *= k;
        _core.transform.localScale = coreScale;

        Vector3 glowScale = _glowScale;
        glowScale.x *= k;
        glowScale.y *= _glowStartThickness;
        _glow.transform.localScale = glowScale;
    }

    private void SetVisible(bool visible)
    {
        _core.enabled = visible;
        _glow.enabled = visible;
        if (visible == false)
        {
            _core.transform.localScale = _coreScale;
            _glow.transform.localScale = _glowScale;
            _core.color = _coreColor;
            _glow.color = _glowColor;
        }
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
