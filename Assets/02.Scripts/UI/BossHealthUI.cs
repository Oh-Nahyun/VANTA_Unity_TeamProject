using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("보스 현재 체력 슬라이더")]
    [SerializeField] private Slider _currentFillSlider;

    [Header("보스 딜레이 체력 슬라이더")]
    [SerializeField] private Slider _delayedFillSlider;

    [Header("보스 딜레이 체력 감소 전 대기 시간")]
    [SerializeField] private float _delayedDuration = 0.5f;

    [Header("보스 딜레이 체력 감소 속도")]
    [SerializeField] private float _delayedDecreaseSpeed = 0.8f;

    [Header("보스 체력 UI 왼쪽 이미지")]
    [SerializeField] private RectTransform _leftImage;

    [Header("보스 체력 UI 오른쪽 이미지")]
    [SerializeField] private RectTransform _rightImage;

    [Header("보스 체력 좌우 UI 이동 정도")]
    [SerializeField] private float _moveAmount = 10f;

    [Header("보스 체력 UI 피격 연출 시간")]
    [SerializeField] private float _hitEffectDuration = 0.08f;

    [Header("보스 체력 UI 등장 연출")]
    [SerializeField] private BossHealthFadeInEffect _fadeInEffect;

    [Header("보스 체력 UI 충격 연출")]
    [SerializeField] private BossHealthImpactEffect _impactEffect;

    [Header("보스 체력 UI가 나타날 때 효과음")]
    [SerializeField] private AudioClip _appearSound;

    [Header("보스 체력 UI 피격 효과음")]
    [SerializeField] private AudioClip _hitSound;

    [Header("보스 체력 UI가 사라질 때 효과음")]
    [SerializeField] private AudioClip _disappearSound;

    /// <summary>
    /// 보스 게임 오브젝트
    /// </summary>
    private GameObject _boss;

    /// <summary>
    /// 보스1 체력 클래스
    /// </summary>
    private Boss1Life _boss1;

    /// <summary>
    /// 보스2 체력 클래스
    /// </summary>
    private Boss2 _boss2;

    /// <summary>
    /// 보스 체력 비율
    /// </summary>
    private float _healthRatio;

    /// <summary>
    /// 이전 프레임의 보스 체력 비율
    /// </summary>
    private float _previousHealthRatio;

    /// <summary>
    /// 보스 체력 UI 왼쪽 이미지 원래 위치
    /// </summary>
    private Vector2 _leftOriginalPosition;

    /// <summary>
    /// 보스 체력 UI 오른쪽 이미지 원래 위치
    /// </summary>
    private Vector2 _rightOriginalPosition;

    /// <summary>
    /// 보스 딜레이 체력 감소 코루틴
    /// </summary>
    private Coroutine _delayedHpCoroutine;

    /// <summary>
    /// 보스 피격 연출 코루틴
    /// </summary>
    private Coroutine _hitEffectCoroutine;

    /// <summary>
    /// 보스 체력 UI 페이드 아웃 시작 여부 확인용 변수
    /// </summary>
    private bool _isFadeOutStarted;

    private void OnEnable()
    {
        SoundManager.Instance.PlayEffectSound(_appearSound);
    }

    private void Start()
    {
        _healthRatio = 1f;
        _previousHealthRatio = _healthRatio;
        _currentFillSlider.value = _healthRatio;
        _delayedFillSlider.value = _healthRatio;
        _leftOriginalPosition = _leftImage.anchoredPosition;
        _rightOriginalPosition = _rightImage.anchoredPosition;

        if (_fadeInEffect != null)
        {
            _fadeInEffect.PlayFadeIn();
        }
    }

    private void Update()
    {
        if (_boss == null)
        {
            _boss = GameObject.FindGameObjectWithTag("Boss");

            if (_boss == null) return;

            _boss1 = _boss.GetComponent<Boss1Life>();
            _boss2 = _boss.GetComponent<Boss2>();
        }

        if (_boss1 == null && _boss2 == null) return;

        if (_boss1 != null)
        {
            _healthRatio = _boss1.HealthRatio;
        }
        else if (_boss2 != null)
        {
            _healthRatio = _boss2.HealthRatio;
        }

        if (_healthRatio != _previousHealthRatio)
        {
            UpdateHp(_healthRatio);
        }
    }

    public void PlayFadeOut()
    {
        if (_isFadeOutStarted) return;

        _isFadeOutStarted = true;
        SoundManager.Instance.PlayEffectSound(_disappearSound);

        if (_fadeInEffect != null)
        {
            _fadeInEffect.PlayFadeOut();
        }
    }

    private void UpdateHp(float healthRatio)
    {
        if (healthRatio < _previousHealthRatio)
        {
            _currentFillSlider.value = healthRatio;

            if (_delayedHpCoroutine != null)
            {
                StopCoroutine(_delayedHpCoroutine);
                _delayedHpCoroutine = null;
            }

            _delayedHpCoroutine = StartCoroutine(DecreaseDelayedHp(healthRatio));

            if (_hitEffectCoroutine != null)
            {
                StopCoroutine(_hitEffectCoroutine);
                _hitEffectCoroutine = null;
            }

            _hitEffectCoroutine = StartCoroutine(PlayHitEffect());

            SoundManager.Instance.PlayEffectSound(_hitSound);

            if (_impactEffect != null)
            {
                _impactEffect.PlayDamageShake();
            }
        }
        else
        {
            if (_delayedHpCoroutine != null)
            {
                StopCoroutine(_delayedHpCoroutine);
                _delayedHpCoroutine = null;
            }

            _currentFillSlider.value = healthRatio;
            _delayedFillSlider.value = healthRatio;
        }

        _previousHealthRatio = healthRatio;
    }

    private IEnumerator DecreaseDelayedHp(float healthRatio)
    {
        yield return new WaitForSeconds(_delayedDuration);

        while (_delayedFillSlider.value > healthRatio)
        {
            _delayedFillSlider.value =
                Mathf.MoveTowards(_delayedFillSlider.value, healthRatio,
                    Time.deltaTime * _delayedDecreaseSpeed);

            yield return null;
        }

        _delayedHpCoroutine = null;
    }

    private IEnumerator PlayHitEffect()
    {
        _leftImage.anchoredPosition = _leftOriginalPosition + Vector2.left * _moveAmount;
        _rightImage.anchoredPosition = _rightOriginalPosition + Vector2.right * _moveAmount;

        yield return new WaitForSeconds(_hitEffectDuration);

        _leftImage.anchoredPosition = _leftOriginalPosition;
        _rightImage.anchoredPosition = _rightOriginalPosition;

        _hitEffectCoroutine = null;
    }

    public void PlayLandingImpact()
    {
        if (_impactEffect != null)
        {
            _impactEffect.PlayLandingImpact();
        }
    }
}
