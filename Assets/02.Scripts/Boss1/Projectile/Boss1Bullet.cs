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
}
