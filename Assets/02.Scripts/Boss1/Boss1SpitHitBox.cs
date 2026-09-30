using UnityEngine;

public class Boss1SpitHitBox : MonoBehaviour
{
    private BoxCollider2D _collider;

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
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log("플레이어 침뱉기 피격");
    }
}
