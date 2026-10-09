using UnityEngine;

public class CrystalPrisonVFX : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 생성 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonCreateVFX;

    [Header("펫 크리스탈 프리즌 유지 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonHoldVFX;

    [Header("펫 크리스탈 프리즌 종료 시 유지 이펙트가 사라지는 속도 배율")]
    [SerializeField] private float _crystalPrisonHoldEndSpeed = 4f;

    [Header("펫 크리스탈 프리즌 파괴 준비 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonBreakReadyVFX;

    [Header("펫 크리스탈 프리즌 파괴 준비 시 유지 이펙트 중단 여부")]
    [SerializeField] private bool _isStopHoldVFXWhenBreakReady = true;

    [Header("펫 크리스탈 프리즌 종료 시 파괴 준비 이펙트가 사라지는 속도")]
    [SerializeField] private float _crystalPrisonBreakReadyEndSpeed = 4f;

    [Header("펫 크리스탈 프리즌 파괴 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonBreakVFX;

    [Header("펫 크리스탈 프리즌 파괴 카메라 흔들림 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonImpactShakeVFX;

    [Header("펫 크리스탈 프리즌 파괴 이펙트가 삭제되기까지의 시간")]
    [SerializeField] private float _crystalPrisonBreakVFXLifetime = 1.6f;

    /// <summary>
    /// 펫 크리스탈 프리즌 생성 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonCreateParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 유지 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonHoldParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 준비 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonBreakReadyParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonBreakParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 이펙트 재생 여부 확인용 변수
    /// </summary>
    private bool _isBreakPlaying;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 준비 이펙트 재생 여부 확인용 변수
    /// </summary>
    private bool _isBreakReadyPlaying;

    private void Awake()
    {
        _crystalPrisonCreateParticles = _crystalPrisonCreateVFX.GetComponentsInChildren<ParticleSystem>();
        _crystalPrisonHoldParticles = _crystalPrisonHoldVFX.GetComponentsInChildren<ParticleSystem>();
        _crystalPrisonBreakReadyParticles = _crystalPrisonBreakReadyVFX.GetComponentsInChildren<ParticleSystem>();
        _crystalPrisonBreakParticles = _crystalPrisonBreakVFX.GetComponentsInChildren<ParticleSystem>();
    }

    private void Start()
    {
        PlayCrystalPrisonCreateVFX();
        PlayCrystalPrisonHoldVFX();
    }

    private void PlayCrystalPrisonCreateVFX()
    {
        for (int i = 0; i < _crystalPrisonCreateParticles.Length; i++)
        {
            _crystalPrisonCreateParticles[i].Play();
        }
    }

    public void StopCrystalPrisonCreateVFX()
    {
        for (int i = 0; i < _crystalPrisonCreateParticles.Length; i++)
        {
            _crystalPrisonCreateParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void PlayCrystalPrisonHoldVFX()
    {
        for (int i = 0; i < _crystalPrisonHoldParticles.Length; i++)
        {
            _crystalPrisonHoldParticles[i].Play();
        }
    }

    public void StopCrystalPrisonHoldVFX()
    {
        for (int i = 0; i < _crystalPrisonHoldParticles.Length; i++)
        {
            ParticleSystem.MainModule main = _crystalPrisonHoldParticles[i].main;
            main.simulationSpeed = _crystalPrisonHoldEndSpeed;

            _crystalPrisonHoldParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void PlayCrystalPrisonBreakReadyVFX()
    {
        if (_isBreakReadyPlaying) return;

        _isBreakReadyPlaying = true;

        if (_isStopHoldVFXWhenBreakReady)
        {
            StopCrystalPrisonHoldVFX();
        }

        for (int i = 0; i < _crystalPrisonBreakReadyParticles.Length; i++)
        {
            _crystalPrisonBreakReadyParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _crystalPrisonBreakReadyParticles[i].Play();
        }
    }

    public void StopCrystalPrisonBreakReadyVFX()
    {
        for (int i = 0; i < _crystalPrisonBreakReadyParticles.Length; i++)
        {
            ParticleSystem.MainModule main = _crystalPrisonBreakReadyParticles[i].main;
            main.simulationSpeed = _crystalPrisonBreakReadyEndSpeed;

            _crystalPrisonBreakReadyParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void PlayCrystalPrisonBreakVFX()
    {
        if (_isBreakPlaying) return;

        _isBreakPlaying = true;
        _crystalPrisonBreakVFX.transform.SetParent(null, true);

        for (int i = 0; i < _crystalPrisonBreakParticles.Length; i++)
        {
            _crystalPrisonBreakParticles[i].Play();
        }

        if (_crystalPrisonImpactShakeVFX != null)
        {
            _crystalPrisonImpactShakeVFX.SetActive(true);
        }

        Destroy(_crystalPrisonBreakVFX, _crystalPrisonBreakVFXLifetime);
    }

    private void OnDisable()
    {
        ResetParticles(_crystalPrisonCreateParticles);
        ResetParticles(_crystalPrisonHoldParticles);
        ResetParticles(_crystalPrisonBreakReadyParticles);

        _isBreakReadyPlaying = false;
    }

    private void ResetParticles(ParticleSystem[] particles)
    {
        if (particles == null) return;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;

            ParticleSystem.MainModule main = particles[i].main;
            main.simulationSpeed = 1f;

            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
