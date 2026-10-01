using DG.Tweening;
using UnityEngine;

public class SkillIndicatorMove : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _target;
    [SerializeField] private SpriteRenderer _targetSpriteRenderer;

    [Header("Pet Formation")]
    [SerializeField] private float _backDistance = 1.6f;
    [SerializeField] private float _sideDistance = 1.0f;
    [SerializeField] private float _verticalSpacing = 0.35f;
    [SerializeField] private float _formationHeight = 1.2f;
    [SerializeField] private float _formationScale = 0.8f;
    [Header("Movement")]
    [SerializeField] private float _smoothTime = 0.2f;
    [SerializeField] private float _maxSpeed = 10f;

    [Header("Floating")]
    [SerializeField] private float _floatAmount = 0.08f;
    [SerializeField] private float _floatSpeed = 2f;

    [Header("Movement Animation")]
    [SerializeField] private float _moveThreshold = 0.01f;

    [Header("Point Movement")]
    [SerializeField] private float _pointMoveDuration = 0.5f;
    [SerializeField] private Ease _pointMoveEase = Ease.OutQuad;

    private SkillIndicatorOrbit _skillIndicatorOrbit;
    private SkillIndicatorSkillLocReveal _skillIndicatorLocReveal;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private bool _isPointFix;

    private Vector3 _velocity;
    private Vector3 _previousPosition;

    private Tween _moveTween;

    private int _petNumber;
    private float _floatTimeOffset;

    private void Awake()
    {
        _skillIndicatorOrbit =
            GetComponent<SkillIndicatorOrbit>();

        _skillIndicatorLocReveal =
            GetComponent<SkillIndicatorSkillLocReveal>();

        _animator =
            GetComponent<Animator>();

        _spriteRenderer =
            GetComponent<SpriteRenderer>();

        _previousPosition =
            transform.position;
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

        Vector3 formationOffset =
            GetFormationOffset(_petNumber);

        bool playerLookingLeft =
            _targetSpriteRenderer != null &&
            _targetSpriteRenderer.flipX;

        if (playerLookingLeft)
        {
            formationOffset.x =
                Mathf.Abs(formationOffset.x);
        }
        else
        {
            formationOffset.x =
                -Mathf.Abs(formationOffset.x);
        }

        float floatingY =
            Mathf.Sin(
                (Time.time + _floatTimeOffset) *
                _floatSpeed
            ) * _floatAmount;

        Vector3 targetPosition =
            _target.position
            + formationOffset
            + Vector3.up * floatingY;

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                _smoothTime,
                _maxSpeed
            );
    }

    private Vector3 GetFormationOffset(int petNumber)
    {
        Vector3 offset;

        switch (petNumber)
        {
            case 0:
                offset = new Vector3(
                    0f,
                    0.8f,
                    0f
                );
                break;

            case 1:
                offset = new Vector3(
                    -0.3f,
                    0.8f,
                    0f
                );
                break;

            case 2:
                offset = new Vector3(
                    -0.8f,
                    0.8f,
                    0f
                );
                break;

            case 3:
                offset = new Vector3(
                    -0.8f,
                    0.3f,
                    0f
                );
                break;

            case 4:
                offset = new Vector3(
                    -0.8f,
                    -0.2f,
                    0f
                );
                break;

            default:
                offset = Vector3.zero;
                break;
        }

        return offset * _formationScale;
    }

    private void UpdateAnimation()
    {
        Vector3 movement =
            transform.position -
            _previousPosition;

        bool isMoving =
            movement.sqrMagnitude >
            _moveThreshold * _moveThreshold;

        _animator.SetBool(
            "IsMove",
            isMoving
        );

        if (Mathf.Abs(movement.x) >
            _moveThreshold)
        {
            _spriteRenderer.flipX =
                movement.x < 0f;
        }

        _previousPosition =
            transform.position;
    }

    public void InitSetting(
        GameObject target,
        int petNumber)
    {
        _target = target.transform;
        _petNumber = petNumber;

        // 일단 자동 탐색
        if (_targetSpriteRenderer == null)
        {
            _targetSpriteRenderer =
                target.GetComponentInParent<SpriteRenderer>();
        }

        _floatTimeOffset =
            petNumber * 0.7f;

        _animator.SetTrigger("Spawn");

        _skillIndicatorLocReveal.SetPetNumber(
            petNumber
        );
    }

    public void MoveToPoint(Vector3 point)
    {
        _isPointFix = true;

        _moveTween?.Kill();
        _velocity = Vector3.zero;

        _moveTween =
            transform
                .DOMove(
                    point,
                    _pointMoveDuration
                )
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
