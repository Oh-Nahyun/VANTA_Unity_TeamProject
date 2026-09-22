using UnityEngine;

public class Boss2AttackBase : MonoBehaviour
{
    //private enum AttackState {NoAttack,Nomal,AreaOfEffect,InstantKill}
    [Header("데미지")]
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private int _areaOfEffectDamage = 1;
    [SerializeField] private int _instantKillDamage = 1;
    
    [SerializeField] private BoxCollider2D _attackBoxCollider;
    private Animator _animator;

    [SerializeField] private int _comboCounter = 0;
    [SerializeField] private int _maxCombo = 2;
    private bool _isAttacking = false;
    private bool _comboQueued = false;
    
     private void Start()
    {
        if (_attackBoxCollider != null)
        {
            _attackBoxCollider.enabled = false;
        }
        _animator = GetComponent<Animator>();
    }

     private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_isAttacking == false)
            {
                _isAttacking = true;
                StartAttack();
            }
            else
            {
                _comboQueued = true;

            }
        }
    }

    private void StartAttack()
    {
        _comboCounter = (_comboCounter +1)%_maxCombo;
        _animator.SetInteger("AttackCount",_comboCounter);
        _animator.SetTrigger("Attack");
        _attackBoxCollider.enabled = true;
        
        
        
    }

    public void EndAttack()
    {
        _attackBoxCollider.enabled = false;
        // 0보다 작으면 종료, 크면 콤보 
        //if (_comboCounter > 0)
        //{
        //    StartAttack();
        //}
        //else
        //{
        //    
        //}
    }

    public void OnComboTrigger()
    {
        if (_comboQueued == true)
        {
            _comboCounter++;
            _animator.SetTrigger("Attack");
        }
        else
        {
            // 버퍼 비우기 
        }
    }
     
}
