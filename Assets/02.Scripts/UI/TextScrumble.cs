using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextScrumble : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    private TMP_Text _text;

    [Header("스크램블 설정")]
    [SerializeField, Min(0.01f)]
    private float _duration = 1f;

    [SerializeField]
    private Ease _ease = Ease.Linear;

    [Header("랜덤 문자 변경 간격")]
    [SerializeField, Min(0.01f)]
    private float _scrambleInterval = 0.03f;

    [Header("스크램블 문자")]
    [SerializeField]
    private string _scrambleCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private string _originalText;
    private Tween _scrambleTween;

    private float _progress;
    private float _scrambleTimer;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();

        if (_text == null)
            return;

        _originalText = _text.text;
    }

    private void OnEnable()
    {
        PlayScramble();
    }

    public void PlayScramble()
    {
        if (_text == null)
            return;

        _scrambleTween?.Kill();

        _progress = 0f;
        _scrambleTimer = 0f;

        _text.text = "";

        _scrambleTween = DOTween.To(
                () => _progress,
                value =>
                {
                    _progress = value;

                    _scrambleTimer += Time.unscaledDeltaTime;

                    if (_scrambleTimer >= _scrambleInterval)
                    {
                        _scrambleTimer = 0f;
                        UpdateScrambleText();
                    }
                },
                1f,
                _duration
            )
            .SetEase(_ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _text.text = _originalText;
                _scrambleTween = null;
            });
    }

    private void UpdateScrambleText()
    {
        if (string.IsNullOrEmpty(_originalText))
            return;

        int length = _originalText.Length;

        int fixedCount =
            Mathf.FloorToInt(length * _progress);

        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            char originalChar = _originalText[i];

            // 공백은 그대로 유지
            if (char.IsWhiteSpace(originalChar))
            {
                result[i] = originalChar;
                continue;
            }

            // 확정된 문자
            if (i < fixedCount)
            {
                result[i] = originalChar;
            }
            else
            {
                // 아직 확정되지 않은 문자는 랜덤
                int randomIndex = Random.Range(
                    0,
                    _scrambleCharacters.Length
                );

                result[i] =
                    _scrambleCharacters[randomIndex];
            }
        }

        _text.text = new string(result);
    }

    private void OnDisable()
    {
        _scrambleTween?.Kill();
        _scrambleTween = null;
    }

    private void OnDestroy()
    {
        _scrambleTween?.Kill();
    }
}
