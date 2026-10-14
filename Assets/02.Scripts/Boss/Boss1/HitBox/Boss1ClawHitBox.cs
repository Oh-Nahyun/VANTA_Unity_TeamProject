using System.Runtime.CompilerServices;
using UnityEngine;

public class Boss1ClawHitBox : MonoBehaviour
{
    private BoxCollider2D _collider;
    [SerializeField] private Boss1MeleeAvoid _meleeAvoid;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.enabled = false;
    }

    public void EnableHitBox()
    {
        _collider.enabled = true;
    }

    public void DisableHitBox()
    {
        _collider.enabled = false;
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
            
            DisableHitBox(); // 같은 공격에 두 번 걸리지 않게

            if (_meleeAvoid == null)
            {
                player.TakeDamage();
                return;
            }

            _meleeAvoid.HandleHit(player, true);
        }
    }
}
