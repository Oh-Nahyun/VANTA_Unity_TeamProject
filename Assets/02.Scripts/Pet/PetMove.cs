using UnityEngine;

public class PetMove : MonoBehaviour
{
    [Header("추적할 플레이어 클래스")]
    [SerializeField] private Player _player;

    [Header("추적할 보스")]
    [SerializeField] private Transform _boss;

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

    [Header("추적을 멈추는 거리 비율")]
    [SerializeField] private float _chaseStopDistanceRatio = 0.7f;

    [Header("플레이어를 바라보는 방향을 바꾸지 않는 거리")]
    [SerializeField] private float _flipDeadZone = 0.05f;

    [Header("보스 방향 전환 대기 시간")]
    [SerializeField] private float _bossDirectionChangeDelay = 0.3f;

    [Header("보스 방향 판정 최소 거리")]
    [SerializeField] private float _bossDirectionDeadZone = 0.2f;

    [Header("보스와 펫 사이의 최소 거리")]
    [SerializeField] private float _minBossDistance = 3f;

    /// <summary>
    /// 펫 클래스
    /// </summary>
    private Pet _pet;

    /// <summary>
    /// 펫 이동 시 나타날 먼지 파티클 렌더러
    /// </summary>
    private ParticleSystemRenderer _footStepDustRenderer;

    /// <summary>
    /// 추적 거리 판정에 사용할 플레이어 콜라이더
    /// </summary>
    private Collider2D _playerCollider;

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
    /// 현재 전투 상태인지 확인
    /// </summary>
    private bool _isInGame;

    /// <summary>
    /// 펫이 유지해야 할 보스 반대 방향
    /// </summary>
    private int _bossDirection;

    /// <summary>
    /// 변경 예정인 보스 방향
    /// </summary>
    private int _pendingBossDirection;

    /// <summary>
    /// 보스 방향 변경 대기 시간
    /// </summary>
    private float _bossDirectionTimer;

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
        _playerCollider = _player.GetComponent<Collider2D>();
        _footStepDustRenderer = _footStepDustVFX.GetComponent<ParticleSystemRenderer>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeRun = 1.7f;
        _canChase = true;
        CanMove = true;

        _nextIdleSoundInterval = Random.Range(
            _idleSoundMinInterval,
            _idleSoundMaxInterval
        );
    }

    private void Update()
    {
        if (!CanMove) return;

        _timer += Time.deltaTime;

        if (_isInGame && FindBoss())
        {
            UpdateBossDirection();
            UpdateInGameChaseState();
        }
        else
        {
            UpdateChaseState();
        }

        LookAtPlayer();

        if (_canChase && _timer >= _timeBeforeRun)
        {
            if (_isInGame && _boss != null)
            {
                ChaseInGame();
            }
            else
            {
                Chase();
            }
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
        _nextIdleSoundInterval = Random.Range(
            _idleSoundMinInterval,
            _idleSoundMaxInterval
        );

        PlayRandomIdleSound();
    }

    private void PlayRandomIdleSound()
    {
        if (_idleSounds == null || _idleSounds.Length == 0) return;

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
        _pet.Animator.SetBool("IsRun", _canChase && _timer >= _timeBeforeRun);
    }

    private void Chase()
    {
        Vector2 direction = _player.transform.position - transform.position;
        direction.Normalize();

        Move(direction);
    }

    private void ChaseInGame()
    {
        Vector3 targetPosition = GetInGameTargetPosition();

        Vector2 direction = targetPosition - transform.position;
        direction.Normalize();

        Move(direction);
    }

    private void Move(Vector2 direction)
    {
        Vector3 dustFlip = _footStepDustRenderer.flip;
        Vector3 dustOffset = new Vector3(0f, _footStepDustOffsetY, 0f);

        if (_pet.SpriteRenderer.flipX)
        {
            dustOffset.x = -_footStepDustOffsetX;
            dustFlip.x = 1f;
        }
        else
        {
            dustOffset.x = _footStepDustOffsetX;
            dustFlip.x = 0f;
        }

        transform.Translate(
            (Vector3)(direction * _moveSpeed) * Time.deltaTime,
            Space.World
        );

        _footStepDustVFX.transform.position = transform.position + dustOffset;
        _footStepDustRenderer.flip = dustFlip;
    }

    private void UpdateChaseState()
    {
        float distance = _pet.CircleCollider2D.Distance(_playerCollider).distance;

        if (_canChase)
        {
            float worldRadius = _pet.CircleCollider2D.bounds.extents.x;
            float stopDepth = worldRadius * (1f - _chaseStopDistanceRatio);

            if (distance <= -stopDepth)
            {
                _canChase = false;
            }
        }
        else if (distance > 0f)
        {
            _canChase = true;
        }
    }

    private void UpdateInGameChaseState()
    {
        Vector3 targetPosition = GetInGameTargetPosition();

        float distance = Vector2.Distance(
            transform.position,
            targetPosition
        );

        float worldRadius = _pet.CircleCollider2D.bounds.extents.x;

        float stopDistance = worldRadius *
                             (1f - _chaseStopDistanceRatio);

        float resumeDistance = worldRadius;

        if (_canChase)
        {
            if (distance <= stopDistance)
            {
                _canChase = false;
            }
        }
        else if (distance > resumeDistance)
        {
            _canChase = true;
        }
    }

    private Vector3 GetInGameTargetPosition()
    {
        Vector3 playerPosition = _player.transform.position;
        Vector3 targetPosition = playerPosition;

        targetPosition.x -= _bossDirection * _inGameChaseDistance;

        float bossDistance = Mathf.Abs(_boss.position.x - targetPosition.x);

        if (bossDistance < _minBossDistance)
        {
            targetPosition.x = _boss.position.x - _bossDirection * _minBossDistance;
        }

        targetPosition.z = transform.position.z;

        return targetPosition;
    }

    private bool FindBoss()
    {
        if (_boss != null) return true;

        GameObject bossObject = GameObject.FindWithTag("Boss");

        if (bossObject == null) return false;

        _boss = bossObject.transform;

        InitializeBossDirection();

        return true;
    }

    private void InitializeBossDirection()
    {
        if (_boss == null) return;

        float distanceX =
            _boss.position.x - _player.transform.position.x;

        if (Mathf.Abs(distanceX) <= _bossDirectionDeadZone)
        {
            _bossDirection = (_bossDirection == 0) ? 1 : _bossDirection;
        }
        else
        {
            _bossDirection = distanceX > 0f ? 1 : -1;
        }

        _pendingBossDirection = 0;
        _bossDirectionTimer = 0f;
    }

    private void UpdateBossDirection()
    {
        float distanceX =
            _boss.position.x - _player.transform.position.x;

        if (Mathf.Abs(distanceX) <= _bossDirectionDeadZone)
        {
            _pendingBossDirection = 0;
            _bossDirectionTimer = 0f;
            return;
        }

        int newDirection = distanceX > 0f ? 1 : -1;

        if (newDirection == _bossDirection)
        {
            _pendingBossDirection = 0;
            _bossDirectionTimer = 0f;
            return;
        }

        if (newDirection != _pendingBossDirection)
        {
            _pendingBossDirection = newDirection;
            _bossDirectionTimer = 0f;
        }

        _bossDirectionTimer += Time.deltaTime;

        if (_bossDirectionTimer >= _bossDirectionChangeDelay)
        {
            _bossDirection = newDirection;
            _pendingBossDirection = 0;
            _bossDirectionTimer = 0f;
        }
    }

    private void LookAtPlayer()
    {
        float directionX;

        if (_canChase && _timer >= _timeBeforeRun)
        {
            if (_isInGame && _boss != null)
            {
                directionX = GetInGameTargetPosition().x - transform.position.x;
            }
            else
            {
                directionX = _player.transform.position.x - transform.position.x;
            }
        }
        else
        {
            directionX = _player.transform.position.x - transform.position.x;
        }

        if (Mathf.Abs(directionX) < _flipDeadZone) return;

        _pet.SpriteRenderer.flipX = directionX < 0f;
    }

    public void PlayFootStepDustParticle()
    {
        _footStepDustVFX.Emit(_footStepDustCount);
        SoundManager.Instance.PlayEffectSound(_footStepSound);
    }

    public void ChangeChaseDistanceWhenGameStart()
    {
        _isInGame = false;
        _pet.CircleCollider2D.radius = _gameStartChaseDistance;
        _canChase = true;
    }

    public void ChangeChaseDistanceWhenInGame()
    {
        _isInGame = true;
        _pet.CircleCollider2D.radius = _inGameChaseDistance;
        _canChase = true;

        if (FindBoss())
        {
            InitializeBossDirection();
        }
    }
}
