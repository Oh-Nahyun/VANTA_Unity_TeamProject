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

    private bool _isPhase2;

    public bool IsFinished{ get; private set; }

    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    public bool CanUse()
    {
        return true;
    }

    public float GetWeight()
    {
        return 1f;
    }
    
    public void EnterAttack()
    {
        IsFinished = false;
        
        _boss1Movement.StartControlled();

        Debug.Log("Boss1 근거리 패턴 시작");
    }

    public void UpdateAttack() //현재 프레임의 근거리 패턴 전체 관리
    {
        if (IsFinished)
        {
            return;
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
}
