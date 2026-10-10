using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalPrisonCastVFX : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 시전 차징 시간")]
    [SerializeField] private float _chargeDuration = 0.2f;

    [Header("펫 크리스탈 프리즌 시전 방출 시간 (차징 완료 후 섬광과 충격파가 나오는 시간)")]
    [SerializeField] private float _releaseDuration = 0.05f;

    [Header("펫 크리스탈 프리즌 차징 이펙트 프리팹이 기준으로 만들어진 차징 시간")]
    [SerializeField] private float _chargeVFXReferenceDuration = 0.2f;

    [Header("펫 크리스탈 프리즌 시전 중심 위치 (펫 피벗 기준, 코어와 마력 구체 시작 위치)")]
    [SerializeField] private Vector3 _castCenterOffset = new Vector3(0f, 0.2f, 0f);

    [Header("펫 크리스탈 프리즌 마력 구체 이동 시간")]
    [SerializeField] private float _magicOrbMoveDuration = 0.4f;

    [Header("펫 크리스탈 프리즌 마력 구체 경로 휘어짐 정도")]
    [SerializeField] private float _magicOrbCurveAmount = 0.35f;

    [Header("펫 크리스탈 프리즌 마력 구체 경로 물결 세기")]
    [SerializeField] private float _magicOrbWaveAmount = 0.25f;

    [Header("펫 크리스탈 프리즌 마력 구체 경로 물결 횟수")]
    [SerializeField] private float _magicOrbWaveCount = 1.5f;

    [Header("펫 크리스탈 프리즌 마력 구체 도착 후 남은 파편이 사라질 때까지 남겨두는 시간")]
    [SerializeField] private float _magicOrbFadeOutTime = 0.4f;

    [Header("펫 크리스탈 프리즌 마력 흐름 사용 여부")]
    [SerializeField] private bool _isMagicStreamEnabled = true;

    [Header("펫 크리스탈 프리즌 마력 흐름 표시 시간")]
    [SerializeField] private float _magicStreamDuration = 0.3f;

    [Header("펫 크리스탈 프리즌 마력 흐름 파편이 대상까지 도착하는 데 걸리는 시간")]
    [SerializeField] private float _magicStreamTravelTime = 0.3f;

    [Header("펫 크리스탈 프리즌 마력 흐름 파편 방출이 유지되는 시점 (표시 시간 대비 비율)")]
    [SerializeField, Range(0f, 1f)] private float _magicStreamEmitRatio = 0.6f;
    [Header("펫 크리스탈 프리즌 시전 마법진 납작한 정도 (Y 비율)")]
    [SerializeField] private float _sigilFlatten = 0.45f;

    [Header("펫 크리스탈 프리즌 시전 마법진 등장 시간")]
    [SerializeField] private float _sigilAppearDuration = 0.12f;

    [Header("펫 크리스탈 프리즌 시전 마법진 시작 크기")]
    [SerializeField] private float _sigilStartScale = 0.3f;

    [Header("펫 크리스탈 프리즌 시전 마법진 최대 크기")]
    [SerializeField] private float _sigilEndScale = 1f;

    [Header("펫 크리스탈 프리즌 시전 마법진 바깥쪽 회전 속도 (초당 각도)")]
    [SerializeField] private float _sigilOuterRotateSpeed = -40f;

    [Header("펫 크리스탈 프리즌 시전 마법진 안쪽 회전 속도 (초당 각도)")]
    [SerializeField] private float _sigilInnerRotateSpeed = 90f;

    [Header("펫 크리스탈 프리즌 시전 마법진 등장 시 밝기")]
    [SerializeField, Range(0f, 1f)] private float _sigilStartAlpha = 0.3f;

    [Header("펫 크리스탈 프리즌 시전 마법진 차징 완료 시 밝기")]
    [SerializeField, Range(0f, 1f)] private float _sigilEndAlpha = 0.9f;

    [Header("펫 크리스탈 프리즌 시전 마법진 사라지는 시간")]
    [SerializeField] private float _sigilVanishDuration = 0.08f;

    [Header("펫 크리스탈 프리즌 시전 차징 오라 이펙트 오브젝트")]
    [SerializeField] private GameObject _chargeAuraVFX;

    [Header("펫 크리스탈 프리즌 시전 차징 코어 이펙트 오브젝트")]
    [SerializeField] private GameObject _chargeCoreVFX;

    [Header("펫 크리스탈 프리즌 시전 발동 섬광 이펙트 오브젝트")]
    [SerializeField] private GameObject _releaseGlowVFX;

    [Header("펫 크리스탈 프리즌 시전 발동 충격파 이펙트 오브젝트")]
    [SerializeField] private GameObject _releaseRingVFX;

    [Header("펫 크리스탈 프리즌 시전 마법진 루트")]
    [SerializeField] private Transform _castSigilRoot;

    [Header("펫 크리스탈 프리즌 시전 마법진 바깥쪽 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer _castSigilOuter;

    [Header("펫 크리스탈 프리즌 시전 마법진 안쪽 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer _castSigilInner;

    [Header("펫 크리스탈 프리즌 마력 구체 프리팹")]
    [SerializeField] private GameObject _magicOrbPrefab;

    [Header("펫 크리스탈 프리즌 마력 도착 섬광 프리팹")]
    [SerializeField] private GameObject _magicArrivalFlashPrefab;

    [Header("펫 크리스탈 프리즌 마력 도착 섬광 삭제 시간")]
    [SerializeField] private float _magicArrivalFlashLifetime = 1f;

    [Header("펫 크리스탈 프리즌 마력 흐름 프리팹")]
    [SerializeField] private GameObject _magicStreamPrefab;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 차징 오라 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _chargeAuraParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 차징 코어 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _chargeCoreParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 발동 섬광 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _releaseGlowParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 발동 충격파 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _releaseRingParticles;

    /// <summary>
    /// 시전 중 생성된 마력 구체, 연결선 오브젝트 목록. 비활성화 시 정리용
    /// </summary>
    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 마법진 코루틴
    /// </summary>
    private Coroutine _sigilCoroutine;

    /// <summary>
    /// 마지막으로 시작된 시전 번호. 시전이 겹쳤을 때 마지막 시전만 오라와 마법진을 제어하기 위한 변수
    /// </summary>
    private int _latestCastId;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 마법진 바깥쪽 현재 회전 각도
    /// </summary>
    private float _sigilOuterAngle;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 마법진 안쪽 현재 회전 각도
    /// </summary>
    private float _sigilInnerAngle;

    /// <summary>
    /// 펫 크리스탈 프리즌 시전 연출이 마력 구체 도착까지 걸리는 시간. 펫 이동 정지 시간 계산용
    /// </summary>
    public float CastDuration => _chargeDuration + _releaseDuration + _magicOrbMoveDuration;

    private void Awake()    {
        _chargeAuraParticles = GetParticles(_chargeAuraVFX);
        _chargeCoreParticles = GetParticles(_chargeCoreVFX);
        _releaseGlowParticles = GetParticles(_releaseGlowVFX);
        _releaseRingParticles = GetParticles(_releaseRingVFX);

        SetLocalPosition(_chargeAuraVFX, _castCenterOffset);
        SetLocalPosition(_chargeCoreVFX, _castCenterOffset);
        SetLocalPosition(_releaseGlowVFX, _castCenterOffset);

        HideSigil();
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

    /// <param name="target">마력을 전달할 대상</param>
    /// <param name="onArrive">마력 구체가 대상에 도착한 순간 대상을 넘겨 호출. 시전 도중 대상이 사라지면 호출되지 않는다.</param>
    public void PlayCast(GameObject target, Action<GameObject> onArrive)
    {
        if (!IsTargetAlive(target)) return;

        _latestCastId++;

        StartChargeVFX();
        StartCoroutine(CastCoroutine(_latestCastId, target, onArrive));
    }

    private IEnumerator CastCoroutine(int castId, GameObject target, Action<GameObject> onArrive)
    {
        yield return new WaitForSeconds(_chargeDuration);

        bool isLatestCast = castId == _latestCastId;

        if (isLatestCast)
        {
            ReleaseChargeVFX();
        }

        yield return new WaitForSeconds(_releaseDuration);

        if (!IsTargetAlive(target)) yield break;

        GameObject magicOrb = SpawnTracked(_magicOrbPrefab, transform.position + _castCenterOffset);
        Vector3 startPosition = transform.position + _castCenterOffset;
        Vector3 lastPosition = startPosition;
        float curveSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float timer = 0f;

        while (timer < _magicOrbMoveDuration)
        {
            if (!IsTargetAlive(target))
            {
                RetireMagicOrb(magicOrb);
                SpawnArrivalFlash(lastPosition);
                yield break;
            }

            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _magicOrbMoveDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            Vector3 endPosition = target.transform.position;
            Vector3 direction = endPosition - startPosition;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float curve = envelope * _magicOrbCurveAmount * curveSign;
            float wave = Mathf.Sin(progress * Mathf.PI * 2f * _magicOrbWaveCount) * _magicOrbWaveAmount * envelope;

            lastPosition = Vector3.Lerp(startPosition, endPosition, easedProgress) + perpendicular * (curve + wave);

            if (magicOrb != null)
            {
                magicOrb.transform.position = lastPosition;
            }

            yield return null;
        }

        if (!IsTargetAlive(target))
        {
            RetireMagicOrb(magicOrb);
            SpawnArrivalFlash(lastPosition);
            yield break;
        }

        RetireMagicOrb(magicOrb);
        SpawnArrivalFlash(target.transform.position);

        if (onArrive != null)
        {
            onArrive.Invoke(target);
        }

        if (_isMagicStreamEnabled)
        {
            yield return StreamCoroutine(target);
        }
    }

    private IEnumerator StreamCoroutine(GameObject target)
    {
        GameObject stream = SpawnTracked(_magicStreamPrefab, transform.position + _castCenterOffset);

        if (stream == null) yield break;

        ParticleSystem[] particles = stream.GetComponentsInChildren<ParticleSystem>();
        float travelTime = Mathf.Max(0.05f, _magicStreamTravelTime);
        float timer = 0f;
        bool isEmitStopped = false;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startLifetime = travelTime;
        }

        while (timer < _magicStreamDuration)
        {
            if (!IsTargetAlive(target)) break;

            timer += Time.deltaTime;

            Vector3 startPosition = transform.position + _castCenterOffset;
            Vector3 endPosition = target.transform.position;
            endPosition.z = startPosition.z;

            Vector3 direction = endPosition - startPosition;
            float speed = direction.magnitude / travelTime;

            stream.transform.position = startPosition;
            stream.transform.right = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.startSpeed = speed;
            }

            if (!isEmitStopped && timer >= _magicStreamDuration * _magicStreamEmitRatio)
            {
                isEmitStopped = true;
                StopEmitting(particles);
            }

            yield return null;
        }

        StopEmitting(particles);

        if (stream != null)
        {
            Destroy(stream, travelTime + 0.1f);
        }
    }

    private void StopEmitting(ParticleSystem[] particles)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
    private void StartChargeVFX()
    {
        SetSimulationSpeed(_chargeAuraParticles);
        SetSimulationSpeed(_chargeCoreParticles);
        RestartParticles(_chargeAuraParticles);
        RestartParticles(_chargeCoreParticles);

        if (_sigilCoroutine != null)
        {
            StopCoroutine(_sigilCoroutine);
        }

        _sigilCoroutine = StartCoroutine(SigilAppearCoroutine());
    }

    private void ReleaseChargeVFX()
    {
        for (int i = 0; i < _chargeAuraParticles.Length; i++)
        {
            _chargeAuraParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        for (int i = 0; i < _chargeCoreParticles.Length; i++)
        {
            _chargeCoreParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        RestartParticles(_releaseGlowParticles);
        RestartParticles(_releaseRingParticles);

        if (_sigilCoroutine != null)
        {
            StopCoroutine(_sigilCoroutine);
        }

        _sigilCoroutine = StartCoroutine(SigilVanishCoroutine());
    }

    private void SetSimulationSpeed(ParticleSystem[] particles)
    {
        float speed = _chargeVFXReferenceDuration / Mathf.Max(0.01f, _chargeDuration);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.simulationSpeed = speed;
        }
    }

    private IEnumerator SigilAppearCoroutine()
    {
        if (_castSigilRoot == null) yield break;

        float timer = 0f;

        SetSigilRenderersEnabled(true);

        while (true)
        {
            timer += Time.deltaTime;
            _sigilOuterAngle += _sigilOuterRotateSpeed * Time.deltaTime;
            _sigilInnerAngle += _sigilInnerRotateSpeed * Time.deltaTime;

            float appearProgress = Mathf.Clamp01(timer / _sigilAppearDuration);
            float easeOutProgress = 1f - Mathf.Pow(1f - appearProgress, 3f);
            float chargeProgress = Mathf.Clamp01(timer / _chargeDuration);

            ApplySigil(Mathf.Lerp(_sigilStartScale, _sigilEndScale, easeOutProgress),
                Mathf.Lerp(_sigilStartAlpha, _sigilEndAlpha, chargeProgress * chargeProgress));

            yield return null;
        }
    }

    private IEnumerator SigilVanishCoroutine()
    {
        if (_castSigilRoot == null) yield break;

        float timer = 0f;
        float startScale = _castSigilRoot.localScale.x;
        float startAlpha = _castSigilOuter != null ? _castSigilOuter.color.a : _sigilEndAlpha;

        while (timer < _sigilVanishDuration)
        {
            timer += Time.deltaTime;
            _sigilOuterAngle += _sigilOuterRotateSpeed * 3f * Time.deltaTime;
            _sigilInnerAngle += _sigilInnerRotateSpeed * 3f * Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _sigilVanishDuration);
            float easeInProgress = progress * progress;

            ApplySigil(Mathf.Lerp(startScale, 0f, easeInProgress), Mathf.Lerp(startAlpha, 0f, progress));

            yield return null;
        }

        HideSigil();
        _sigilCoroutine = null;
    }

    private void ApplySigil(float scale, float alpha)
    {
        _castSigilRoot.localScale = new Vector3(scale, scale * _sigilFlatten, 1f);

        ApplySigilRenderer(_castSigilOuter, _sigilOuterAngle, alpha);
        ApplySigilRenderer(_castSigilInner, _sigilInnerAngle, alpha);
    }

    private void ApplySigilRenderer(SpriteRenderer sigilRenderer, float angle, float alpha)
    {
        if (sigilRenderer == null) return;

        Color color = sigilRenderer.color;
        color.a = alpha;

        sigilRenderer.color = color;
        sigilRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void HideSigil()
    {
        ApplySigilAlpha(_castSigilOuter, 0f);
        ApplySigilAlpha(_castSigilInner, 0f);
        SetSigilRenderersEnabled(false);
    }

    private void ApplySigilAlpha(SpriteRenderer sigilRenderer, float alpha)
    {
        if (sigilRenderer == null) return;

        Color color = sigilRenderer.color;
        color.a = alpha;

        sigilRenderer.color = color;
    }

    private void SetSigilRenderersEnabled(bool isEnabled)
    {
        if (_castSigilOuter != null)
        {
            _castSigilOuter.enabled = isEnabled;
        }

        if (_castSigilInner != null)
        {
            _castSigilInner.enabled = isEnabled;
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

    private GameObject SpawnTracked(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;

        _spawnedObjects.RemoveAll(obj => obj == null);

        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        _spawnedObjects.Add(spawned);

        return spawned;
    }

    private void RetireMagicOrb(GameObject magicOrb)
    {
        if (magicOrb == null) return;

        ParticleSystem[] particles = magicOrb.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(magicOrb, _magicOrbFadeOutTime);
    }

    private void SpawnArrivalFlash(Vector3 position)
    {
        if (_magicArrivalFlashPrefab == null) return;

        GameObject flash = Instantiate(_magicArrivalFlashPrefab, position, Quaternion.identity);
        Destroy(flash, _magicArrivalFlashLifetime);
    }

    private bool IsTargetAlive(GameObject target)
    {
        return target != null && target.activeInHierarchy;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _sigilCoroutine = null;

        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            if (_spawnedObjects[i] != null)
            {
                Destroy(_spawnedObjects[i]);
            }
        }

        _spawnedObjects.Clear();

        ClearParticles(_chargeAuraParticles);
        ClearParticles(_chargeCoreParticles);
        ClearParticles(_releaseGlowParticles);
        ClearParticles(_releaseRingParticles);
        HideSigil();
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