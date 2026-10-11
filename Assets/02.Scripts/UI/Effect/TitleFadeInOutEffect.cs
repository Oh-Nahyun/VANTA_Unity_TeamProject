using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleFadeInOutEffect : MonoBehaviour
{
    [Header("페이드 효과 적용할 이미지")]
    [SerializeField] private Image _fadeImage;

    [Header("페이드 효과 지속 시간")]
    public float FadeDuration = 1f;

    /// <summary>
    /// 페이드 효과 적용할 이미지 머티리얼
    /// </summary>
    private Material _material;

    /// <summary>
    /// 페이드 진행도를 제어하는 셰이더 프로퍼티 ID
    /// </summary>
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private void Awake()
    {
        _material = _fadeImage.material;
        _material.SetFloat(Progress, 0f);

        ChangeSetActive(false);
    }

    public void FadeOut()
    {
        ChangeSetActive(true);
        StartCoroutine(FadeCoroutine(0f, 1f));
    }

    public void FadeIn()
    {
        ChangeSetActive(true);
        StartCoroutine(FadeCoroutine(1f, 0f));
    }

    private void ChangeSetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    private IEnumerator FadeCoroutine(float start, float end)
    {
        float timer = 0f;
        _material.SetFloat(Progress, start);

        while (timer < FadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / FadeDuration);
            float progress = Mathf.Lerp(start, end, t);

            _material.SetFloat(Progress, progress);

            yield return null;
        }

        _material.SetFloat(Progress, end);
    }
}
