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

    public Boss1Phase CurrentPhase {get; private set;}

    public event Action<Boss1Phase> OnPhaseChanged;

    private void Awake()
    {
        CurrentPhase = Boss1Phase.phase1;
    }

    private void OnEnable()
    {
        _boss1Life.OnPhase2Triggered += EnterPhase2;
    }

    private void OnDisable()
    {
        _boss1Life.OnPhase2Triggered -= EnterPhase2;
    }

    private void EnterPhase2()
    {
        CurrentPhase = Boss1Phase.phase2;
        OnPhaseChanged
    }
}
