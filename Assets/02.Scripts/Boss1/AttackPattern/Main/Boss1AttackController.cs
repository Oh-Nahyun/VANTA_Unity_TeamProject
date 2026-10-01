using UnityEngine;

public class Boss1AttackController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] _attackBehaviours;
    
    [SerializeField] private float _chaseDuration = 1f;

    private AttackState _currentState; // 추격 or 공격
    private IBoss1Attack _currentAttack; // 근거리 패턴 or 원거리 패턴

    private bool _isAttackLocked; // 페이즈2 전환 중일 때 공격 못하게 하기

    private float _stateTimer;

    private enum AttackState
    {
        Chase,
        Attack
    }

    private void Start()
    {
        ChangeState(AttackState.Chase);
    }

    private void Update()
    {
        if (_isAttackLocked)
        {
            return;
        }
        
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

    public void LockAttack()
    {
        _isAttackLocked = true;

        if (_currentAttack != null)
        {
            _currentAttack.ExitAttack();
            _currentAttack = null;
        }
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
            ChangeState(AttackState.Chase);
            return;
        }
        
        _currentAttack.UpdateAttack();

        if (!_currentAttack.IsFinished)
        {
            return;
        }
        
        _currentAttack.ExitAttack();
        _currentAttack = null;
        ChangeState(AttackState.Chase);
    }
    
    private IBoss1Attack SelectRandomAttack()
    {
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

            totalWeight += attack.GetWeight();
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

            currentWeight += attack.GetWeight();

            if (randomValue <= currentWeight)
            {
                return attack;
            }
        }

        return null;
    }

    private void ChangeState(AttackState newState)
    {
        _currentState = newState;
        _stateTimer = 0f;
    }
}
