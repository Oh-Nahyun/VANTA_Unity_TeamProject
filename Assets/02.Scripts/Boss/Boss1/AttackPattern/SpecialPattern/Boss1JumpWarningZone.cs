using System.Collections.Generic;
using UnityEngine;

public class Boss1JumpWarningZone : MonoBehaviour
{
    private Collider2D _collider;
    private ContactFilter2D _jumpWarningZone;
    private readonly List<Collider2D> _results = new List<Collider2D>();

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _jumpWarningZone = new ContactFilter2D();
        _jumpWarningZone.useTriggers = true;
    }
    
    public void AttackPlayer()
    {
        _results.Clear();
        _collider.Overlap(_jumpWarningZone, _results);

        foreach (Collider2D other in _results)
        {
            if (!other.CompareTag("Player"))
            {
                continue;
            }

            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                continue;
            }

            player.TakeDamage();
            return;
        }
    }
}
