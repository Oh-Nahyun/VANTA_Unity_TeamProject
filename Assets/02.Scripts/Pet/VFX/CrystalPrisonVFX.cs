using UnityEngine;

public class CrystalPrisonVFX : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 생성 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonCreateVFX;

    [Header("펫 크리스탈 프리즌 유지 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonHoldVFX;

    [Header("펫 크리스탈 프리즌 종료 시 유지 이펙트가 사라지는 속도 배율")]
    [SerializeField] private float _crystalPrisonHoldEndSpeed = 4f;

    /// <summary>
    /// 펫 크리스탈 프리즌 생성 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonCreateParticles;

    /// <summary>
    /// 펫 크리스탈 프리즌 유지 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonHoldParticles;

    private void Awake()
    {
        _crystalPrisonCreateParticles = _crystalPrisonCreateVFX.GetComponentsInChildren<ParticleSystem>();
        _crystalPrisonHoldParticles = _crystalPrisonHoldVFX.GetComponentsInChildren<ParticleSystem>();
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
}
