using DG.Tweening;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{


    [Header("더블 슬래쉬 까지 필요 공격 횟수")][SerializeField] private int _countForDoubleSlash;
    [Header("최소공격 쿨타임")][SerializeField] private float _minAttackCoolTime = 0.3f;
    [Header("최대공격 쿨타임")][SerializeField] private float _maxAttackCoolTime = 0.7f;

    [Header("더블 슬래시용 콤보 유지시간")][SerializeField] private float _doubleSlashComboTime = 0.5f;
    [Header("공격 속도 증가용 콤보 유지시간")][SerializeField] private float _attackSpeedUpComboTime = 0.5f;

    [Header("공격 거리")]
    [SerializeField] private float _attackDistance = 0.2f;
    [Header("공격 범위")]
    [SerializeField] private float _attackRadius = 1f;

    [Header("공격한 콤보수")]
    [SerializeField] private int _attackComboCount = 0;

    public int AttackComboCount => _attackComboCount;

    [Header("슬래시 데미지")][SerializeField] private float _slashDamage = 50;
    [Header("더블 슬래시 데미지")][SerializeField] private float _doubleSlashDamage = 75;


    [Header("슬래시 공격 이팩트")]
    [SerializeField] private GameObject _slashEffect;
    [Header("더블 슬래시 공격 이팩트")]
    [SerializeField] private GameObject _doubleSlashEffect;

    [Header("참조")]
    [SerializeField] private SkillIndicatorController _skillIndicatorController;
    [SerializeField]private SkillManager _skillManager;
    
    private int _comboCount = 0;
    private bool _isSlashing;
    private Tween _comboTimer;
    private Tween _damageComboTimer;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    
    


    void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        _skillManager = GetComponentInChildren<SkillManager>();
        _skillIndicatorController = GetComponentInChildren<SkillIndicatorController>();
    }

    void Update()
    {
        SlashInput();
    }

    private void SlashInput()
    {
        if (Input.GetButton("Fire1") && !_isSlashing)
        {
            if (_countForDoubleSlash <= _comboCount)
            {
                _comboCount = 0;
                DoubleSlash();
            }
            else
            {
                _comboCount++;
                Slash();
            }

        }
    }

    private void Slash()
    {
        _isSlashing = true;
        _animator.SetTrigger("Slash");
        ExecuteComboTime();
        ExecuteCoolTime();
    }

    private void DoubleSlash()
    {
        _isSlashing = true;
        _animator.SetTrigger("DoubleSlash");

        ExecuteCoolTime();
    }

    private void ExecuteCoolTime()
    {
        DOVirtual.DelayedCall(GetCooltime(), () =>
        {
            _isSlashing = false;
        });

    }

    private void ExecuteComboTime()
    {
        _comboTimer?.Kill();

        _comboTimer = DOVirtual.DelayedCall(GetCooltime() + _doubleSlashComboTime, () =>
        {
            if (_comboCount > 0)
            {
                _comboCount = 0;
            }
        });
    }

    private float GetCooltime()
    {
        float comboRatio = Mathf.Clamp01(_attackComboCount / 20f);

        float coolTime = Mathf.Lerp(
            _maxAttackCoolTime,
            _minAttackCoolTime,
            comboRatio
        );
        return coolTime;
    }

    public void ExamineSlash()
    {
        Collider2D[] hits = ShotTraceForAttackExamine();


        foreach (Collider2D hit in hits)
        {
            HitableObject hitObject = hit.GetComponent<HitableObject>();
            if (hitObject != null)
            {
                hitObject.ExecuteHitEvent(_slashDamage, DamageIndicatorType.Normal);
                Instantiate(_slashEffect, hit.transform.position, Quaternion.identity);
                _skillIndicatorController.CommandToPetAttack();

                StackforSkillPoint();
            }
        }
    }

    public void ExamineDoubleSlash()
    {
        Collider2D[] hits = ShotTraceForAttackExamine();

        foreach (Collider2D hit in hits)
        {
            HitableObject hitObject = hit.GetComponent<HitableObject>();
            if (hitObject != null)
            {
                hitObject.ExecuteHitEvent(_doubleSlashDamage, DamageIndicatorType.Middle);
                Instantiate(_doubleSlashEffect, hit.transform.position, Quaternion.identity);
                _skillIndicatorController.CommandToPetAttack();
                PlayerPetCaller.Instance.CallPetToFire();

                StackforSkillPoint();
            }

        }
    }

    private Collider2D[] ShotTraceForAttackExamine()
    {
        Vector2 direction = _spriteRenderer.flipX
            ? Vector2.left
            : Vector2.right;

        Vector2 attackPosition =
            (Vector2)transform.position + direction * _attackDistance;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition,
            _attackRadius
        );

        DrawDebugCircle(attackPosition, _attackRadius, 1f);

        return hits;
    }

    private void DrawDebugCircle(Vector2 center, float radius, float duration)
    {
        int segments = 32;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2f;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector2 point1 = center + new Vector2(
                Mathf.Cos(angle1),
                Mathf.Sin(angle1)
            ) * radius;

            Vector2 point2 = center + new Vector2(
                Mathf.Cos(angle2),
                Mathf.Sin(angle2)
            ) * radius;

            Debug.DrawLine(point1, point2, Color.red, duration);
        }
    }

    private void StackforSkillPoint()
    {
        _skillManager.CalHitCount();
    }
    
    
}
