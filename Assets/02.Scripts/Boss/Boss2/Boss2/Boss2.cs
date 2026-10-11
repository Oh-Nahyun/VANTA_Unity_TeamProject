using UnityEngine;
using UnityEngine.Events;

public class Boss2 : Boss
{
    [Header("보스2 스탯")]
    [SerializeField] private int _health = 100;
    [SerializeField] private int _maxHealth;
    private float _cachedAnimationSpeed = 1f;
    
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
    private Boss2Controller _controller;
    // Death 가 이미 시작됐는지 (엔딩 중 애니메이션 이벤트로 다시 불리는 것을 막는다)
    private bool _isDeathStarted;
    
    [Header("사운드")]
    [SerializeField] private GameObject _hitSound;

    [Header("이벤트")]
    public UnityEvent OnFinisher;
    
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
        _controller = GetComponent<Boss2Controller>();
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
                DisableColliders();
                Finisher();
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

    public override void PauseBoss()
    {
        _controller.Pause();
        // 속도 저장
        _cachedAnimationSpeed = _animator.speed;
        _animator.speed = 0;
    }

    public override void ResumeBoss()
    {
        _controller.Resume();
        _animator.speed = _cachedAnimationSpeed;
    }

    private void Finisher()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            PlayerFinisher playerFinisher = playerObject.GetComponent<PlayerFinisher>();
            if (playerFinisher != null)
            {
                PauseBoss();
                playerFinisher.StartFinisher();
            }
        }
    }
    
    /// <summary>
    /// 피니셔가 끝나면(PlayerFinisher._gameClear) 호출된다. 엔딩 연출이 있으면 연출을 먼저 재생하고, 암흑까지 끝난 뒤 마무리한다.
    /// 엔딩 중 Death 애니메이션 끝의 이벤트로 다시 불려도 한 번만 처리한다.
    /// </summary>
    public void Death()
    {
        if (_isDeathStarted)
            return;
        _isDeathStarted = true;
        // 체력 0 이 아닌 다른 경로로 바로 불려도 사망 후에는 판정을 받지 않게 한다.
        DisableColliders();

        Boss2EndingCinematic ending = GetComponent<Boss2EndingCinematic>();
        if (ending != null && ending.Play(FinishDeath))
            return;

        FinishDeath();
    }

    // 사망 후(피니셔·사망 연출 동안) 데미지 판정을 받지 않도록 본체 피격 박스와 공격 박스(AttackZone)의 콜라이더를 모두 끈다.
    // 피니셔와 사망 연출은 콜라이더 없이 보스 위치만 쓰므로 영향이 없다.
    private void DisableColliders()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }
    }

    private void FinishDeath()
    {
        // Effet
        //Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);
        if (OnFinisher != null)
        {
            OnFinisher.Invoke();
        }

        // Destroy
        Destroy(gameObject);
    }

    protected override void HitEvent(int damage)
    {
        if (_isInvincible == true) return;
        TakeDamage(damage);
    }
    
    public void HitSoundPoint()
    {
        if (_hitSound != null)
        {
            _hitSound.GetComponent<SoundGenerator>().ActivateSound();
        }
    }
}
