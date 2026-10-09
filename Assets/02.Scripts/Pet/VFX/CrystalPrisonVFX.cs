using UnityEngine;

public class CrystalPrisonVFX : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 생성 이펙트 오브젝트")]
    [SerializeField] private GameObject _crystalPrisonCreateVFX;

    /// <summary>
    /// 펫 크리스탈 프리즌 생성 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _crystalPrisonCreateParticles;

    private void Awake()
    {
        _crystalPrisonCreateParticles = _crystalPrisonCreateVFX.GetComponentsInChildren<ParticleSystem>();
    }

    private void Start()
    {
        PlayCrystalPrisonCreateVFX();
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
}
