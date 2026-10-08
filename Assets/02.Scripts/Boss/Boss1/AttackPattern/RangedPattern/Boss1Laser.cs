using UnityEngine;

public class Boss1Laser : MonoBehaviour
{
    [Header("레이저 라인")]
    [SerializeField] private LineRenderer _warningLineRenderer;
    [SerializeField] private LineRenderer _laserLineRenderer;

    [Header("레이저 설정")]
    [SerializeField] private Transform _laserHeadPivot;
    [SerializeField] private float _laserLength = 20f;

    [Header("피격")]
    [SerializeField] private LayerMask _playerLayer;

    /// <summary>
    /// 데미지가 없는 조준 경고선 표시
    /// </summary>
    public void ShowAim(Vector2 direction)
    {
        _laserLineRenderer.enabled = false;

        DrawLine( _warningLineRenderer, direction );
    }

    /// <summary>
    /// 실제 레이저 표시 및 피격 검사
    /// </summary>
    public void Fire(Vector2 direction)
    {
        _warningLineRenderer.enabled = false;

        DrawLine(_laserLineRenderer, direction);

        CheckHit(direction);
    }

    /// <summary>
    /// 경고선만 숨김
    /// </summary>
    public void HideAim()
    {
        _warningLineRenderer.enabled = false;
    }

    /// <summary>
    /// 경고선과 실제 레이저 모두 숨김
    /// </summary>
    public void Hide()
    {
        _warningLineRenderer.enabled = false;
        _laserLineRenderer.enabled = false;
    }

    private void DrawLine(LineRenderer lineRenderer, Vector2 direction)
    {
        Vector2 startPos = _laserHeadPivot.position;

        Vector2 endPos = startPos + direction * _laserLength;

        lineRenderer.enabled = true;

        lineRenderer.SetPosition( 0, startPos);

        lineRenderer.SetPosition( 1, endPos);
    }

    private void CheckHit(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast( _laserHeadPivot.position, direction, _laserLength, _playerLayer);

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
    }
}
