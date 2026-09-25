using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 에너지 탄 프리팹")]
    [SerializeField] private GameObject _energyBulletPrefab;
    
    [Header("펫 에너지 탄 생성 위치")]
    [SerializeField] private Transform _energyBulletSpawnPosition;
    
    [Header("펫 에너지 탄 생성 갯수")]
    [SerializeField] private int _energyBulletCount = 1;
    
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
        // TODO : [펫] 에너지 탄 발사 조건은 플레이어 상태 고려해서 정하기
        Fire();
    }

    private void Fire()
    {
        _pet.Animator.SetTrigger("IsFire");

        // TODO : [펫] 에너지 탄 발사 갯수 조절하기
        for (int i = 0; i < _energyBulletCount; i++)
        {
            Instantiate(_energyBulletPrefab, _energyBulletSpawnPosition.position, transform.rotation);
        }
    }
}
