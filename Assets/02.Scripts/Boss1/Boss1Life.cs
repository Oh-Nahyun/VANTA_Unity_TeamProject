using System;
using UnityEngine;

public class Boss1Life : MonoBehaviour
{
    [SerializeField] private int _maxHealth= 1000;
    
    private int _currentHealth;
    private bool _isDead = false;
    public event Action OnDeath;
    
    
    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Max(_currentHealth - damage, 0);
        Debug.Log($"보스 HP : {_currentHealth} / {_maxHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("보스1 사망");
        OnDeath?.Invoke();
    }
}
