using DG.Tweening;
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

    [Header("경고선 연출")]
    [SerializeField] private float _aimStartWidthRatio = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _aimBlinkStart = 0.75f;
    [SerializeField] private float _aimBlinkSpeed = 14f;
    [SerializeField] private float _aimBlinkMinAlpha = 0.3f;

    [Header("발사 두께 연출")]
    [SerializeField] private float _fireInDuration = 0.08f;
    [SerializeField] private float _firePeakWidthRatio = 1.5f;
    [SerializeField] private float _fireSettleDuration = 0.1f;
    [SerializeField] private float _fireFlickerAmount = 0.06f;
    [SerializeField] private float _fireFlickerSpeed = 40f;
    [SerializeField] private float _fireOutDuration = 0.1f;

    private float _warningBaseWidth;
    private float _laserBaseWidth;

    private bool _isFiring;
    private float _fireTimer;
    private Tween _hideTween;

    private void Awake()
    {
        _warningBaseWidth = _warningLineRenderer.widthMultiplier;
        _laserBaseWidth = _laserLineRenderer.widthMultiplier;
    }
    
    public void ShowAim(Vector2 direction, float progress)
    {
        ResetLaserLine();

        float _laserProgress = Mathf.Clamp01(progress);
        _warningLineRenderer.widthMultiplier = _warningBaseWidth * Mathf.Lerp(_aimStartWidthRatio, 1f, _laserProgress);

        float alpha = 1f;
        if (_laserProgress >= _aimBlinkStart)
        {
            bool isOn = Mathf.Repeat(Time.time * _aimBlinkSpeed, 1f) < 0.5f;
            alpha = isOn ? 1f : _aimBlinkMinAlpha;
        }
        SetLineAlpha(_warningLineRenderer, alpha);

        DrawLine(_warningLineRenderer, direction);
    }
    
    public void Fire(Vector2 direction)
    {
        _warningLineRenderer.enabled = false;

        if (!_isFiring)
        {
            _hideTween?.Kill();
            _isFiring = true;
            _fireTimer = 0f;
        }

        _fireTimer += Time.deltaTime;
        _laserLineRenderer.widthMultiplier = _laserBaseWidth * GetFireWidthRatio(_fireTimer);

        DrawLine(_laserLineRenderer, direction);
        CheckHit(direction);
    }
    
    public void HideAim()
    {
        _warningLineRenderer.enabled = false;
    }
    
    public void Hide()
    {
        _warningLineRenderer.enabled = false;
        
        if (_hideTween != null && _hideTween.IsActive())
        {
            return;
        }

        if (!_isFiring || !_laserLineRenderer.enabled)
        {
            ResetLaserLine();
            return;
        }

        _isFiring = false;
        _hideTween = DOTween.To(
                () => _laserLineRenderer.widthMultiplier,
                x => _laserLineRenderer.widthMultiplier = x,
                0f,
                _fireOutDuration)
            .SetLink(gameObject)
            .OnComplete(ResetLaserLine);
    }

    private float GetFireWidthRatio(float time)
    {
        if (time < _fireInDuration)
        {
            return Mathf.Lerp(0f, _firePeakWidthRatio, time / _fireInDuration);
        }

        float settleTime = time - _fireInDuration;
        if (settleTime < _fireSettleDuration)
        {
            return Mathf.Lerp(_firePeakWidthRatio, 1f, settleTime / _fireSettleDuration);
        }

        return 1f + Mathf.Sin(Time.time * _fireFlickerSpeed) * _fireFlickerAmount;
    }

    private void ResetLaserLine()
    {
        _hideTween?.Kill();
        _isFiring = false;
        _laserLineRenderer.enabled = false;
        _laserLineRenderer.widthMultiplier = _laserBaseWidth;
    }

    private void SetLineAlpha(LineRenderer lineRenderer, float alpha)
    {
        Color start = lineRenderer.startColor;
        Color end = lineRenderer.endColor;
        start.a = alpha;
        end.a = alpha;
        lineRenderer.startColor = start;
        lineRenderer.endColor = end;
    }

    private void DrawLine(LineRenderer lineRenderer, Vector2 direction)
    {
        Vector2 startPos = _laserHeadPivot.position;
        Vector2 endPos = startPos + direction * _laserLength;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
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
    }
}
