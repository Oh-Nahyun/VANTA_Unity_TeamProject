using UnityEngine;

public class Boss1LaserAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Transform _laserHeadPivot;
    [SerializeField] private Transform _playerTransform;


    [SerializeField] private float _aimDuration = 1f;
    [SerializeField] private float _sweepAngle = 60f;
    [SerializeField] private float _sweepDuration = 1.2f;
    
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _laserLength = 20f;

    private float _targetAngle;
    
    private float _timer;
    private LaserState _currentState;

    private Vector2 _lockedDirection;
    private float _lockedAngle;
    
    public bool IsFinished { get; private set; }

    private enum LaserState
    {
        Aim,
        Sweep,
        End
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        _timer = 0f;
        
        ChangeState(LaserState.Aim);

        Debug.Log("Laser 패턴 시작");
    }

    public void UpdateAttack()
    {
        switch(_currentState)
        {
            case LaserState.Aim:
                UpdateAim();
                break;
            case LaserState.Sweep:
                UpdateSweep();
                break;
            case LaserState.End:
                IsFinished = true;
                break;
        }
    }

    public void ExitAttack()
    {
        Debug.Log("Laser 패턴 종료");
    }

    private void UpdateAim()
    {
        if (_timer == 0f)
        {
            _lockedDirection = (_playerTransform.position - _laserHeadPivot.position).normalized;
            _lockedAngle = Mathf.Atan2(_lockedDirection.y, _lockedDirection.x) * Mathf.Rad2Deg;
        }
        
        _timer += Time.deltaTime;
        
        Vector2 startPos = _laserHeadPivot.position;
        Vector2 endPos = startPos + _lockedDirection * _laserLength;
        
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);

        if (_timer < _aimDuration)
        {
            return;
        }
        
        Vector2 
        
        ChangeState(LaserState.Sweep);
    }

    private void UpdateSweep()
    {
        _timer += Time.deltaTime;

        float timePercent = Mathf.Clamp01(_timer / _sweepDuration);
        
        float currentAngle = Mathf.Lerp(_lockedAngle, _targetAngle, timePercent);
        
        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        Vector2 startPos = _laserHeadPivot.position;
        Vector2 endPos = startPos + direction * _laserLength;
        
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);

        if (_timer < _sweepDuration)
        {
            return;
        }
        
        _lineRenderer.enabled = false;
        
        ChangeState(LaserState.End);
        
    }

    private void ChangeState(LaserState newState)
    {
        _currentState = newState;
        _timer = 0f;
    }
    
}
