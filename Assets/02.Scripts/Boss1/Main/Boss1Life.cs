using System;
using UnityEngine;

public class Boss1Life : HitableObject
{
    [SerializeField] private int _maxHealth= 10000;
    [SerializeField] private float _phase2HealthPercent = 0.4f;
    
    private int _currentHealth;
    public float HealthRatio => _maxHealth > 0f ? Mathf.Clamp01((float)_currentHealth / (float)_maxHealth) : 0f;

    private bool _isPhase2Triggered; // 2페이즈 시작하라고 전했는지
    private bool _isDead = false;

    public event Action OnPhase2Triggered; // 페이즈2 전환 이벤트 
    public event Action OnDeath; //보스1 죽음 이벤트
    
    private void Awake()
    {   
        _currentHealth = _maxHealth;
    }

    protected override void HitEvent(int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        if (_isDead)
        {
            return;
        }
        _currentHealth = Mathf.Max(_currentHealth - damage, 0);

        Debug.Log($"보스 HP : {_currentHealth} / {_maxHealth}");

        if (_currentHealth <= 0f)
        {
            Die();
        }

        if (_isPhase2Triggered == false && _currentHealth <= _maxHealth * _phase2HealthPercent)
        {
            _isPhase2Triggered = true;
            OnPhase2Triggered?.Invoke();
        }
        
    }

    private void Die()
    {
        if (_isDead)
        {
            return;
        }
        
        _isDead = true;
        Debug.Log("보스1 사망");
        OnDeath?.Invoke();
    }
}
