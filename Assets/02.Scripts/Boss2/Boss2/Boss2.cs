using TMPro;
using UnityEngine;


public class Boss2 : HitableObject
{
    [Header("보스2 스탯")]
    [SerializeField] private int _health = 100;
    [SerializeField] private int _maxHealth;
    
    private Animator _animator;

    // 체력 비율 계산용. 시작 시 체력을 최대 체력으로 본다.
    private bool _isInvincible = false;

    public int Health => _health;
    public float HealthRatio => _maxHealth > 0f ? Mathf.Clamp01((float)_health / (float)_maxHealth) : 0f;
    public bool IsDead => _health <= 0;

    // 실제로 깎인 데미지 값으로 알린다. (컨트롤러가 큰 데미지 판별·체력 선 검사에 쓴다)
    public event System.Action<int> Damaged;

    private Boss2AttackPattern _attackPattern;
    private Boss2SkillPattern _skillPattern;

    // 공격·스킬을 하는 중인지. 이때는 데미지만 받고 피격 모션으로 행동이 끊기지 않는다(슈퍼아머).
    private bool IsActing =>
        (_attackPattern != null && (_attackPattern.IsPatternRunning || _attackPattern.IsAttacking))
        || (_skillPattern != null && _skillPattern.IsSkillRunning);

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _attackPattern = GetComponent<Boss2AttackPattern>();
        _skillPattern = GetComponent<Boss2SkillPattern>();
        _maxHealth = _health;
    }

    public void SetInvincible(bool bInvincible)
    {
        _isInvincible = bInvincible;
    }

public void TakeDamage(int damage)
    {
        // 이미 죽었으면 피격·사망 트리거를 다시 걸지 않는다.
        if (IsDead)
            return;

        if (damage > 0)
        {
            // 행동 중에는 피격 모션을 생략한다. 단, 죽는 한 방은 Hit → Death 전이를 타야 하므로 항상 건다.
            bool isLethal = _health - damage <= 0;
            if (IsActing == false || isLethal)
            {
                _animator.SetTrigger("Hit");
            }

            _health -= damage;
            Damaged?.Invoke(damage);

            if (_health <= 0)
            {
                //Death();
                _animator.SetTrigger("Death");
                // 체력 바를 위해서
                _health = 0;
            }
        }
        else
        {
            Debug.LogWarning("데미지는 음수 불가");
        }
    }

    private void Death()
    {
        // Effet
        //Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);

        // Destroy
        Destroy(gameObject);
    }

    protected override void HitEvent(int damage)
    {
        if (_isInvincible == true) return;
        TakeDamage(damage);
    }
}
