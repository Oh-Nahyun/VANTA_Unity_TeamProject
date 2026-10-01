using DG.Tweening;
using UnityEngine;

public class SkillIndicatorAttack : MonoBehaviour
{
    [Header("공격 이팩트")]
    [SerializeField]private GameObject _hitEffect;
    [Header("Attack")]
    [SerializeField] private float _attackDuration = 0.25f;
    [SerializeField] private float _attackDamage = 10f;

    [Header("Circle Movement")]
    [SerializeField] private float _attackCurve = 0.4f;
    [SerializeField] private float _returnCurve = 0.4f;
    [SerializeField] private float _returnDuration = 0.35f;

    [Header("Idle Rotation")]
    [SerializeField] private float _idleMinAngle = 70f;
    [SerializeField] private float _idleMaxAngle = 110f;
    [SerializeField] private float _idleMinDuration = 0.8f;
    [SerializeField] private float _idleMaxDuration = 1.5f;

    private SkillIndicatorMove _skillIndicatorMove;
    private Animator _animator;

    private bool _isAttacking;
    public bool IsAttacking => _isAttacking;

    private Sequence _attackSequence;
    private Tween _idleRotationTween;

    private Vector3 _previousPosition;

    private void Awake()
    {
        _skillIndicatorMove =
            GetComponent<SkillIndicatorMove>();

        _animator =
            GetComponent<Animator>();

        SetDefaultAngle();
        StartIdleRotation();
    }

    private void SetDefaultAngle()
    {
        transform.rotation =
            Quaternion.Euler(0f, 0f, 90f);
    }

    private void StartIdleRotation()
    {
        if (_isAttacking)
            return;

        _idleRotationTween?.Kill();

        float targetAngle =
            Random.Range(
                _idleMinAngle,
                _idleMaxAngle
            );

        float duration =
            Random.Range(
                _idleMinDuration,
                _idleMaxDuration
            );

        _idleRotationTween =
            transform
                .DORotate(
                    new Vector3(
                        0f,
                        0f,
                        targetAngle
                    ),
                    duration
                )
                .SetEase(Ease.InOutSine)
                .OnComplete(StartIdleRotation);
    }

    private void StopIdleRotation()
    {
        _idleRotationTween?.Kill();
        _idleRotationTween = null;
    }

    private void UpdateRotationByMovement()
    {
        Vector3 movement =
            transform.position - _previousPosition;

        if (movement.sqrMagnitude > 0.000001f)
        {
            float angle =
                Mathf.Atan2(
                    movement.y,
                    movement.x
                ) * Mathf.Rad2Deg;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }

        _previousPosition =
            transform.position;
    }

    public void Attack()
    {
        if (_isAttacking)
            return;

        HitableObject target =
            FindAnyObjectByType<HitableObject>();

        if (target == null)
            return;

        StopIdleRotation();

        _isAttacking = true;

        _skillIndicatorMove.enabled = false;

        _attackSequence?.Kill();

        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            target.transform.position;

        Vector3 direction =
            (targetPosition - startPosition).normalized;

        Vector3 perpendicular =
            new Vector3(
                -direction.y,
                direction.x,
                0f
            );

        Vector3 attackPoint1 =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                0.33f
            )
            + perpendicular * _attackCurve;

        Vector3 attackPoint2 =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                0.66f
            )
            + perpendicular * _attackCurve;

        Vector3[] attackPath =
        {
            attackPoint1,
            attackPoint2,
            targetPosition
        };

        Vector3 returnPoint1 =
            Vector3.Lerp(
                targetPosition,
                startPosition,
                0.33f
            )
            - perpendicular * _returnCurve;

        Vector3 returnPoint2 =
            Vector3.Lerp(
                targetPosition,
                startPosition,
                0.66f
            )
            - perpendicular * _returnCurve;

        Vector3[] returnPath =
        {
            returnPoint1,
            returnPoint2,
            startPosition
        };

        _previousPosition =
            transform.position;

        _attackSequence =
            DOTween.Sequence();

        _attackSequence.Append(
            transform.DOPath(
                    attackPath,
                    _attackDuration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.InQuad)
                .OnUpdate(UpdateRotationByMovement)
        );

        _attackSequence.InsertCallback(
            _attackDuration * 0.55f,
            () =>
            {
                _animator.SetTrigger("Strike");
            }
        );

        _attackSequence.AppendCallback(() =>
        {
            if (target == null)
                return;

            target.ExecuteHitEvent(
                _attackDamage,
                DamageIndicatorType.Normal
            );
            Instantiate(_hitEffect, transform.position, Quaternion.identity);
        });

        _attackSequence.AppendCallback(() =>
        {
            _previousPosition =
                transform.position;
        });

        _attackSequence.Append(
            transform.DOPath(
                    returnPath,
                    _returnDuration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.OutQuad)
                .OnUpdate(UpdateRotationByMovement)
        );

        _attackSequence.OnComplete(() =>
        {
            _isAttacking = false;

            if (_skillIndicatorMove != null)
            {
                _skillIndicatorMove.enabled = true;
            }

            SetDefaultAngle();
            StartIdleRotation();
        });
    }

    public void PauseAttack()
    {
        _attackSequence?.Kill();

        _isAttacking = false;

        _animator.SetTrigger("Choice");

        if (_skillIndicatorMove != null)
        {
            _skillIndicatorMove.enabled = true;
        }

        SetDefaultAngle();
        StartIdleRotation();
    }

    private void OnDestroy()
    {
        _attackSequence?.Kill();
        _idleRotationTween?.Kill();
    }
}
