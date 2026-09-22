using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("플레이어 움직임 속도")]
    [SerializeField] private float _moveSpeed=5;
    
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator=GetComponent<Animator>();
    }
    void Update()
    {
        Movement();
    }

    private void Movement()
    {
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector2 normalDirection = new Vector2(0, 0);
        if (h != 0 || v != 0) 
        {
            normalDirection = new Vector2(h, v); 
            Vector2 normalizedDirection = normalDirection.normalized; 
            transform.Translate(normalizedDirection * _moveSpeed * Time.deltaTime);
            SetAnimation(true);
        }
        else
        {
            SetAnimation(false);
        }
        
        PlayerRotate(h,v);
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
