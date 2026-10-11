using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Boss1JumpAttack : MonoBehaviour, IBoss1Attack
{
    [Header("점프 공격 가중치")]
    [SerializeField] private float _weight = 10f;

    [Header("점프 공격 전 대기 시간")]
    [SerializeField] private float _telegraphDuration = 1f;
    
    [Header("위험범위 표시 후 착지 전 대기 시간")]
    [SerializeField] private float _warningDuration = 0.7f;
    
    [Header("착지 후 회복 시간")]
    [SerializeField] private float _recoveryDuration = 0.5f;
    
    [Header("점프할 때 흔들림")]
    [SerializeField] private float _jumpImpulseForce = 0.15f; // 착지보다 약하게 해야함
    
    [Header("착지 연출")]
    [SerializeField] private float _landImpactTime = 0.1f;
    [SerializeField] private float _landImpulseForce = 0.5f;
    [SerializeField] private float _landLightIntensity = 3f;
    [SerializeField] private float _landLightDuration = 0.3f;
    
    [Header("점프 착지 이벤트")]
    [SerializeField] private UnityEvent _onJumpLanded;

    private Tween _landLightTween;
    
    [SerializeField] private Light2D _landLight;
    [SerializeField] private CinemachineImpulseSource _landImpulseSource;
    private Boss1Movement _boss1Movement;
    private BoxCollider2D _boxCollider2D;
    private Boss1PhaseController _boss1PhaseController;
    
    private Transform _playerTransform;

    [SerializeField] private Transform _visualPivot;
    
    [SerializeField] private Animator _animator;

    [SerializeField] private GameObject _jumpWarningZonePrefab;
    
    private GameObject _jumpWarningZone;
    
    private float _timer;

    private Vector2 _landingPosition;

    private bool _isPhase2;

    private bool _isAscendStarted;
    private bool _isWarningStarted;
    private bool _isLandStarted;
    
    private enum JumpState
    {
        Telegraph,
        Ascend,
        Warning,
        Land,
        Recovery
    }
    
    private JumpState _currentState;
    
    
    public bool IsFinished{ get; private set; }

    private void Awake()
    {
        _boss1Movement = GetComponent<Boss1Movement>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _boss1PhaseController  = GetComponent<Boss1PhaseController>();
        _playerTransform = GameObject.FindWithTag("Player").transform;
        
        if (_landLight != null)
        {
            _landLight.intensity = 0f;
        }
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
    
    public bool CanUse()
    {
        return _isPhase2;
    }
    
    public float GetWeight()
    {
        return _weight;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        
        _timer = 0f;
        _isAscendStarted = false;
        _isWarningStarted = false;
        _isLandStarted = false;
        
        _boss1Movement.Stop();
        _currentState = JumpState.Telegraph;
    }

    public void UpdateAttack() //현재 프레임의 점프 공격 전체 관리
    {
        if (IsFinished)
        {
            return;
        }

        switch(_currentState)
        {
            case JumpState.Telegraph:
                UpdateTelegraph();
                break;
            case JumpState.Ascend:
                UpdateAscend();
                break;
            case JumpState.Warning:
                UpdateWarning();
                break;
            case JumpState.Land:
                UpdateLand();
                break;
            case JumpState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public void ExitAttack()
    {
        Debug.Log($"[{Time.frameCount}] Jump Exit 시작");
        
        _boss1Movement.StartNormalChase();
        
        Debug.Log($"[{Time.frameCount}] Jump Exit 끝");
    }

    private void UpdateTelegraph()
    {
        _timer += Time.deltaTime;

        if (_timer >= _telegraphDuration)
        {
            _timer = 0f;
            _currentState = JumpState.Ascend;
        }
    }

    private void UpdateAscend()
    {
        if (_isAscendStarted == false)
        {
            _animator.SetTrigger("JumpAttackAscend");
            _isAscendStarted = true;
        }
        
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        
        if (stateInfo.IsName("JumpAttackAscend") && stateInfo.normalizedTime >= 1f)
        {
            _visualPivot.gameObject.SetActive(false);
            _boxCollider2D.enabled = false;
            
            _isAscendStarted = false;
            _currentState = JumpState.Warning;
        }
    }

    private void UpdateWarning()
    {
        if (_isWarningStarted == false)
        {
            
            _landingPosition = _playerTransform.position;
            _jumpWarningZone = Instantiate(_jumpWarningZonePrefab, _landingPosition, Quaternion.identity);
            
            Boss1WarningZoneVFX zoneVFX = _jumpWarningZone.GetComponent<Boss1WarningZoneVFX>();
            
            if (zoneVFX != null)
            {
                zoneVFX.Play(_warningDuration + _landImpactTime); // 착지 프레임에 딱 가득 차게
            }
            
            _isWarningStarted = true;
        }
        
        _timer += Time.deltaTime;

        if (_timer >= _warningDuration)
        {   
            _timer = 0f;
            _isWarningStarted = false;
            _currentState = JumpState.Land;
        }
        
    }

    private void UpdateLand()
    {
        if (_isLandStarted == false)
        {
            transform.position = _landingPosition;
            _visualPivot.gameObject.SetActive(true);
            
            _animator.SetTrigger("JumpAttackLand");
            
            _isLandStarted = true;
        }
    }

    private void UpdateRecovery()
    {
        _timer += Time.deltaTime;

        if (_timer >= _recoveryDuration)
        {
            _timer = 0f;
            IsFinished = true;
        }
    }

    public void JumpLandImpact()
    {
        if (_jumpWarningZone != null)
        {
            Boss1JumpWarningZone warningZone = _jumpWarningZone.GetComponent<Boss1JumpWarningZone>();
            warningZone.AttackPlayer();

            Boss1WarningZoneVFX zoneVFX = _jumpWarningZone.GetComponent<Boss1WarningZoneVFX>();
            if (zoneVFX != null)
            {
                zoneVFX.Remove();
            }
            else
            {
                Destroy(_jumpWarningZone);
            }

            _jumpWarningZone = null;
        }

        _boxCollider2D.enabled = true;

        PlayLandImpulse();
        PlayLandLight();
        
        _onJumpLanded?.Invoke();
    }
    
    private void PlayLandImpulse()
    {
        if (_landImpulseSource == null)
        {
            return;
        }
        _landImpulseSource.GenerateImpulseWithVelocity(Vector3.down * _landImpulseForce);
    }

    private void PlayLandLight()
    {
        if (_landLight == null)
        {
            return;
        }
        _landLightTween?.Kill();
        _landLight.intensity = _landLightIntensity;
        _landLightTween = DOTween.To(() => _landLight.intensity, x => _landLight.intensity = x, 0f, _landLightDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
    }
    
    public void JumpImpulse()
    {
        if (_landImpulseSource == null)
        {
            return;
        }
        // 위로 박차고 오르는 방향
        _landImpulseSource.GenerateImpulseWithVelocity(Vector3.up * _jumpImpulseForce);
    }

    public void EndJumpLand()
    {
        _isLandStarted = false;
        _timer = 0f;
        _currentState = JumpState.Recovery;
    }
}
