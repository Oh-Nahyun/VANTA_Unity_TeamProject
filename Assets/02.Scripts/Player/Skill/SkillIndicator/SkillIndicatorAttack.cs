using System;
using DG.Tweening;
using UnityEngine;

public class SkillIndicatorAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float _attackDuration = 0.2f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackJumpPower = 1f;

    [Header("Bounce")]
    [SerializeField] private float _bounceDistance = 2f;
    [SerializeField] private float _bouncePower = 1.5f;
    [SerializeField] private float _bounceDuration = 0.35f;

    private SkillIndicatorMove _skillIndicatorMove;
    private Animator _animator;

    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;

    private Sequence _attackSequence;

    private void Awake()
    {
        _skillIndicatorMove = GetComponent<SkillIndicatorMove>();
        _animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        if (_isAttacking)
            return;

        HitableObject target = FindAnyObjectByType<HitableObject>();

        if (target == null)
            return;

        // 기존 이동 정지
        _skillIndicatorMove.enabled = false;

        _isAttacking = true;

        // 기존 공격 Tween 제거
        _attackSequence?.Kill();

        _attackSequence = DOTween.Sequence();

        // 현재 위치 기준 공격 방향
        Vector3 attackDirection =
            (target.transform.position - transform.position).normalized;

        // 타격 후 튕겨나갈 위치
        Vector3 landingPosition =
            target.transform.position
            - attackDirection * _bounceDistance;

        _attackSequence.Append(
            transform.DOJump(
                    target.transform.position,
                    _attackJumpPower,
                    1,
                    _attackDuration
                )
                .SetEase(Ease.InQuad)
        );

// 점프 진행 시간의 정확히 절반에서 공격 애니메이션 실행
        _attackSequence.InsertCallback(
            _attackDuration * 0.5f,
            () =>
            {
                _animator.SetTrigger("Strike");
            }
        );

        // =========================
        // 2. 타격
        // =========================

        _attackSequence.AppendCallback(() =>
        {
            if (target != null)
            {

                target.ExecuteHitEvent(
                    _attackDamage,
                    DamageIndicatorType.Normal
                );
            }
        });

        // =========================
        // 3. 반동으로 튕겨나가기
        // =========================

        _attackSequence.Append(
            transform.DOJump(
                    landingPosition,
                    _bouncePower,
                    1,
                    _bounceDuration
                )
                .SetEase(Ease.OutQuad)
        );

        // =========================
        // 4. 공격 종료
        // =========================

        _attackSequence.OnComplete(() =>
        {
            _isAttacking = false;

            _skillIndicatorMove.enabled = true;
        });
    }

    public void PauseAttack()
    {
        _attackSequence?.Kill();

        _isAttacking = false;

        if (_skillIndicatorMove != null)
            _skillIndicatorMove.enabled = true;
    }

    private void OnDestroy()
    {
        _attackSequence?.Kill();
    }
}
