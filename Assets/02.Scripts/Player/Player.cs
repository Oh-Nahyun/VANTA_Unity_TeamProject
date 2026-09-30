using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("플레이어 피격 가능 횟수")][SerializeField] private int _health = 2;
    [Header("플레이어 피격시 무적 시간")][SerializeField] private float _hitCoolTime = 2;
    private float _curTime;
    private Animator _animator;
    private bool _isHitCoolTime;
    private bool _isHittable;

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
        if (_isHitCoolTime)
        {
            _curTime += Time.deltaTime;
            if (_curTime >= _hitCoolTime)
            {
                _isHitCoolTime = false;
            }
        }
    }

    public void OnPlayerHittable()
    {
        _isHittable = true;
    }

    public void OffPlayerHittable()
    {
        _isHittable = false;
    }

    public void TakeDamage()
    {
        if (_isHitCoolTime||!_isHittable)
        {
            return;
        }

        _isHitCoolTime = true;

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
