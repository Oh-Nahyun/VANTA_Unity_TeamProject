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

    [Header("펫 기본 공격 기모으기 효과음")]
    [SerializeField] private AudioClip _energyBulletChargeSound;

    [Header("펫 기본 공격 에너지 탄 발사 효과음")]
    [SerializeField] private AudioClip _energyBulletFireSound;

    [Header("펫 공격 스킬 크리스탈 프리즌 프리팹")]
    [SerializeField] private GameObject _crystalPrisonPrefab;

    [Header("펫 공격 스킬 크리스탈 프리즌 발동 연출 끝난 뒤 추적 전 정지 시간")]
    [SerializeField] private float _crystalPrisonMoveStopTime = 1.5f;

    [Header("펫 공격 스킬 크리스탈 프리즌 보스2 생성 위치 오프셋")]
    [SerializeField] private Vector3 _boss2CrystalPrisonOffset;

    [Header("펫 유틸 스킬 크리스탈 생츄어리 프리팹")]
    [SerializeField] private GameObject _crystalSanctuaryPrefab;

    [Header("펫 유틸 스킬 크리스탈 생츄어리 생성 갯수")]
    [SerializeField, Min(1)] private int _crystalSanctuaryCount = 3;

    [Header("펫 유틸 스킬 크리스탈 생츄어리 발동 연출 끝난 뒤 추적 전 정지 시간")]
    [SerializeField] private float _crystalSanctuaryMoveStopTime = 0.3f;

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
    /// 펫 유틸 스킬 크리스탈 생츄어리 발동 이펙트 클래스
    /// </summary>
    private CrystalSanctuaryCastVFX _crystalSanctuaryCastVFX;

    /// <summary>
    /// 펫 유틸 스킬 위치 및 스케일
    /// </summary>
    private float _crystalSanctuaryMinPositionX;

    private float _crystalSanctuaryMaxPositionX;
    private float _crystalSanctuaryMinPositionY;
    private float _crystalSanctuaryMaxPositionY;
    private float _crystalSanctuaryMinScale;
    private float _crystalSanctuaryMaxScale;

    /// <summary>
    /// 펫 스킬 발동 중인지 확인용 변수
    /// </summary>
    private bool _isCastingSkill;

    /// <summary>
    /// 펫 스킬 발동 중 이동 정지 코루틴
    /// </summary>
    private Coroutine _skillMoveStopCoroutine;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
        _crystalPrisonCastVFX = GetComponentInChildren<CrystalPrisonCastVFX>();
        _crystalSanctuaryCastVFX = GetComponentInChildren<CrystalSanctuaryCastVFX>();
    }

    private void Start()
    {
        _energyBulletSpawnPosition = _energyBulletSpawnTransform.localPosition;
        _crystalSanctuaryMinPositionX = -6f;
        _crystalSanctuaryMaxPositionX = 6f;
        _crystalSanctuaryMinPositionY = -3f;
        _crystalSanctuaryMaxPositionY = 1.5f;
        _crystalSanctuaryMinScale = 1f;
        _crystalSanctuaryMaxScale = 2f;

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

        _energyBulletCount = Mathf.Clamp(_pet.PlayerSkillManager.GetSkillStack, 1, 5);

        float middleIndex = (_energyBulletCount - 1) * 0.5f;

        _pet.Animator.SetTrigger("IsFireEnergyBullet");
        SoundManager.Instance.PlayEffectSound(_energyBulletFireSound);

        for (int i = 0; i < _energyBulletCount; i++)
        {
            GameObject bullet = Instantiate(
                _energyBulletPrefab,
                _energyBulletSpawnTransform.position,
                transform.rotation
            );

            EnergyBullet energyBullet = bullet.GetComponent<EnergyBullet>();

            energyBullet.AttackTarget = _attackTarget;
            energyBullet.IsPlayFlySound = (i == 0) ? true : false;

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

        StartSkillMoveStop(_crystalPrisonCastVFX.CastDuration + _crystalPrisonMoveStopTime);
        _crystalPrisonCastVFX.PlayCast(_attackTarget, SpawnCrystalPrison);
    }

    private void StartSkillMoveStop(float duration)
    {
        if (_skillMoveStopCoroutine != null)
        {
            StopCoroutine(_skillMoveStopCoroutine);
        }

        _skillMoveStopCoroutine = StartCoroutine(MoveStopDuringSkill(duration));
    }

    private IEnumerator MoveStopDuringSkill(float duration)
    {
        _isCastingSkill = true;
        _pet.PetMove.CanMove = false;
        _pet.Animator.SetBool("IsRun", false);

        yield return new WaitForSeconds(duration);

        _isCastingSkill = false;
        _skillMoveStopCoroutine = null;

        if (_energyBulletGameFeelStartCoroutine == null)
        {
            _pet.PetMove.CanMove = true;
        }
    }

    private void SpawnCrystalPrison(GameObject target)
    {
        if (target == null) return;

        Vector3 spawnPosition = target.transform.position;

        if (target.GetComponent<Boss2>() != null)
        {
            spawnPosition += _boss2CrystalPrisonOffset;
        }

        GameObject prison = Instantiate(_crystalPrisonPrefab, spawnPosition, transform.rotation);
        CrystalPrison crystalPrison = prison.GetComponent<CrystalPrison>();
        crystalPrison.AttackTarget = target;
    }

    public void UseCrystalSanctuary()
    {
        _speechBubbles.ChangeSpriteToCrystalSanctuary();
        _pet.Animator.SetTrigger("IsUseCrystalSanctuary");

        Vector3[] positions = new Vector3[_crystalSanctuaryCount];
        float[] scales = new float[_crystalSanctuaryCount];

        for (int i = 0; i < _crystalSanctuaryCount; i++)
        {
            positions[i] = new Vector3(Random.Range(_crystalSanctuaryMinPositionX, _crystalSanctuaryMaxPositionX),
                Random.Range(_crystalSanctuaryMinPositionY, _crystalSanctuaryMaxPositionY), 0f);
            scales[i] = Random.Range(_crystalSanctuaryMinScale, _crystalSanctuaryMaxScale);
        }

        StartSkillMoveStop(_crystalSanctuaryCastVFX.GetCastDuration(_crystalSanctuaryCount) +
                           _crystalSanctuaryMoveStopTime);
        _crystalSanctuaryCastVFX.PlayCast(positions, scales, SpawnCrystalSanctuary);
    }

    private void SpawnCrystalSanctuary(Vector3 position, float scale)
    {
        GameObject sanctuary = Instantiate(_crystalSanctuaryPrefab, position, transform.rotation);
        sanctuary.transform.localScale = new Vector3(scale, scale, scale);
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

        if (!_isCastingSkill)
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
        SoundManager.Instance.PlayEffectSound(_energyBulletChargeSound);

        _energyBulletChargeVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _energyBulletChargeVFX.transform.position = _energyBulletSpawnTransform.position;
        _energyBulletChargeVFX.Play(true);
    }
}
