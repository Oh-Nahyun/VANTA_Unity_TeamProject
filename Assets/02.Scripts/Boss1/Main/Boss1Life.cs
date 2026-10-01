using System;
using UnityEngine;

public class Boss1Life : HitableObject
{
    [SerializeField] private float _maxHealth= 10000;
    
    private float _currentHealth;
    public float HelathRatio;
    private bool _isDead = false;
    public event Action OnDeath;
    
    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    protected override void HitEvent(float damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(float damage)
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
