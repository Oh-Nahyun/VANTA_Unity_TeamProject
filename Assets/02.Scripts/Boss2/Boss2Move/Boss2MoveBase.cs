
using System.Collections.Generic;
using UnityEngine;


public class Boss2MoveBase : MonoBehaviour
{
    private enum MoveState
    {
        Normal, Dodge
    }

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

    private const float InputDeadZone = 0.0001f;

    // 방향과 크기를 함께 가진 단일 상태.
    // 입력 방향을 매 프레임 곱하지 않으므로 키를 떼도 속도가 남아 감속이 눈에 보인다.
    private Vector2 _velocity;

    private MoveState _state = MoveState.Normal;
    private Vector2 _dodgeDir;
    private float _dodgeTimer;
    private float _dodgeCooldownTimer;

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

    public bool IsDodging => _state == MoveState.Dodge;
    public bool IsSprinting => _state == MoveState.Normal && _velocity.magnitude > _walkSpeed + 0.1f;
    public float CurrentSpeed => _velocity.magnitude;

    public bool IsInvincible
    {
        get
        {
            if (_state != MoveState.Dodge)
                return false;
            float elapsed = _dodgeDuration - _dodgeTimer;
            return elapsed >= _iFrameStart && elapsed <= _iFrameEnd;
        }
    }
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Vector2 input = ReadInput();

        // 여기서 애니메이션 제어를 매번 해주면 프레임드랍이...
        if (Mathf.Approximately(input.x, 0f) == true && Mathf.Approximately(input.y, 0f) == true)
        {
            _animator.SetBool("IsMove", false);
        }
        else
        {
            _animator.SetBool("IsMove", true);
        }

        // TickTimers 가 UpdateVelocity 보다 먼저 와야 구르기가 끝난 프레임에
        // 곧바로 Normal 규칙으로 속도가 계산되어 한 프레임이 비지 않는다.
        TickTimers();

        if (_state == MoveState.Normal)
        {
            TryStartDodge(input);
        }

        UpdateVelocity(input);
        ApplyMove();
    }

    private static Vector2 ReadInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        return new Vector2(horizontal, vertical).normalized;
    }

    private void TickTimers()
    {
        if (_dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer = Mathf.Max(_dodgeCooldownTimer - Time.deltaTime, 0f);
        }

        if (_state != MoveState.Dodge)
            return;

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
        if (_dodgeCooldownTimer > 0f)
            return;

        foreach ((KeyCode key, Vector2 dir) in DodgeKeys)
        {
            if (Input.GetKeyDown(key) == false)
                continue;

            if (_lastTapTime.TryGetValue(key, out float lastTap)
                && Time.time - lastTap <= _doubleTapInterval)
            {
                // 대각선으로 입력 중이면 그쪽을, 아니면 더블탭한 키 방향을 쓴다.
                StartDodge(input.sqrMagnitude > InputDeadZone ? input : dir);

                // 기록을 소진시키지 않으면 세 번째 탭이 또 발동한다.
                _lastTapTime[key] = float.NegativeInfinity;
                return;
            }

            _lastTapTime[key] = Time.time;
        }
    }

    private void StartDodge(Vector2 dir)
    {
        _animator.SetBool("IsDodge", true);

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

    private void UpdateVelocity(Vector2 input)
    {
        if (_state == MoveState.Dodge)
        {
            // 구르는 동안 방향을 고정하고 입력을 무시한다.
            // 끝날 무렵 _dodgeEndSpeed 까지 내려와 있어야 급정지 없이 아래 감속으로 인계된다.
            float t = 1f - Mathf.Clamp01(_dodgeTimer / Mathf.Max(_dodgeDuration, 0.01f));
            _velocity = _dodgeDir * Mathf.Lerp(_dodgeSpeed, _dodgeEndSpeed, t);
            return;
        }

        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > InputDeadZone;
        float targetSpeed = wantsSprint ? _sprintSpeed : _walkSpeed;

        // 애니메이션 달리기
        _animator.SetBool("IsSprint", wantsSprint);



        // 입력이 없으면 목표가 (0,0) 이 되어 0 으로 서서히 수렴한다.
        Vector2 targetVelocity = input * targetSpeed;

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
