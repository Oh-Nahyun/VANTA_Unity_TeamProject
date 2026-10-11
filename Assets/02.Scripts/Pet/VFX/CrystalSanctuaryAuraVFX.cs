using System.Collections;
using UnityEngine;

public class CrystalSanctuaryAuraVFX : MonoBehaviour
{
    [Header("펫 크리스탈 생츄어리 유지 이펙트가 서서히 나타나는 시간")]
    [SerializeField] private float _fadeInDuration = 1f;

    [Header("펫 크리스탈 생츄어리 유지 이펙트 입자 수 배율")]
    [SerializeField] private float _particleCountScale = 1f;

    [Header("펫 크리스탈 생츄어리 플레이어가 밖에 있을 때 이펙트 색상")]
    [SerializeField] private Color _outsideColor = new Color(0.3f, 0.85f, 0.95f, 1f);

    [Header("펫 크리스탈 생츄어리 플레이어가 안에 있을 때 이펙트 색상")]
    [SerializeField] private Color _insideColor = new Color(0.25f, 0.4f, 1f, 1f);

    [Header("펫 크리스탈 생츄어리 소멸 직전 경고 연출 시간")]
    [SerializeField] private float _collapseWarningTime = 0.3f;

    [Header("펫 크리스탈 생츄어리 소멸 직전 마법진 에너지가 수축하는 크기 비율")]
    [SerializeField, Range(0f, 1f)] private float _collapseRingShrinkRatio = 0.7f;

    [Header("펫 크리스탈 생츄어리 소멸 직전 유지 이펙트 파티클 속도 배율")]
    [SerializeField] private float _collapseSimulationSpeed = 2.5f;

    [Header("펫 크리스탈 생츄어리 소멸 직전 마법진 에너지가 불안정하게 흔들리는 세기")]
    [SerializeField] private float _collapseFlickerStrength = 1.2f;

    [Header("펫 크리스탈 생츄어리 유지 이펙트 루트")]
    [SerializeField] private Transform _auraRoot;

    [Header("펫 크리스탈 생츄어리 마법진 테두리 에너지 트랜스폼")]
    [SerializeField] private Transform _ringEnergy;

    [Header("펫 크리스탈 생츄어리 유지 이펙트 오브젝트")]
    [SerializeField] private GameObject _resonanceVFX;

    [Header("펫 크리스탈 생츄어리 소멸 직전 떨어지는 파편 이펙트 오브젝트")]
    [SerializeField] private GameObject _collapseWarningVFX;

    [Header("펫 크리스탈 생츄어리 소멸 시 빛 입자로 분해되는 이펙트 오브젝트")]
    [SerializeField] private GameObject _collapseDissolveVFX;

    [Header("펫 크리스탈 생츄어리 소멸 후 남는 약한 잔광 이펙트 오브젝트")]
    [SerializeField] private GameObject _afterGlowVFX;

    [Header("펫 크리스탈 생츄어리 유지 중 반복 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryHoldSound;

    [Header("펫 크리스탈 생츄어리 유지 반복 효과음 볼륨 비율")]
    [SerializeField, Range(0f, 1f)] private float _crystalSanctuaryHoldSoundVolume = 0.4f;

    [Header("펫 크리스탈 생츄어리 소멸 직전 경고 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryCollapseWarnSound;

    [Header("펫 크리스탈 생츄어리 소멸 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryCollapseSound;

    [Header("펫 크리스탈 생츄어리 소멸 관련 효과음 볼륨 비율")]
    [SerializeField, Range(0f, 1f)] private float _crystalSanctuaryCollapseSoundVolume = 0.5f;

    /// <summary>
    /// 펫 크리스탈 생츄어리 소멸 직전 경고 연출 시간. 생츄어리가 경고를 시작할 시점 계산용
    /// </summary>
    public float CollapseWarningTime => _collapseWarningTime;

    /// <summary>
    /// 펫 크리스탈 생츄어리 콜라이더 2D
    /// </summary>
    private CapsuleCollider2D _capsuleCollider2D;

    /// <summary>
    /// 펫 크리스탈 생츄어리 유지 이펙트 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _resonanceParticles;

    /// <summary>
    /// 펫 크리스탈 생츄어리 소멸 직전 파편 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _collapseWarningParticles;

    /// <summary>
    /// 펫 크리스탈 생츄어리 분해 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _collapseDissolveParticles;

    /// <summary>
    /// 펫 크리스탈 생츄어리 잔광 파티클 시스템 목록
    /// </summary>
    private ParticleSystem[] _afterGlowParticles;

    /// <summary>
    /// 유지 이펙트 파티클 시스템별 기본 초당 방출량
    /// </summary>
    private float[] _baseRates;

    /// <summary>
    /// 펫 크리스탈 생츄어리 생성 시 콜라이더 폭. 반지름 감소 비율 계산 기준
    /// </summary>
    private float _initialWidth;

    /// <summary>
    /// 펫 크리스탈 생츄어리 마법진 테두리 에너지 기본 스케일
    /// </summary>
    private Vector3 _ringBaseScale;

    /// <summary>
    /// 소멸 직전 마법진 에너지 수축 비율
    /// </summary>
    private float _ringShrinkFactor = 1f;

    /// <summary>
    /// 유지 이펙트가 나타난 정도 (0 ~ 1). 입자 방출량에 곱해진다.
    /// </summary>
    private float _fadeFactor;

    /// <summary>
    /// 소멸 직전 마법진 에너지 흔들림 배율
    /// </summary>
    private float _flickerFactor = 1f;

    /// <summary>
    /// 소멸 연출이 시작되어 유지 이펙트 방출이 끝났는지 확인용 변수
    /// </summary>
    private bool _isCollapsed;

    /// <summary>
    /// 펫 크리스탈 생츄어리 유지 중 반복 효과음 오디오 소스
    /// </summary>
    private AudioSource _crystalSanctuaryHoldSoundSource;

    private void Awake()
    {
        _capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        _initialWidth = _capsuleCollider2D.size.x;

        _resonanceParticles = GetParticles(_resonanceVFX);
        _collapseWarningParticles = GetParticles(_collapseWarningVFX);
        _collapseDissolveParticles = GetParticles(_collapseDissolveVFX);
        _afterGlowParticles = GetParticles(_afterGlowVFX);

        _baseRates = new float[_resonanceParticles.Length];

        for (int i = 0; i < _resonanceParticles.Length; i++)
        {
            _baseRates[i] = _resonanceParticles[i].emission.rateOverTimeMultiplier;
        }

        if (_ringEnergy != null)
        {
            _ringBaseScale = _ringEnergy.localScale;
        }

        _auraRoot.localPosition = _capsuleCollider2D.offset;
    }

    private ParticleSystem[] GetParticles(GameObject vfx)
    {
        if (vfx == null) return new ParticleSystem[0];

        return vfx.GetComponentsInChildren<ParticleSystem>();
    }

    private void Start()
    {
        SetPlayerInside(false);
        PlayParticles(_resonanceParticles);
        StartCoroutine(FadeInCoroutine());
        _crystalSanctuaryHoldSoundSource = SoundManager.Instance.PlayLoopSound(_crystalSanctuaryHoldSound,
            _crystalSanctuaryHoldSoundVolume, _fadeInDuration);
    }

    private void Update()
    {
        // 생츄어리 반지름(콜라이더)이 줄어들면 보조 이펙트 범위도 같이 줄인다.
        float ratio = Mathf.Clamp01(_capsuleCollider2D.size.x / _initialWidth);

        _auraRoot.localScale = Vector3.one * ratio;
        ApplyRingScale();
        ApplyEmission();
    }

    /// <summary>
    /// 플레이어가 생츄어리 안/밖에 있는 상태에 맞춰 이펙트 색상을 바꾼다. 새로 나오는 입자부터 적용되어 자연스럽게 전환된다.
    /// </summary>
    public void SetPlayerInside(bool isInside)
    {
        Color color = isInside ? _insideColor : _outsideColor;

        SetStartColor(_resonanceParticles, color);
        SetStartColor(_collapseWarningParticles, color);
        SetStartColor(_collapseDissolveParticles, color);
        SetStartColor(_afterGlowParticles, color);
    }

    public void PlayCollapseWarning()
    {
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryCollapseWarnSound, _crystalSanctuaryCollapseSoundVolume);
        StopCrystalSanctuaryHoldSound(_collapseWarningTime);
        StartCoroutine(CollapseWarningCoroutine());
    }

    public void PlayCollapse()
    {
        _isCollapsed = true;
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryCollapseSound, _crystalSanctuaryCollapseSoundVolume);

        StopEmitting(_collapseWarningParticles);
        StopEmitting(_resonanceParticles);
        PlayParticles(_collapseDissolveParticles);
        PlayParticles(_afterGlowParticles);
    }

    private IEnumerator FadeInCoroutine()
    {
        float timer = 0f;

        while (timer < _fadeInDuration)
        {
            timer += Time.deltaTime;
            _fadeFactor = Mathf.Clamp01(timer / _fadeInDuration);

            yield return null;
        }

        _fadeFactor = 1f;
    }

    private IEnumerator CollapseWarningCoroutine()
    {
        float timer = 0f;

        PlayParticles(_collapseWarningParticles);
        SetSimulationSpeed(_resonanceParticles, _collapseSimulationSpeed);

        while (timer < _collapseWarningTime)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / _collapseWarningTime);
            float noise = Mathf.PerlinNoise(Time.time * 25f, 0f);

            _ringShrinkFactor = Mathf.Lerp(1f, _collapseRingShrinkRatio, progress * progress);
            _flickerFactor = 1f + noise * _collapseFlickerStrength;

            yield return null;
        }

        _flickerFactor = 1f;
    }

    private void ApplyRingScale()
    {
        if (_ringEnergy == null) return;

        _ringEnergy.localScale = _ringBaseScale * _ringShrinkFactor;
    }

    private void ApplyEmission()
    {
        for (int i = 0; i < _resonanceParticles.Length; i++)
        {
            ParticleSystem.EmissionModule emission = _resonanceParticles[i].emission;
            float factor = _isCollapsed ? 0f : _fadeFactor * _flickerFactor * _particleCountScale;

            emission.rateOverTimeMultiplier = _baseRates[i] * factor;
        }
    }

    private void SetStartColor(ParticleSystem[] particles, Color color)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startColor = color;
        }
    }

    private void SetSimulationSpeed(ParticleSystem[] particles, float speed)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.simulationSpeed = speed;
        }
    }

    private void PlayParticles(ParticleSystem[] particles)
    {
        for (int i = 0; i < particles.Length; i++)
        {
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

    private void StopCrystalSanctuaryHoldSound(float fadeOutDuration)
    {
        if (_crystalSanctuaryHoldSoundSource == null) return;

        SoundManager.Instance.StopLoopSound(_crystalSanctuaryHoldSoundSource, fadeOutDuration);
        _crystalSanctuaryHoldSoundSource = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (SoundManager.Instance == null) return;

        StopCrystalSanctuaryHoldSound(0f);
    }
}
