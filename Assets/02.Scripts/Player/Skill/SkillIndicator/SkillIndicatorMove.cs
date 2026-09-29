using DG.Tweening;
using UnityEngine;

public class SkillIndicatorMove : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _target;

    [Header("Movement")]
    [SerializeField] private float _followDistance = 1f;
    [SerializeField] private float _smoothTime = 0.15f;
    [SerializeField] private float _maxSpeed = 10f;

    [Header("Movement Animation")]
    [SerializeField] private float _moveThreshold = 0.01f;

    [Header("Point Movement")]
    [SerializeField] private float _pointMoveDuration = 0.5f;
    [SerializeField] private Ease _pointMoveEase = Ease.OutQuad;

    private SkillIndicatorOrbit _skillIndicatorOrbit;
    private SkillIndicatorSkillLocReveal _skillIndicatorLocReveal;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private bool _isPointFix = false;
    private Vector3 _velocity;

    private Tween _moveTween;

    private Vector3 _previousPosition;

    private void Awake()
    {
        _skillIndicatorOrbit = GetComponent<SkillIndicatorOrbit>();
        _skillIndicatorLocReveal=GetComponent<SkillIndicatorSkillLocReveal>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _previousPosition = transform.position;
    }

    private void Update()
    {
        FollowTarget();
        UpdateAnimation();
    }

    private void FollowTarget()
    {
        if (_target == null || _isPointFix)
            return;

        Vector3 direction = transform.position - _target.position;
        float distance = direction.magnitude;

        if (distance < 0.01f)
        {
            direction = -_target.right;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 targetPosition =
            _target.position + direction * _followDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _velocity,
            _smoothTime,
            _maxSpeed
        );
    }

    private void UpdateAnimation()
    {
        Vector3 movement = transform.position - _previousPosition;

        bool isMoving = movement.sqrMagnitude >
                        _moveThreshold * _moveThreshold;

        _animator.SetBool("IsMove", isMoving);

        // 실제로 좌우 이동했을 때만 방향 변경
        if (Mathf.Abs(movement.x) > _moveThreshold)
        {
            // 기본 스프라이트가 오른쪽을 바라본다고 가정
            _spriteRenderer.flipX = movement.x < 0f;
        }

        _previousPosition = transform.position;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void MoveToPoint(Vector3 point)
    {
        _isPointFix = true;

        _moveTween?.Kill();

        _velocity = Vector3.zero;

        _moveTween = transform
            .DOMove(point, _pointMoveDuration)
            .SetEase(_pointMoveEase)
            .OnComplete(() =>
            {
                _skillIndicatorOrbit.Initialize();
                _skillIndicatorLocReveal.StartReveal();
            });
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}
