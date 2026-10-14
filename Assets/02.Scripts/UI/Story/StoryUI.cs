using System.Collections;
using UnityEngine;

public class StoryUI : MonoBehaviour
{
    [Header("게임 스토리 텍스트 배열")]
    [SerializeField] private GameObject[] _storyTexts;

    [Header("게임 스토리 텍스트 생성 간격")]
    [SerializeField, Min(0f)] private float _storySpeed = 2f;

    [Header("스토리 텍스트 생성 위치")]
    [SerializeField] private RectTransform _storyTextParent;

    /// <summary>
    /// 게임 스토리 텍스트 생성 코루틴
    /// </summary>
    private Coroutine _showTextCoroutine;

    private void OnEnable()
    {
        ShowText();
    }

    private void OnDisable()
    {
        if (_showTextCoroutine != null)
        {
            StopCoroutine(_showTextCoroutine);
            _showTextCoroutine = null;
        }
    }

    private void ShowText()
    {
        if (_showTextCoroutine != null)
        {
            StopCoroutine(_showTextCoroutine);
        }

        _showTextCoroutine = StartCoroutine(ShowTextCoroutine());
    }

    private IEnumerator ShowTextCoroutine()
    {
        RectTransform parent = _storyTextParent;

        if (parent == null)
        {
            parent = transform as RectTransform;
        }

        if (parent == null)
        {
            Debug.LogWarning("[StoryUI] UI 텍스트를 생성할 RectTransform이 없습니다.", this);

            _showTextCoroutine = null;
            yield break;
        }

        for (int i = 0; i < _storyTexts.Length; i++)
        {
            if (_storyTexts[i] != null)
            {
                GameObject storyText = Instantiate(_storyTexts[i], parent, false);

                if (storyText.transform is RectTransform rectTransform)
                {
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            float timer = 0f;

            while (timer < _storySpeed)
            {
                if (Time.timeScale > 0f)
                {
                    timer += Time.unscaledDeltaTime;
                }

                yield return null;
            }
        }

        _showTextCoroutine = null;
    }
}
