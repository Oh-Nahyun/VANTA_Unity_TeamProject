using System.Collections;
using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격 연출 시 타겟을 바라보는 시간")]
    [SerializeField] private float _watchTargetTime = 3f;

    [Header("펫 기본 공격 에너지 탄 생성 갯수")]
    [SerializeField] private int _energyBulletCount = 1;

    [Header("펫 기본 공격 에너지 탄 스폰 트랜스폼")]
    [SerializeField] private Transform _energyBulletSpawnTransform;

    [Header("펫 기본 공격 에너지 탄 프리팹")]
    [SerializeField] private GameObject _energyBulletPrefab;

    [Header("펫 공격 스킬 크리스탈 프리즌 프리팹")]
    [SerializeField] private GameObject _crystalPrisonPrefab;

    [Header("펫 공격 스킬 크리스탈 생츄어리 프리팹")]
    [SerializeField] private GameObject _crystalSanctuaryPrefab;

    [Header("펫 기본 공격 및 스킬 발동 시 나타날 말풍선")]
    [SerializeField] private SpeechBubbles _speechBubbles;

    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    private GameObject _attackTarget;

    /// <summary>
    /// 펫 기본 공격 에너지 탄 생성 로컬 위치
    /// </summary>
    private Vector3 _energyBulletSpawnPosition;

    /// <summary>
    /// 펫 기본 공격 연출 코루틴
    /// </summary>
    private Coroutine _energyBulletGameFeelCoroutine;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
    }

    private void Start()
    {
        _energyBulletSpawnPosition = _energyBulletSpawnTransform.localPosition;
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
        EnergyBulletGameFeel();

        _energyBulletCount = _pet.PlayerSkillManager.GetSkillStack;
        float middleIndex = (_energyBulletCount - 1) * 0.5f;
        _pet.Animator.SetTrigger("IsFire");

        for (int i = 0; i < _energyBulletCount; i++)
        {
            GameObject bullet = Instantiate(_energyBulletPrefab, _energyBulletSpawnTransform.position,
                transform.rotation);
            EnergyBullet energyBullet = bullet.GetComponent<EnergyBullet>();
            energyBullet.AttackTarget = _attackTarget;

            float curveAmount = (i - middleIndex) * 2f;
            energyBullet.BezierCurve.SetCurveAmount(curveAmount);
        }
    }

    public void UseCrystalPrison()
    {
        _speechBubbles.ChangeSpriteToCrystalPrison();

        GameObject prison = Instantiate(_crystalPrisonPrefab, _attackTarget.transform.position, transform.rotation);
        CrystalPrison crystalPrison = prison.GetComponent<CrystalPrison>();
        crystalPrison.AttackTarget = _attackTarget;
    }

    public void UseCrystalSanctuary()
    {
        _speechBubbles.ChangeSpriteToCrystalSanctuary();

        for (int i = 0; i < 3; i++)
        {
            float randomFloat = Random.Range(1f, 2f);
            Vector3 randomScale = new Vector3(randomFloat, randomFloat, randomFloat);
            Vector3 randomPosition = new Vector3(Random.Range(-6f, 6f), Random.Range(-3f, 1.5f), 0f);

            GameObject sanctuary = Instantiate(_crystalSanctuaryPrefab, randomPosition, transform.rotation);
            sanctuary.transform.localScale = randomScale;
        }
    }

    private void EnergyBulletGameFeel()
    {
        _speechBubbles.ChangeSpriteToEnergyBullet();

        if (_energyBulletGameFeelCoroutine != null)
        {
            StopCoroutine(_energyBulletGameFeelCoroutine);
        }

        _energyBulletGameFeelCoroutine = StartCoroutine(PetMoveStopAndWatchTarget());
    }

    private IEnumerator PetMoveStopAndWatchTarget()
    {
        Vector2 direction = _attackTarget.transform.position - transform.position;
        Vector3 bulletPosition = _energyBulletSpawnPosition;
        float bulletPositionX = Mathf.Abs(bulletPosition.x);

        _pet.PetMove.CanMove = false;

        if (direction.x < 0)
        {
            _pet.SpriteRenderer.flipX = true;
        }
        else if (direction.x > 0)
        {
            _pet.SpriteRenderer.flipX = false;
        }

        if (direction.x > -0.5f && direction.x < 0.5f)
        {
            bulletPosition.x = 0f;
            bulletPosition.y = 0.75f;
        }
        else
        {
            bulletPosition.x = (direction.x < 0) ? -bulletPositionX : bulletPositionX;
        }

        _energyBulletSpawnTransform.localPosition = bulletPosition;

        yield return new WaitForSeconds(_watchTargetTime);

        _pet.PetMove.CanMove = true;
        _energyBulletGameFeelCoroutine = null;
    }
}
