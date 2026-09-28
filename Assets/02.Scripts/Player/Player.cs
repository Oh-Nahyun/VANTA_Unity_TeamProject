using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("플레이어 피격 가능 횟수")][SerializeField]private int _health=2;
    [Header("플레이어 피격시 무적 시간")][SerializeField]private float _hitCoolTime=2;
    private float _curTime;
    private Animator _animator;
    private bool _isHited;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }


    private void Update()
    {
        CalculateHitCoolTime();
    }

    private void CalculateHitCoolTime()
    {
        if (_isHited)
        {
            _curTime += Time.deltaTime;
            if (_curTime >= _hitCoolTime)
            {
                _isHited = false;
            }
        }
    }

    public void TakeDamage()
    {
        if (_isHited)
        {
            return;
        }

        _isHited = true;
        
        _health--;
        _animator.SetTrigger("Hurt");
        
        if (_health <= 0)
        {
            Death();
        }
    }

    
    
    private void Death()
    {
        
    }
}
