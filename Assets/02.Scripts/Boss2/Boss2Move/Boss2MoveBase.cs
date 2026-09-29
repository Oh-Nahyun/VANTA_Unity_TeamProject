
using System.Collections.Generic;
using UnityEngine;


public class Boss2MoveBase : MonoBehaviour, IBossMovable
{
    private enum MoveState { Normal, Dodge }

    [Header("이동")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 10f;
    // 가속도 단위는 units/s^2 이다. 가속을 감속보다 크게 잡아야
    // 조작은 즉각 반응하면서 멈출 때만 묵직하게 미끄러진다.
    [SerializeField] private float _acceleration = 40f;
    [SerializeField] private float _deceleration = 25f;

    [Header("구르기")]
    [SerializeField] private float _dodgeSpeed = 30f;
    [SerializeField] private float _dodgeEndSpeed = 6f;
    [SerializeField] private float _dodgeDuration = 0.25f;
    [SerializeField] private float _dodgeCooldown = 0.6f;
    [SerializeField] private float _doubleTapInterval = 0.25f;

    [Header("무적 프레임 (구르기 시작 후 경과 시간 기준)")]
    [SerializeField] private float _iFrameStart = 0.05f;
    [SerializeField] private float _iFrameEnd = 0.2f;

    [Header("테스트 입력")]
    // 끄면 키 입력을 무시하고 자식의 TickPattern 이 기본 이동을 호출해 움직인다.
    [SerializeField] private bool _useKeyboardInput = true;

    private const float InputDeadZone = 0.0001f;

    // 기본 이동(Walk / Run / StopMove)이 세팅하고 UpdateVelocity 가 읽는 이번 프레임의 이동 의도.
    private Vector2 _moveDirection;
    private bool _wantsSprint;

    // 방향과 크기를 함께 가진 단일 상태.
    // 입력 방향을 매 프레임 곱하지 않으므로 키를 떼도 속도가 남아 감속이 눈에 보인다.
    private Vector2 _velocity;

    private MoveState _state = MoveState.Normal;
    private Vector2 _dodgeDir;
    private float _dodgeTimer;
    private float _dodgeCooldownTimer;

    // 외부 효과(크리스탈 프리즌 등)에 의한 이동 봉쇄.
    private bool _isMoveBlocked;
    // 봉쇄를 풀 때 되돌릴 원래 제약 (회전 고정 등).
    private RigidbodyConstraints2D _constraintsBeforeBlock;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody2D;
    [SerializeField] private GameObject _attackZone;
    

    private static readonly (KeyCode key, Vector2 dir)[] DodgeKeys =
    {
        (KeyCode.W, Vector2.up),        (KeyCode.S, Vector2.down),
        (KeyCode.A, Vector2.left),      (KeyCode.D, Vector2.right),
        (KeyCode.UpArrow, Vector2.up),  (KeyCode.DownArrow, Vector2.down),
        (KeyCode.LeftArrow, Vector2.left), (KeyCode.RightArrow, Vector2.right),
    };

    private readonly Dictionary<KeyCode, float> _lastTapTime = new Dictionary<KeyCode, float>();

    public bool IsMoveBlocked => _isMoveBlocked;
    public bool IsDodging => _state == MoveState.Dodge;
    public bool IsSprinting => _state == MoveState.Normal && _velocity.magnitude > _walkSpeed + 0.1f;
    public float CurrentSpeed => _velocity.magnitude;

    public bool IsInvincible
    {
        get
        {
            if (_state != MoveState.Dodge) return false;
            float elapsed = _dodgeDuration - _dodgeTimer;
            return elapsed >= _iFrameStart && elapsed <= _iFrameEnd;
        }
    }
    private void Awake()
    {
        _animator =  GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // TickTimers 가 UpdateVelocity 보다 먼저 와야 구르기가 끝난 프레임에
        // 곧바로 Normal 규칙으로 속도가 계산되어 한 프레임이 비지 않는다.
        // 쿨타임도 먼저 줄어 있어야 이번 프레임의 TryDodge 판정이 정확하다.
        TickTimers();

        // 봉쇄 중에는 입력과 패턴을 모두 무시하고 제자리에 있는다. 쿨타임은 위에서 계속 흐른다.
        if (_isMoveBlocked)
            return;

        if (_useKeyboardInput)
        {
            ReadKeyboard();
        }
        else
        {
            TickPattern();
        }

        // 여기서 애니메이션 제어를 매번 해주면 프레임드랍이...
        _animator.SetBool("IsMove", _moveDirection.sqrMagnitude > InputDeadZone);

        UpdateVelocity();
        ApplyMove();
    }

    /// <summary>
    /// 자식이 기본 이동(Walk / Run / TryDodge / StopMove)을 조합해 상위 이동을 만드는 자리.
    /// 키 입력을 끈 상태에서 매 프레임 호출된다.
    /// </summary>
    protected virtual void TickPattern()
    {
    }

    // ---- 기본 이동 ----
    protected void Walk(Vector2 direction)
    {
        _moveDirection = direction.normalized;
        _wantsSprint = false;
    }

    protected void Run(Vector2 direction)
    {
        _moveDirection = direction.normalized;
        _wantsSprint = true;
    }

    protected void StopMove()
    {
        _moveDirection = Vector2.zero;
        _wantsSprint = false;
    }

    /// <summary>
    /// 구르기 시도. 이미 구르는 중이거나 쿨타임이면 실패한다.
    /// </summary>
    protected bool TryDodge(Vector2 direction)
    {
        if (_state != MoveState.Normal || _dodgeCooldownTimer > 0f) return false;
        if (direction.sqrMagnitude <= InputDeadZone) return false;

        StartDodge(direction);
        return true;
    }

    // ---- 이동 봉쇄 (IBossMovable) ----
    /// <summary>
    /// 보스를 제자리에 묶는다. 구르는 중이면 즉시 끊고, 밀려서도 움직이지 않게 위치를 고정한다.
    /// 이미 봉쇄된 상태에서 다시 불러도 한 번 부른 것과 같다.
    /// </summary>
    public void MoveBlock()
    {
        if (_isMoveBlocked)
            return;

        _isMoveBlocked = true;

        if (_state == MoveState.Dodge)
        {
            _state = MoveState.Normal;
            _dodgeTimer = 0f;
            // 풀리자마자 다시 구르지 않도록 정상 종료와 같은 쿨타임을 준다.
            _dodgeCooldownTimer = _dodgeCooldown;
            EndDodge();
        }

        StopMove();
        _velocity = Vector2.zero;
        _rigidbody2D.linearVelocity = Vector2.zero;

        _constraintsBeforeBlock = _rigidbody2D.constraints;
        _rigidbody2D.constraints = _constraintsBeforeBlock | RigidbodyConstraints2D.FreezePosition;

        _animator.SetBool("IsMove", false);
        _animator.SetBool("IsSprint", false);
    }

    /// <summary>
    /// 봉쇄를 푼다. 원래 제약을 되돌리고, 다음 프레임부터 입력·패턴대로 다시 움직인다.
    /// 봉쇄 중이 아닐 때 불러도 아무 일도 없다.
    /// </summary>
    public void MoveUnblock()
    {
        if (_isMoveBlocked == false)
            return;

        _isMoveBlocked = false;
        _rigidbody2D.constraints = _constraintsBeforeBlock;
    }

    // ---- 테스트용 키 입력 ----
    // 키 입력도 기본 이동을 거치므로 키로 확인한 동작이 곧 패턴이 쓰는 동작이다.
    private void ReadKeyboard()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(horizontal, vertical).normalized;

        if (_state == MoveState.Normal)
        {
            TryStartDodge(input);
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            Run(input);
        }
        else
        {
            Walk(input);
        }
    }

    private void TickTimers()
    {
        if (_dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer = Mathf.Max(_dodgeCooldownTimer - Time.deltaTime, 0f);
        }

        if (_state != MoveState.Dodge) return;

        _dodgeTimer -= Time.deltaTime;
        if (_dodgeTimer <= 0f)
        {
            // 회피가 종료하는 부분
            _dodgeTimer = 0f;
            _state = MoveState.Normal;
            _dodgeCooldownTimer = _dodgeCooldown;
        }
    }

    private void TryStartDodge(Vector2 input)
    {
        if (_dodgeCooldownTimer > 0f) return;

        foreach ((KeyCode key, Vector2 dir) in DodgeKeys)
        {
            if (Input.GetKeyDown(key) == false) continue;

            if (_lastTapTime.TryGetValue(key, out float lastTap)
                && Time.time - lastTap <= _doubleTapInterval)
            {
                // 대각선으로 입력 중이면 그쪽을, 아니면 더블탭한 키 방향을 쓴다.
                TryDodge(input.sqrMagnitude > InputDeadZone ? input : dir);

                // 기록을 소진시키지 않으면 세 번째 탭이 또 발동한다.
                _lastTapTime[key] = float.NegativeInfinity;
                return;
            }

            _lastTapTime[key] = Time.time;
        }
    }

    private void StartDodge(Vector2 dir)
    {
        _animator.SetBool("IsDodge",true);
        
        _state = MoveState.Dodge;
        _dodgeDir = dir.normalized;
        _dodgeTimer = Mathf.Max(_dodgeDuration, 0.01f);

        // 일반 이동과 달리 가속 램프를 건너뛰고 직접 대입한다. 이 대비가 순간 가속 감각이다.
        _velocity = _dodgeDir * _dodgeSpeed;
    }

    private void EndDodge()
    {
        _animator.SetBool("IsDodge", false);
    }

    private void UpdateVelocity()
    {
        if (_state == MoveState.Dodge)
        {
            // 구르는 동안 방향을 고정하고 입력을 무시한다.
            // 끝날 무렵 _dodgeEndSpeed 까지 내려와 있어야 급정지 없이 아래 감속으로 인계된다.
            float t = 1f - Mathf.Clamp01(_dodgeTimer / Mathf.Max(_dodgeDuration, 0.01f));
            _velocity = _dodgeDir * Mathf.Lerp(_dodgeSpeed, _dodgeEndSpeed, t);
            return;
        }

        bool wantsSprint = _wantsSprint && _moveDirection.sqrMagnitude > InputDeadZone;
        float targetSpeed = wantsSprint ? _sprintSpeed : _walkSpeed;

        // 애니메이션 달리기
        _animator.SetBool("IsSprint", wantsSprint);

        // 이동 의도가 없으면 목표가 (0,0) 이 되어 0 으로 서서히 수렴한다.
        Vector2 targetVelocity = _moveDirection * targetSpeed;

        float rate = targetVelocity.sqrMagnitude > _velocity.sqrMagnitude
            ? _acceleration
            : _deceleration;

        _velocity = Vector2.MoveTowards(_velocity, targetVelocity, rate * Time.deltaTime);
    }

    private void ApplyMove()
    {
        int x = (int)_velocity.x;
        if (x > 0)
        {
            _spriteRenderer.flipX = false;
            _attackZone.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        }
        else if (x < 0)
        {
            _spriteRenderer.flipX = true;
            _attackZone.transform.localPosition = new Vector3(-0.4f, 0f, 0f);
        }
        
        // UpdateVelocity 가 이미 가속 램프를 거쳐 units/s 단위 속도를 만들어 두었으므로
        // 힘을 더하지 않고 그대로 대입한다. AddForce 를 쓰면 가속이 두 번 적분된다.
        _rigidbody2D.linearVelocity = _velocity;
    }
}
