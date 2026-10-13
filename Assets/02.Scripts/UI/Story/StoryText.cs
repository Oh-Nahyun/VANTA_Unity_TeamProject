using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class StoryText : MonoBehaviour
{
    [Header("표시할 텍스트")]
    [SerializeField, TextArea(3, 10)] private string _content;

    [Header("글자 등장 간격")]
    [SerializeField, Min(0.01f)] private float _typingInterval = 0.1f;

    [Header("타이핑 완료 후 유지 시간")]
    [SerializeField, Min(0.01f)] private float _destroyDelay = 2f;

    [Header("오디오 클립")]
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private AudioClip _typingSound;

    [SerializeField, Range(0f, 1f)] private float _typingVolume = 1f;

    [Header("타이핑 Pitch 랜덤")]
    [SerializeField] private Vector2 _pitchRange = new Vector2(0.9f, 1.1f);

    private TextMeshProUGUI _text;
    private Sequence _typingSequence;
    private Action _onDestroyed;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();

        if (_text == null)
        {
            _text = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (_text != null)
        {
            _text.maxVisibleCharacters = 0;
        }

        // 직접 할당하지 않았다면 AudioSource 추가
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
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
                "[Intro] TextMeshProUGUI가 없습니다.",
                this
            );

            Destroy(gameObject);
            return;
        }

        _text.text = _content;
        _text.maxVisibleCharacters = 0;
        _text.ForceMeshUpdate();

        int characterCount = _text.textInfo.characterCount;

        _typingSequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 1; i <= characterCount; i++)
        {
            int visibleCount = i;

            _typingSequence.AppendInterval(_typingInterval);

            _typingSequence.AppendCallback(() =>
            {
                _text.maxVisibleCharacters = visibleCount;

                int characterIndex = visibleCount - 1;

                if (characterIndex < _text.textInfo.characterCount)
                {
                    char character =
                        _text.textInfo.characterInfo[
                            characterIndex
                        ].character;

                    // 공백, 줄바꿈, 탭에서는 소리 재생 X
                    if (!char.IsWhiteSpace(character)
                        && _typingSound != null)
                    {
                        _audioSource.pitch =
                            UnityEngine.Random.Range(
                                _pitchRange.x,
                                _pitchRange.y
                            );

                        _audioSource.PlayOneShot(
                            _typingSound,
                            _typingVolume
                        );
                    }
                }
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
