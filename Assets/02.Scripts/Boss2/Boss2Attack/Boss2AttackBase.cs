using UnityEngine;
using UnityEngine.Serialization;

public class Boss2AttackBase : MonoBehaviour
{
    private enum AttackState {NoAttack,Nomal,AreaOfEffect,InstantKill}
    [Header("데미지")]
    private int _baseDamage = 1;
    private int _areaOfEffectDamage = 1;
    private int _instantKillDamage = 1;
    
    [SerializeField] private BoxCollider2D _attackBoxCollider;
    
    
     private void Start()
    {
        if (_attackBoxCollider != null)
        {
            _attackBoxCollider.isTrigger = false;
        }
    }

     private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        _attackBoxCollider.isTrigger = true;
    }

    private void EndAttack()
    {
        _attackBoxCollider.isTrigger = false;
    }
     
}
