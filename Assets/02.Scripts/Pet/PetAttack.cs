using System;
using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격력")]
    [SerializeField] private float _attackPower = 10f;
    
    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// [Test] 펫 공격 타겟
    /// </summary>
    private GameObject _monster;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
    }

    private void Update()
    {
        if (_monster == null)
        {
            _monster = GameObject.FindGameObjectWithTag("Monster");
        }
    }

    private void Attack()
    {
        _pet.Animator.SetTrigger("IsAttack");
        // _monster.TakeDamage(_attackPower);
    }
}
