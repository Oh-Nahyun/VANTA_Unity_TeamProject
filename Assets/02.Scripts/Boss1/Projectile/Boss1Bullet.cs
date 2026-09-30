using System;
using UnityEngine;

public class Boss1Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _lifeTime = 3f;
    
    private Rigidbody2D _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction)
    {
        _rigidBody.linearVelocity = direction.normalized * _speed;
        Destroy(gameObject, _lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                return;
            }
            player.TakeDamage();
            Debug.Log("플레이어가 총알에 맞음");
            Destroy(gameObject);
        }
    }
}
