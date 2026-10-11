using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class PetJump : MonoBehaviour
{
    [Header("점프 높이")]
    [SerializeField, Min(0f)]
    private float _jumpHeight = 1f;

    [Header("점프 전체 시간")]
    [SerializeField, Min(0.01f)]
    private float _jumpDuration = 0.5f;

    [Header("평소 이동 거리 범위 — 원래 위치 기준")]
    [SerializeField]
    private Vector2 _moveDistanceRange = new Vector2(0.2f, 0.6f);

    [Header("이동 시간 범위")]
    [SerializeField]
    private Vector2 _moveDurationRange = new Vector2(0.7f, 1.5f);

    [Header("이동 전 대기 시간 범위")]
    [SerializeField]
    private Vector2 _waitDurationRange = new Vector2(0.2f, 1f);

    [Header("현재 방향을 유지할 확률")]
    [SerializeField, Range(0f, 1f)]
    private float _keepDirectionChance = 0.25f;

    private Sequence _jumpSequence;
    private Sequence _moveSequence;

    private Vector3 _startLocalPosition;
    private Vector3 _jumpStartLocalPosition;

    [Header("점프이벤트")]
    [SerializeField]private UnityEvent _jump;
    private bool _isJumping;
    private int _moveDirection;

    private void Awake()
    {
  
    }

    private void OnEnable()
    {
        _startLocalPosition = transform.localPosition;
        StartIdleMovement();
    }

    public void Jump()
    {
        if (_isJumping || !isActiveAndEnabled)
            return;

        _jump.Invoke(); 
        StopIdleMovement();

        _isJumping = true;
        _jumpStartLocalPosition = transform.localPosition;

        float halfDuration =
            Mathf.Max(0.01f, _jumpDuration) * 0.5f;

        _jumpSequence = DOTween.Sequence();

        _jumpSequence.Append(
            transform.DOLocalMoveY(
                _jumpStartLocalPosition.y + _jumpHeight,
                halfDuration
            ).SetEase(Ease.OutQuad)
        );

        _jumpSequence.Append(
            transform.DOLocalMoveY(
                _jumpStartLocalPosition.y,
                halfDuration
            ).SetEase(Ease.InQuad)
        );

        _jumpSequence.OnComplete(() =>
        {
            _jumpSequence = null;
            _isJumping = false;

            StartIdleMovement();
        });
    }

    private void StartIdleMovement()
    {
        StopIdleMovement();

        if (_isJumping || !isActiveAndEnabled)
            return;

        if (_moveDirection == 0)
        {
            _moveDirection = Random.value < 0.5f ? -1 : 1;
        }

        // 시작할 때는 대기 없이 바로 이동
        PlayNextMovement(false);
    }

    private void PlayNextMovement(bool waitBeforeMove = true)
    {
        if (_isJumping || !isActiveAndEnabled)
            return;

        float distance = GetRandomRange(
            _moveDistanceRange,
            0f
        );

        float duration = GetRandomRange(
            _moveDurationRange,
            0.01f
        );

        float targetX =
            _startLocalPosition.x + distance * _moveDirection;

        _moveSequence = DOTween.Sequence();

        if (waitBeforeMove)
        {
            float wait = GetRandomRange(
                _waitDurationRange,
                0f
            );

            _moveSequence.AppendInterval(wait);
        }

        _moveSequence.Append(
            transform.DOLocalMoveX(
                targetX,
                duration
            ).SetEase(Ease.InOutSine)
        );

        _moveSequence.OnComplete(() =>
        {
            _moveSequence = null;

            // 다음 이동 방향 결정
            if (Random.value >= _keepDirectionChance)
            {
                _moveDirection *= -1;
            }

            // 거리, 이동 시간, 대기 시간을 매번 새로 선택
            PlayNextMovement();
        });
    }

    private static float GetRandomRange(
        Vector2 range,
        float minimum)
    {
        float min = Mathf.Max(
            minimum,
            Mathf.Min(range.x, range.y)
        );

        float max = Mathf.Max(
            min,
            Mathf.Max(range.x, range.y)
        );

        return Random.Range(min, max);
    }

    private void StopIdleMovement()
    {
        _moveSequence?.Kill();
        _moveSequence = null;
    }

    private void OnDisable()
    {
        StopIdleMovement();

        _jumpSequence?.Kill();
        _jumpSequence = null;

        if (_isJumping)
        {
            Vector3 position = transform.localPosition;
            position.y = _jumpStartLocalPosition.y;
            transform.localPosition = position;
        }

        _isJumping = false;
    }

    private void OnDestroy()
    {
        _jumpSequence?.Kill();
        _moveSequence?.Kill();
    }
}
