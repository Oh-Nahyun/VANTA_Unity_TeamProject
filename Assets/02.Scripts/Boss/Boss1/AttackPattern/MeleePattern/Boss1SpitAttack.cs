using System;
using UnityEngine;

public class Boss1SpitAttack : MonoBehaviour, IBoss1MeleeAttack
{
    [SerializeField] private Boss1Movement _boss1Movement;
    private Transform _playerTransform;
    
    [Header("공격지점 선정 제한 시간")]
    [SerializeField] private float _maxPositioningTime = 0.5f;
    
    [Header("포지셔닝 중 방향전환 최소 x축 거리")]
    [SerializeField] private float _minFaceDistance = 0.3f;
    
    [Serializable]
    private class PhaseSettings
    {
        [Header("공격전 이동속도 급상승")]
        [SerializeField] private float _moveSpeed = 7f; // 침뱉기 공격전 y축 맞춰야해서 빠르게 설정
        
        [Header("공격전 전조 시간")]
        [SerializeField] private float _telegraphDuration = 0.2f;

        [Header("공격후 후딜레이 시간")]
        [SerializeField] private float _recoveryDuration = 0.4f;
        
        public float MoveSpeed => _moveSpeed;
        public float TelegraphDuration => _telegraphDuration;
        public float RecoveryDuration => _recoveryDuration;
    }

    [SerializeField] private float _minAttackDistance = 1.2f;
    [SerializeField] private float _maxAttackDistance = 3f;
    [SerializeField] private float _yTolerance = 0.5f;
    
    [SerializeField] private Boss1SpitHitBox _spitHitBox;
    
    [SerializeField] private Boss1MeleeTelegraph _telegraphFlash;

    [SerializeField] private Animator _animator;
    
    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;
    
    private Boss1PhaseController _boss1PhaseController;
    
    private float _timer;
    private float _backTimer;
    private float _positioningTimer;

    private SpitState _currentState;
    
    private bool _isAttackActive;
    
    private bool _isPhase2;
    
    private Boss1Sound _boss1Sound;
    
    private enum SpitState
    {
        Positioning,
        Telegraph,
        SpitAttack,
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
        _positioningTimer = 0f;
        _backTimer = 0f;
        _spitHitBox.DisableHitBox();
        _boss1Movement.LockFacing();
        _currentState = SpitState.Positioning;
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }

        switch (_currentState)
        {
            case SpitState.Positioning:
                UpdatePositioning();
                break;
            case SpitState.Telegraph:
                UpdateTelegraph();
                break;
            case SpitState.SpitAttack:
                UpdateSpitAttack();
                break;
            case SpitState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public void ExitAttack()
    {
        _isAttackActive = false;
        
        _spitHitBox.DisableHitBox();
        _boss1Movement.UnlockFacing();
        _boss1Movement.StopMove();   
        
        if (_telegraphFlash != null)
        {
            _telegraphFlash.Stop();
        }
    }

    private void UpdatePositioning()
    {
        float spitY = _spitHitBox.transform.position.y;
        
        float xDifference = _playerTransform.position.x - transform.position.x;
        float yDifference = _playerTransform.position.y - spitY;
        
        if (Mathf.Abs(xDifference) > _minFaceDistance)
        {
            _boss1Movement.FaceTarget(_playerTransform.position);
        }
        
        float xDistance = Mathf.Abs(xDifference);
        float yDistance = Mathf.Abs(yDifference);
        
        if (xDistance < _minAttackDistance)
        {
            _backTimer += Time.deltaTime;
            
            if (_backTimer < _maxPositioningTime)
            {
                float facing = Mathf.Sign(_spitHitBox.transform.position.x - transform.position.x);
                _boss1Movement.MoveInDirection(new Vector2(-facing, 0f), CurrentPhaseSettings.MoveSpeed);
                return;
            }
        }

        if (xDistance > _maxAttackDistance)
        {
            Vector2 direction = new Vector2(xDifference, yDifference);
            _boss1Movement.MoveInDirection(direction, CurrentPhaseSettings.MoveSpeed);
            return;
        }
        
        if (yDistance > _yTolerance)
        {
            _positioningTimer += Time.deltaTime;
            
            if (_positioningTimer < _maxPositioningTime)
            {
                Vector2 direction = new Vector2(0f, Mathf.Sign(yDifference));
                _boss1Movement.MoveInDirection(direction, CurrentPhaseSettings.MoveSpeed);
                return;
            }
        }

        EnterTelegraph();
    }
    
    private void EnterTelegraph()
    {
        _boss1Movement.StopMove();
        _positioningTimer = 0f;
        _backTimer = 0f;
        _timer = 0f;
        _currentState = SpitState.Telegraph;

        if (_telegraphFlash != null)
        {
            _telegraphFlash.Play();
        }
    }

    private void UpdateTelegraph()
    {
        _boss1Movement.StopMove();
        _timer += Time.deltaTime;

        if (_timer >= CurrentPhaseSettings.TelegraphDuration)
        {
            _timer = 0f;
            _animator.SetTrigger("SpitAttack");
            _currentState = SpitState.SpitAttack;
        }
    }

    private void UpdateSpitAttack()
    {
        _boss1Movement.StopMove();
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

    public void EnableSpitHitBox()
    {
        if (!_isAttackActive)
        {
            return;
        }
        
        _spitHitBox.EnableHitBox();
        _boss1Sound.Play(Boss1SoundType.Spit);
    }

    public void DisableSpitHitBox()
    {
        _spitHitBox.DisableHitBox();
    }

    public void EndSpitAttack()
    {
        _spitHitBox.DisableHitBox();
        
        _timer = 0f;
        _currentState = SpitState.Recovery;
    }
}
