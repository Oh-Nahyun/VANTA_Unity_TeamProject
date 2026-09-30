using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Sequence = DG.Tweening.Sequence;

public class SkillAttack : MonoBehaviour
{
    [Header("대쉬 직전 타임 슬로우 정도")]
    [SerializeField] private float _dashTimeSlow = 0.05f;

    [Header("대쉬 속도")]
    [SerializeField] private float _dashDuration = 0.15f;

    [Header("대쉬 중 줌 정도")]
    [SerializeField] private float _dashZoomAmount = 1f;

    [Header("대쉬간 대기시간")]
    [SerializeField] private float _dashWaitTime = 0.5f;

    [SerializeField] private Ease _dashEase = Ease.OutQuad;

    [Header("대쉬 공격 판정")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _dashDamage = 10f;
    [SerializeField] private float _dashHitRadius = 0.5f;
    [SerializeField] private float _weaknessAngleTolerance = 0.5f;

    [Header("참조")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SkillManager _skillManager;

    [Header("VFX")]
    [SerializeField] private GameObject _skillDashStartVfxPrefab;
    [SerializeField] private GameObject _skillDashEndVfxPrefab;
    [SerializeField] private GameObject _skillDashAttackVfxPrefab;
    [SerializeField] private GameObject _skillDashAttackCriticalVfxPrefab;
    [SerializeField] private GameObject _weakHitVfx;

    [SerializeField] private UnityEvent  _weaknessAttackSuccessAction;

    private bool _isWeaknessAttackSuccess;
    private int _dashCount;
    
    private SkillSpaceController _skillSpaceController;
    private SkillDashScreenController _skillDashScreenController;
    private SkillDashSlashScreenController _skillDashSlashScreenController;
    private Collider2D[] _playerColliders;
    private ObjectZoom _objectZoom;

    private Sequence _dashSequence;
    private Vector2 _weaknessDirection;

    private void Awake()
    {
        if (_rigidbody != null)
        {
            _playerColliders =
                _rigidbody.GetComponentsInChildren<Collider2D>(true);
        }

        _skillDashScreenController =
            GetComponent<SkillDashScreenController>();
        _skillDashSlashScreenController = GetComponent<SkillDashSlashScreenController>();
        _objectZoom =
            GetComponent<ObjectZoom>();
        _skillSpaceController=GetComponent<SkillSpaceController>();
    }

    public void DashStart(
        List<SkillIndicator> selectedIndicators,
        Action onComplete)
    {
        if (_rigidbody == null ||
            selectedIndicators == null ||
            selectedIndicators.Count == 0)
        {
            return;
        }

        _isWeaknessAttackSuccess = false;
        _dashCount = selectedIndicators.Count;
        StartDashSequence();

        foreach (SkillIndicator indicator in selectedIndicators)
        {
            if (indicator == null)
                continue;

            AppendSingleDash(
                indicator.transform.position,
                () =>
                {
                    indicator.SkillAttackComplete();
                },
                true
            );
        }

   
        CompleteDashSequence(onComplete);
    }

    public void SingleDash(
        Vector2 targetPosition,
        Action onComplete)
    {
        if (_rigidbody == null)
            return;

        StartDashSequence();

        AppendSingleDash(
            targetPosition,
            null,
            false
        );

        CompleteDashSequence(onComplete);
    }

    private void StartDashSequence()
    {
        _dashSequence?.Kill();

        Time.timeScale = 1f;

        _objectZoom.ZoomIn(_dashZoomAmount);

        SetPlayerColliders(false);

        _dashSequence = DOTween.Sequence()
            .SetUpdate(true);
    }

    private void AppendSingleDash(
        Vector2 targetPosition,
        Action dashComplete,
        bool checkDashHit)
    {
        Vector2 dashStartPosition = Vector2.zero;

        _dashSequence.AppendCallback(() =>
        {
            dashStartPosition = _rigidbody.position;

            Vector2 moveDirection =
                targetPosition - _rigidbody.position;

            if (Mathf.Abs(moveDirection.x) > 0.01f)
            {
                _spriteRenderer.flipX =
                    moveDirection.x < 0f;
            }

            _skillDashScreenController
                .StartDashScreenSetting(moveDirection);

            Time.timeScale = _dashTimeSlow;

            Instantiate(
                _skillDashStartVfxPrefab,
                transform.position,
                Quaternion.identity
            );
        });

        _dashSequence.AppendInterval(_dashWaitTime);

        _dashSequence.AppendCallback(() =>
        {
            Time.timeScale = 1f;
        });

        _dashSequence.Append(
            _rigidbody
                .DOMove(targetPosition, _dashDuration)
                .SetEase(_dashEase)
        );

        _dashSequence.AppendCallback(() =>
        {
            Vector2 dashEndPosition =
                _rigidbody.position;

            _animator.SetTrigger("OneDashSkill");

            if (checkDashHit)
            {
                CheckDashHit(
                    dashStartPosition,
                    dashEndPosition
                );
            }

            _skillDashScreenController
                .EndDashScreenSetting();

            Instantiate(
                _skillDashEndVfxPrefab,
                transform.position,
                Quaternion.identity
            );

            dashComplete?.Invoke();
        });

        _dashSequence.AppendInterval(_dashWaitTime);
    }

    private void CompleteDashSequence(Action onComplete)
    {
     

        _dashSequence
            .OnComplete(() =>
            {
                Time.timeScale = 1f;

                SetPlayerColliders(true);

                _objectZoom.ZoomOut();

                _dashSequence = null;

                if (_dashCount == 5)
                {
                    PlayerPetCaller.Instance.CallPetToUtillSkill();

                }

                onComplete?.Invoke();
            })
            .OnKill(() =>
            {
                Time.timeScale = 1f;

                SetPlayerColliders(true);

                _skillDashScreenController
                    .EndDashScreenSetting();

                _objectZoom.ZoomOut();
            });
    }

    public void SetWeaknessDirection(Vector2 direction)
    {
        _weaknessDirection = direction;
    }

    private void SetPlayerColliders(bool enabled)
    {
        if (_playerColliders == null)
            return;

        foreach (Collider2D playerCollider in _playerColliders)
        {
            if (playerCollider != null)
                playerCollider.enabled = enabled;
        }
    }

    private void CheckDashHit(
        Vector2 start,
        Vector2 end)
    {
        Vector2 direction = end - start;

        float distance =
            direction.magnitude;

        if (distance <= 0f)
            return;

        direction.Normalize();

        _skillDashSlashScreenController.PlaySlash(
            start,
            end
        );

        float dot = Mathf.Abs(
            Vector2.Dot(
                direction,
                _weaknessDirection.normalized
            )
        );

        float threshold =
            Mathf.Cos(
                _weaknessAngleTolerance *
                Mathf.Deg2Rad
            );

        bool isWeaknessAttack =
            !_isWeaknessAttackSuccess &&
            dot >= threshold;

        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                start,
                _dashHitRadius,
                direction,
                distance,
                _enemyLayer
            );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            HitableObject hitableObject =
                hit.collider.GetComponent<HitableObject>();

            if (hitableObject == null)
                continue;

            if (isWeaknessAttack)
            {
                _isWeaknessAttackSuccess = true;

                SuccessWeaknessSkillAttack(
                    hitableObject
                );
            }
            else
            {
                SuccessNormalSkillAttack(
                    hitableObject
                );
            }
        }
    }
    private void SuccessNormalSkillAttack(
        HitableObject hitableObject)
    {
        hitableObject.ExecuteHitEvent(
            _dashDamage,
            DamageIndicatorType.Middle
        );

        Instantiate(
            _skillDashAttackVfxPrefab,
            hitableObject.transform.position,
            Quaternion.identity
        );
    }

    private void SuccessWeaknessSkillAttack(
        HitableObject hitableObject)
    {
        hitableObject.ExecuteHitEvent(
            _dashDamage * 2,
            DamageIndicatorType.Critical
        );

        Instantiate(
            _skillDashAttackCriticalVfxPrefab,
            hitableObject.transform.position,
            Quaternion.identity
        );

        _weaknessAttackSuccessAction?.Invoke();
    }
}
