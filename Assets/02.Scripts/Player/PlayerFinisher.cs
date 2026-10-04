using DG.Tweening;
using UnityEngine;

public class PlayerFinisher : MonoBehaviour
{
    [Header("타임슬로우 량")]
    [SerializeField] private float _slowAmount = 0.2f;

    [Header("회피기 사용할 때 줌 량")]
    [SerializeField] private float _avoidZoomAmount = 1.5f;

    [Header("좌클릭 공격 가능 시간 (실제 시간)")]
    [SerializeField] private float _avoidDuration = 2f;

    [Header("회피 거리")]
    [SerializeField] private float _dashDistance = 1.5f;

    [Header("회피 이동 시간")]
    [SerializeField] private float _avoidDashDuration = 0.5f;

    [Header("회피 공격 관통 거리")]
    [SerializeField] private float _attackPassDistance = 1.5f;

    [Header("Focus")]
    [SerializeField]
    private SkillFocusScreenController _focusScreenController;

    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private SkillAttack _skillAttack;
    private SkillManager _skillManager;
    private Rigidbody2D _rigidbody;

    private bool _isAvoidInteraction;
    private bool _isDashing;
    private bool _isReverseDashing;
    private bool _hasReportedSuccess;

    private GameObject _avoidCaller;

    private Tween _avoidTimer;
    private Tween _avoidMoveTween;

    private float _previousTimeScale;
    private bool _previousMoveEnabled;
    private bool _previousAttackEnabled;
    private bool _previousSkillManagerEnabled;

    private void Awake()
    {
        _objectZoom = GetComponentInChildren<ObjectZoom>();
        _playerAvoidKeyGenerator = GetComponent<PlayerAvoidKeyGenerator>();
        _playerMove = GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _skillAttack = GetComponentInChildren<SkillAttack>();
        _skillManager = GetComponentInChildren<SkillManager>();
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartPlayerFinisher();
        }

        CheckInputSuccess();
    }

    private void CheckInputSuccess()
    {
        if (!_isAvoidInteraction)
            return;

        if (_isDashing || _isReverseDashing)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            AttackToBoss();
        }
    }

    public void StartPlayerFinisher(GameObject caller = null)
    {
        if (_isAvoidInteraction || _isDashing)
            return;

        if (_rigidbody == null || _skillAttack == null)
        {
            Debug.LogError("Rigidbody2D 또는 SkillAttack 없음");
            return;
        }

        HitableObject boss = FindAnyObjectByType<HitableObject>();

        if (boss == null)
        {
            Debug.LogError("HitableObject 없음");
            return;
        }

        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _isAvoidInteraction = true;
        _isReverseDashing = false;
        _hasReportedSuccess = false;
        _avoidCaller = caller;

        // 피니셔 시작 전 상태 저장
        _previousTimeScale = Time.timeScale;


        _playerAvoidKeyGenerator.CreateFinisherKey();

        if (_playerMove != null)
        {
            _previousMoveEnabled = _playerMove.enabled;
            _playerMove.enabled = false;
        }

        if (_playerAttack != null)
        {
            _previousAttackEnabled = _playerAttack.enabled;
            _playerAttack.enabled = false;
        }

        if (_skillManager != null)
        {
            _previousSkillManagerEnabled = _skillManager.enabled;
            _skillManager.enabled = false;
        }

        _objectZoom.ZoomIn(
          _avoidZoomAmount
      );

        SetFinisherReadyState();
        DashToReverseSide();

        if (!_isAvoidInteraction)
            return;

        // 대기 상태로 돌아가도 타이머는 초기화하지 않음
        _avoidTimer = DOVirtual
            .DelayedCall(_avoidDuration, FinisherEnd)
            .SetUpdate(true);
    }

    // 피니셔 시작과 돌진 후 대기에 공통으로 적용
    private void SetFinisherReadyState()
    {
        if (!_isAvoidInteraction)
            return;

        Time.timeScale = _slowAmount;

        if (_animator != null)
        {
            _animator.ResetTrigger("Avoid");
            _animator.SetBool("AvoidReady", true);
        }

        if (_objectZoom != null)
        {
            _objectZoom.ZoomIn(_avoidZoomAmount);
        }

     

        //if (_focusScreenController != null)
        //{
        //    _focusScreenController.EndFocusScreenSetting();
        //    _focusScreenController.StartFocusScreenSetting(null);
        //}
    }

    public void DashToReverseSide()
    {
        if (!_isAvoidInteraction)
            return;

        HitableObject boss = FindAnyObjectByType<HitableObject>();

        if (_rigidbody == null || boss == null)
        {
            Debug.LogError("Rigidbody2D 또는 HitableObject 없음");
            FinisherEnd();
            return;
        }

        Vector2 direction =
            (_rigidbody.position - (Vector2)boss.transform.position)
            .normalized;

        Vector2 targetPosition =
            _rigidbody.position + direction * _dashDistance;

        _avoidMoveTween?.Kill();

        _isReverseDashing = true;

        _avoidMoveTween = _rigidbody
            .DOMove(targetPosition, _avoidDashDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _avoidMoveTween = null;
                _isReverseDashing = false;

                // 시작 때 적용한 대기 상태를 유지하고 입력 대기
            });
    }

    private void AttackToBoss()
    {
        if (!_isAvoidInteraction || _isDashing || _isReverseDashing)
            return;

        HitableObject boss = FindAnyObjectByType<HitableObject>();

        if (boss == null || _rigidbody == null || _skillAttack == null)
        {
            Debug.LogError(
                "HitableObject, Rigidbody2D 또는 SkillAttack 없음"
            );

            FinisherEnd();
            return;
        }

        Vector2 startPosition = _rigidbody.position;
        Vector2 bossPosition = boss.transform.position;

        Vector2 attackDirection =
            (bossPosition - startPosition).normalized;

        Vector2 targetPosition =
            bossPosition + attackDirection * _attackPassDistance;

        _isDashing = true;

        // 공격 중에는 대기 포커스 해제
        if (_focusScreenController != null)
        {
            _focusScreenController.EndFocusScreenSetting();
        }

        if (_animator != null)
        {
            _animator.SetBool("AvoidReady", false);
            _animator.SetTrigger("Avoid");
        }


        // 첫 번째 공격 입력에서만 성공 결과 전달
        if (!_hasReportedSuccess)
        {
            _hasReportedSuccess = true;

            if (_avoidCaller != null)
            {
                IAvoidInteraction interaction =
                    _avoidCaller.GetComponent<IAvoidInteraction>();

                interaction?.AvoidResultBehavior(true);
            }
        }

        // 성공 결과 처리 중 종료된 경우 돌진하지 않음
        if (!_isAvoidInteraction)
        {
            _isDashing = false;
            return;
        }

        _skillAttack.SingleDash(
            targetPosition,
            AttackToBossComplete
        );
    }

    private void AttackToBossComplete()
    {
        _isDashing = false;

        if (!_isAvoidInteraction)
            return;

        // 처음과 동일한 대기 연출로 복귀
        SetFinisherReadyState();
    }

    private void FinisherEnd()
    {
        if (!_isAvoidInteraction)
            return;

        _isAvoidInteraction = false;
        _isReverseDashing = false;

        _avoidTimer?.Kill();
        _avoidTimer = null;

        _avoidMoveTween?.Kill();
        _avoidMoveTween = null;

        if (_animator != null)
        {
            _animator.ResetTrigger("Avoid");
            _animator.SetBool("AvoidReady", false);
        }

        if (_playerAvoidKeyGenerator != null)
        {
            //_playerAvoidKeyGenerator.ClearAlarmKeyPad();
        }

        Time.timeScale = _previousTimeScale;

        if (_objectZoom != null)
        {
            _objectZoom.ZoomOut();
        }

        if (_focusScreenController != null)
        {
            _focusScreenController.EndFocusScreenSetting();
        }

        if (_playerMove != null)
        {
            _playerMove.enabled = _previousMoveEnabled;
        }

        if (_playerAttack != null)
        {
            _playerAttack.enabled = _previousAttackEnabled;
        }

        if (_skillManager != null)
        {
            _skillManager.enabled = _previousSkillManagerEnabled;
        }

        _avoidCaller = null;
    }

    private void OnDisable()
    {
        FinisherEnd();

        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _avoidTimer = null;
        _avoidMoveTween = null;
    }

    private void OnDestroy()
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();
    }
}
