using UnityEngine;

public class BossHealthImpactVFX : MonoBehaviour
{
    [Header("연출 적용 대상")]
    [SerializeField] private RectTransform _target;

    [Header("피격 흔들림")]
    [SerializeField] private float _shakeDuration = 0.18f;

    [SerializeField] private float _shakeX = 12f;
    [SerializeField] private float _shakeY = 4f;

    [Header("착지 충격")]
    [SerializeField] private float _landingDuration = 0.32f;

    [SerializeField] private float _landingDrop = 14f;
    [SerializeField] private float _landingRebound = 5f;

    private Vector2 _origin;
    private float _shakeTimer;
    private float _landingTimer;

    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponent<RectTransform>();
        }

        _origin = _target.anchoredPosition;
    }

    private void LateUpdate()
    {
        Vector2 shakeOffset = Vector2.zero;
        Vector2 landingOffset = Vector2.zero;

        // 피격 흔들림
        if (_shakeTimer > 0f)
        {
            _shakeTimer = Mathf.Max(0f, _shakeTimer - Time.unscaledDeltaTime);

            float strength = _shakeTimer / _shakeDuration;
            shakeOffset = new Vector2(Random.Range(-_shakeX, _shakeX), Random.Range(-_shakeY, _shakeY)) * strength;
        }

        // 착지 충격 및 반동
        if (_landingTimer > 0f)
        {
            _landingTimer = Mathf.Max(0f, _landingTimer - Time.unscaledDeltaTime);

            float t = 1f - _landingTimer / _landingDuration;
            float y;

            if (t < 0.2f)
            {
                y = Mathf.Lerp(0f, -_landingDrop, t / 0.2f);
            }
            else if (t < 0.5f)
            {
                y = Mathf.Lerp(-_landingDrop, _landingRebound, (t - 0.2f) / 0.3f);
            }
            else if (t < 0.75f)
            {
                y = Mathf.Lerp(_landingRebound, -2f, (t - 0.5f) / 0.25f);
            }
            else
            {
                y = Mathf.Lerp(-2f, 0f, (t - 0.75f) / 0.25f);
            }

            landingOffset = new Vector2(0f, y);
        }

        _target.anchoredPosition = _origin + shakeOffset + landingOffset;
    }

    public void PlayDamageShake()
    {
        _shakeTimer = _shakeDuration;
    }

    public void PlayLandingImpact()
    {
        _landingTimer = _landingDuration;
    }

    private void OnDisable()
    {
        if (_target != null)
        {
            _target.anchoredPosition = _origin;
        }

        _shakeTimer = 0f;
        _landingTimer = 0f;
    }
}
