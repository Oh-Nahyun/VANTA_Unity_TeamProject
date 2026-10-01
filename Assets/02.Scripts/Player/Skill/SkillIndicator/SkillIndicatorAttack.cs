using DG.Tweening;
using UnityEngine;

public class SkillIndicatorAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float _attackDuration = 0.25f;
    [SerializeField] private float _attackDamage = 10f;

    [Header("Circle Movement")]
    [SerializeField] private float _attackCurve = 0.4f;
    [SerializeField] private float _returnCurve = 0.4f;
    [SerializeField] private float _returnDuration = 0.35f;

    private SkillIndicatorMove _skillIndicatorMove;
    private ObjectRotator _objectRotator;
    private Animator _animator;

    private bool _isAttacking;
    public bool IsAttacking => _isAttacking;

    private Sequence _attackSequence;

    private void Awake()
    {
        _skillIndicatorMove =
            GetComponent<SkillIndicatorMove>();

        _animator =
            GetComponent<Animator>();
        _objectRotator = GetComponent<ObjectRotator>();
        SetDefaultAngle();
    }

    private void SetDefaultAngle()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, 90f);

    }
    public void Attack()
    {
        if (_isAttacking)
            return;

        HitableObject target =
            FindAnyObjectByType<HitableObject>();

        if (target == null)
            return;

        _objectRotator.enabled = true;
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

        // =========================
        // 공격 원호
        // =========================

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

        // =========================
        // 복귀 원호
        // 반대 방향으로 휘어짐
        // =========================

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

        _attackSequence =
            DOTween.Sequence();

        // =========================
        // 1. 원호를 그리며 공격
        // =========================

        _attackSequence.Append(
            transform.DOPath(
                    attackPath,
                    _attackDuration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.InQuad)
        );

        // =========================
        // 2. 공격 애니메이션
        // =========================

        _attackSequence.InsertCallback(
            _attackDuration * 0.55f,
            () =>
            {
                _animator.SetTrigger("Strike");
            }
        );

        // =========================
        // 3. 타격
        // =========================

        _attackSequence.AppendCallback(() =>
        {
            if (target == null)
                return;

            target.ExecuteHitEvent(
                _attackDamage,
                DamageIndicatorType.Normal
            );
        });

        // =========================
        // 4. 반대쪽 원호로 복귀
        // =========================

        _attackSequence.Append(
            transform.DOPath(
                    returnPath,
                    _returnDuration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.OutQuad)
        );

        // =========================
        // 5. 종료
        // =========================

        _attackSequence.OnComplete(() =>
        {
            _isAttacking = false;

            if (_skillIndicatorMove != null)
            {
                _skillIndicatorMove.enabled = true;
                _objectRotator.enabled = false;
                SetDefaultAngle();

            }
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
    }

    private void OnDestroy()
    {
        _attackSequence?.Kill();
    }
}
