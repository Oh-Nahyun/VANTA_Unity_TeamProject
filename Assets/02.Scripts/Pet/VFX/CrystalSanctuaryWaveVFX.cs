using UnityEngine;

public class CrystalSanctuaryWaveVFX : MonoBehaviour
{
    [Header("펫 크리스탈 생츄어리 생성 시 바깥으로 퍼지는 파동 이펙트 프리팹")]
    [SerializeField] private GameObject _spawnWaveVFXPrefab;

    [Header("펫 크리스탈 생츄어리 생성 파동이 퍼지는 거리 (생츄어리 지름 대비 배율)")]
    [SerializeField] private float _spawnWaveScale = 1.8f;

    [Header("펫 크리스탈 생츄어리 플레이어 진입 시 파동 이펙트 프리팹")]
    [SerializeField] private GameObject _enterWaveVFXPrefab;

    [Header("펫 크리스탈 생츄어리 플레이어 이탈 시 파동 이펙트 프리팹")]
    [SerializeField] private GameObject _exitWaveVFXPrefab;

    [Header("펫 크리스탈 생츄어리 파동 이펙트 프리팹이 기준으로 만들어진 지름")]
    [SerializeField] private float _waveReferenceWidth = 3.2f;

    [Header("펫 크리스탈 생츄어리 파동 이펙트 삭제 시간")]
    [SerializeField] private float _waveVFXLifetime = 1f;

    [Header("펫 크리스탈 생츄어리 생성 직후 플레이어 진입 파동을 건너뛰는 시간 (생성 파동과 겹치지 않게 함)")]
    [SerializeField] private float _enterWaveSuppressTime = 0.7f;

    [Header("펫 크리스탈 생츄어리 플레이어 진입 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryEnterSound;

    [Header("펫 크리스탈 생츄어리 플레이어 이탈 효과음")]
    [SerializeField] private AudioClip _crystalSanctuaryExitSound;

    /// <summary>
    /// 펫 크리스탈 생츄어리 콜라이더 2D
    /// </summary>
    private CapsuleCollider2D _capsuleCollider2D;

    /// <summary>
    /// 플레이어가 크리스탈 생츄어리 안에 있는지 확인용 변수. 진입/이탈 파동이 경계에서 반복해서 나오지 않게 하기 위한 변수
    /// </summary>
    private bool _isPlayerInside;

    /// <summary>
    /// 펫 크리스탈 생츄어리 생성 시각. 생성 직후 진입 파동 건너뛰기 계산용
    /// </summary>
    private float _spawnTime;

    private void Awake()
    {
        _capsuleCollider2D = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        _spawnTime = Time.time;
        PlayWaveVFX(_spawnWaveVFXPrefab, _spawnWaveScale);
    }

    public void PlayEnterWaveVFX()
    {
        if (_isPlayerInside) return;

        _isPlayerInside = true;

        if (Time.time - _spawnTime < _enterWaveSuppressTime) return;

        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryEnterSound);
        PlayWaveVFX(_enterWaveVFXPrefab, 1f);
    }

    public void PlayExitWaveVFX()
    {
        if (!_isPlayerInside) return;

        _isPlayerInside = false;
        SoundManager.Instance.PlayEffectSound(_crystalSanctuaryExitSound);
        PlayWaveVFX(_exitWaveVFXPrefab, 1f);
    }

    private void PlayWaveVFX(GameObject wavePrefab, float scaleMultiplier)
    {
        // 생성 직후에는 콜라이더 bounds가 물리 동기화 전일 수 있어서 크기와 중심을 트랜스폼으로 직접 계산한다.
        float width = _capsuleCollider2D.size.x * Mathf.Abs(transform.lossyScale.x);
        Vector3 center = transform.TransformPoint(_capsuleCollider2D.offset);

        GameObject wave = Instantiate(wavePrefab, center, Quaternion.identity);
        wave.transform.localScale = Vector3.one * (width / _waveReferenceWidth * scaleMultiplier);

        Destroy(wave, _waveVFXLifetime);
    }
}