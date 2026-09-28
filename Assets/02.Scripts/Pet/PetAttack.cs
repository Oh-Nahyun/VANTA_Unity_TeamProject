using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격 에너지 탄 프리팹")]
    [SerializeField] private GameObject _energyBulletPrefab;

    [Header("펫 기본 공격 에너지 탄 생성 위치")]
    [SerializeField] private Transform _energyBulletSpawnPosition;

    [Header("펫 기본 공격 에너지 탄 생성 갯수")]
    [SerializeField] private int _energyBulletCount = 1;

    [Header("펫 공격 스킬 크리스탈 프리즌 프리팹")]
    [SerializeField] private GameObject _crystalPrisonPrefab;

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
        // TODO : [펫] 기본 공격 및 공격 스킬 조건은 플레이어 상태 고려해서 정하기
        UseCrystalPrison();
    }

    public void FireEnergyBullet()
    {
        float middleIndex = (_energyBulletCount - 1) * 0.5f;
        _energyBulletCount = _pet.PlayerSkillManager.GetSkillStack;
        _pet.Animator.SetTrigger("IsFire");

        for (int i = 0; i < _energyBulletCount; i++)
        {
            GameObject bullet = Instantiate(_energyBulletPrefab, _energyBulletSpawnPosition.position,
                transform.rotation);
            EnergyBullet energyBullet = bullet.GetComponent<EnergyBullet>();

            float curveAmount = (i - middleIndex) * 2f;
            energyBullet.BezierCurve.SetCurveAmount(curveAmount);
        }
    }

    private void UseCrystalPrison()
    {
        Instantiate(_crystalPrisonPrefab, transform.position, transform.rotation);
    }
}
