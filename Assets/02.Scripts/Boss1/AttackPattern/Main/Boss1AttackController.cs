using UnityEngine;

public class Boss1AttackController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] _attackBehaviours;
    
    [Header("플레이어 추격 시간")]
    [SerializeField] private float _chaseDuration = 1.5f;
    
    [Header("공격 패턴 가중치 증감 판단용 플레이어 거리")]
    [SerializeField] private float _distanceThreshold = 5f;
    
    [Header("플레이어 거리에 따른 공격 패턴 가중치 보정")]
    [SerializeField] private float _meleeWeightBonus = 5f;
    [SerializeField] private float _rangedWeightBonus = 5f;

    [Header("원거리 패턴 미선택 가중치 보정")]
    [SerializeField] private float _rangedMissBonusAmount = 2f;
    [Header("원거리 패턴 미선택 가중치 보정 최대치")]
    [SerializeField] private float _maxRangedMissBonus = 8f;
    
    [Header("점프 공격 가중치 보정 최소 요구 시간")]
    [SerializeField] private float _jumpBonusStartTime = 3f;
    [Header("점프 공격 가중치 보정 초당 가중치 증가량")]
    [SerializeField] private float _jumpBonusPerSecond = 2f;
    [Header("점프 공격 가중치 보정 최대치")]
    [SerializeField] private float _maxJumpBonus = 10f;
    [Header("점프 공격 미선택 가중치 보정")]
    [SerializeField] private float _jumpMissBonusAmount = 1f;
    [Header("점프 공격 미선택 가중치 보정 최대치")]
    [SerializeField] private float _maxJumpMissBonus = 4f;

    private float _playerFarTimer;
    private float _rangedMissBonus;
    private float _jumpMissBonus;
    
    private Transform _playerTransform;
    private Boss1Movement _boss1Movement;
    
    private AttackState _currentState; // 추격 or 공격
    private IBoss1Attack _currentAttack; // 근거리 패턴 or 원거리 패턴 or 점프 공격
    
    private bool _isBattleStarted = false; // 보스 등장 인트로 후에 이거 true여야지, 보스 패턴 실행함
    private bool _isAttackLocked; // 페이즈2 전환 중일 때 공격 못하게 하기

    private float _stateTimer; // Chase를 얼마나 하고 있는지

    private enum AttackState
    {
        Chase,
        Attack
    }

    private void Awake()
    {
        _boss1Movement = GetComponent<Boss1Movement>();
    }

    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (!_isBattleStarted)
        {
            return;
        }
        
        if (_isAttackLocked)
        {
            return;
        }

        UpdatePlayerFarTimer();
        
        switch (_currentState)
        {
            case AttackState.Chase:
                UpdateChase();
                break;

            case AttackState.Attack:
                UpdateCurrentAttack();
                break;
        }
    }
    
    public void StartBattle()
    {
        if (_isBattleStarted)
        {
            return;
        }
        
        _isBattleStarted = true;
        
        EnterChaseState();
    }

    public void LockAttack()
    {
        _isAttackLocked = true;

        if (_currentAttack != null)
        {
            _currentAttack.ExitAttack();
            _currentAttack = null;
        }
        
        _playerFarTimer = 0f;
        _rangedMissBonus = 0f;
        _jumpMissBonus = 0f;
        
        ChangeState(AttackState.Chase);
    }

    public void UnlockAttack()
    {
        _isAttackLocked = false;
    }
    

    /// <summary>
    /// 공격 사이의 추격 상태
    /// </summary>
    private void UpdateChase()
    {
        _stateTimer += Time.deltaTime;

        if (_stateTimer < _chaseDuration)
        {
            return;
        }

        _currentAttack = SelectRandomAttack();

        if (_currentAttack == null) return;
        
        ChangeState(AttackState.Attack);
        _currentAttack.EnterAttack();
    }

    /// <summary>
    /// 현재 선택된 공격 실행 상태
    /// </summary>
    private void UpdateCurrentAttack()
    {
        if (_currentAttack == null)
        {
            EnterChaseState();
            return;
        }
        
        _currentAttack.UpdateAttack();

        if (!_currentAttack.IsFinished)
        {
            return;
        }

        if (_currentAttack is Boss1JumpAttack)
        {
            _playerFarTimer = 0f;
        }
        
        _currentAttack.ExitAttack();
        _currentAttack = null;
        EnterChaseState();
    }

    private void UpdatePlayerFarTimer()
    {
        float distance = Vector2.Distance(_playerTransform.position, transform.position);

        if (distance > _distanceThreshold)
        {
            _playerFarTimer += Time.deltaTime;
        }
        else
        {
            _playerFarTimer = 0f;
        }
    }

    private float GetAdjustedWeight(IBoss1Attack attack, float distance)
    {
        float weight = attack.GetWeight();

        if (distance <= _distanceThreshold && attack is Boss1MeleePattern)
        {
            weight += _meleeWeightBonus;
        }

        if (distance > _distanceThreshold && attack is Boss1RangedPattern)
        {
            weight += _rangedWeightBonus;
        }

        if (attack is Boss1RangedPattern)
        {
            weight += _rangedMissBonus;
        }

        if (attack is Boss1JumpAttack)
        {
            weight += _jumpMissBonus;

            if (_playerFarTimer >= _jumpBonusStartTime)
            {
                float jumpBonus = (_playerFarTimer - _jumpBonusStartTime) * _jumpBonusPerSecond;
            
                jumpBonus = Mathf.Min(jumpBonus, _maxJumpBonus);
            
                weight += jumpBonus;
            }
        }

        return weight;
    }
    
    private IBoss1Attack SelectRandomAttack()
    {
        float distance = Vector2.Distance(_playerTransform.position, transform.position);
        
        float totalWeight = 0f;

        foreach (MonoBehaviour behaviour in _attackBehaviours)
        {
            IBoss1Attack attack = behaviour as IBoss1Attack;

            if (attack == null)
            {
                continue;
            }

            if (!attack.CanUse())
            {
                continue;
            }

            totalWeight += GetAdjustedWeight(attack, distance);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (MonoBehaviour behaviour in _attackBehaviours)
        {
            IBoss1Attack attack = behaviour as IBoss1Attack;

            if (attack == null)
            {
                continue;
            }

            if (!attack.CanUse())
            {
                continue;
            }

            currentWeight += GetAdjustedWeight(attack, distance);

            if (randomValue <= currentWeight)
            {
                UpdatePatternBonus(attack);
                return attack;
            }
        }

        return null;
    }

    private void UpdatePatternBonus(IBoss1Attack selectedAttack)
    {
        if (selectedAttack is Boss1RangedPattern)
        {
            _rangedMissBonus = 0f;
        }

        else
        {
            _rangedMissBonus += _rangedMissBonusAmount;
            _rangedMissBonus = Mathf.Min(_rangedMissBonus, _maxRangedMissBonus);
        }

        if (selectedAttack is Boss1JumpAttack)
        {
            _jumpMissBonus = 0f;
            _playerFarTimer = 0f;
        }

        else
        {
            _jumpMissBonus += _jumpMissBonusAmount;
            _jumpMissBonus = Mathf.Min(_jumpMissBonus, _maxJumpMissBonus);
        }
    }
    
    private void EnterChaseState()
    {
        ChangeState(AttackState.Chase);
        _boss1Movement.StartNormalChase();
    }
    
    private void ChangeState(AttackState newState)
    {
        _currentState = newState;
        _stateTimer = 0f;
    }
}
