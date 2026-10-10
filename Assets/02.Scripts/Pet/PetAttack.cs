using System.Collections;
using UnityEngine;

public class PetAttack : MonoBehaviour
{
    [Header("펫 기본 공격 연출 시 타겟을 바라보는 시간")]
    [SerializeField] private float _watchTargetDuration = 3f;

    [Header("펫 기본 공격 에너지 탄 발사 반동 거리")]
    [SerializeField] private float _energyBulletRecoilDistance = 0.2f;

    [Header("펫 기본 공격 에너지 탄 발사 반동 시간")]
    [SerializeField] private float _energyBulletRecoilDuration = 0.1f;

    [Header("펫 기본 공격 에너지 탄 발사 반동 복귀 시간")]
    [SerializeField] private float _energyBulletRecoverDuration = 0.13f;

    [Header("펫 기본 공격 에너지 탄 생성 갯수")]
    [SerializeField] private int _energyBulletCount = 1;

    [Header("펫 기본 공격 에너지 탄 스폰 트랜스폼")]
    [SerializeField] private Transform _energyBulletSpawnTransform;

    [Header("펫 기본 공격 에너지 탄 프리팹")]
    [SerializeField] private GameObject _energyBulletPrefab;

    [Header("펫 기본 공격 기모으기 파티클 시스템")]
    [SerializeField] private ParticleSystem _energyBulletChargeVFX;

    [Header("펫 공격 스킬 크리스탈 프리즌 프리팹")]
    [SerializeField] private GameObject _crystalPrisonPrefab;

    [Header("펫 공격 스킬 크리스탈 프리즌 발동 연출이 끝난 뒤 추적 전 정지 시간")]
    [SerializeField] private float _crystalPrisonMoveStopTime = 1.5f;

    [Header("펫 유틸 스킬 크리스탈 생츄어리 프리팹")]
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

    /// <summary>
    /// 펫 공격 스킬 크리스탈 프리즌 발동 이펙트 클래스
    /// </summary>
    private CrystalPrisonCastVFX _crystalPrisonCastVFX;

    /// <summary>
    /// 펫 공격 스킬 크리스탈 프리즌 발동하는 중인지 확인용 변수
    /// </summary>
    private bool _isCastingCrystalPrison;

    /// <summary>
    /// 펫 공격 스킬 크리스탈 프리즌 발동 중 이동 정지 코루틴
    /// </summary>
    private Coroutine _crystalPrisonMoveStopCoroutine;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
        _crystalPrisonCastVFX = GetComponentInChildren<CrystalPrisonCastVFX>();
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
        _pet.Animator.SetTrigger("IsFireEnergyBullet");

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
        if (_attackTarget == null) return;

        _speechBubbles.ChangeSpriteToCrystalPrison();
        _pet.Animator.SetTrigger("IsUseCrystalPrison");

        StartCrystalPrisonMoveStop();
        _crystalPrisonCastVFX.PlayCast(_attackTarget, SpawnCrystalPrison);
    }

    private void StartCrystalPrisonMoveStop()
    {
        if (_crystalPrisonMoveStopCoroutine != null)
        {
            StopCoroutine(_crystalPrisonMoveStopCoroutine);
        }

        _crystalPrisonMoveStopCoroutine = StartCoroutine(MoveStopDuringCrystalPrison());
    }

    private IEnumerator MoveStopDuringCrystalPrison()
    {
        float duration = _crystalPrisonCastVFX.CastDuration + _crystalPrisonMoveStopTime;

        _isCastingCrystalPrison = true;
        _pet.PetMove.CanMove = false;
        _pet.Animator.SetBool("IsRun", false);

        yield return new WaitForSeconds(duration);

        _isCastingCrystalPrison = false;
        _crystalPrisonMoveStopCoroutine = null;

        if (_energyBulletGameFeelStartCoroutine == null)
        {
            _pet.PetMove.CanMove = true;
        }
    }

    private void SpawnCrystalPrison(GameObject target)
    {
        if (target == null) return;

        GameObject prison = Instantiate(_crystalPrisonPrefab, target.transform.position, transform.rotation);
        CrystalPrison crystalPrison = prison.GetComponent<CrystalPrison>();
        crystalPrison.AttackTarget = target;
    }

    public void UseCrystalSanctuary()
    {
        _speechBubbles.ChangeSpriteToCrystalSanctuary();
        _pet.Animator.SetTrigger("IsUseCrystalSanctuary");

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

        if (!_isCastingCrystalPrison)
        {
            _pet.PetMove.CanMove = true;
        }

        _energyBulletGameFeelStartCoroutine = null;
    }

    private IEnumerator PetRecoilAfterFireEnergyBullet()
    {
        float timer = 0f;
        float directionX = _attackTarget.transform.position.x - transform.position.x;
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition;

        endPosition.x += (directionX < 0f) ? _energyBulletRecoilDistance : -_energyBulletRecoilDistance;

        yield return new WaitForSeconds(0.8f);

        while (timer < _energyBulletRecoilDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _energyBulletRecoilDuration);
            float easeOutQuadProgress = 1f - Mathf.Pow(1f - progress, 2f);

            transform.position = Vector3.Lerp(startPosition, endPosition, easeOutQuadProgress);

            yield return null;
        }

        transform.position = endPosition;

        yield return new WaitForSeconds(0.5f);

        _pet.Animator.SetBool("IsRun", true);
        timer = 0f;

        while (timer < _energyBulletRecoverDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _energyBulletRecoverDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            transform.position = Vector3.Lerp(endPosition, startPosition, easedProgress);

            yield return null;
        }

        transform.position = startPosition;
        _energyBulletGameFeelEndCoroutine = null;
        _pet.Animator.SetBool("IsRun", false);
    }

    public void PlayEnergyBulletChargeVFX()
    {
        _energyBulletChargeVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _energyBulletChargeVFX.transform.position = _energyBulletSpawnTransform.position;
        _energyBulletChargeVFX.Play(true);
    }
}
