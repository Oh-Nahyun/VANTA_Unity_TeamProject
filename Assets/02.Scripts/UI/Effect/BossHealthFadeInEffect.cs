using System.Collections;
using UnityEngine;

public class BossHealthFadeInEffect : MonoBehaviour
{
    [Header("보스 체력 UI 등장 시간")]
    [SerializeField] private float _fadeInDuration = 0.6f;

    [Header("보스 체력 UI 사라지는 시간")]
    [SerializeField] private float _fadeOutDuration = 0.6f;

    /// <summary>
    /// 캔버스 그룹 컴포넌트
    /// </summary>
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// 보스 체력 UI 페이드 인 효과 코루틴
    /// </summary>
    private Coroutine _fadeInCoroutine;

    /// <summary>
    /// 보스 체력 UI 페이드 아웃 효과 코루틴
    /// </summary>
    private Coroutine _fadeOutCoroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        _canvasGroup.alpha = 0f;
    }

    public void PlayFadeIn()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
        }

        _fadeInCoroutine = StartCoroutine(FadeIn());
    }

    public void PlayFadeOut()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
        }

        _fadeOutCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;

        _canvasGroup.alpha = 0f;

        while (timer < _fadeInDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _fadeInDuration);

            _canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _fadeInCoroutine = null;
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        float startAlpha = _canvasGroup.alpha;

        while (timer < _fadeOutDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _fadeOutDuration);

            _canvasGroup.alpha = Mathf.SmoothStep(startAlpha, 0f, progress);

            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _fadeOutCoroutine = null;
    }
}
