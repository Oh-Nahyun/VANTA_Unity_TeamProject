using System;
using UnityEngine;

public class Boss1PhaseController : MonoBehaviour
{
    public enum Boss1Phase
    {
        phase1,
        phase2
    }
    
    [SerializeField] private Boss1Life _boss1Life;
    private Boss1AttackController _boss1AttackController;
    private Boss1Movement _boss1Movement;
    private Boss1ProjectileManager _boss1ProjectileManager;

    private bool _isPhase2Transitioning; // 지금 페이즈 전환을 진행 중인지

    public Boss1Phase CurrentPhase {get; private set;}

    public event Action<Boss1Phase> OnPhaseChanged; // 페이즈 바꾸라고 알림

    private void Awake()
    {
        CurrentPhase = Boss1Phase.phase1;
        
        _boss1AttackController = GetComponent<Boss1AttackController>();
        _boss1Movement = GetComponent<Boss1Movement>();
        _boss1ProjectileManager = GetComponent<Boss1ProjectileManager>();
    }

    private void OnEnable()
    {
        _boss1Life.OnPhase2Triggered += StartPhase2Transition;
    }

    private void OnDisable()
    {
        _boss1Life.OnPhase2Triggered -= StartPhase2Transition;
    }

    private void StartPhase2Transition()
    {
        _isPhase2Transitioning = true;
        
        _boss1AttackController.LockAttack();
        _boss1Movement.Stop();
        _boss1ProjectileManager.DestroyAllProjectiles();
        
        Debug.Log("보스1 페이즈 2 전환 시작");
    }
    
    private void EnterPhase2()
    {
        CurrentPhase = Boss1Phase.phase2;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }
}
