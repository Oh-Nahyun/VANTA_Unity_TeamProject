using System.Collections;
using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격 연출 시 타겟을 바라보는 시간")]
    [SerializeField] private float _watchTargetDuration = 3f;

    [Header("펫 에너지 탄 발사 반동 거리")]
    [SerializeField] private float _energyBulletRecoilDistance = 0.12f;

    [Header("펫 에너지 탄 발사 반동 시간")]
    [SerializeField] private float _energyBulletRecoilDuration = 0.08f;

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
    /// 펫 기본 공격 연출 시작 코루틴
    /// </summary>
    private Coroutine _energyBulletGameFeelStartCoroutine;

    /// <summary>
    /// 펫 기본 공격 연출 종료 코루틴
    /// </summary>
    private Coroutine _energyBulletGameFeelEndCoroutine;

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
        StartEnergyBulletGameFeel();

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

        EndEnergyBulletGameFeel();
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

    private void StartEnergyBulletGameFeel()
    {
        _speechBubbles.ChangeSpriteToEnergyBullet();

        if (_energyBulletGameFeelStartCoroutine != null)
        {
            StopCoroutine(_energyBulletGameFeelStartCoroutine);
        }

        _energyBulletGameFeelStartCoroutine = StartCoroutine(PetMoveStopAndWatchTarget());
    }

    private void EndEnergyBulletGameFeel()
    {
        if (_energyBulletGameFeelEndCoroutine != null)
        {
            StopCoroutine(_energyBulletGameFeelEndCoroutine);
        }

        _energyBulletGameFeelEndCoroutine = StartCoroutine(PetRecoilAfterFireEnergyBullet());
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

        yield return new WaitForSeconds(_watchTargetDuration);

        _pet.PetMove.CanMove = true;
        _energyBulletGameFeelStartCoroutine = null;
    }

    private IEnumerator PetRecoilAfterFireEnergyBullet()
    {
        float directionX = _attackTarget.transform.position.x - transform.position.x;
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition;

        endPosition.x += (directionX < 0f) ? _energyBulletRecoilDistance : -_energyBulletRecoilDistance;

        yield return new WaitForSeconds(0.6f);

        float timer = 0f;

        while (timer < _energyBulletRecoilDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _energyBulletRecoilDuration);
            float easeOutQuadProgress = 1f - Mathf.Pow(1f - progress, 2f);

            transform.position = Vector3.Lerp(startPosition, endPosition, easeOutQuadProgress);

            yield return null;
        }

        transform.position = endPosition;
        _energyBulletGameFeelEndCoroutine = null;
    }
}
