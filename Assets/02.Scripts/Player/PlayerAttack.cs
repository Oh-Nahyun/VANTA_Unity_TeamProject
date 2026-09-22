using DG.Tweening;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("더블 슬래쉬 까지 필요 공격 횟수")][SerializeField]private int _countForDoubleSlash;
    [Header("공격 쿨타임")][SerializeField]private float _attackCoolTime=0.5f;
    [Header("콤보 유지시간")][SerializeField]private float _comboTime=0.5f;
    private int _comboCount=0;
    private Animator _animator;
    private bool _isSlashing;
    private Tween _comboTimer;

    void Start()
    {
        _animator = GetComponent<Animator>();
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
    
}
