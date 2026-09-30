using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격 에너지 탄 생성 갯수")]
    [SerializeField] private int _energyBulletCount = 1;

    [Header("펫 기본 공격 에너지 탄 생성 위치")]
    [SerializeField] private Transform _energyBulletSpawnPosition;

    [Header("펫 기본 공격 에너지 탄 프리팹")]
    [SerializeField] private GameObject _energyBulletPrefab;

    [Header("펫 공격 스킬 크리스탈 프리즌 프리팹")]
    [SerializeField] private GameObject _crystalPrisonPrefab;

    [Header("펫 공격 스킬 크리스탈 생츄어리 프리팹")]
    [SerializeField] private GameObject _crystalSanctuaryPrefab;

    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    private GameObject _attackTarget;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
    }

    private void Start()
    {
        _attackTarget = GameObject.FindGameObjectWithTag("Boss");

        if (_attackTarget == null) return;

        // TODO : [펫] 아래 함수 호출 코드는 테스트용으로 추후 삭제하기
        //FireEnergyBullet();
        //UseCrystalPrison();
        //UseCrystalSanctuary();
    }

    private void Update()
    {
        if (_attackTarget == null)
        {
            _attackTarget = GameObject.FindGameObjectWithTag("Boss");

            if (_attackTarget == null) return;
        }
    }

    public void FireEnergyBullet()
    {
        float middleIndex = (_energyBulletCount - 1) * 0.5f;
        _energyBulletCount = _pet.PlayerSkillManager.GetSkillStack + 1;
        _pet.Animator.SetTrigger("IsFire");

        for (int i = 0; i < _energyBulletCount; i++)
        {
            GameObject bullet = Instantiate(_energyBulletPrefab, _energyBulletSpawnPosition.position,
                transform.rotation);
            EnergyBullet energyBullet = bullet.GetComponent<EnergyBullet>();
            energyBullet.AttackTarget = _attackTarget;

            float curveAmount = (i - middleIndex) * 2f;
            energyBullet.BezierCurve.SetCurveAmount(curveAmount);
        }
    }

    public void UseCrystalPrison()
    {
        GameObject prison = Instantiate(_crystalPrisonPrefab, _attackTarget.transform.position, transform.rotation);
        CrystalPrison crystalPrison = prison.GetComponent<CrystalPrison>();
        crystalPrison.AttackTarget = _attackTarget;
    }

    public void UseCrystalSanctuary()
    {
        for (int i = 0; i < 3; i++)
        {
            float randomFloat = Random.Range(1f, 2f);
            Vector3 randomScale = new Vector3(randomFloat, randomFloat, randomFloat);
            Vector3 randomPosition = new Vector3(Random.Range(-6f, 6f), Random.Range(-3f, 1.5f), 0f);

            Instantiate(_crystalSanctuaryPrefab, randomPosition, transform.rotation);
            _crystalSanctuaryPrefab.transform.localScale = randomScale;
        }
    }
}
