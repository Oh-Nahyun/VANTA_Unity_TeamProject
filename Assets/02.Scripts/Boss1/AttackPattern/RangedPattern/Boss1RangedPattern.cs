using System;
using System.Collections.Generic;
using UnityEngine;

public class Boss1RangedPattern : MonoBehaviour, IBoss1Attack
{
    [Serializable]
    private class RangedAttackData
    {
        [SerializeField] private MonoBehaviour _attackBehaviour;
        [SerializeField] private float _weight = 1f;
        
        public IBoss1RangedAttack Attack => _attackBehaviour as IBoss1RangedAttack;
        
        public float Weight => _weight;
    }

    [Serializable]
    private class PhaseSettings
    {
        [Range(0f, 1f)]
        [SerializeField] private float _overlapChance = 0.2f;

        [SerializeField] private float _overlapCheckDelay = 1f;
        [SerializeField] private float _cycleInterval = 1f;

        [SerializeField] private int _maxConcurrentAttacks = 2;
        [SerializeField] private int _cycleCount = 3;
        
        public float OverlapChance => _overlapChance;
        public float OverlapCheckDelay => _overlapCheckDelay;
        public float CycleInterval => _cycleInterval;
        public int MaxConcurrentAttacks => _maxConcurrentAttacks;
        public int CycleCount => _cycleCount;
    }
    
    [Header("원거리 공격 패턴")]
    [SerializeField] private RangedAttackData[]  _rangedAttacks;
    
    [Header("페이즈별 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;

    private readonly List<IBoss1RangedAttack> _activeAttacks = new();

    private bool _isPhase2;

    private float _cycleTimer;
    private float _intervalTimer;

    private int _currentCycleCount;

    private bool _isOverlapChecked;
    private bool _isWaitingNextCycle;

    public bool IsFinished{ get; private set; }

    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    public bool CanUse()
    {
        return true;
    }

    public float GetWeight()
    {
        return 10f;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;

        _currentCycleCount = 0;
        _activeAttacks.Clear();

        StartCycle();

        Debug.Log("Boss1 원거리 패턴 시작");
    }

    public void UpdateAttack() //현재 프레임의 원거리 패턴 전체 관리
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

        UpdateActiveAttacks();
        UpdateOverlapCheck();

        if (_activeAttacks.Count == 0)
        {
            FinishCycle();
        }
    }

    public void ExitAttack()
    {
        ExitAllActiveAttacks();

        Debug.Log("Boss1 원거리 패턴 종료");
    }

    public void SetPhase2(bool isPhase2)
    {
        _isPhase2 = isPhase2;
    }

    private void StartCycle()
    {
        _cycleTimer = 0f;
        _isOverlapChecked = false;
        _isWaitingNextCycle = false;

        IBoss1RangedAttack attack = SelectRandomAttack();

        if (attack == null)
        {
            IsFinished = true;
            return;
        }

        StartAttack(attack);

        Debug.Log("Boss1 원거리 사이클 시작");
    }

    private void UpdateActiveAttacks() //리스트에 등록된 원거리 패턴 실행
    {
        for (int i = _activeAttacks.Count - 1; i >= 0; i--)
        {
            IBoss1RangedAttack attack = _activeAttacks[i];

            attack.UpdateAttack();

            if (!attack.IsFinished)
            {
                continue;
            }

            attack.ExitAttack();
            _activeAttacks.RemoveAt(i);
        }
    }

    private void UpdateOverlapCheck()
    {
        if (_isOverlapChecked)
        {
            return;
        }

        _cycleTimer += Time.deltaTime;

        if (_cycleTimer < CurrentPhaseSettings.OverlapCheckDelay)
        {
            return;
        }

        _isOverlapChecked = true;

        // 첫 공격이 이미 끝났다면 추가 공격 없음
        if (_activeAttacks.Count == 0)
        {
            return;
        }

        // 최대 동시 공격 수(2개) 판정
        if (_activeAttacks.Count >= CurrentPhaseSettings.MaxConcurrentAttacks)
        {
            return;
        }

        // 겹침 확률 판정 실패
        if (UnityEngine.Random.value > CurrentPhaseSettings.OverlapChance)
        {
            return;
        }

        IBoss1RangedAttack additionalAttack = SelectRandomAttack();

        if (additionalAttack != null)
        {
            StartAttack(additionalAttack);
        }
    }

    private void StartAttack(IBoss1RangedAttack attack)
    {
        attack.EnterAttack();
        _activeAttacks.Add(attack);
    }

    private IBoss1RangedAttack SelectRandomAttack()
    {
        float totalWeight = 0f;

        foreach (RangedAttackData attackData in _rangedAttacks)
        {
            IBoss1RangedAttack attack = attackData.Attack;

            if (attack == null)
            {
                continue;
            }

            // 같은 공격 동시 실행 방지
            if (_activeAttacks.Contains(attack))
            {
                continue;
            }

            totalWeight += attackData.Weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (RangedAttackData attackData in _rangedAttacks)
        {
            IBoss1RangedAttack attack = attackData.Attack;

            if (attack == null)
            {
                continue;
            }

            if (_activeAttacks.Contains(attack))
            {
                continue;
            }

            currentWeight += attackData.Weight;

            if (randomValue <= currentWeight)
            {
                return attack;
            }
        }

        return null;
    }

    private void FinishCycle()
    {
        _currentCycleCount++;

        Debug.Log(
            $"Boss1 원거리 사이클 종료 : {_currentCycleCount}"
        );

        if (_currentCycleCount >= CurrentPhaseSettings.CycleCount)
        {
            IsFinished = true;
            return;
        }

        _intervalTimer = 0f;
        _isWaitingNextCycle = true;
    }

    private void UpdateCycleInterval()
    {
        _intervalTimer += Time.deltaTime;

        if (_intervalTimer < CurrentPhaseSettings.CycleInterval)
        {
            return;
        }

        StartCycle();
    }

    private void ExitAllActiveAttacks()
    {
        foreach (IBoss1RangedAttack attack in _activeAttacks)
        {
            attack.ExitAttack();
        }

        _activeAttacks.Clear();
    }
}
