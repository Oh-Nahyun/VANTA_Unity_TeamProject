using System;
using UnityEngine;

public class Boss1Life : HitableObject
{
    [SerializeField] private int _maxHealth= 10000;
    [SerializeField] private float _phase2HealthPercent = 0.4f;
    [SerializeField] private GameObject _boss2;
    
    private Boss1HitFlash _boss1HitFlash;
    
    private int _currentHealth;
    public float HealthRatio => _maxHealth > 0f ? Mathf.Clamp01((float)_currentHealth / (float)_maxHealth) : 0f;

    private bool _isPhase2Triggered; // 2페이즈 시작하라고 전했는지
    private bool _isDead = false;

    public event Action OnPhase2Triggered; // 페이즈2 전환하라고 알림 
    public event Action OnDeath; //보스1 죽었따고 알림
    
    private void Awake()
    {   
        _currentHealth = _maxHealth;
        _boss1HitFlash = GetComponent<Boss1HitFlash>();
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
        _boss1HitFlash.PlayHitFlash();

        if (_currentHealth <= 0f)
        {
            Die();
            return;
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
        
        _boss2.SetActive(true); // 임시: 보스2 활성화
        
        OnDeath?.Invoke(); // 지금은 역할 없음, 나중에 사망 연출 때 추가
        
        Destroy(gameObject); // 임시: 보스1 파괴
    }
}
