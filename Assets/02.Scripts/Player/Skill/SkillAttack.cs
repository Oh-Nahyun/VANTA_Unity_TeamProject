using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class SkillAttack : MonoBehaviour
{
    [Header("대쉬 직전 타임 슬로우 정도")]
    [SerializeField]
    private float _dashTimeSlow = 0.05f;

    [Header("대쉬 속도")][SerializeField] private float _dashDuration = 0.15f;
    [Header("대쉬 중 줌 정도")][SerializeField] private float _dashZoomAmount = 1f;

    [Header("대쉬간 대기시간")][SerializeField] private float _dashWaitTime = 0.5f;
    [SerializeField] private Ease _dashEase = Ease.OutQuad;
    [Header("대쉬 공격 판정")][SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _dashDamage = 10f;
    [SerializeField] private float _dashHitRadius = 0.5f;
    [SerializeField] private float _weaknessAngleTolerance = 0.5f;

    [Header("참조")][SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [Header("vfx 프리팹")][SerializeField] private GameObject _skillDashStartVfxPrefab;
    [Header("vfx 프리팹")][SerializeField] private GameObject _skillDashEndVfxPrefab;

    [Header("vfx 프리팹")][SerializeField] private GameObject _skillDashAttackVfxPrefab;
    [Header("vfx 프리팹")][SerializeField] private GameObject _skillDashAttackCriticalVfxPrefab;


    private ObjectZoom _objectZoom;
    private SkillDashScreenController _skillDashScreenController;
    private Collider2D[] _playerColliders;
    private Sequence _dashSequence;
    [SerializeField] private Animator _animator;
    private Vector2 _weaknessDirection;


    private void Awake()
    {
        if (_rigidbody != null)
            _playerColliders = _rigidbody.GetComponentsInChildren<Collider2D>(true);
        _skillDashScreenController = GetComponent<SkillDashScreenController>();
        _objectZoom=GetComponent<ObjectZoom>();
    }

    public void DashStart(List<SkillIndicator> selectedIndicators, Action onComplete)
    {
        if (_rigidbody == null || selectedIndicators == null || selectedIndicators.Count == 0)
            return;

        _objectZoom.ZoomIn(_dashZoomAmount);
        
        _dashSequence?.Kill();

        SetPlayerColliders(false);
        _dashSequence = DOTween.Sequence()
            .SetUpdate(true);

        foreach (SkillIndicator indicator in selectedIndicators)
        {
            if (indicator == null)
                continue;



            Vector2 dashStartPosition = Vector2.zero;


            _dashSequence.AppendCallback(() =>
            {
                dashStartPosition = _rigidbody.position;

                Vector3 moveDirection =
                    indicator.transform.position - transform.position;

                if (Mathf.Abs(moveDirection.x) > 0.01f)
                {
                    _spriteRenderer.flipX = moveDirection.x < 0f;
                }

                _skillDashScreenController.StartDashScreenSetting(moveDirection);

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
                    .DOMove(indicator.transform.position, _dashDuration)
                    .SetEase(_dashEase)
            );

            _dashSequence.AppendCallback(() =>
            {
                Vector2 dashEndPosition = _rigidbody.position;
                _animator.SetTrigger("OneDashSkill");

                // 시작 → 끝 전체 공격 판정  
                CheckDashHit(
                    dashStartPosition,
                    dashEndPosition
                );

                _skillDashScreenController.EndDashScreenSetting();

                indicator.SkillAttackComplete();
                Instantiate(_skillDashEndVfxPrefab, indicator.transform.position, Quaternion.identity);
            });

            _dashSequence.AppendInterval(_dashWaitTime);

        }

        _dashSequence
            .OnComplete(() =>
            {
                SetPlayerColliders(true);
                _dashSequence = null;
                _objectZoom.ZoomOut();

                onComplete.Invoke();
            })
            .OnKill(() => SetPlayerColliders(true));
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

    private void CheckDashHit(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0f)
            return;

        direction.Normalize();

        float dot = Mathf.Abs(
            Vector2.Dot(direction, _weaknessDirection.normalized)
        );

        float threshold =
            Mathf.Cos(_weaknessAngleTolerance * Mathf.Deg2Rad);

        bool isWeaknessAttack = dot >= threshold;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
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


            if (!isWeaknessAttack)
            {
                hitableObject.ExecuteHitEvent(
                    _dashDamage,
                    DamageIndicatorType.Middle
                );

                Instantiate(
                    _skillDashAttackVfxPrefab,
                    hit.point,
                    Quaternion.identity
                );
            }
            else
            {
                hitableObject.ExecuteHitEvent(
                    _dashDamage * 2,
                    DamageIndicatorType.Critical
                );

                Instantiate(
                    _skillDashAttackCriticalVfxPrefab,
                    hit.point,
                    Quaternion.identity
                );

            }
        }
    }
    
    public void SingleDash(Vector2 targetPosition, Action onComplete)
    {
        if (_rigidbody == null)
            return;

        _objectZoom.ZoomIn(_dashZoomAmount);

        _dashSequence?.Kill();

        SetPlayerColliders(false);
        
        Vector3 moveDirection =
            targetPosition - _rigidbody.position;

        if (Mathf.Abs(moveDirection.x) > 0.01f)
        {
            _spriteRenderer.flipX =
                moveDirection.x < 0f;
        }

        _dashSequence = DOTween.Sequence()
            .SetUpdate(true);

        _dashSequence.AppendCallback(() =>
        {
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
        HitableObject boss=FindAnyObjectByType<HitableObject>();

        _dashSequence.AppendCallback(() =>
        {

            _animator.SetTrigger("OneDashSkill");
            

            _skillDashScreenController
                .EndDashScreenSetting();
            Instantiate(
                _skillDashAttackCriticalVfxPrefab,
                boss.transform.position,
                Quaternion.identity
            );
        });

        _dashSequence.AppendInterval(_dashWaitTime);

        _dashSequence
            .OnComplete(() =>
            {
                SetPlayerColliders(true);
                boss.ExecuteHitEvent(200,damageType:DamageIndicatorType.Critical);

                _dashSequence = null;

                _objectZoom.ZoomOut();

                onComplete?.Invoke();
            })
            .OnKill(() =>
            {
                SetPlayerColliders(true);
            });
    }
}
