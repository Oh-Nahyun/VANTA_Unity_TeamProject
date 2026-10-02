using UnityEngine;

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
    
    private Boss1Movement _boss1Movement;
    private BoxCollider2D _boxCollider2D;
    
    private Transform _playerTransform;

    [SerializeField] private Transform _visualPivot;
    
    [SerializeField] private Animator _animator;

    [SerializeField] private GameObject _jumpWarningZonePrefab;
    
    private GameObject _jumpWarningZone;
    
    private float _timer;

    private Vector2 _landingPosition;

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
    
    public bool CanUse()
    {
        return true;
    }

    private void Awake()
    {
        _boss1Movement = GetComponent<Boss1Movement>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _playerTransform = GameObject.FindWithTag("Player").transform;
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
        _boss1Movement.StartNormalChase();
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
        
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        
        if (stateInfo.IsName("JumpAttackLand") && stateInfo.normalizedTime >= 1f)
        {
            _isLandStarted = false;
            _timer = 0f;
            _currentState = JumpState.Recovery;
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
        Boss1JumpWarningZone warningZone = _jumpWarningZone.GetComponent<Boss1JumpWarningZone>();
        warningZone.AttackPlayer();
        
        Destroy(_jumpWarningZone);
        _boxCollider2D.enabled = true;
    }
}
