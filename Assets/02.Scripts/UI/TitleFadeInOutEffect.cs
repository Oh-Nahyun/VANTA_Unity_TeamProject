using System;
using System.Collections;
using UnityEngine;

public class TitleFadeInOutEffect : MonoBehaviour
{
    [Header("페이드용 캔버스 그룹")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Header("페이드 지속 시간")]
    [SerializeField] private float _fadeDuration = 0.5f;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void FadeOut()
    {
        SetActiveTrue();
        StartCoroutine(Fade(0f, 1f));
    }

    public void FadeIn()
    {
        SetActiveTrue();
        StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        _fadeCanvasGroup.alpha = startAlpha;

        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / _fadeDuration;
            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);

            yield return null;
        }

        _fadeCanvasGroup.alpha = endAlpha;
    }

    private void SetActiveTrue()
    {
        gameObject.SetActive(true);
    }
}
