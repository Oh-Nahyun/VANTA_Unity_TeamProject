using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 표시만 담당한다. 연출 중단과 전투 상태 변경은 ActingController가 맡는다.
[DisallowMultipleComponent]
public class ActingScreenFade : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.25f;
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.25f;
    private CanvasGroup _group;
    private GameObject _overlay;

    public IEnumerator FadeOut()
    {
        EnsureOverlay();
        _group.blocksRaycasts = true;
        yield return FadeTo(1f, _fadeOutDuration);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeTo(0f, _fadeInDuration);
        Clear();
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = _group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _group.alpha = target;
    }

    private void EnsureOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = new GameObject("ActingBlackout", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(GraphicRaycaster));
        Canvas canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        _group = _overlay.GetComponent<CanvasGroup>();
        _group.alpha = 0f;

        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_overlay.transform, false);
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Color.black;
    }

    public void Clear()
    {
        if (_group == null)
            return;

        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        if (_overlay != null)
            Destroy(_overlay);
    }
}
