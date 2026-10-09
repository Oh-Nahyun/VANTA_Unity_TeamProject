using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Credit : MonoBehaviour
{
    [Header("표시할 텍스트")]
    [SerializeField, TextArea(3, 10)]
    private string _content;

    [Header("글자 등장 간격")]
    [SerializeField, Min(0.01f)]
    private float _typingInterval = 0.1f;

    [Header("타이핑 완료 후 유지 시간")]
    [SerializeField, Min(0.01f)]
    private float _destroyDelay = 2f;

    private TMP_Text _text;
    private Sequence _typingSequence;
    private Action _onDestroyed;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();

        if (_text == null)
        {
            _text = GetComponentInChildren<TMP_Text>(true);
        }

        if (_text != null)
        {
            _text.maxVisibleCharacters = 0;
        }
    }

    public void Initialize(Action onDestroyed)
    {
        _onDestroyed = onDestroyed;
    }

    private void Start()
    {
        if (_text == null)
        {
            Debug.LogWarning(
                "[Credit] TextMeshPro가 없습니다.",
                this
            );

            Destroy(gameObject);
            return;
        }

        _text.text = _content;
        _text.maxVisibleCharacters = 0;
        _text.ForceMeshUpdate();

        int characterCount = _text.textInfo.characterCount;

        _typingSequence = DOTween.Sequence();

        for (int i = 1; i <= characterCount; i++)
        {
            int visibleCount = i;

            _typingSequence.AppendInterval(_typingInterval);

            _typingSequence.AppendCallback(() =>
            {
                _text.maxVisibleCharacters = visibleCount;
            });
        }

        // 마지막 글자까지 표시한 상태로 유지
        _typingSequence.AppendInterval(_destroyDelay);

        _typingSequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        _typingSequence?.Kill();
        _typingSequence = null;

        Action callback = _onDestroyed;
        _onDestroyed = null;
        callback?.Invoke();
    }
}
