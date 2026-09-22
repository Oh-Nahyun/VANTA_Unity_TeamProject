using DG.Tweening;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("더블 슬래쉬 까지 필요 공격 횟수")][SerializeField]private int _countForDoubleSlash;
    [Header("공격 쿨타임")][SerializeField]private float _attackCoolTime=0.5f;
    [Header("콤보 유지시간")][SerializeField]private float _comboTime=0.5f;

    [Header("공격 거리")]
    [SerializeField] private float _attackDistance = 0.2f;

    [Header("공격 범위")]
    [SerializeField] private float _attackRadius = 1f;
    private int _comboCount=0;
    private Animator _animator;
    private bool _isSlashing;
    private Tween _comboTimer;
    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer=GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        SlashInput();
    }

    private void SlashInput()
    {
        if (Input.GetButton("Fire1")&&!_isSlashing)
        {
            if (_countForDoubleSlash <= _comboCount)
            {
                _comboCount=0;
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
        DOVirtual.DelayedCall(_attackCoolTime, () =>
        {
            _isSlashing = false;
        });

    }

    private void ExecuteComboTime()
    {
        _comboTimer?.Kill();

        _comboTimer = DOVirtual.DelayedCall(_attackCoolTime+_comboTime, () =>
        {
            if (_comboCount > 0)
            {
                _comboCount  = 0;
            }
        });
    }

    public void ExamineAttack()
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
        
        
        foreach (Collider2D hit in hits)
        {
            hit.GetComponent<HitableObject>()?.ExecuteHitEvent();
        }
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
}
