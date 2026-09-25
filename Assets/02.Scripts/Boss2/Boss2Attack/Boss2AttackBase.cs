using System;
using UnityEngine;

[System.Serializable]
public class ComboStep
{

    public string StateName;
    public float MaxStepTime = 0.8f;
    public float ComboTimingStart = 0.3f;
    public float ComboTimingEnd = 0.7f;
}
public class Boss2AttackBase : MonoBehaviour
{
    //private enum AttackState {NoAttack,Nomal,AreaOfEffect,InstantKill}
    [Header("데미지")]
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private int _areaOfEffectDamage = 1;
    [SerializeField] private int _instantKillDamage = 1;

    [SerializeField] private BoxCollider2D _attackBoxCollider;
    private Animator _animator;

    [Header("공격 신호")]
    // 끄면 키 입력을 무시하고 RequestAttack / RequestCombo 신호로만 공격한다. (AI 전용)
    [SerializeField] private bool _useKeyboardInput = true;
    [SerializeField] private KeyCode _attackKey = KeyCode.Space;

    // Combo Section
    [SerializeField] private ComboStep[] _comboSteps;
    // 콤보 창이 열리기 전에 미리 들어온 신호를 이 시간만큼 기억해 둔다. (입력/AI 공통)
    [Tooltip("신호 버퍼: 콤보 창보다 이 시간 이내로 먼저 들어온 신호도 다음 타로 인정")]
    [SerializeField] private float _inputBufferTime = 0.2f;
    private int _currentCombo = -1;
    private float _comboStartTime = 0f;
    private float _lastSignalTime = float.NegativeInfinity;
    // RequestCombo 로 예약된 목표 타수. 0 이면 예약 없음.
    private int _targetComboCount = 0;

    // 프로퍼티 
    public bool IsAttacking => _currentCombo >= 0;
    public int CurrentComboIndex => _currentCombo;

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

        if (_currentCombo >= 0)
        {
            UpdateCombo();
        }
    }

    /// <summary>
    /// 단발 공격 신호. 키를 한 번 누른 것과 같다.
    /// 공격 중이 아니면 1타를 시작하고, 공격 중이면 다음 타를 예약한다.
    /// </summary>
    public void RequestAttack()
    {
        if (HasComboSteps() == false) return;

        if (_currentCombo < 0)
        {
            StartCombo(0);
            return;
        }

        // 바로 다음 타로 넘기지 않고 시간만 기록한다.
        // 실제 진행 여부는 UpdateCombo 에서 콤보 창과 버퍼를 보고 정한다.
        _lastSignalTime = Time.time;
    }

    /// <summary>
    /// N타 공격 신호. hitCount 타까지 콤보 창마다 자동으로 이어간다.
    /// 공격 도중 호출되면 목표 타수만 바꾼다.
    /// </summary>
    public void RequestCombo(int hitCount)
    {
        if (HasComboSteps() == false || hitCount <= 0) return;

        _targetComboCount = Mathf.Clamp(hitCount, 1, _comboSteps.Length);

        if (_currentCombo < 0)
        {
            StartCombo(0);
        }
    }

    private bool HasComboSteps()
    {
        return _comboSteps != null && _comboSteps.Length > 0;
    }

    private void StartCombo(int index)
    {
        _currentCombo = Mathf.Clamp(index, 0,_comboSteps.Length-1);
        _comboStartTime = Time.time;
        // 이전 타에서 쓴 신호가 다음 타 창에서 또 소비되지 않게 비운다.
        _lastSignalTime = float.NegativeInfinity;

        _animator.CrossFade(_comboSteps[_currentCombo].StateName, 0.05f);
        ComboStepStarted?.Invoke(_currentCombo);
    }

    private void UpdateCombo()
    {
        ComboStep step = _comboSteps[_currentCombo];
        float elapsed = Time.time - _comboStartTime;

        // 경과시간이 스탭 최대 시간을 지나면 콤보 리셋
        // 이 부분을 위로 올림으로 경과시간이 지나면 반환
        if (elapsed >= step.MaxStepTime)
        {
            ResetCombo();
            return;
        }

        bool isInTiming = elapsed >= step.ComboTimingStart && elapsed <= step.ComboTimingEnd;
        if (isInTiming == false) return;

        int next = _currentCombo + 1;
        bool hasBufferedSignal = (Time.time - _lastSignalTime) <= _inputBufferTime;
        bool hasReservedCombo = next < _targetComboCount;

        if (hasBufferedSignal || hasReservedCombo)
        {
            if (next < _comboSteps.Length)
            {
                StartCombo(next);
            }
            else
            {
                ResetCombo();
            }
        }
    }

    private void ResetCombo()
    {
        _currentCombo = -1;
        _lastSignalTime = float.NegativeInfinity;
        _targetComboCount = 0;
        
        ComboEnded?.Invoke();
    }

    // 공격 애니메이션 클립의 EndAttack 이벤트에서 호출된다.

    private void EndAttack()
    {
        
    }

    public void StartDamage()
    {
        if (_attackBoxCollider != null)
        {
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

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // AttackBoxCollider에 대한 처리 로직만 여기로 들어가게 처리
            if (_attackBoxCollider.enabled == true)
            {
                // 지금 플레이어가 없는 관계로 데미지를 넘기는 더미로 대체
                HitDummy dummy = other.gameObject.GetComponent<HitDummy>();
                if (dummy != null)
                {
                    dummy.TakeDamage(_baseDamage);
                }
                else
                {
                    Debug.Log("dummy is Null");
                }    
            }
            
        }
    }
}
