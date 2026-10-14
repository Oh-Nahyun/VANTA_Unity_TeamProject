using System;
using UnityEngine;

public class Boss1MeleePattern : MonoBehaviour, IBoss1Attack
{
    [Header("근거리 공격 가중치")]
    [SerializeField] private float _weight = 10f;
    
    [Header("근접 패턴 시작 가능 거리 (이보다 멀면 근접을 안 고름)")]
    [SerializeField] private float _maxStartDistance = 3.5f;
    
    [Serializable]
    private class MeleeAttackData
    {
        [SerializeField] private MonoBehaviour _attackBehaviour;
        [SerializeField] private float _weight = 1f;
        
        public IBoss1MeleeAttack Attack => _attackBehaviour as IBoss1MeleeAttack;
        
        public float Weight => _weight;
    }

    [Serializable]
    private class PhaseSettings
    {
        [Header("다음 근거리 공격까지의 대기 시간")]
        [SerializeField] private float _cycleInterval = 0.5f;
        
        [Header("근거리 공격 사이클 반복 횟수")]
        [SerializeField] private int _cycleCount = 2;
        
        public float CycleInterval => _cycleInterval;
        public int CycleCount => _cycleCount;
    }
    
    [Header("근거리 공격 하위 패턴")]
    [SerializeField] private MeleeAttackData[]  _meleeAttacks;
    
    [Header("페이즈별 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;

    [Header("보스 이동 기능")]
    [SerializeField] private Boss1Movement _boss1Movement;

    private Transform _playerTransform;

    [Header("근거리 공격 선택")]
    [SerializeField] private float _spitPreferredDistance = 4f;

    [SerializeField] private float _preferredWeightMultiplier = 2f;
    
    [SerializeField] private Boss1PhaseController _boss1PhaseController;

    private bool _isPhase2;

    private int _currentCycleCount;
    private float _intervalTimer;
    
    private bool _isWaitingNextCycle;

    private IBoss1MeleeAttack _currentAttack;

    public bool IsFinished{ get; private set; }

    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    public bool CanUse()
    {
        return Vector2.Distance(_playerTransform.position, transform.position) <= _maxStartDistance;
    }
    
    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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
    
    

    public float GetWeight()
    {
        return _weight;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        _currentCycleCount = 0;
        _isWaitingNextCycle = false;
        _intervalTimer = 0f;
        
        _boss1Movement.StartControlled();
        _currentAttack = SelectAttack();
        
        if (_currentAttack == null)
        {
            IsFinished = true;
            return;
        }
        
        _currentAttack.EnterAttack();
    }

    public void UpdateAttack() //현재 프레임의 근거리 패턴  관리
    {
        if (IsFinished)
        {
            return;
        }

        if (_isWaitingNextCycle)
        {
            UpdateCycleInterval();
            return;
        }

        if (_currentAttack == null)
        {
            IsFinished = true;
            return;
        }
        
        _currentAttack.UpdateAttack();

        if (_currentAttack.IsFinished)
        {
            _currentAttack.ExitAttack();
            _currentAttack = null;

            _currentCycleCount++;

            if (_currentCycleCount >= CurrentPhaseSettings.CycleCount)
            {
                IsFinished = true;
                return;
            }

            _intervalTimer = 0f;
            _isWaitingNextCycle = true;
        }
    }

    public void ExitAttack()
    {
        if (_currentAttack != null)
        {
            _currentAttack.ExitAttack();
            _currentAttack = null;
        }
        
        _isWaitingNextCycle = false;
        _intervalTimer = 0f;
        
        _boss1Movement.StartNormalChase();
    }

    private IBoss1MeleeAttack SelectAttack()
    {
        float xDistance = Mathf.Abs(_playerTransform.position.x - transform.position.x);

        float totalWeight = 0f;

        foreach (MeleeAttackData attackData in _meleeAttacks)
        {
            IBoss1MeleeAttack attack = attackData.Attack;

            if (attackData.Attack == null)
            {
                continue;
            }

            float weight = GetAdjustedWeight(attackData, xDistance);

            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }
        
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        
        float currentWeight = 0f;
        
        foreach (MeleeAttackData attackData in _meleeAttacks)
        {
            IBoss1MeleeAttack attack = attackData.Attack;

            if (attack == null)
            {
                continue;
            }

            float weight = GetAdjustedWeight(attackData, xDistance);

            currentWeight += weight;
            
            if (randomValue <= currentWeight)
            {
                return attackData.Attack;
            }
        }

        return null;
    }

    private float GetAdjustedWeight(MeleeAttackData attackData, float xDistance)
    {
        float weight = attackData.Weight;

        bool isFar = xDistance >= _spitPreferredDistance;

        if (!isFar && attackData.Attack is Boss1ClawAttack)
        {
            weight *= _preferredWeightMultiplier;
        }

        if (isFar && attackData.Attack is Boss1SpitAttack)
        {
            weight *= _preferredWeightMultiplier;
        }

        return weight;
    }

    private void UpdateCycleInterval()
    {
        _intervalTimer += Time.deltaTime;

        if (_intervalTimer < CurrentPhaseSettings.CycleInterval)
        {
            return;
        }
        
        _intervalTimer = 0f;
        _isWaitingNextCycle = false;
        
        if (!CanUse())
        {
            IsFinished = true;
            return;
        }

        _currentAttack = SelectAttack();

        if (_currentAttack == null)
        {
            IsFinished = true;
            return;
        }
        
        _currentAttack.EnterAttack();
    }
}
