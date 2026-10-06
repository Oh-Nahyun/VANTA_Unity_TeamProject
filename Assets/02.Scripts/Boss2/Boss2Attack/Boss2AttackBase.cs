using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class ComboStep
{
    public string StateName;

    [Tooltip("다음 타가 예약돼 있을 때 넘어가는 시점(초). 이 스텝의 마지막 EndDamage 이후여야 판정이 살아 있다")]
    [FormerlySerializedAs("ComboTimingStart")]
    public float ChainTime = 0.7f;

    [Tooltip("키 입력을 다음 타 예약으로 인정하는 마감(초)")]
    [FormerlySerializedAs("ComboTimingEnd")]
    public float InputDeadline = 0.8f;

    [Tooltip("다음 타 예약이 없으면 콤보를 끝내는 시간(초). 클립 길이와 맞춘다")]
    public float MaxStepTime = 0.83f;
}

public class Boss2AttackBase : MonoBehaviour
{
    [Header("데미지")]
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private int _areaOfEffectDamage = 1;
    [SerializeField] private int _instantKillDamage = 3;

    [SerializeField] private BoxCollider2D _attackBoxCollider;
    protected Animator _animator;
    private const string HitTrigger = "Hit";
    // 공격 박스 판정 한 번(StartDamage ~ EndDamage)에서 이미 맞은 대상
    private readonly HashSet<GameObject> _hitTargetsThisSwing = new HashSet<GameObject>();

    [Header("공격 신호")]
    // 끄면 키 입력을 무시하고 RequestAttack / RequestCombo 신호로만 공격한다. (AI 전용)
    [SerializeField] private bool _useKeyboardInput = true;
    [SerializeField] private KeyCode _attackKey = KeyCode.Space;

    [Header("콤보")]
    // 스텝을 순서대로 돌려 쓴다. 타수가 스텝 수보다 많으면 처음 스텝으로 돌아가 번갈아 시전한다.
    [SerializeField] private ComboStep[] _comboSteps;
    [SerializeField] private int _maxComboCount = 8;

    [Tooltip("연타 속도 배율. 애니메이션(AttackSpeed 파라미터)과 ComboStep 의 시간 값을 같은 비율로 바꾼다.\nComboStep 의 시간은 1배속 기준으로 적는다.")]
    [SerializeField] private float _comboSpeed = 1f;
    private const string AttackSpeedParam = "AttackSpeed";

    // 발사체 시작 좌표 ( 자식에서도 활용 가능 )
    protected Vector2[] _projectileDirections;
    
    // 이번 콤보에서 몇 번째 타인지. 대기 중이면 -1.
    private int _currentHit = -1;
    // 이번 콤보에서 칠 총 타수. AI 명령은 한 번에 정하고, 키 입력은 누를 때마다 1씩 늘린다.
    private int _targetHitCount = 0;
    private float _stepStartTime = 0f;

    [FormerlySerializedAs("_smashImpulse")]
    [Header("이펙트")]
    [Tooltip("보스 본체의 CinemachineImpulseSource. 비워두면 흔들지 않는다.")] 
    [SerializeField] private CinemachineImpulseSource _cameraImpulse;
    

    
    public bool IsAttacking => _currentHit >= 0;
    // 자식이 인스펙터의 즉사기 데미지 설정을 쓰도록 연다.
    protected int InstantKillDamage => _instantKillDamage;
    public int CurrentHitIndex => _currentHit;
    public int CurrentStepIndex => IsAttacking ? _currentHit % _comboSteps.Length : -1;

    // 인자는 이번 콤보에서 몇 번째 타인지(0부터)다. 재생 중인 스텝은 CurrentStepIndex 로 본다.
    public event System.Action<int> ComboStepStarted;
    public event System.Action ComboEnded;

    private void Awake()
    {
        // 위(0번)부터 시계 방향으로 45° 간격. 인덱스 ±1 이 곧 양옆 인접 방향이다.
        _projectileDirections = new Vector2[8]
        {
            new Vector2(0, 1),      // 0 위
            new Vector2(1, 1),      // 1 오른위
            new Vector2(1, 0),      // 2 오른
            new Vector2(1, -1),     // 3 오른아래
            new Vector2(0, -1),     // 4 아래
            new Vector2(-1, -1),    // 5 왼아래
            new Vector2(-1, 0),     // 6 왼
            new Vector2(-1, 1)      // 7 왼위
        };
    }

    // 음수·8 이상도 0~7 로 감싼다. (C# 의 % 는 -1 % 8 == -1 이라 한 번 더 더해 준다)
    protected int WrapDirectionIndex(int index)
    {
        int count = _projectileDirections.Length;
        return ((index % count) + count) % count;
    }

    // 입력 방향에 가장 가까운 _projectileDirections 인덱스.
    // Atan2(x, y) 는 위에서부터 시계 방향 각도다. (위 0, 오른 90, 아래 ±180, 왼 -90)
    protected int DirectionToIndex(Vector2 direction)
    {
        float clockwiseAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        return WrapDirectionIndex(Mathf.RoundToInt(clockwiseAngle / 45f));
    }
    private void Start()
    {
        if (_attackBoxCollider != null)
        {
            _attackBoxCollider.enabled = false;
        }
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_useKeyboardInput && Input.GetKeyDown(_attackKey))
        {
            RequestAttack();
        }

        if (IsAttacking)
        {
            UpdateCombo();
        }
    }

    /// <summary>
    /// 단발 공격 신호. 키를 한 번 누른 것과 같다.
    /// 공격 중이 아니면 1타를 시작하고, 공격 중이면 입력 마감 전일 때 한 타를 더 예약한다.
    /// </summary>
    public void RequestAttack()
    {
        if (HasComboSteps() == false)
            return;

        if (IsAttacking == false)
        {
            _targetHitCount = 1;
            StartHit(0);
            return;
        }

        float elapsed = Time.time - _stepStartTime;
        if (elapsed > ScaleComboTime(CurrentStep.InputDeadline))
            return;

        // 이미 예약된 타보다 한 타만 더. 연타로 여러 번 눌러도 누른 횟수만큼만 쌓인다.
        _targetHitCount = Mathf.Min(Mathf.Max(_targetHitCount, _currentHit + 1) + 1, _maxComboCount);
    }

    /// <summary>
    /// N타 콤보 신호 (AI 명령). 스텝을 번갈아 가며 N타를 연속으로 친다.
    /// 공격 도중 호출되면 목표 타수만 바꾼다. 이미 친 타수보다 줄일 수는 없다.
    /// </summary>
    public void RequestCombo(int hitCount)
    {
        if (HasComboSteps() == false || hitCount <= 0)
            return;

        int target = Mathf.Clamp(hitCount, 1, _maxComboCount);

        if (IsAttacking)
        {
            _targetHitCount = Mathf.Max(target, _currentHit + 1);
            return;
        }

        _targetHitCount = target;
        StartHit(0);
    }

    /// <summary>
    /// 진행 중인 콤보를 즉시 끝낸다. (피격 경직, 패턴 중단 등)
    /// </summary>
    public void CancelCombo()
    {
        if (IsAttacking)
        {
            ResetCombo();
        }
    }

    private ComboStep CurrentStep => _comboSteps[_currentHit % _comboSteps.Length];

    // ComboStep 의 시간(1배속 기준)을 현재 연타 속도에 맞춘 실제 시간으로 바꾼다.
    private float ScaleComboTime(float time)
    {
        return time / Mathf.Max(_comboSpeed, 0.01f);
    }

    private bool HasComboSteps()
    {
        return _comboSteps != null && _comboSteps.Length > 0;
    }

    // 행동을 시작하기 직전에 맞아 걸려 있던 피격 트리거를 지운다.
    // 남아 있으면 행동 애니메이션으로 넘어간 직후 AnyState → Hit 전이가 발동해 행동이 끊긴다.
    protected void ClearPendingHit()
    {
        if (_animator != null)
        {
            _animator.ResetTrigger(HitTrigger);
        }
    }

    private void StartHit(int hitIndex)
    {
        // CrossFade 로 끊긴 이전 타의 EndDamage 이벤트가 누락됐을 수 있으므로 히트박스를 먼저 끈다.
        EndDamage();
        ClearPendingHit();

        _currentHit = hitIndex;
        _stepStartTime = Time.time;

        // 연타 상태(Boss2Attack / Boss2DoubleAttack)의 Speed Multiplier. 판정 이벤트도 같이 빨라진다.
        _animator.SetFloat(AttackSpeedParam, _comboSpeed);

        // layer 0, 시간 오프셋 0 을 명시해야 같은 상태로 넘어갈 때도 처음부터 재생된다.
        _animator.CrossFade(CurrentStep.StateName, 0.05f, 0, 0f);
        ComboStepStarted?.Invoke(_currentHit);
    }

    private void UpdateCombo()
    {
        ComboStep step = CurrentStep;
        float elapsed = Time.time - _stepStartTime;

        // 연결 검사를 리셋 검사보다 먼저 한다.
        // ChainTime 이 MaxStepTime 에 가까워도 예약된 다음 타가 리셋에 먹히지 않게 하기 위함.
        bool hasNextHit = _currentHit + 1 < _targetHitCount;
        if (hasNextHit && elapsed >= ScaleComboTime(step.ChainTime))
        {
            StartHit(_currentHit + 1);
            return;
        }

        if (elapsed >= ScaleComboTime(step.MaxStepTime))
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        EndDamage();

        _currentHit = -1;
        _targetHitCount = 0;

        ComboEnded?.Invoke();
    }

    // 공격 애니메이션 클립의 EndAttack 이벤트에서 호출된다.
    // 콤보 종료는 UpdateCombo 가 시간으로 판단하므로 여기서는 하는 일이 없다.
    private void EndAttack()
    {

    }

    public void StartDamage()
    {
        if (_attackBoxCollider != null)
        {
            // 판정이 새로 켜질 때마다 "이번 판정에서 맞은 대상"을 비운다. (DoubleAttack 은 판정마다 1회씩)
            _hitTargetsThisSwing.Clear();
            _attackBoxCollider.enabled = true;
        }
        

    }

    public void EndDamage()
    {
        if (_attackBoxCollider != null)
        {
            _attackBoxCollider.enabled = false;
        }
    }

    public void CameraShakePoint(float shakeForce)
    {
        // 카메라 쉐이크
        if (_cameraImpulse != null)
        {
            _cameraImpulse.GenerateImpulseWithForce(shakeForce);
        }
    }
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") == false)
            return;

        // 박스를 쓰지 않는 자식 컴포넌트(Boss2SkillPattern 등)는 비워 두므로 null 을 먼저 거른다.
        if (_attackBoxCollider == null || _attackBoxCollider.enabled == false)
            return;

        // 이 메시지는 보스의 모든 트리거(몸통 포함)에서 온다.
        // 공격 박스에 실제로 닿은 대상만 치고, 몸통에만 닿은 대상은 공격으로 보지 않는다.
        if (_attackBoxCollider.IsTouching(other) == false)
            return;

        // 몸통과 공격 박스에 동시에 닿으면 이벤트가 두 번 오므로 판정당 한 번만 친다.
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (_hitTargetsThisSwing.Add(target))
        {
            DeliverDamage(target, _baseDamage);
        }
    }
    

    /// <summary>
    /// center 를 중심으로 radius 안에 있는 대상에게 범위 데미지(_areaOfEffectDamage)를 한 번씩 준다.
    /// 자기 자신은 제외하고, 콜라이더가 여러 개인 대상도 한 번만 맞는다. 맞은 대상 수를 돌려준다.
    /// </summary>
    protected int DealAreaDamage(Vector2 center, float radius, LayerMask targetLayers)
    {
        return DealAreaDamage(center, radius, targetLayers, _areaOfEffectDamage);
    }

    /// <summary>
    /// 데미지를 직접 정하는 범위 판정. 즉사기·광역기 더미처럼 범위 공격 기본값과 다른 데미지를 줄 때 쓴다.
    /// </summary>
    protected int DealAreaDamage(Vector2 center, float radius, LayerMask targetLayers, int damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetLayers);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (Collider2D hit in hits)
        {
            // 프리팹 원본 태그가 Player 라서 자기 콜라이더도 걸릴 수 있다.
            if (hit.transform.IsChildOf(transform))
                continue;
            if (hit.CompareTag("Player") == false)
                continue;

            GameObject target = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            if (damaged.Add(target))
            {
                DeliverDamage(target, damage);
            }
        }

        return damaged.Count;
    }

    /// <summary>
    /// 범위 판정 없이 대상 하나에게 바로 범위 공격 데미지(_areaOfEffectDamage)를 준다. 회피 실패처럼 맞을 대상이 이미 정해진 공격에 쓴다.
    /// </summary>
    protected void DealDirectDamage(GameObject target)
    {
        if (target == null)
            return;

        DeliverDamage(target, _areaOfEffectDamage);
    }

    private void DeliverDamage(GameObject target, int damage)
    {
        // 지금 플레이어가 없는 관계로 데미지를 넘기는 더미로 대체
        Player player = target.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage();
        }
        else
        {
            Debug.Log("player is Null");
        }
    }

    // ---- 순간이동 (즉사기·회전베기 공용) ----

    /// <summary>
    /// 이동 패턴이 쫓는 대상을 우선 쓰고, 없으면 Player 태그 중 가장 가까운 대상(자기 자신 제외)을 쓴다.
    /// </summary>
    protected Transform FindAttackTarget()
    {
        Boss2MovePattern movePattern = GetComponent<Boss2MovePattern>();
        if (movePattern != null && movePattern.Target != null)
            return movePattern.Target;

        Transform nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (candidate.transform.IsChildOf(transform))
                continue;

            float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = candidate.transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 대상 위 height 만큼 떨어진 자리로 순간이동한다. 대상이 없으면 제자리에 남고 false 를 돌려준다.
    /// Boss2MoveArea 가 있으면, 위쪽이 맵 밖이거나 벽이면 아래·왼쪽·오른쪽 중 가능한 자리로 바꾼다.
    /// 자리가 바뀌었으면 Boss2AfterImage 로 원래 자리(대상 위)에서 순보해 온 것처럼 잔상을 남긴다.
    /// </summary>
    protected bool AppearAbove(Transform target, float height)
    {
        if (target == null)
            return false;

        Vector2 idealPosition = (Vector2)target.position + Vector2.up * height;
        Vector2 appearPosition = idealPosition;
        Boss2MoveArea moveArea = GetComponent<Boss2MoveArea>();
        if (moveArea != null)
        {
            appearPosition = moveArea.ResolveAppearPosition(target.position, height);
        }

        TeleportWithAfterImage(target, idealPosition, appearPosition, moveArea);
        return true;
    }

    /// <summary>
    /// 그림이 피벗에서 떨어져 그려지는 공격(회전베기 등)용. 보이는 그림의 중심이 대상 위 height 에 오도록 순간이동한다.
    /// visualOffset 은 피벗에서 그림 중심까지, visualSize 는 그림 크기다. 맵 판정도 이 그림 박스로 한다.
    /// </summary>
    protected bool AppearAbove(Transform target, float height, Vector2 visualOffset, Vector2 visualSize)
    {
        if (target == null)
            return false;

        Vector2 idealPosition = (Vector2)target.position + Vector2.up * height - visualOffset;
        Vector2 appearPosition = idealPosition;
        Boss2MoveArea moveArea = GetComponent<Boss2MoveArea>();
        if (moveArea != null)
        {
            appearPosition = moveArea.ResolveAppearPosition(target.position, height, visualOffset, visualSize);
        }

        TeleportWithAfterImage(target, idealPosition, appearPosition, moveArea);
        return true;
    }

    private void TeleportWithAfterImage(Transform target, Vector2 idealPosition, Vector2 appearPosition, Boss2MoveArea moveArea)
    {

        // 실제 위치는 처음부터 갈 수 있는 자리로 정하고, 순보는 잔상으로만 보여 준다.
        // 그래서 판정·회피 타이밍은 바뀌지 않고, 맵 밖 잔상은 그리지 않는다.
        Boss2AfterImage afterImage = GetComponent<Boss2AfterImage>();
        if (afterImage != null && (appearPosition - idealPosition).sqrMagnitude > 0.0001f)
        {
            bool faceLeft = target.position.x < appearPosition.x;
            afterImage.Play(idealPosition, appearPosition, faceLeft,
                moveArea != null ? (System.Func<Vector2, bool>)moveArea.IsInsideArea : null);
        }

        // 이동 봉쇄(위치 고정) 중이어도 위치를 직접 정하는 것은 막히지 않는다. 물리 쪽 위치도 함께 옮긴다.
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = appearPosition;
            body.linearVelocity = Vector2.zero;
        }

        transform.position = new Vector3(appearPosition.x, appearPosition.y, transform.position.z);
    }
}
