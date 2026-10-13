using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FireWorkMove : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    private RectTransform _rectTransform;

    [Header("크기 랜덤 범위")]
    [SerializeField]
    private Vector2 _sizeRange =
        new Vector2(0.8f, 1.2f);

    [Header("폭죽 시작 X 범위")]
    [SerializeField]
    private Vector2 _startXRange =
        new Vector2(-500f, 500f);

    [Header("폭죽 시작 Y 범위")]
    [SerializeField]
    private Vector2 _startYRange =
        new Vector2(-800f, -650f);

    [Header("폭발 위치 X 범위")]
    [SerializeField]
    private Vector2 _targetXRange =
        new Vector2(-500f, 500f);

    [Header("폭발 위치 Y 범위")]
    [SerializeField]
    private Vector2 _targetYRange =
        new Vector2(100f, 400f);

    [Header("상승 시간 범위")]
    [SerializeField]
    private Vector2 _moveDurationRange =
        new Vector2(0.8f, 1.3f);

    private Image[] _childImages;
    private Tween _moveTween;

    private void Awake()
    {
        if (_rectTransform == null)
        {
            _rectTransform =
                transform as RectTransform;
        }

        _childImages =
            GetComponentsInChildren<Image>(true);
    }

    private void OnEnable()
    {
        InitializeFireWork();
    }

    private void InitializeFireWork()
    {
        _moveTween?.Kill();

        SetRandomSize();
        SetRandomColor();
        SetStartPosition();
        StartMove();
    }

    private void SetRandomSize()
    {
        float randomScale =
            Random.Range(
                _sizeRange.x,
                _sizeRange.y
            );

        _rectTransform.localScale =
            Vector3.one * randomScale;
    }

    private void SetRandomColor()
    {
        Color randomColor =
            Random.ColorHSV(
                0f, 1f,
                0.7f, 1f,
                0.8f, 1f,
                1f, 1f
            );

        foreach (Image image in _childImages)
        {
            if (image == null)
                continue;

            if (image.gameObject == gameObject)
                continue;

            image.color = randomColor;
        }
    }

    private void SetStartPosition()
    {
        float startX =
            Random.Range(
                _startXRange.x,
                _startXRange.y
            );

        float startY =
            Random.Range(
                _startYRange.x,
                _startYRange.y
            );

        _rectTransform.anchoredPosition =
            new Vector2(
                startX,
                startY
            );
    }

    private void StartMove()
    {
        float targetX =
            Random.Range(
                _targetXRange.x,
                _targetXRange.y
            );

        float targetY =
            Random.Range(
                _targetYRange.x,
                _targetYRange.y
            );

        float duration =
            Random.Range(
                _moveDurationRange.x,
                _moveDurationRange.y
            );

        Vector2 targetPosition =
            new Vector2(
                targetX,
                targetY
            );

        // 이동 방향에 따라 이미지 반전
        SetFlipDirection(targetPosition);

        _moveTween =
            _rectTransform
                .DOAnchorPos(
                    targetPosition,
                    duration
                )
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _moveTween = null;
                    OnReachedTarget();
                });
    }

    private void SetFlipDirection(Vector2 targetPosition)
    {
        float directionX =
            targetPosition.x -
            _rectTransform.anchoredPosition.x;

        bool flipX = directionX < 0f;

        foreach (Image image in _childImages)
        {
            if (image == null)
                continue;

            RectTransform rect = image.rectTransform;

            Vector3 scale = rect.localScale;

            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);

            rect.localScale = scale;
        }
    }

    private void OnReachedTarget()
    {
        // 여기서 폭발 애니메이션 실행
    }

    public void FireWorkEnd()
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }

    public void MoveStop()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }
}
