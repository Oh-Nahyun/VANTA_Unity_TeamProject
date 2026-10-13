using DG.Tweening;
using TMPro;
using UnityEngine;

public class SkillCountText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField]
    private TextMeshPro _countText;

    [Header("Position Offset")]
    [SerializeField]
    private Vector3 _positionOffset = new Vector3(0f, 3f, 0f);

    [Header("Font Size")]
    [SerializeField]
    private float _startFontSize = 15f;

    [SerializeField]
    private float _endFontSize = 5f;

    [Header("Animation")]
    [SerializeField]
    private float _appearDuration = 0.15f;

    [SerializeField]
    private float _disappearDuration = 0.25f;

    [SerializeField]
    private float _appearScale = 1.3f;

    [SerializeField]
    private Ease _appearEase = Ease.OutBack;

    [SerializeField]
    private Ease _disappearEase = Ease.InBack;

    [Header("Bounce Animation")]
    [SerializeField]
    private float _bounceHeight = 0.3f;

    [SerializeField]
    private float _bounceDuration = 0.25f;

    [SerializeField]
    private Ease _bounceEase = Ease.OutBack;

    private Tween _countTween;
    private Sequence _animationSequence;

    private int _remainingTime;
    private int _totalCount;
    private Transform _target;
    private Vector3 _originalScale;
    private Vector3 _originalLocalPosition;
    private Vector3 _targetPositionCorrection;

    private Color _originalColor;

    private void Awake()
    {
        if (_countText == null)
            _countText = GetComponentInChildren<TextMeshPro>();

        Renderer renderer =
            _countText.GetComponent<Renderer>();

        renderer.sortingLayerName = "UI";
        renderer.sortingOrder = 100;

        _originalScale = _countText.transform.localScale;
        _originalLocalPosition = _countText.transform.localPosition;
        _originalColor = _countText.color;

        _countText.text = "";
    }
    private void LateUpdate()
    {
        if (_target == null)
            return;

        transform.position =
            _target.position +
            _targetPositionCorrection +
            _positionOffset;
    }

    public void StartCount(GameObject target, float duration)
    {
        StopCount();

        if (target == null)
            return;

        _target = target.transform;

        // Boss1Life가 있다면 타겟 위치를 먼저 보정
        _targetPositionCorrection =
            target.TryGetComponent<Boss1Life>(out _)
                ? Vector3.up
                : Vector3.zero;

        // 보정된 위치에 Position Offset 적용
        transform.position =
            _target.position +
            _targetPositionCorrection +
            _positionOffset;

        _remainingTime = Mathf.CeilToInt(duration);
        _totalCount = _remainingTime;

        if (_remainingTime <= 0)
            return;

        gameObject.SetActive(true);

        PlayCountAnimation();

        _countTween = DOVirtual.DelayedCall(
                1f,
                CountDown
            )
            .SetLoops(_remainingTime)
            .SetUpdate(true);
    }

    private void CountDown()
    {
        _remainingTime--;

        if (_remainingTime <= 0)
        {
            StopCount();
            return;
        }

        PlayCountAnimation();
    }

    private void PlayCountAnimation()
    {
        _animationSequence?.Kill();
        _animationSequence = null;

        // 카운트다운 진행 비율
        float progress = _totalCount <= 1
            ? 0f
            : (_totalCount - _remainingTime) /
              (float)(_totalCount - 1);

        // 카운트다운이 진행될수록 폰트 크기 감소
        float fontSize = Mathf.Lerp(
            _startFontSize,
            _endFontSize,
            progress
        );

        _countText.fontSize = fontSize;

        _countText.text =
            _remainingTime.ToString();

        // 등장 초기 크기
        _countText.transform.localScale =
            _originalScale * _appearScale;

        // 등장 초기 위치
        _countText.transform.localPosition =
            _originalLocalPosition +
            Vector3.down * _bounceHeight;

        // 등장 초기 투명도
        Color color = _originalColor;
        color.a = 0f;

        _countText.color = color;

        // 애니메이션 시퀀스
        _animationSequence = DOTween.Sequence()
            .SetUpdate(true);

        // 등장 - 크기
        _animationSequence.Append(
            _countText.transform.DOScale(
                _originalScale,
                _appearDuration
            )
            .SetEase(_appearEase)
        );

        // 등장 - 투명도
        _animationSequence.Join(
            _countText.DOFade(
                _originalColor.a,
                _appearDuration
            )
        );

        // 등장 - 아래에서 위로 튀어 오르기
        _animationSequence.Join(
            _countText.transform.DOLocalMoveY(
                _originalLocalPosition.y,
                _bounceDuration
            )
            .SetEase(_bounceEase)
        );

        // 등장 애니메이션 전체 길이
        float appearTotalDuration = Mathf.Max(
            _appearDuration,
            _bounceDuration
        );

        // 유지 시간
        float holdDuration = Mathf.Max(
            0f,
            1f - appearTotalDuration - _disappearDuration
        );

        _animationSequence.AppendInterval(
            holdDuration
        );

        // 퇴장 - 크기 감소
        _animationSequence.Append(
            _countText.transform.DOScale(
                _originalScale * 0.7f,
                _disappearDuration
            )
            .SetEase(_disappearEase)
        );

        // 퇴장 - 투명도 감소
        _animationSequence.Join(
            _countText.DOFade(
                0f,
                _disappearDuration
            )
        );

        // 퇴장 - 위로 살짝 떠오르기
        _animationSequence.Join(
            _countText.transform.DOLocalMoveY(
                _originalLocalPosition.y +
                _bounceHeight * 0.5f,
                _disappearDuration
            )
            .SetEase(Ease.OutQuad)
        );
    }

    public void StopCount()
    {
        _countTween?.Kill();
        _countTween = null;

        _animationSequence?.Kill();
        _animationSequence = null;

        // 타겟 추적 종료
        _target = null;

        if (_countText == null)
            return;

        _countText.text = "";

        _countText.transform.localScale =
            _originalScale;

        _countText.transform.localPosition =
            _originalLocalPosition;

        _countText.color = _originalColor;
    }

    private void OnDisable()
    {
        StopCount();
    }

    private void OnDestroy()
    {
        _countTween?.Kill();
        _animationSequence?.Kill();
    }
}
