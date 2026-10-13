using System;
using Unity.Cinemachine;
using UnityEngine;

public class Boss1ClawAttack : MonoBehaviour, IBoss1MeleeAttack
{
    [SerializeField] private Boss1Movement _boss1Movement;
    private Transform _playerTransform;
    
    [Header("공격지점 선정 제한 시간")]
    [SerializeField] private float _maxPositioningTime = 0.5f;
    
    [Header("위치잡기 전체 제한 시간 (넘으면 공격 포기)")]
    [SerializeField] private float _maxApproachTime = 2f;
    private float _approachTimer;

    [Header("런지 설정")]
    [SerializeField] private float _lungeStartTime = 0.3f; //clawattack 애니메이션 피격 판정 시작 시간
    [SerializeField] private float _lungeEndTime = 0.5f; //clawattack 애니메이션 피격 판정 종료 시간
    [SerializeField] private float _maxLungeDistance = 0.6f;
    
    [Header("공격 애니메이션 중 y추적")]
    [SerializeField] private float _yTrackSpeed = 1.5f; //clawattack 애니메이션중 y추적 속도
    [SerializeField] private float _yTrackEndTime = 0.15f; //clawattack 애니메이션중 y 추적 시간
 
    [Header("포지셔닝 중 방향전환 최소 x축 거리")]
    [SerializeField] private float _minFaceDistance = 0.3f;

    
    [Header("위치잡기 예측 조준")]
    [SerializeField] private float _predictTime = 0.4f;
    [SerializeField] private float _maxPredictDistance = 1.2f;

    private Vector2 _lastPlayerPosition;
    private Vector2 _playerVelocity;
    
    [Serializable]
    private class PhaseSettings
    {
        [Header("공격전 이동속도 급상승")]
        [SerializeField] private float _moveSpeed = 7f; // 근거리 공격전 위치 조절이라서 빠르게 설정
        
        [Header("공격전 전조 시간")]
        [SerializeField] private float _telegraphDuration = 0.2f;
        
        [Header("공격후 후딜레이 시간")]
        [SerializeField] private float _recoveryDuration = 0.4f;
        
        public float MoveSpeed => _moveSpeed;
        public float TelegraphDuration => _telegraphDuration;
        public float RecoveryDuration => _recoveryDuration;
    }

    [SerializeField] private float _minAttackDistance = 2f;
    [SerializeField] private float _maxAttackDistance = 4f;
    [SerializeField] private float _yTolerance = 0.3f;

    [SerializeField] private Boss1ClawHitBox _clawHitBox;
    
    [SerializeField] private Boss1MeleeTelegraph _telegraphFlash;
    
    [SerializeField] private Animator _animator;
    
    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;
    
    [Header("카메라 셰이크")]
    [SerializeField] private CinemachineImpulseSource _clawImpulseSource;
    [SerializeField] private float _impulseForce = 0.15f;
    
    private Boss1PhaseController _boss1PhaseController;
    
    private float _timer;
    private float _positioningTimer;

    private ClawState _currentState;

    private bool _isAttackActive;
    
    private bool _isPhase2;

    private bool _isLungeStarted;
    private float _lungeDirection;
    private float _lungeSpeed;
    
    private Boss1Sound _boss1Sound;
    
    private enum ClawState
    {
        Positioning,
        Telegraph,
        ClawAttack,
        Recovery
    }
    
    public bool IsFinished { get; private set; }
    
    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;
    
    private void Awake()
    {
        _boss1PhaseController = GetComponent<Boss1PhaseController>();
        _boss1Sound = GetComponent<Boss1Sound>();
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
        _isAttackActive = true;
        IsFinished = false;
        _timer = 0f;
        _approachTimer = 0f;
        _positioningTimer = 0f;
        _lastPlayerPosition = _playerTransform.position;
        _playerVelocity = Vector2.zero;
        _clawHitBox.DisableHitBox();
        _boss1Movement.LockFacing();
        _currentState = ClawState.Positioning;
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }
        
        UpdatePlayerVelocity();

        switch (_currentState)
        {
            case ClawState.Positioning:
                UpdatePositioning();
                break;
            case ClawState.Telegraph:
                UpdateTelegraph();
                break;
            case ClawState.ClawAttack:
                UpdateClawAttack();
                break;
            case ClawState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public void ExitAttack()
    {
        _isAttackActive = false;
        
        _clawHitBox.DisableHitBox();
        _boss1Movement.UnlockFacing();
        _boss1Movement.StopMove();   
        
        if (_telegraphFlash != null)
        {
            _telegraphFlash.Stop();
        }
    }

    private void UpdatePositioning()
    {
        float xDifference = _playerTransform.position.x - transform.position.x;
        float yDifference = GetPredictedPlayerY() - transform.position.y;

        if (Mathf.Abs(xDifference) > _minFaceDistance)
        {
            _boss1Movement.FaceTarget(_playerTransform.position);
        }
        
        _approachTimer += Time.deltaTime;
        if (_approachTimer >= _maxApproachTime)
        {
            _boss1Movement.StopMove();
            IsFinished = true; // 휘두르지 않고 끝 → 근접 패턴이 다음으로 넘어감
            return;
        }

        float xDistance = Mathf.Abs(xDifference);
        float yDistance = Mathf.Abs(yDifference);

        if (xDistance < _minAttackDistance)
        {
            _positioningTimer += Time.deltaTime;
            
            if (_positioningTimer >= _maxPositioningTime)
            {
                EnterTelegraph();
                return;
            }
            
            Vector2 direction = new Vector2(-Mathf.Sign(xDifference), 0f).normalized;
            _boss1Movement.MoveInDirection(direction, CurrentPhaseSettings.MoveSpeed);
            return;
        }

        if (xDistance > _maxAttackDistance)
        {
            Vector2 direction = new Vector2(xDifference, yDifference).normalized;
            _boss1Movement.MoveInDirection(direction, CurrentPhaseSettings.MoveSpeed);
            return;
        }

        if (yDistance > _yTolerance)
        {
            Vector2 direction = new Vector2(0f, Mathf.Sign(yDifference)).normalized;
            _boss1Movement.MoveInDirection(direction, CurrentPhaseSettings.MoveSpeed);
            return;
        }
        
        EnterTelegraph();
    }
    
    private void EnterTelegraph()
    {
        _boss1Movement.StopMove();
        _positioningTimer = 0f;
        _timer = 0f;
        _currentState = ClawState.Telegraph;

        if (_telegraphFlash != null)
        {
            _telegraphFlash.Play();
        }
    }

    private void UpdateTelegraph()
    {
        _boss1Movement.Lunge(new Vector2(0f, YTrackVelocity())); // 전조 중엔 y만 따라감
        _timer += Time.deltaTime;

        if (_timer >= CurrentPhaseSettings.TelegraphDuration)
        {
            _timer = 0f;
            _isLungeStarted = false;
            _animator.SetTrigger("ClawAttack");
            _currentState = ClawState.ClawAttack;
        }
    }

    private void UpdateClawAttack()
    {
        _timer += Time.deltaTime;
        
        float xVelocity = 0f;
        float yVelocity = 0f;
        
        if (_timer < _yTrackEndTime)
        {
            yVelocity = YTrackVelocity();
        }

        if (_timer >= _lungeStartTime && _timer <= _lungeEndTime)
        {
            if (_isLungeStarted == false)
            {
                _isLungeStarted = true;
            
                _lungeDirection = Mathf.Sign(_clawHitBox.transform.position.x - transform.position.x);
            
                float playerDistance = (_playerTransform.position.x - transform.position.x) * _lungeDirection;
            
                float hitBoxCenter = Mathf.Abs(_clawHitBox.transform.localPosition.x);
                float lungeDistance = Mathf.Clamp(playerDistance - hitBoxCenter, 0f, _maxLungeDistance);
            
                _lungeSpeed = lungeDistance / (_lungeEndTime - _lungeStartTime);
            }
            
            xVelocity = _lungeDirection * _lungeSpeed;
        }
        
        _boss1Movement.Lunge(new Vector2(xVelocity, yVelocity));
    }

    private void UpdateRecovery()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= CurrentPhaseSettings.RecoveryDuration)
        {
            _timer = 0f;
            IsFinished = true;
        }
    }
    
    private float YTrackVelocity()
    {
        float yDifference = GetPredictedPlayerY() - transform.position.y;

        // 차이에 비례해 따라가되 최대 _yTrackSpeed (가까워지면 저절로 느려져서 떨리지 않음)
        return Mathf.Clamp(yDifference * 10f, -_yTrackSpeed, _yTrackSpeed);
    }
    
    private void UpdatePlayerVelocity()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        Vector2 currentPosition = _playerTransform.position;
        Vector2 frameVelocity = (currentPosition - _lastPlayerPosition) / Time.deltaTime;
        
        _playerVelocity = Vector2.Lerp(_playerVelocity, frameVelocity, 0.3f);
        _lastPlayerPosition = currentPosition;
    }
    
    private float GetPredictedPlayerY()
    {
        float predictY = Mathf.Clamp(_playerVelocity.y * _predictTime, -_maxPredictDistance, _maxPredictDistance);
        return _playerTransform.position.y + predictY;
    }
    
    private void PlayClawImpulse()
    {
        if (_clawImpulseSource == null)
        {
            return;
        }
        
        float facing = Mathf.Sign(_clawHitBox.transform.position.x - transform.position.x);

        // 할퀴는 방향 + 살짝 아래로 내리찍는 느낌
        Vector3 velocity = new Vector3(facing * _impulseForce, -_impulseForce * 0.5f, 0f);
        _clawImpulseSource.GenerateImpulseWithVelocity(velocity);
    }

    public void EnableClawHitBox()
    {
        if (!_isAttackActive)
        {
            return;
        }
        _clawHitBox.EnableHitBox();
        PlayClawImpulse();
        _boss1Sound.Play(Boss1SoundType.Claw);
    }

    public void DisableClawHitBox()
    {
        _clawHitBox.DisableHitBox();
    }

    public void EndClawAttack()
    {
        _clawHitBox.DisableHitBox();
        
        _timer = 0f;
        _currentState = ClawState.Recovery;
    }
}
