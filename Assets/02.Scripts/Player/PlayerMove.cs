using DG.Tweening;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("플레이어 움직임 속도")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("플레이어 대쉬")]
    [SerializeField] private float _dashDistance = 2f;
    [SerializeField] private float _dashDuration = 0.15f;

    [Header("대쉬 충돌")]
    [SerializeField] private LayerMask _dashCollisionMask;
    [SerializeField] private float _collisionOffset = 0.05f;

    [Header("대쉬 이펙트")]
    [SerializeField] private GameObject _dashEffect;

    private Vector2 _moveDirection;
    private Vector2 _lastMoveDirection = Vector2.right;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;

    private bool _isDashing;

    private Tween _dashTween;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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

        Instantiate(
            _dashEffect,
            transform.position,
            Quaternion.identity
        );

        SetDashAnimation();

        StartDash(
            _lastMoveDirection,
            _dashDistance,
            _dashDuration
        );
    }

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

        StartDash(
            direction,
            _dashDistance,
            0.1f
        );
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

        StartDash(
            direction,
            _dashDistance/2,
            1f
        );
    }

    private void StartDash(
        Vector2 direction,
        float distance,
        float duration)
    {
        if (direction == Vector2.zero)
            return;

        direction.Normalize();

        _isDashing = true;

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
                    _dashTween = null;
                });
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

    private void OnDestroy()
    {
        _dashTween?.Kill();
    }
}
