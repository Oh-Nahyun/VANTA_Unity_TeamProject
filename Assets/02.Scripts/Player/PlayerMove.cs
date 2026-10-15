using System;
using DG.Tweening;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("플레이어 움직임 속도")]
    [SerializeField]
    private float _moveSpeed = 5f;

    [Header("플레이어 대쉬")]
    [SerializeField]
    private float _dashDistance = 2f;

    [SerializeField]
    private float _dashDuration = 0.15f;

    [SerializeField] private float _reverseMoveDistance = 3f;
    [SerializeField] private float _reverseMoveDuration = 1f;
    [Header("대쉬 충돌")]
    [SerializeField]
    private LayerMask _dashCollisionMask;

    [SerializeField]
    private float _collisionOffset = 0.05f;

    [Header("대쉬 이펙트")]
    [SerializeField]
    private GameObject _dashEffect;

    private Vector2 _moveDirection;
    private Vector2 _lastMoveDirection = Vector2.right;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;

    private PlayerAfterImageController
        _playerAfterImageController;

    private Player _player;

    private bool _isDashing;

    // Reverse 대쉬는 Disable 되어도 끝까지 이동
    private bool _keepDashOnDisable;

    private Tween _dashTween;
    private Tween _actingMoveDelay;
    private Vector2? _actingMoveTarget;

    private void Awake()
    {
        _spriteRenderer =
            GetComponent<SpriteRenderer>();

        _animator =
            GetComponent<Animator>();

        _rigidbody =
            GetComponent<Rigidbody2D>();

        _collider =
            GetComponent<Collider2D>();

        _player =
            GetComponent<Player>();

        _playerAfterImageController =
            GetComponent<PlayerAfterImageController>();
    }

    private void Update()
    {
        if (GameSettingsUI.IsGamePaused)
        {
            return;
        }
        
        MovementInput();
        DashInput();
    }

    private void FixedUpdate()
    {
        if (!_isDashing)
        {
            Movement();
        }
    }

    private void MovementInput()
    {
        float h =
            Input.GetAxisRaw("Horizontal");

        float v =
            Input.GetAxisRaw("Vertical");

        _moveDirection =
            new Vector2(h, v).normalized;

        if (_moveDirection != Vector2.zero)
        {
            _lastMoveDirection =
                _moveDirection;
        }

        SetMoveAnimation(
            _moveDirection != Vector2.zero
        );

        PlayerRotate(h, v);
    }

    private void Movement()
    {
        _rigidbody.MovePosition(
            _rigidbody.position +
            _moveDirection *
            _moveSpeed *
            Time.fixedDeltaTime
        );
    }

    private void PlayerRotate(
        float h,
        float v)
    {
        if (h > 0)
        {
            _spriteRenderer.flipX = false;
        }
        else if (h < 0)
        {
            _spriteRenderer.flipX = true;
        }
    }

    private void DashInput()
    {
        if (Input.GetButtonDown("Jump") &&
            _moveDirection != Vector2.zero)
        {
            Dash();
        }
    }

    private void Dash()
    {
        if (_isDashing)
            return;

        // 일반 대쉬는 Disable 시 중단
        _keepDashOnDisable = false;

        Instantiate(
            _dashEffect,
            transform.position,
            Quaternion.identity
        );

        _playerAfterImageController
            .OnAfterImage();

        SetDashAnimation();

        StartDash(
            _lastMoveDirection,
            _dashDistance,
            _dashDuration
        );
    }

    public void DashToPoint(
        Vector2 position,
        Action onComplete)
    {
        if (_isDashing)
            return;

        // 일반 DashToPoint도 Disable 시 중단
        _keepDashOnDisable = false;

        Vector2 currentPosition =
            _rigidbody.position;

        Vector2 direction =
            position - currentPosition;

        float distance =
            direction.magnitude;

        if (distance <= 0f)
        {
            onComplete?.Invoke();
            return;
        }

        direction.Normalize();

        Instantiate(
            _dashEffect,
            transform.position,
            Quaternion.identity
        );

        _playerAfterImageController
            .OnAfterImage();

        SetDashAnimation();

        StartDash(
            direction,
            distance,
            _dashDuration
        );

        _actingMoveDelay?.Kill();
        _actingMoveDelay = DOVirtual.DelayedCall(
            _dashDuration,
            () =>
            {
                onComplete?.Invoke();
            }
        );
    }

    // ==============================
    // Reverse Dash
    // Disable 되어도 끝까지 이동
    // ==============================

    public void DashToReverseSide()
    {
        if (_isDashing)
            return;

        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (boss == null)
            return;

        Vector2 direction =
            (
                _rigidbody.position -
                (Vector2)boss.transform.position
            ).normalized;

        // Disable 되어도 Tween 유지
        _keepDashOnDisable = true;

        StartDash(
            direction,
            _dashDistance,
            0.1f
        );
    }
    public void MoveToReverseSide()
    {
        if (_isDashing)
            return;
        
        _dashTween?.Kill();
        _dashTween = null;
        
        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (boss == null)
            return;

        Vector2 direction =
        (
            _rigidbody.position -
            (Vector2)boss.transform.position
        ).normalized;

        if (direction == Vector2.zero)
            return;

        // 이동 방향 바라보기
        PlayerRotate(direction.x, direction.y);

        // 이동 중 일반 입력 차단
        _isDashing = true;

        // 비활성화되어도 이동 유지
        _keepDashOnDisable = true;

        // 걷기 애니메이션
        SetMoveAnimation(true);

        // 충돌을 고려한 이동 거리
        float availableDistance =
            GetDashDistance(
                direction,
                _reverseMoveDistance
            );

        Vector2 targetPosition =
            _rigidbody.position +
            direction * availableDistance;

        _actingMoveTarget = targetPosition;

        _dashTween?.Kill();

        _dashTween =
            _rigidbody
                .DOMove(
                    targetPosition,
                    _reverseMoveDuration
                )
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _dashTween = null;

                    _isDashing = false;
                    _keepDashOnDisable = false;

                    // 걷기 종료
                    SetMoveAnimation(false);

                    // Idle 복귀
                    _animator.Play(
                        "Idle",
                        0,
                        0f
                    );
                });
    }
    public void DashToReverseSideByHit()
    {
        if (_isDashing)
            return;
 
        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (boss == null)
            return;

        Vector2 direction =
            (
                _rigidbody.position -
                (Vector2)boss.transform.position
            ).normalized;

        // Disable 되어도 Tween 유지
        _keepDashOnDisable = true;

        StartDash(
            direction,
            _dashDistance / 2f,
            1f
        );
    }

    
    
    private void StartDash(
        Vector2 direction,
        float distance,
        float duration)
    {
        if (direction == Vector2.zero)
        {
            _keepDashOnDisable = false;
            return;
        }

        direction.Normalize();

        _isDashing = true;

        _player.OffPlayerHittable();

        float availableDistance =
            GetDashDistance(
                direction,
                distance
            );

        Vector2 targetPosition =
            _rigidbody.position +
            direction *
            availableDistance;

        _dashTween?.Kill();

        _dashTween =
            _rigidbody
                .DOMove(
                    targetPosition,
                    duration
                )
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _isDashing = false;

                    // Reverse Dash였으면
                    // 도착하는 순간 Idle로 복귀
                    if (_keepDashOnDisable &&
                        _animator != null)
                    {
                        _animator.SetBool(
                            "isMove",
                            false
                        );

                        _animator.ResetTrigger(
                            "Dash"
                        );

                        _animator.Play(
                            "Idle",
                            0,
                            0f
                        );

                        _animator.Update(0f);
                    }
                 
                    _keepDashOnDisable = false;

                    if (_player != null)
                    {
                        _player.OnPlayerHittable();
                    }

                    if (_playerAfterImageController != null)
                    {
                        _playerAfterImageController
                            .OffAfterImage();
                    }

                    _dashTween = null;
                });
    }

    public void StopPlayerMovement()
    {
        _dashTween?.Kill();
        _dashTween = null;

        _isDashing = false;
        _keepDashOnDisable = false;

        _moveDirection = Vector2.zero;

        _rigidbody.linearVelocity =
            Vector2.zero;

        _rigidbody.angularVelocity = 0f;

        SetMoveAnimation(false);

        _playerAfterImageController
            .OffAfterImage();

        _player.OnPlayerHittable();
    }

    private float GetDashDistance(
        Vector2 direction,
        float distance)
    {
        if (_collider == null)
        {
            RaycastHit2D hit =
                Physics2D.Raycast(
                    _rigidbody.position,
                    direction,
                    distance,
                    _dashCollisionMask
                );

            if (hit.collider == null)
                return distance;

            return Mathf.Max(
                0f,
                hit.distance -
                _collisionOffset
            );
        }

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            _dashCollisionMask
        );

        filter.useTriggers = false;

        RaycastHit2D[] hits =
            new RaycastHit2D[8];

        int hitCount =
            _collider.Cast(
                direction,
                filter,
                hits,
                distance
            );

        if (hitCount == 0)
            return distance;

        float nearestDistance =
            distance;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            if (hits[i].distance <
                nearestDistance)
            {
                nearestDistance =
                    hits[i].distance;
            }
        }

        return Mathf.Max(
            0f,
            nearestDistance -
            _collisionOffset
        );
    }

    private void SetMoveAnimation(
        bool isMove)
    {
        _animator.SetBool(
            "isMove",
            isMove
        );
    }

    private void SetDashAnimation()
    {
        _animator.SetTrigger(
            "Dash"
        );
    }

    private void OnDisable()
    {
        /*
         * Reverse Dash가 진행 중이라면
         * DOMove Tween을 죽이지 않는다.
         *
         * PlayerMove.enabled = false가 되어도
         * DOTween이 계속 Rigidbody를 이동시킨다.
         */
        if (_keepDashOnDisable &&
            _dashTween != null &&
            _dashTween.IsActive())
        {
            // 입력값만 초기화
            _moveDirection =
                Vector2.zero;

            return;
        }

        // ==============================
        // 일반적인 Disable 처리
        // ==============================

        _dashTween?.Kill();
        _dashTween = null;

        _isDashing = false;
        _keepDashOnDisable = false;

        _moveDirection =
            Vector2.zero;

        // 물리 속도 초기화
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity =
                Vector2.zero;

            _rigidbody.angularVelocity =
                0f;
        }

        // 잔상 종료
        if (_playerAfterImageController != null)
        {
            _playerAfterImageController
                .OffAfterImage();
        }

        // Animator 초기화
        if (_animator != null)
        {
            _animator.SetBool(
                "isMove",
                false
            );

            _animator.ResetTrigger(
                "Dash"
            );

            _animator.Play(
                "Idle",
                0,
                0f
            );

            _animator.Update(0f);
        }

        // 피격 가능 상태 복구
        if (_player != null)
        {
            _player.OnPlayerHittable();
        }
    }

    public void CancelActingMove()
    {
        _actingMoveDelay?.Kill();
        _actingMoveDelay = null;
        _keepDashOnDisable = false;
        OnDisable();
    }

    public void SetActingPosition(Vector3 position)
    {
        CancelActingMove();
        transform.position = position;
        _rigidbody.position = position;
    }

    public void FinishActingMoveImmediately()
    {
        if (!_actingMoveTarget.HasValue)
            return;
        Vector3 position = transform.position;
        position.x = _actingMoveTarget.Value.x;
        position.y = _actingMoveTarget.Value.y;
        SetActingPosition(position);
        _actingMoveTarget = null;
    }

    private void OnDestroy()
    {
        _actingMoveDelay?.Kill();
        _dashTween?.Kill();
        _dashTween = null;
    }
}
