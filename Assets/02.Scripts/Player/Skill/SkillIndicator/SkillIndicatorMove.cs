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

    [Header("Point Movement")]
    [SerializeField] private float _pointMoveDuration = 0.5f;
    [SerializeField] private Ease _pointMoveEase = Ease.OutQuad;

    
    private SkillIndicatorOrbit _skillIndicatorOrbit;
    private bool _isPointFix = false;
    private Vector3 _velocity;

    private Tween _moveTween;

    private void Awake()
    {
        _skillIndicatorOrbit = GetComponent<SkillIndicatorOrbit>();
    }
    private void Update()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (_target == null||_isPointFix)
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
            _target.position + direction.normalized * _followDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _velocity,
            _smoothTime,
            _maxSpeed
        );
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
            });


    }

}
