using UnityEngine;

public class Boss1LaserAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Boss1Laser _laser;
    [SerializeField] private Transform _laserHeadPivot;
    [SerializeField] private Transform _playerTransform;


    [SerializeField] private float _aimDuration = 1f;
    [SerializeField] private float _sweepAngle = 60f;
    [SerializeField] private float _sweepDuration = 1.2f;

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
        _laser.Hide();
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
        
        _laser.ShowAim(_lockedDirection);

        if (_timer < _aimDuration)
        {
            return;
        }

        SetSweepTarget();
        
        ChangeState(LaserState.Sweep);
    }

    private void UpdateSweep()
    {
        _timer += Time.deltaTime;

        float timePercent = Mathf.Clamp01(_timer / _sweepDuration);
        
        float currentAngle = Mathf.Lerp(_lockedAngle, _targetAngle, timePercent);
        
        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        _laser.Fire(direction);
        
        if (_timer < _sweepDuration)
        {
            return;
        }
        
        ChangeState(LaserState.End);
        
    }

    private void ChangeState(LaserState newState)
    {
        _currentState = newState;
        _timer = 0f;

        if (_currentState == LaserState.End)
        {
            _laser.Hide();
        }
    }

    private void SetSweepTarget()
    {
        Vector2 playerDirection = (_playerTransform.position - _laserHeadPivot.position).normalized;
        float playerAngle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
        
        float angleDifference = Mathf.DeltaAngle(_lockedAngle, playerAngle);
        
        float sweepDirection = angleDifference >= 0f ? 1f : -1f;
        _targetAngle = _lockedAngle + (_sweepAngle * sweepDirection);
    }
}
