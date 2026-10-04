using System;
using TMPro;
using UnityEngine;

public class Boss1Bullet : MonoBehaviour
{
    [Header("일반탄 이동 속도")]
    [SerializeField] private float _speed = 3f;
    
    [Header("일반탄 살아있는 시간")]
    [SerializeField] private float _lifeTime = 3f;

    private float _lifeTimer;
    
    private Boss1ProjectileManager _projectileManager;
    private Rigidbody2D _rigidBody;
    private Animator _animator;
    
    private bool _isDestroying;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_isDestroying)
        {
            return;
        }
        
        _lifeTimer += Time.deltaTime;

        if (_lifeTimer >= _lifeTime)
        {
            StartDisappear();
        }
    }
    

    public void Initialize(Vector2 direction, Boss1ProjectileManager projectileManager)
    {
        _projectileManager = projectileManager;
        _projectileManager.RegisterProjectile(gameObject);
        _rigidBody.linearVelocity = direction.normalized * _speed;

        _lifeTimer = 0f;
        _isDestroying = false;
    }

    private void OnDestroy()
    {
        if (_projectileManager != null)
        {
            _projectileManager.UnregisterProjectile(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDestroying)
        {
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                return;
            }
            player.TakeDamage();
            StartImpact();
        }
    }

    private void StartImpact()
    {
        _isDestroying = true;
        _rigidBody.linearVelocity = Vector2.zero;
        
        Collider2D collider = GetComponent<Collider2D>();
        collider.enabled = false;

        _animator.SetTrigger("Impact");
    }
    
    private void StartDisappear()
    {
        _isDestroying = true;

        _rigidBody.linearVelocity = Vector2.zero;

        Collider2D collider = GetComponent<Collider2D>();
        collider.enabled = false;

        _animator.SetTrigger("Disappear");
    }
    
    public void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
