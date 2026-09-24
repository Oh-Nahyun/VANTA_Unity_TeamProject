using System;
using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 에너지 탄")]
    [SerializeField] private GameObject _petEnergyBulletPrefab;
    
    [Header("펫 에너지 탄 생성 위치")]
    [SerializeField] private Transform _petEnergyBulletSpawnPosition;
    
    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
    }

    private void Start()
    {
        Fire();
    }

    private void Fire()
    {
        _pet.Animator.SetTrigger("IsFire");
        Instantiate(_petEnergyBulletPrefab, _petEnergyBulletSpawnPosition.position, transform.rotation);
        
        // TODO : [펫] 발동 조건(플레이어 상태) 설정 및 몬스터 데미지 처리하기
        // _monster.TakeDamage(_energyBulletDamage);
    }
}
