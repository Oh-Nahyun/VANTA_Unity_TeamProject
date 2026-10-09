using UnityEngine;

public class PetMove : MonoBehaviour
{
    [Header("추적할 플레이어 클래스")]
    [SerializeField] private Player _player;

    [Header("펫 이동 속도")]
    [SerializeField] private float _moveSpeed = 1f;

    [Header("펫 이동 시 나타날 먼지 파티클 시스템")]
    [SerializeField] private ParticleSystem _footStepDustParticle;

    [Header("펫 이동 시 나타날 먼지 파티클 X 위치 오프셋")]
    [SerializeField] private float _footStepDustOffsetX = -0.23f;

    [Header("펫 이동 시 나타날 먼지 파티클 Y 위치 오프셋")]
    [SerializeField] private float _footStepDustOffsetY = -0.09f;

    [Header("펫 이동 시 나타날 먼지 파티클 갯수")]
    [SerializeField] private int _footStepDustCount = 3;

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
    /// 펫의 게임 시작 시 플레이어 추적 거리
    /// </summary>
    private float _gameStartChaseDistance;

    /// <summary>
    /// 펫의 전투 시작 시 플레이어 추적 거리
    /// </summary>
    private float _inGameChaseDistance;

    /// <summary>
    /// 펫이 이동 가능한 상태인지 확인용 변수
    /// </summary>
    public bool CanMove;

    private void Awake()
    {
        _pet = GetComponent<Pet>();
        _footStepDustRenderer = _footStepDustParticle.GetComponent<ParticleSystemRenderer>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeRun = 1.7f;
        _canChase = true;
        _gameStartChaseDistance = 1f;
        _inGameChaseDistance = 2.5f;
        CanMove = true;

        ChangeChaseDistanceWhenGameStart();
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
        _footStepDustParticle.transform.position = transform.position + dustOffset;
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
        _footStepDustParticle.Emit(_footStepDustCount);
    }

    private void ChangeChaseDistanceWhenGameStart()
    {
        _pet.CircleCollider2D.radius = _gameStartChaseDistance;
    }

    public void ChangeChaseDistanceWhenInGame()
    {
        _pet.CircleCollider2D.radius = _inGameChaseDistance;
    }
}
