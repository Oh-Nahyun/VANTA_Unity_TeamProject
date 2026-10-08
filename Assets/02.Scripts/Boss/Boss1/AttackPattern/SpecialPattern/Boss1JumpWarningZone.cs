using UnityEngine;

public class Boss1JumpWarningZone : MonoBehaviour
{
    private bool _isPlayerInside;
    private GameObject _player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        _isPlayerInside = true;
        _player = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _player = null;
    }

    public void AttackPlayer()
    {
        
    }
}
