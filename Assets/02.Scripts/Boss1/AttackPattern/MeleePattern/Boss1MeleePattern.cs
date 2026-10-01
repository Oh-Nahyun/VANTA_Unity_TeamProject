using System;
using System.Collections.Generic;
using UnityEngine;

public class Boss1MeleePattern : MonoBehaviour, IBoss1Attack
{
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
        
    }
    
    [Header("근거리 공격 패턴")]
    [SerializeField] private MeleeAttackData[]  _meleeAttacks;
    
    [Header("페이즈별 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;

    [Header("보스 이동 기능")]
    [SerializeField] private Boss1Movement _boss1Movement;

    [SerializeField] private Transform _playerTransform;

    [Header("근거리 공격 선택")]
    [SerializeField] private float _spitPreferredDistance = 4f;

    [SerializeField] private float _preferredWeightMultiplier = 2f;

    private bool _isPhase2;

    private IBoss1MeleeAttack _currentAttack;

    public bool IsFinished{ get; private set; }

    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    public bool CanUse()
    {
        return true;
    }

    public float GetWeight()
    {
        return 100f;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        
        _boss1Movement.StartControlled();
        _currentAttack = SelectAttack();
        _currentAttack.EnterAttack();

        Debug.Log("Boss1 근거리 패턴 시작");
    }

    public void UpdateAttack() //현재 프레임의 근거리 패턴 전체 관리
    {
        if (IsFinished)
        {
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
            
            IsFinished = true;
        }
    }

    public void ExitAttack()
    {
        _boss1Movement.StartNormalChase();
        
        Debug.Log("Boss1 근거리 패턴 종료");
    }

    public void SetPhase2(bool isPhase2)
    {
        _isPhase2 = isPhase2;
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
}
