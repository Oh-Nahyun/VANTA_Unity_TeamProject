using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 광역기 연출. 맵 전체(Global Light 2D)를 어둡게 하고, 대상(플레이어) 주변만 밝힌다.
/// 대상을 따라가는 라이트는 대상의 자식이 아니라 이 오브젝트의 자식이라 Player 쪽은 건드리지 않는다.
/// 인디케이터·폭발처럼 Unlit 머티리얼을 쓰는 것은 어둠과 상관없이 그대로 밝게 보인다.
/// </summary>
public class Boss2DarknessEffect : MonoBehaviour
{
    [Header("라이트")]
    [Tooltip("씬의 Global Light 2D. 어둠 동안 세기를 낮춘다")]
    [SerializeField] private Light2D _globalLight;

    [Tooltip("대상(플레이어)을 따라다니는 라이트. Point(원) 또는 Sprite(이미지 모양) 타입")]
    [SerializeField] private Light2D _focusLight;

    [Header("세기")]
    [Tooltip("어둠 동안 Global Light 세기")]
    [SerializeField] private float _darkIntensity = 0.15f;

    [Tooltip("어둠 동안 대상 주변 라이트 세기")]
    [SerializeField] private float _focusIntensity = 1f;

    [Tooltip("어둠 동안 함께 켜는 추가 라이트(보스 등) 세기")]
    [SerializeField] private float _extraIntensity = 0.5f;

    [Header("시간")]
    [SerializeField] private float _fadeInTime = 0.4f;
    [SerializeField] private float _fadeOutTime = 0.6f;

    private Transform _target;
    private Light2D _extraLight;
    private float _defaultGlobalIntensity;
    private bool _isActive;
    private Sequence _fadeSequence;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (_globalLight != null)
        {
            _defaultGlobalIntensity = _globalLight.intensity;
        }

        SetIntensity(_focusLight, 0f);
    }

    private void OnDisable()
    {
        // 씬 전환·오브젝트 비활성화로 끊겨도 맵이 어두운 채로 남지 않게 즉시 되돌린다.
        _fadeSequence?.Kill();
        if (_isActive)
        {
            SetIntensity(_globalLight, _defaultGlobalIntensity);
            SetIntensity(_focusLight, 0f);
            SetIntensity(_extraLight, 0f);
            _isActive = false;
        }
    }

    /// <summary>
    /// 어둠 시작. target 주변을 밝히고, extraLight(보스 라이트 등)도 함께 약하게 켠다.
    /// </summary>
    public void Begin(Transform target, Light2D extraLight = null)
    {
        _target = target;
        _extraLight = extraLight;
        FollowTarget();

        if (_isActive == false && _globalLight != null)
        {
            // 다른 연출이 바꿔 둔 값일 수 있으니 시작할 때마다 원래 세기를 기억한다.
            _defaultGlobalIntensity = _globalLight.intensity;
        }

        _isActive = true;
        PlayFade(_darkIntensity, _focusIntensity, _extraIntensity, _fadeInTime);
    }

    /// <summary>
    /// 어둠 끝. 원래 밝기로 되돌린다.
    /// </summary>
    public void End()
    {
        if (_isActive == false)
            return;

        _isActive = false;
        PlayFade(_defaultGlobalIntensity, 0f, 0f, _fadeOutTime);
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (_target == null || _focusLight == null)
            return;

        Vector3 position = _target.position;
        position.z = _focusLight.transform.position.z;
        _focusLight.transform.position = position;
    }

    private void PlayFade(float globalIntensity, float focusIntensity, float extraIntensity, float duration)
    {
        _fadeSequence?.Kill();
        _fadeSequence = DOTween.Sequence();
        AppendFade(_globalLight, globalIntensity, duration);
        AppendFade(_focusLight, focusIntensity, duration);
        AppendFade(_extraLight, extraIntensity, duration);
    }

    private void AppendFade(Light2D light, float intensity, float duration)
    {
        if (light == null)
            return;

        _fadeSequence.Join(DOTween.To(() => light.intensity, value => light.intensity = value, intensity, duration));
    }

    private static void SetIntensity(Light2D light, float intensity)
    {
        if (light != null)
        {
            light.intensity = intensity;
        }
    }
}
