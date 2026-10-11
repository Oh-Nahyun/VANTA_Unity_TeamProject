using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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

    [Header("점프 이벤트")]
    [SerializeField]
    private UnityEvent _jump = new UnityEvent();

    [Header("점프 끝날 때 이벤트")]
    [SerializeField]
    private UnityEvent _jumpEnd = new UnityEvent();

    private Sequence _jumpSequence;
    private Sequence _moveSequence;

    private Vector3 _startLocalPosition;
    private Vector3 _jumpStartLocalPosition;

    private bool _isJumping;
    private bool _stopRequested;
    private int _moveDirection;

    private Action _onLanded;

    private void OnEnable()
    {
        _stopRequested = false;
        _onLanded = null;

        _startLocalPosition = transform.localPosition;
        StartIdleMovement();
    }

    // 점프 중이면 착지까지 기다리고,
    // 점프 중이 아니면 바로 콜백 실행
    public void StopAfterLanding(Action onLanded)
    {
        _stopRequested = true;
        StopIdleMovement();

        // 여러 번 요청하면 가장 최근 요청으로 교체
        _onLanded = onLanded;

        if (!_isJumping)
            CompleteStop();
    }

    private void CompleteStop()
    {
        Action callback = _onLanded;
        _onLanded = null;

        callback?.Invoke();
    }

    public void Jump()
    {
        if (_isJumping || _stopRequested || !isActiveAndEnabled)
            return;

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

            _jumpEnd?.Invoke();

            if (!isActiveAndEnabled)
                return;

            if (_stopRequested)
            {
                CompleteStop();
                return;
            }

            StartIdleMovement();
        });

        _jump?.Invoke();
    }

    private void StartIdleMovement()
    {
        StopIdleMovement();

        if (_isJumping || _stopRequested || !isActiveAndEnabled)
            return;

        if (_moveDirection == 0)
            _moveDirection = Random.value < 0.5f ? -1 : 1;

        PlayNextMovement(false);
    }

    private void PlayNextMovement(bool waitBeforeMove = true)
    {
        if (_isJumping || _stopRequested || !isActiveAndEnabled)
            return;

        float distance = GetRandomRange(_moveDistanceRange, 0f);
        float duration = GetRandomRange(_moveDurationRange, 0.01f);

        float targetX =
            _startLocalPosition.x + distance * _moveDirection;

        _moveSequence = DOTween.Sequence();

        if (waitBeforeMove)
        {
            float wait = GetRandomRange(_waitDurationRange, 0f);
            _moveSequence.AppendInterval(wait);
        }

        _moveSequence.Append(
            transform.DOLocalMoveX(targetX, duration)
                .SetEase(Ease.InOutSine)
        );

        _moveSequence.OnComplete(() =>
        {
            _moveSequence = null;

            if (Random.value >= _keepDirectionChance)
                _moveDirection *= -1;

            PlayNextMovement();
        });
    }

    private static float GetRandomRange(Vector2 range, float minimum)
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
        _onLanded = null;
        _stopRequested = false;
    }

    private void OnDestroy()
    {
        _jumpSequence?.Kill();
        _moveSequence?.Kill();
    }
}
