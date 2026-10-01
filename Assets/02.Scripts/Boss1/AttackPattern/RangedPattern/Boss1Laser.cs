using UnityEngine;

public class Boss1Laser : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Transform _laserHeadPivot;
    [SerializeField] private float _laserLength = 20f;
    [SerializeField] private LayerMask _playerLayer;

    public void ShowAim(Vector2 direction)
    {
        DrawLaser(direction);
    }

    public void Fire(Vector2 direction)
    {
        DrawLaser(direction);
        CheckHit(direction);
    }
    
    public void Hide()
    {
        _lineRenderer.enabled = false;
    }

    private void DrawLaser(Vector2 direction)
    {
        Vector2 startPos = _laserHeadPivot.position;
        Vector2 endPos = startPos + direction * _laserLength;
        
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    private void CheckHit(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(_laserHeadPivot.position, direction, _laserLength, _playerLayer);

        if (hit.collider == null)
        {
            return;
        }
        Player player = hit.collider.GetComponent<Player>();
        
        if (player == null)
        {
            return;
        }
        
        player.TakeDamage();
        
        Debug.Log("플레이어 레이저 피격");
    }
}
