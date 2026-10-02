using System;
using UnityEngine;

public class Boss1Bullet : MonoBehaviour
{
    [Header("일반탄 이동 속도")]
    [SerializeField] private float _speed = 3f;
    
    [Header("일반탄 살아있는 시간")]
    [SerializeField] private float _lifeTime = 3f;
    
    private Boss1ProjectileManager _projectileManager;
    
    private Rigidbody2D _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, Boss1ProjectileManager projectileManager)
    {
        _projectileManager = projectileManager;
        _projectileManager.RegisterProjectile(gameObject);
        _rigidBody.linearVelocity = direction.normalized * _speed;
        Destroy(gameObject, _lifeTime);
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
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                return;
            }
            player.TakeDamage();
            Destroy(gameObject);
        }
    }
}
