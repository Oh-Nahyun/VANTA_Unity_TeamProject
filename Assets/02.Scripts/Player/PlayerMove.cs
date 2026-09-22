using DG.Tweening;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("플레이어 움직임 속도")]
    [SerializeField] private float _moveSpeed=5;
    [Header("플레이어 대쉬")]
    [SerializeField] private float _dashDistance = 2f;
    [SerializeField] private float _dashDuration = 0.15f;
    
    private Vector2 _moveDirection;
    private Vector2 _lastMoveDirection = Vector2.right;
    
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private bool _isDashing;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator=GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
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

        _moveDirection = new Vector2(h, v).normalized;

        if (_moveDirection != Vector2.zero)
        {
            _lastMoveDirection = _moveDirection;
        }

        SetMoveAnimation(_moveDirection != Vector2.zero);
        PlayerRotate(h, v);
    }
    
    private void Movement()
    {
        _rigidbody.MovePosition(
            _rigidbody.position +
            _moveDirection * _moveSpeed * Time.fixedDeltaTime
        );
    }
    
    private void PlayerRotate(float h, float v)
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
        if (Input.GetButtonDown("Jump")&& _moveDirection != Vector2.zero)
        {
            Dash();
        }
    }

    private void Dash()
    {
        if (_isDashing)
            return;

        _isDashing = true;
        SetDashAnimation();
        
        Vector2 targetPosition =
            _rigidbody.position +
            _lastMoveDirection * _dashDistance;

        _rigidbody.DOMove(targetPosition, _dashDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _isDashing = false;

            });
    }
    
    private void SetMoveAnimation(bool isMove)
    {
        _animator.SetBool("isMove",isMove);
    }
    
    private void SetDashAnimation()
    {
        _animator.SetTrigger("Dash");
    }
}
