using UnityEngine;

public class Boss2AttackBase : MonoBehaviour
{
    private enum AttackState {NoAttack,Nomal,AreaOfEffect,InstantKill}
    [Header("데미지")]
    private int _baseDamage = 1;
    private int _areaOfEffectDamage = 1;
    private int _instantKillDamage = 1;
    
    
     private void Start()
    {
        
    }

     private void Update()
    {
        
    }
}
