using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("플레이어 움직임 속도")]
    [SerializeField] private float _moveSpeed=5;
    
    private Vector2 _moveDirection;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rigidbody;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator=GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        MovementInput();
    }

    private void FixedUpdate()
    {
        Movement();
    }
    
    private void MovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        _moveDirection = new Vector2(h, v).normalized;

        SetAnimation(_moveDirection != Vector2.zero);

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

    private void SetAnimation(bool isMove)
    {
        _animator.SetBool("isMove",isMove);
    }
}
