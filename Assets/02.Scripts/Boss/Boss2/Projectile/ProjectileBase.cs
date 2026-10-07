using System;
using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private Vector2 _velocity;

    [SerializeField] private int _damage = 1;
    //private Animator _animator;
    private SpriteRenderer _renderer;

    [SerializeField] private ParticleSystem _explosionEffect;

    // Launch 로 진행 방향을 향해 회전했는지
    private bool _isRotatedToDirection;

    
    public ProjectileBase(Vector2 velocity)
    {
        _velocity = velocity;    
    }

    public void SetVelocity(Vector2 velocity)
    {
        _velocity = velocity;
    }

    /// <summary>
    /// direction 쪽을 바라보도록 회전시키고 그 방향으로 날린다. (8방향 발사용, 원본 스프라이트는 오른쪽을 본다고 가정)
    /// </summary>
    public void Launch(Vector2 direction, float speed)
    {
        Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _velocity = normalized * speed;

        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        _isRotatedToDirection = true;
    }

    private void Awake()
    {
        //_animator = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        // 회전으로 방향을 정했으면 좌우 뒤집기를 하지 않는다. (뒤집으면 왼쪽 발사체가 거꾸로 보인다)
        if (_isRotatedToDirection)
            return;

        if (_velocity.x > 0)
        {
            _renderer.flipX = false;
        }
        else if (_velocity.x < 0)
        {
            _renderer.flipX = true;
        }
        
        
    }

     private void Update()
    {
        _lifeTime -= Time.deltaTime;
        if (_lifeTime <= 0)
        {
            Despawn();
            return;
        }

        Move();


    }
    
     private void Move()
     {
         // 회전과 상관없이 월드 방향으로 움직인다. (기본값 Space.Self 면 회전된 발사체가 엉뚱한 방향으로 간다)
         transform.Translate(Time.deltaTime * _velocity, Space.World);
     }

     public void Explode()
     {
         if (_explosionEffect != null)
         {
             // 발사체와 함께 지워지지 않게 떼어낸 뒤 재생하고, 끝나면 지운다.
             _explosionEffect.transform.SetParent(null, true);
             _explosionEffect.Play();
             ParticleSystem.MainModule main = _explosionEffect.main;
             Destroy(_explosionEffect.gameObject, main.duration + main.startLifetime.constantMax);
         }

         Despawn();
     }
     
    /// <summary>
    /// 투사체를 지운다. 불꽃 연출(Boss2FireTrailFx)이 있으면 떼어내 남은 불꽃이 자연스럽게 꺼지게 한다.
    /// 수명 종료 외에 명중 등으로 지울 때도 Destroy 대신 이것을 부른다.
    /// </summary>
    public void Despawn()
    {
        Boss2FireTrailFx fireFx = GetComponent<Boss2FireTrailFx>();
        if (fireFx != null)
        {
            fireFx.Detach();
        }

        Destroy(gameObject);
    }

    // CircleCollider2D(Trigger) 와 짝을 이루는 2D 트리거. (OnTriggerEnter(Collider) 는 3D 용이라 호출되지 않는다)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 콜라이더가 Player 의 자식에 붙어 있어도 찾도록 Rigidbody 쪽을 먼저 본다. (태그가 자식에 없을 수 있어 컴포넌트로 판별)
        Player player = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<Player>()
            : other.GetComponent<Player>();
        if (player == null)
            return;

        player.TakeDamage();
        Explode();
    }
}
