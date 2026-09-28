using UnityEngine;
using UnityEngine.XR;

public class Boss1AttackController : MonoBehaviour
{
    private AttackState _currentState;
    private IBoss1Attack _currentAttack;


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
        switch (_currentState)
        {
            case AttackState.Chase:
                UpdateChase();
                break;
            
            case AttackState.Attack:
                UpdateAttack();
                break;
        }
    }
}
