using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerFinisher : MonoBehaviour
{
    [Header("피니셔 유지 시간 - 실제 시간")]
    [SerializeField, Min(0.1f)]
    private float _finisherDuration = 3f;

    [Header("타임슬로우")]
    [SerializeField, Range(0.01f, 1f)]
    private float _slowAmount = 0.2f;

    [Header("보스 중심 대쉬 반경")]
    [SerializeField, Min(0.1f)]
    private float _slashRadius = 2.5f;

    [Header("대쉬 시간 - 실제 시간")]
    [SerializeField, Min(0.01f)]
    private float _dashDuration = 0.12f;

    [Header("공격 시작점 이동 시간 - 실제 시간")]
    [SerializeField, Min(0.01f)]
    private float _repositionDuration = 0.04f;

    [Header("피니셔 줌")]
    [SerializeField]
    private float _finisherZoomAmount = 1.5f;

    [Header("종료 연출 라이트")]
    [SerializeField]
    private Light2D _finalLight;

    [SerializeField, Min(0f)]
    private float _finalLightRadius = 1f;

    [SerializeField, Min(0.01f)]
    private float _finalLightShrinkDuration = 0.3f;

    [Header("Focus")]
    [SerializeField]
    private SkillFocusScreenController _focusScreenController;

    private ObjectZoom _objectZoom;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private SkillAttack _skillAttack;
    private SkillManager _skillManager;
    private Rigidbody2D _rigidbody;

    private HitableObject _target;
    private GameObject _caller;

    private Tween _finisherTimer;
    private Tween _lightTween;

    private bool _isFinishing;
    private bool _isDashing;
    private bool _isFinalPhase;
    private bool _hasReportedSuccess;

    private bool _hasFinalDashArrived;
    private bool _hasFinishedAttackPhase;
    private bool _hasStartedLightEffect;

    private int _finisherVersion;

    private void Awake()
    {
        _objectZoom = GetComponentInChildren<ObjectZoom>();
        _playerMove = GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _skillAttack = GetComponentInChildren<SkillAttack>();
        _skillManager = GetComponentInChildren<SkillManager>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 테스트용 시작 키
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartPlayerFinisher();
        }

        if (!_isFinishing || _hasFinishedAttackPhase)
            return;

        // 최종 대쉬의 이동 완료 콜백을 기다림
        if (_isFinalPhase)
            return;

        if (!IsTargetValid())
        {
            FinishAttackPhase();
            return;
        }

        // 대쉬 중 클릭은 무시
        if (!_isDashing && Input.GetMouseButtonDown(0))
        {
            PlaySlash(false);
        }
    }

    public void StartPlayerFinisher(GameObject caller = null)
    {
        if (_isFinishing)
            return;

        StartFinisher(
            FindAnyObjectByType<HitableObject>(),
            caller
        );
    }

    public void StartFinisher(
        HitableObject target,
        GameObject caller = null)
    {
        if (_isFinishing || !isActiveAndEnabled)
            return;

        if (_rigidbody == null ||
            _skillAttack == null ||
            !_skillAttack.isActiveAndEnabled ||
            target == null ||
            !target.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                "피니셔에 필요한 컴포넌트 또는 타겟 없음",
                this
            );
            return;
        }

        if (!_skillAttack.BeginFinisher())
            return;

        _finisherVersion++;
        int version = _finisherVersion;

        _target = target;
        _caller = caller;

        _isFinishing = true;
        _isDashing = false;
        _isFinalPhase = false;
        _hasReportedSuccess = false;

        _hasFinalDashArrived = false;
        _hasFinishedAttackPhase = false;
        _hasStartedLightEffect = false;

        if (_playerMove != null)
        {
            _playerMove.enabled = false;
        }

        if (_playerAttack != null)
        {
            _playerAttack.enabled = false;
        }

        if (_skillManager != null)
        {
            _skillManager.enabled = false;
        }

        _rigidbody.linearVelocity = Vector2.zero;

        Time.timeScale = _slowAmount;

        if (_objectZoom != null)
        {
            _objectZoom.ZoomIn(_finisherZoomAmount);
        }

        if (_focusScreenController != null)
        {
            _focusScreenController.EndFocusScreenSetting();
        }

        _finisherTimer?.Kill();

        // 지정 시간이 되면 클릭 여부와 관계없이 최종 대쉬
        _finisherTimer = DOVirtual
            .DelayedCall(
                Mathf.Max(0.1f, _finisherDuration),
                () =>
                {
                    if (!IsCurrentFinisher(version))
                        return;

                    _finisherTimer = null;
                    StartFinalDash();
                }
            )
            .SetUpdate(true);
    }

    private void StartFinalDash()
    {
        if (!_isFinishing ||
            _isFinalPhase ||
            _hasFinishedAttackPhase)
        {
            return;
        }

        if (!IsTargetValid())
        {
            FinishAttackPhase();
            return;
        }

        _isFinalPhase = true;

        // 진행 중인 일반 대쉬의 콜백 무효화
        _finisherVersion++;

        _skillAttack.EndFinisher();

        if (!_skillAttack.BeginFinisher())
        {
            FinishAttackPhase();
            return;
        }

        _isDashing = false;

        PlaySlash(true);
    }

    private void PlaySlash(bool isFinal)
    {
        if (!_isFinishing ||
            _isDashing ||
            _hasFinishedAttackPhase)
        {
            return;
        }

        if (!isFinal && _isFinalPhase)
            return;

        if (!IsTargetValid())
        {
            FinishAttackPhase();
            return;
        }

        _isDashing = true;

        int version = _finisherVersion;

        // 매 공격마다 랜덤 방향
        float angle = Random.Range(0f, 360f);
        float radians = angle * Mathf.Deg2Rad;

        Vector2 axis = new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );

        Vector2 center = _target.transform.position;

        Vector2 startPosition =
            center + axis * Mathf.Max(0.1f, _slashRadius);

        // 공격 시작점으로 이동: 공격 판정 없음
        _skillAttack.FinisherDash(
            startPosition,
            Mathf.Max(0.01f, _repositionDuration),
            false,
            () =>
            {
                if (!IsCurrentFinisher(version))
                    return;

                if (!IsTargetValid())
                {
                    FinishAttackPhase();
                    return;
                }

                PlayAttackDash(axis, version, isFinal);
            }
        );
    }

    private void PlayAttackDash(
        Vector2 axis,
        int version,
        bool isFinal)
    {
        if (!IsCurrentFinisher(version))
            return;

        if (!IsTargetValid())
        {
            FinishAttackPhase();
            return;
        }

        Vector2 center = _target.transform.position;

        Vector2 direction =
            center - _rigidbody.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -axis;
        }

        direction.Normalize();

        Vector2 endPosition =
            center + direction * Mathf.Max(0.1f, _slashRadius);

        // 보스를 관통하는 대쉬 공격
        _skillAttack.FinisherDash(
            endPosition,
            Mathf.Max(0.01f, _dashDuration),
            true,
            () =>
            {
                if (!IsCurrentFinisher(version))
                    return;

                // 최종 대쉬 이동 완료 시점에만 기록
                if (isFinal)
                {
                    _hasFinalDashArrived = true;
                }

                ReportSuccess();

                if (!IsCurrentFinisher(version))
                    return;

                _isDashing = false;

                if (isFinal || !IsTargetValid())
                {
                    FinishAttackPhase();
                }
            }
        );
    }

    private bool IsCurrentFinisher(int version)
    {
        return _isFinishing &&
               !_hasFinishedAttackPhase &&
               version == _finisherVersion;
    }

    private bool IsTargetValid()
    {
        return _target != null &&
               _target.gameObject.activeInHierarchy &&
               _rigidbody != null &&
               _skillAttack != null &&
               _skillAttack.isActiveAndEnabled;
    }

    private void ReportSuccess()
    {
        if (_hasReportedSuccess)
            return;

        _hasReportedSuccess = true;

        if (_caller != null)
        {
            IAvoidInteraction interaction =
                _caller.GetComponent<IAvoidInteraction>();

            interaction?.AvoidResultBehavior(true);
        }
    }

    private void FinishAttackPhase()
    {
        if (!_isFinishing || _hasFinishedAttackPhase)
            return;

        _hasFinishedAttackPhase = true;

        _isFinalPhase = true;
        _isDashing = false;

        _finisherVersion++;

        _finisherTimer?.Kill();
        _finisherTimer = null;

        if (_skillAttack != null)
        {
            _skillAttack.EndFinisher();
        }

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
        }

        // 最終 대쉬가 도착했을 때만 라이트 연출
        if (_hasFinalDashArrived)
        {
            FinisherEnd();
        }
    }

    // 종료 시 라이트 로직만 실행
    public void FinisherEnd()
    {
        if (!_isFinishing ||
            !_hasFinalDashArrived ||
            _hasStartedLightEffect ||
            !isActiveAndEnabled)
        {
            return;
        }

        if (_finalLight == null)
        {
            Debug.LogWarning(
                "종료 연출에 사용할 Light2D를 지정해주세요.",
                this
            );
            return;
        }

        // 라이트 연출 중복 실행 방지
        _hasStartedLightEffect = true;

        _lightTween?.Kill();
        _lightTween = null;

        // 지정한 라이트만 남기기
        Light2D[] lights = FindObjectsByType<Light2D>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Light2D light in lights)
        {
            light.enabled = light == _finalLight;
        }

        _finalLight.gameObject.SetActive(true);
        _finalLight.enabled = true;

        float targetRadius =
            Mathf.Max(0f, _finalLightRadius);

        float startOuterRadius =
            _finalLight.pointLightOuterRadius;

        float startInnerRadius =
            _finalLight.pointLightInnerRadius;

        float innerRatio = startOuterRadius > 0f
            ? Mathf.Clamp01(
                startInnerRadius / startOuterRadius
            )
            : 0f;

        _lightTween = DOTween.To(
                () => _finalLight != null
                    ? _finalLight.pointLightOuterRadius
                    : targetRadius,
                radius =>
                {
                    if (_finalLight == null)
                        return;

                    _finalLight.pointLightInnerRadius =
                        radius * innerRatio;

                    _finalLight.pointLightOuterRadius = radius;
                },
                targetRadius,
                Mathf.Max(0.01f, _finalLightShrinkDuration)
            )
            .SetDelay(1f)
            .SetEase(Ease.OutExpo)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _lightTween = null;
            });
    }

    private void StopTweens()
    {
        _finisherVersion++;

        _finisherTimer?.Kill();
        _finisherTimer = null;

        _lightTween?.Kill();
        _lightTween = null;

        if (_isFinishing && _skillAttack != null)
        {
            _skillAttack.EndFinisher();
        }

        _isFinishing = false;
        _isDashing = false;
        _isFinalPhase = false;

        _hasFinalDashArrived = false;
        _hasFinishedAttackPhase = false;
        _hasStartedLightEffect = false;
    }

    private void OnDisable()
    {
        StopTweens();
    }

    private void OnDestroy()
    {
        StopTweens();
    }
}
