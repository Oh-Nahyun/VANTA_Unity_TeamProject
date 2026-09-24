using System;
using UnityEngine;

public class PetEnergyBullet : MonoBehaviour
{
    [Header("펫 에너지 탄 이동 속도")]
    [SerializeField] private float _energyBulletMoveSpeed = 5f;
    
    [Header("펫 에너지 탄 공격력")]
    [SerializeField] private float _energyBulletDamage = 10f;
    
    /// <summary>
    /// 펫 에너지 탄 스폰 시 이동 전 대기 시간 계산용 변수
    /// </summary>
    private float _timer;
    
    /// <summary>
    /// 펫 에너지 탄이 이동하기 전에 필요한 대기 시간
    /// </summary>
    private float _timeBeforeMove;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    private GameObject _monster;

    private void Start()
    {
        _timeBeforeMove = 0.6f;
    }

    private void Update()
    {
        if (_monster == null)
        {
            _monster = GameObject.FindGameObjectWithTag("Monster");

            if (_monster == null) return;
        }
        
        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeMove)
        {
            Move();
        }
    }

    private void Move()
    {
        Vector2 direction = _monster.transform.position - transform.position;
        transform.Translate((Vector3)(direction.normalized * _energyBulletMoveSpeed) * Time.deltaTime, Space.World);
    }
}
