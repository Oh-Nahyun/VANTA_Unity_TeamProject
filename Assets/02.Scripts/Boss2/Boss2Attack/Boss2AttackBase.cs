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
        if (elapsed > CurrentStep.InputDeadline)
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

    private bool HasComboSteps()
    {
        return _comboSteps != null && _comboSteps.Length > 0;
    }

    private void StartHit(int hitIndex)
    {
        // CrossFade 로 끊긴 이전 타의 EndDamage 이벤트가 누락됐을 수 있으므로 히트박스를 먼저 끈다.
        EndDamage();

        _currentHit = hitIndex;
        _stepStartTime = Time.time;

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
        if (hasNextHit && elapsed >= step.ChainTime)
        {
            StartHit(_currentHit + 1);
            return;
        }

        if (elapsed >= step.MaxStepTime)
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
}
