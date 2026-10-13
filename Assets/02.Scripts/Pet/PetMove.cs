using UnityEngine;

public class PetMove : MonoBehaviour
{
    [Header("추적할 플레이어 클래스")]
    [SerializeField] private Player _player;

    [Header("펫 이동 속도")]
    [SerializeField] private float _moveSpeed = 1f;

    [Header("펫 이동 시 나타날 먼지 파티클 시스템")]
    [SerializeField] private ParticleSystem _footStepDustVFX;

    [Header("펫 이동 시 나타날 먼지 파티클 X 위치 오프셋")]
    [SerializeField] private float _footStepDustOffsetX = -0.23f;

    [Header("펫 이동 시 나타날 먼지 파티클 Y 위치 오프셋")]
    [SerializeField] private float _footStepDustOffsetY = 0f;

    [Header("펫 이동 시 나타날 먼지 파티클 갯수")]
    [SerializeField] private int _footStepDustCount = 3;

    [Header("펫 이동 시 재생할 발소리 효과음")]
    [SerializeField] private AudioClip _footStepSound;

    [Header("펫 대기 상태에서 랜덤으로 재생할 효과음 목록")]
    [SerializeField] private AudioClip[] _idleSounds;

    [Header("펫 대기 효과음 재생 최소 간격")]
    [SerializeField] private float _idleSoundMinInterval = 4f;

    [Header("펫 대기 효과음 재생 최대 간격")]
    [SerializeField] private float _idleSoundMaxInterval = 8f;

    [Header("펫의 게임 시작 시 플레이어 추적 거리")]
    [SerializeField] private float _gameStartChaseDistance;

    [Header("펫의 전투 시작 시 플레이어 추적 거리")]
    [SerializeField] private float _inGameChaseDistance;

    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// 펫 이동 시 나타날 먼지 파티클 렌더러
    /// </summary>
    private ParticleSystemRenderer _footStepDustRenderer;

    /// <summary>
    /// 펫 스폰 시 이동 전 대기 시간 계산용 변수
    /// </summary>
    private float _timer;

    /// <summary>
    /// 펫이 이동하기 전에 필요한 대기 시간
    /// </summary>
    private float _timeBeforeRun;

    /// <summary>
    /// 펫이 추적 가능한 상태인지 확인용 변수
    /// </summary>
    private bool _canChase;

    /// <summary>
    /// 펫이 대기 상태로 머문 시간 계산용 변수
    /// </summary>
    private float _idleSoundTimer;

    /// <summary>
    /// 다음 대기 효과음을 재생하기까지 필요한 시간
    /// </summary>
    private float _nextIdleSoundInterval;

    /// <summary>
    /// 마지막으로 재생한 대기 효과음 인덱스 저장용 변수
    /// </summary>
    private int _lastIdleSoundIndex = -1;

    /// <summary>
    /// 펫이 이동 가능한 상태인지 확인용 변수
    /// </summary>
    public bool CanMove;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
        _footStepDustRenderer = _footStepDustVFX.GetComponent<ParticleSystemRenderer>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeRun = 1.7f;
        _canChase = true;
        CanMove = true;
        _nextIdleSoundInterval = Random.Range(_idleSoundMinInterval, _idleSoundMaxInterval);
    }

    private void Update()
    {
        if (!CanMove) return;

        _timer += Time.deltaTime;
        if (_canChase && _timer >= _timeBeforeRun)
        {
            Chase();
        }

        Run();
        UpdateIdleSound();
    }

    private void UpdateIdleSound()
    {
        if (_canChase)
        {
            _idleSoundTimer = 0f;
            return;
        }

        _idleSoundTimer += Time.deltaTime;

        if (_idleSoundTimer < _nextIdleSoundInterval) return;

        _idleSoundTimer = 0f;
        _nextIdleSoundInterval = Random.Range(_idleSoundMinInterval, _idleSoundMaxInterval);

        PlayRandomIdleSound();
    }

    private void PlayRandomIdleSound()
    {
        if (_idleSounds.Length == 0) return;

        int index = Random.Range(0, _idleSounds.Length);

        if (_idleSounds.Length > 1 && index == _lastIdleSoundIndex)
        {
            index = (index + 1) % _idleSounds.Length;
        }

        _lastIdleSoundIndex = index;
        SoundManager.Instance.PlayEffectSound(_idleSounds[index]);
    }

    private void Run()
    {
        _pet.Animator.SetBool("IsRun", _canChase);
    }

    private void Chase()
    {
        Vector3 dustFlip = _footStepDustRenderer.flip;
        Vector3 dustOffset = new Vector3(0f, _footStepDustOffsetY, 0f);
        Vector2 direction = _player.transform.position - transform.position;
        direction.Normalize();

        if (direction.x < 0)
        {
            _pet.SpriteRenderer.flipX = true;
            dustOffset.x = -_footStepDustOffsetX;
            dustFlip.x = 1f;
        }
        else
        {
            _pet.SpriteRenderer.flipX = false;
            dustOffset.x = _footStepDustOffsetX;
            dustFlip.x = 0f;
        }

        transform.Translate((Vector3)(direction * _moveSpeed) * Time.deltaTime);
        _footStepDustVFX.transform.position = transform.position + dustOffset;
        _footStepDustRenderer.flip = dustFlip;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _canChase = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _canChase = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _canChase = true;
    }

    public void PlayFootStepDustParticle()
    {
        _footStepDustVFX.Emit(_footStepDustCount);
        SoundManager.Instance.PlayEffectSound(_footStepSound);
    }

    public void ChangeChaseDistanceWhenGameStart()
    {
        _pet.CircleCollider2D.radius = _gameStartChaseDistance;
    }

    public void ChangeChaseDistanceWhenInGame()
    {
        _pet.CircleCollider2D.radius = _inGameChaseDistance;
    }
}
