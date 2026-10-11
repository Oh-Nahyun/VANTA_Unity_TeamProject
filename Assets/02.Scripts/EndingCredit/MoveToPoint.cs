using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class MoveToPoint : MonoBehaviour
{
    [Header("도착 위치")]
    [SerializeField] private Transform _targetPoint;

    [Header("이동 시간")]
    [SerializeField, Min(0.01f)]
    private float _moveDuration = 2f;

    [Header("이동 곡선")]
    [SerializeField] private Ease _moveEase = Ease.Linear;

    [Header("도착시 호출")]
    [SerializeField]
    private UnityEvent _tweenFinished = new UnityEvent();

    private Tween _moveTween;
    private Vector3 _originPosition;
    private PetJump _petJump;
    private int _moveRequestId;

    private void Awake()
    {
        _originPosition = transform.position;
        _petJump = GetComponent<PetJump>();
    }

    private void OnEnable()
    {
        if (_targetPoint == null)
            return;

        MoveTo(_targetPoint.position, true);
    }

    public void MoveToOrigin()
    {
        MoveTo(_originPosition, false);
    }

    private void MoveTo(
        Vector3 destination,
        bool enablePetJumpOnComplete)
    {
        _moveTween?.Kill();
        _moveTween = null;

        int requestId = ++_moveRequestId;

        // 점프 중이면 착지한 뒤 실행
        if (_petJump != null && _petJump.isActiveAndEnabled)
        {
            _petJump.StopAfterLanding(() =>
            {
                if (requestId != _moveRequestId ||
                    !isActiveAndEnabled)
                {
                    return;
                }

                BeginMovement(destination, enablePetJumpOnComplete);
            });
        }
        else
        {
            BeginMovement(destination, enablePetJumpOnComplete);
        }
    }

    private void BeginMovement(
        Vector3 destination,
        bool enablePetJumpOnComplete)
    {
        // 이 시점에는 이미 착지한 상태
        if (_petJump != null)
            _petJump.enabled = false;

        _moveTween = transform
            .DOMove(destination, _moveDuration)
            .SetEase(_moveEase)
            .OnComplete(() =>
            {
                _moveTween = null;

                if (_petJump != null && enablePetJumpOnComplete)
                    _petJump.enabled = true;

                _tweenFinished?.Invoke();
            });
    }

    private void OnDisable()
    {
        // 착지를 기다리던 이전 이동 요청도 무효화
        _moveRequestId++;

        _moveTween?.Kill();
        _moveTween = null;
    }
}
