using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalSanctuaryCastVFX : MonoBehaviour
{
    [Header("펫 크리스탈 생츄어리 시전 부유 높이")]
    [SerializeField] private float _levitateHeight = 0.4f;

    [Header("펫 크리스탈 생츄어리 시전 부유 상승 시간")]
    [SerializeField] private float _levitateDuration = 0.15f;

    [Header("펫 크리스탈 생츄어리 시전 부유 중 위아래로 흔들리는 세기")]
    [SerializeField] private float _levitateBobAmount = 0.03f;

    [Header("펫 크리스탈 생츄어리 시전 부유 중 위아래로 흔들리는 속도")]
    [SerializeField] private float _levitateBobSpeed = 6f;

    [Header("펫 크리스탈 생츄어리 시전 후 펫이 내려오는 시간")]
    [SerializeField] private float _descendDuration = 0.2f;

    [Header("펫 크리스탈 생츄어리 공명 오라가 시작되는 시점")]
    [SerializeField] private float _resonanceStartDelay = 0.1f;

    [Header("펫 크리스탈 생츄어리 공명 강화가 시작되는 시점")]
    [SerializeField] private float _boostStartTime = 0.35f;

    [Header("펫 크리스탈 생츄어리 공명 강화 시간")]
    [SerializeField] private float _boostDuration = 0.1f;

    [Header("펫 크리스탈 생츄어리 공명 강화 시 파티클 최대 속도 배율")]
    [SerializeField] private float _boostSimulationSpeed = 1.6f;

    [Header("펫 크리스탈 생츄어리 마력 코어 방출 시차")]
    [SerializeField] private float _coreStaggerTime = 0.05f;

    [Header("펫 크리스탈 생츄어리 마력 코어 이동 시간")]
    [SerializeField] private float _coreMoveDuration = 0.3f;

    [Header("펫 크리스탈 생츄어리 마력 코어 경로 휘어짐 정도")]
    [SerializeField] private float _coreCurveAmount = 0.4f;

    [Header("펫 크리스탈 생츄어리 마력 코어 도착 후 남은 파편이 사라질 때까지 남겨두는 시간")]
    [SerializeField] private float _coreFadeOutTime = 0.4f;

    [Header("펫 크리스탈 생츄어리 생성 위치 모임 이펙트가 기준으로 만들어진 시간")]
    [SerializeField] private float _gatheringReferenceDuration = 0.25f;

    [Header("펫 크리스탈 생츄어리 생성 위치 모임 이펙트 삭제 시간")]
    [SerializeField] private float _gatheringVFXLifetime = 1.5f;

    [Header("펫 크리스탈 생츄어리 생성 위치 기준 중심 오프셋 (생츄어리 크기 1 기준, 마력 코어 도착 위치)")]
    [SerializeField] private Vector3 _sanctuaryCenterOffset = new Vector3(0f, 0.23f, 0f);

    [Header("펫 크리스탈 생츄어리 도착 이펙트 삭제 시간")]
    [SerializeField] private float _arrivalVFXLifetime = 1f;

    [Header("펫 크리스탈 생츄어리 시전 중심 위치 (펫 피벗 기준, 오라와 마력 코어 시작 위치)")]
    [SerializeField] private Vector3 _castCenterOffset = new Vector3(0f, 0.15f, 0f);

    [Header("펫 그림자 트랜스폼 (부유해도 바닥에 남도록 보정)")]
    [SerializeField] private Transform _petShadow;

    [Header("펫 크리스탈 생츄어리 공명 오라 이펙트 오브젝트")]
    [SerializeField] private GameObject _resonanceAuraVFX;

    [Header("펫 크리스탈 생츄어리 공명 강화 빛 이펙트 오브젝트")]
    [SerializeField] private GameObject _resonanceGlowVFX;

    [Header("펫 크리스탈 생츄어리 공명 방출 이펙트 오브젝트")]
    [SerializeField] private GameObject _resonanceBurstVFX;

    [Header("펫 크리스탈 생츄어리 마력 코어 프리팹")]
    [SerializeField] private GameObject _magicCorePrefab;

    [Header("펫 크리스탈 생츄어리 생성 위치 모임 이펙트 프리팹")]
    [SerializeField] private GameObject _gatheringVFXPrefab;

    [Header("펫 크리스탈 생츄어리 도착 이펙트 프리팹")]
    [SerializeField] private GameObject _arrivalVFXPrefab;

    [Header("펫 크리스탈 생츄어리 시전 부유 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryLevitateSound;

    [Header("펫 크리스탈 생츄어리 공명 오라 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryResonanceSound;

    [Header("펫 크리스탈 생츄어리 공명 방출 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryReleaseSound;

    [Header("펫 크리스탈 생츄어리 마력 코어 비행 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryCoreFlySound;

    [Header("펫 크리스탈 생츄어리 마력 코어 비행 효과음 볼륨 비율")]
    [SerializeField, Range(0f, 1f)] private float _crystalSanctuaryCoreFlySoundVolume = 0.6f;

    [Header("펫 크리스탈 생츄어리 생성 위치 모임 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryGatherSound;

    [Header("펫 크리스탈 생츄어리 생성 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryCreateSound;

    /// <summary>
    /// 펫 크리스탈 생츄어리 시전 연출이 펫이 원래 위치로 내려올 때까지 걸리는 시간. 펫 이동 정지 시간 계산용
    /// </summary>
    /// <param name="coreCount">생성할 크리스탈 생츄어리(마력 코어) 개수</param>
    public float GetCastDuration(int coreCount)
    {
        return _boostStartTime + _boostDuration + _coreStaggerTime * (coreCount - 1) + _coreMoveDuration +
               _descendDuration;
    }

    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// 펫 공명 오라 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _resonanceAuraParticles;

    /// <summary>
    /// 펫 공명 강화 빛 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _resonanceGlowParticles;

    /// <summary>
    /// 펫 공명 방출 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _resonanceBurstParticles;

    /// <summary>
    /// 시전 중 생성된 마력 코어 오브젝트 목록. 비활성화 시 정리용
    /// </summary>
    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

    /// <summary>
    /// 펫 부유 코루틴
    /// </summary>
    private Coroutine _levitateCoroutine;

    /// <summary>
    /// 펫 공명 강화 코루틴
    /// </summary>
    private Coroutine _boostCoroutine;

    /// <summary>
    /// 시전 진행 여부 확인용 변수
    /// </summary>
    private bool _isCasting;

    /// <summary>
    /// 현재 펫에 적용 중인 부유 높이
    /// </summary>
    private float _appliedOffset;

    private void Awake()
    {
        _pet = GetComponentInParent<Pet>();
        _resonanceAuraParticles = GetParticles(_resonanceAuraVFX);
        _resonanceGlowParticles = GetParticles(_resonanceGlowVFX);
        _resonanceBurstParticles = GetParticles(_resonanceBurstVFX);

        SetLocalPosition(_resonanceAuraVFX, _castCenterOffset);
        SetLocalPosition(_resonanceGlowVFX, _castCenterOffset);
    }

    private ParticleSystem[] GetParticles(GameObject vfx)
    {
        if (vfx == null) return new ParticleSystem[0];

        return vfx.GetComponentsInChildren<ParticleSystem>();
    }

    private void SetLocalPosition(GameObject vfx, Vector3 position)
    {
        if (vfx == null) return;

        vfx.transform.localPosition = position;
    }

    /// <param name="positions">크리스탈 생츄어리가 생성될 위치. 마력 코어 개수만큼 사용</param>
    /// <param name="scales">크리스탈 생츄어리 크기</param>
    /// <param name="onArrive">마력 코어가 도착한 순간 해당 위치와 크기를 넘겨 호출</param>
    public void PlayCast(Vector3[] positions, float[] scales, Action<Vector3, float> onArrive)
    {
        // 이미 시전 중이면 연출은 건너뛰고 생성만 바로 처리해서 스킬이 사라지지 않게 한다.
        if (_isCasting)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                onArrive.Invoke(positions[i], scales[i]);
            }

            return;
        }

        StartCoroutine(CastCoroutine(positions, scales, onArrive));
    }

    private IEnumerator CastCoroutine(Vector3[] positions, float[] scales, Action<Vector3, float> onArrive)
    {
        _isCasting = true;
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryLevitateSound);
        _levitateCoroutine = StartCoroutine(LevitateCoroutine());

        yield return new WaitForSeconds(_resonanceStartDelay);

        SetSimulationSpeed(_resonanceAuraParticles, 1f);
        RestartParticles(_resonanceAuraParticles);
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryResonanceSound);

        yield return new WaitForSeconds(_boostStartTime - _resonanceStartDelay);

        RestartParticles(_resonanceGlowParticles);
        _boostCoroutine = StartCoroutine(BoostCoroutine());

        yield return new WaitForSeconds(_boostDuration);

        StopEmitting(_resonanceAuraParticles);
        RestartParticles(_resonanceBurstParticles);
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryReleaseSound);

        int count = positions.Length;
        int pending = count;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(CoreCoroutine(i, count, positions[i], scales[i], onArrive, () => pending--));
        }

        while (pending > 0)
        {
            yield return null;
        }

        if (_levitateCoroutine != null)
        {
            StopCoroutine(_levitateCoroutine);
            _levitateCoroutine = null;
        }

        yield return DescendCoroutine();

        _isCasting = false;
    }

    private IEnumerator LevitateCoroutine()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _levitateDuration);
            float easeOutProgress = 1f - Mathf.Pow(1f - progress, 3f);
            float bob = progress >= 1f
                ? Mathf.Sin((timer - _levitateDuration) * _levitateBobSpeed) * _levitateBobAmount
                : 0f;

            SetLevitateOffset(_levitateHeight * easeOutProgress + bob);

            yield return null;
        }
    }

    private IEnumerator DescendCoroutine()
    {
        float timer = 0f;
        float startOffset = _appliedOffset;

        while (timer < _descendDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _descendDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            SetLevitateOffset(Mathf.Lerp(startOffset, 0f, easedProgress));

            yield return null;
        }

        SetLevitateOffset(0f);
    }

    /// <summary>
    /// 펫 루트의 Y를 부유 높이만큼 올리고, 그림자는 반대로 내려 바닥에 남긴다.
    /// 절대 위치가 아니라 변화량만 적용해서 다른 코드가 위치를 바꿔도 덮어쓰지 않는다.
    /// </summary>
    private void SetLevitateOffset(float offset)
    {
        if (_pet == null) return;

        float delta = offset - _appliedOffset;

        if (Mathf.Approximately(delta, 0f)) return;

        _pet.transform.position += new Vector3(0f, delta, 0f);

        if (_petShadow != null)
        {
            _petShadow.position -= new Vector3(0f, delta, 0f);
        }

        _appliedOffset = offset;
    }

    private IEnumerator BoostCoroutine()
    {
        float timer = 0f;

        while (timer < _boostDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _boostDuration);

            SetSimulationSpeed(_resonanceAuraParticles, Mathf.Lerp(1f, _boostSimulationSpeed, progress));

            yield return null;
        }

        _boostCoroutine = null;
    }

    private IEnumerator CoreCoroutine(int index, int count, Vector3 targetPosition, float scale,
        Action<Vector3, float> onArrive, Action onFinished)
    {
        yield return new WaitForSeconds(_coreStaggerTime * index);

        Vector3 startPosition = transform.position + _castCenterOffset;
        Vector3 zoneCenter = targetPosition + _sanctuaryCenterOffset * scale;
        GameObject core = SpawnTracked(_magicCorePrefab, startPosition);

        SpawnGatheringVFX(zoneCenter, scale);
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryCoreFlySound, _crystalSanctuaryCoreFlySoundVolume);

        if (index == 0)
        {
            SoundManager.Instance.PlayEffectSound(_crystalSanctuaryGatherSound);
        }

        float middle = (count - 1) * 0.5f;
        float curveFactor = middle > 0f ? (index - middle) / middle : 0f;
        float timer = 0f;

        Vector3 direction = zoneCenter - startPosition;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;

        while (timer < _coreMoveDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _coreMoveDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            float curve = Mathf.Sin(progress * Mathf.PI) * _coreCurveAmount * curveFactor;

            if (core != null)
            {
                core.transform.position =
                    Vector3.Lerp(startPosition, zoneCenter, easedProgress) + perpendicular * curve;
            }

            yield return null;
        }

        RetireCore(core);
        SpawnArrivalVFX(zoneCenter);

        if (index == 0)
        {
            SoundManager.Instance.PlayEffectSound(_crystalSanctuaryCreateSound);
        }

        onArrive.Invoke(targetPosition, scale);
        onFinished.Invoke();
    }

    private void SetSimulationSpeed(ParticleSystem[] particles, float speed)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.simulationSpeed = speed;
        }
    }

    private void RestartParticles(ParticleSystem[] particles)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play();
        }
    }

    private void StopEmitting(ParticleSystem[] particles)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private GameObject SpawnTracked(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;

        _spawnedObjects.RemoveAll(obj => obj == null);

        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        _spawnedObjects.Add(spawned);

        return spawned;
    }

    private void RetireCore(GameObject core)
    {
        if (core == null) return;

        StopEmitting(core.GetComponentsInChildren<ParticleSystem>());
        Destroy(core, _coreFadeOutTime);
    }

    private void SpawnGatheringVFX(Vector3 position, float scale)
    {
        GameObject gathering = SpawnTracked(_gatheringVFXPrefab, position);

        gathering.transform.localScale = Vector3.one * scale;
        SetSimulationSpeed(gathering.GetComponentsInChildren<ParticleSystem>(),
            _gatheringReferenceDuration / Mathf.Max(0.01f, _coreMoveDuration));

        Destroy(gathering, _gatheringVFXLifetime);
    }

    private void SpawnArrivalVFX(Vector3 position)
    {
        if (_arrivalVFXPrefab == null) return;

        GameObject arrival = Instantiate(_arrivalVFXPrefab, position, Quaternion.identity);
        Destroy(arrival, _arrivalVFXLifetime);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _levitateCoroutine = null;
        _boostCoroutine = null;
        _isCasting = false;

        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            if (_spawnedObjects[i] != null)
            {
                Destroy(_spawnedObjects[i]);
            }
        }

        _spawnedObjects.Clear();

        SetLevitateOffset(0f);
        ClearParticles(_resonanceAuraParticles);
        ClearParticles(_resonanceGlowParticles);
        ClearParticles(_resonanceBurstParticles);
    }

    private void ClearParticles(ParticleSystem[] particles)
    {
        if (particles == null) return;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;

            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
