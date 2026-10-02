using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("플레이어 피격 가능 횟수")][SerializeField] private int _health = 2;
    [Header("플레이어 피격시 무적 시간")][SerializeField] private float _hitCoolTime = 2;
    [Header("플레이어 피격시 vfx")][SerializeField] private GameObject _hitVfx;
    private float _curTime;
    private bool _isHitCoolTime;
    private bool _isHittable=true;

    private Animator _animator;
    private BrokenWidowScreenController _brokenWidowScreenController;

    private void Awake()
    {
      
        _brokenWidowScreenController=GetComponent<BrokenWidowScreenController>();
        _animator = GetComponent<Animator>();
    }
    private void Update()
    {
        CalculateHitCoolTime();

        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage();
        }
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

        Debug.Log("데미지입음");
        _isHitCoolTime = true;

        _health--;
        
        _animator.SetTrigger("Hurt");

        Instantiate(_hitVfx, transform.position, Quaternion.identity);
        _brokenWidowScreenController.ApplyBroken();
        
        if (_health <= 0)
        {
            Death();
        }
    }



    private void Death()
    {

    }
}
