using System;
using UnityEngine;

public class Boss1LaserAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Boss1Laser _laser;
    [SerializeField] private Transform _laserHeadPivot;
    private Transform _playerTransform;


    [Serializable]
    private class PhaseSettings
    {
        [Header("조준 시간")]
        [SerializeField] private float _aimDuration = 1f;
        
        [Header("레이저 휩쓰는 각도")]
        [SerializeField] private float _sweepAngle = 60f;
        
        [Header("레이저 휩쓰는 시간")]
        [SerializeField] private float _sweepDuration = 1.2f;
        
        public float AimDuration => _aimDuration;
        public float SweepAngle => _sweepAngle;
        public float SweepDuration => _sweepDuration;
    }
    
    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;
    private Boss1PhaseController _boss1PhaseController;

    private float _targetAngle;
    
    private float _timer;
    private LaserState _currentState;

    private Vector2 _lockedDirection;
    private float _lockedAngle;
    
    private bool _isPhase2;
    
    public bool IsFinished { get; private set; }

    private enum LaserState
    {
        Aim,
        Sweep,
        End
    }
    
    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;
    
    private void Awake()
    {
        _boss1PhaseController = GetComponent<Boss1PhaseController>();
    }
    
    private void OnEnable()
    {
        _boss1PhaseController.OnPhaseChanged += ChangePhase;
    }
    
    private void OnDisable()
    {
        _boss1PhaseController.OnPhaseChanged -= ChangePhase;
    }

    private void ChangePhase(Boss1PhaseController.Boss1Phase phase)
    {
        _isPhase2 = phase == Boss1PhaseController.Boss1Phase.phase2;
    }
    
    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        
        ChangeState(LaserState.Aim);
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

        if (_timer < CurrentPhaseSettings.AimDuration)
        {
            return;
        }

        SetSweepTarget();
        
        ChangeState(LaserState.Sweep);
    }

    private void UpdateSweep()
    {
        _timer += Time.deltaTime;

        float timePercent = Mathf.Clamp01(_timer / CurrentPhaseSettings.SweepDuration);
        
        float currentAngle = Mathf.Lerp(_lockedAngle, _targetAngle, timePercent);
        
        Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
        
        _laser.Fire(direction);
        
        if (_timer < CurrentPhaseSettings.SweepDuration)
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
        _targetAngle = _lockedAngle + (CurrentPhaseSettings.SweepAngle * sweepDirection);
    }
}
