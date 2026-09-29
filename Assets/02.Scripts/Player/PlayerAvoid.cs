using DG.Tweening;
using UnityEngine;

public class PlayerAvoid : MonoBehaviour
{
    [Header("타임슬로우 량")]
    [SerializeField] private float _slowAmount = 0.2f;

    [Header("회피기 사용할때 줌 량")]
    [SerializeField] private float _avoidZoomAmount = 1.5f;

    [Header("회피 입력 제한시간")]
    [SerializeField] private float _avoidDuration = 2f;

    [Header("회피 거리")]
    [SerializeField] private float _dashDistance = 1.5f;

    [Header("회피 이동 시간")]
    [SerializeField] private float _avoidDashDuration = 0.5f;

    [Header("회피 공격")]
    [SerializeField] private float _avoidAttackDuration = 0.2f;

    [Header("Focus")]
    [SerializeField] private SkillFocusScreenController _focusScreenController;
    [SerializeField] private SkillDashScreenController _dashScreenController;


    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private Rigidbody2D _rigidbody;


    private KeyCode _avoidKey;

    private bool _isAvoidInteraction;
    private bool _isAvoidAttacking;

    private GameObject _avoidCaller;

    private Tween _avoidTimer;
    private Tween _avoidMoveTween;
    private Tween _attackMoveTween;


    private void Awake()
    {
        _objectZoom =
            GetComponentInChildren<ObjectZoom>();

        _playerAvoidKeyGenerator =
            GetComponent<PlayerAvoidKeyGenerator>();

        _playerMove =
            GetComponent<PlayerMove>();

        _playerAttack =
            GetComponent<PlayerAttack>();

        _animator =
            GetComponent<Animator>();

        _rigidbody =
            GetComponent<Rigidbody2D>();
    }


    private void Update()
    {
        CheckInputSuccess();

        // 테스트
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartAvoidInteraction();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartAvoidAttackInteraction();
        }
    }


    // =========================================================
    // Input
    // =========================================================

    private void CheckInputSuccess()
    {
        if (!_isAvoidInteraction)
            return;

        if (Input.GetKeyDown(_avoidKey))
        {
            AvoidSuccess();
        }
    }


    // =========================================================
    // 일반 회피 시작
    // =========================================================

    public void StartAvoidInteraction(GameObject caller = null)
    {
        _avoidTimer?.Kill();

        _avoidKey = KeyCode.Space;

        _isAvoidInteraction = true;
        _isAvoidAttacking = false;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;


        // =========================
        // 타임 슬로우
        // =========================

        Time.timeScale = _slowAmount;


        // =========================
        // 줌
        // =========================

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );


        // =========================
        // 키 UI
        // =========================

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(
                _avoidKey
            );


        // =========================
        // 회피 준비 애니메이션
        // =========================

        _animator.SetBool(
            "AvoidReady",
            true
        );


        // =========================
        // 원형 Focus 시작
        // =========================

        if (_focusScreenController != null)
        {
            _focusScreenController
                .StartFocusScreenSetting(null);
        }


        // =========================
        // 입력 제한시간
        // =========================

        _avoidTimer =
            DOVirtual
                .DelayedCall(
                    _avoidDuration,
                    AvoidEnd
                )
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    AvoidFail();
                });
    }


    // =========================================================
    // 공격 회피 시작
    // =========================================================

    public void StartAvoidAttackInteraction(GameObject caller = null)
    {
        _avoidTimer?.Kill();

        _avoidKey = KeyCode.Mouse1;

        _isAvoidInteraction = true;
        _isAvoidAttacking = true;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;


        // =========================
        // 타임 슬로우
        // =========================

        Time.timeScale = _slowAmount;


        // =========================
        // 줌
        // =========================

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );


        // =========================
        // 키 UI
        // =========================

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(
                _avoidKey
            );


        // =========================
        // 회피 준비 애니메이션
        // =========================

        _animator.SetBool(
            "AvoidReady",
            true
        );


        // =========================
        // 원형 Focus 시작
        // =========================

        if (_focusScreenController != null)
        {
            _focusScreenController
                .StartFocusScreenSetting(null);
        }


        // =========================
        // 입력 제한시간
        // =========================

        _avoidTimer =
            DOVirtual
                .DelayedCall(
                    _avoidDuration,
                    AvoidEnd
                )
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    AvoidFail();
                });
    }


    // =========================================================
    // 회피 성공
    // =========================================================

    private void AvoidSuccess()
    {
        if (!_isAvoidInteraction)
            return;


        // 입력 제한 타이머 종료
        _avoidTimer?.Kill();
        _avoidTimer = null;


        // 키 UI 제거
        _playerAvoidKeyGenerator
            .ClearAlarmKeyPad();


        // 회피 준비 종료
        _animator.SetBool(
            "AvoidReady",
            false
        );


        // 회피 애니메이션
        _animator.SetTrigger(
            "Avoid"
        );


        // 보스 반대 방향으로 이동
        DashToReverseSide();
    }


    // =========================================================
    // 보스 반대 방향 회피
    // =========================================================

    public void DashToReverseSide()
    {
        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (_rigidbody == null)
        {
            Debug.LogError(
                "Rigidbody2D 없음"
            );

            AvoidEnd();

            return;
        }

        if (boss == null)
        {
            Debug.LogError(
                "HitableObject 없음"
            );

            AvoidEnd();

            return;
        }


        // =========================
        // 보스 → 플레이어 방향
        // =========================

        Vector2 direction =
            (
                (Vector2)transform.position -
                (Vector2)boss.transform.position
            ).normalized;


        Vector2 targetPosition =
            _rigidbody.position +
            direction * _dashDistance;


        Debug.Log(
            $"회피 이동 | 현재: {_rigidbody.position}" +
            $" / 방향: {direction}" +
            $" / 목표: {targetPosition}"
        );


        // =========================
        // 이동
        // =========================

        _avoidMoveTween?.Kill();

        _avoidMoveTween =
            _rigidbody
                .DOMove(
                    targetPosition,
                    _avoidDashDuration
                )
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(() =>
                {
                    // =========================
                    // 공격형 회피
                    // =========================

                    if (_isAvoidAttacking)
                    {
                        AttackToBoss();
                    }

                    // =========================
                    // 일반 회피
                    // =========================

                    else
                    {
                        AvoidEnd();
                    }
                });
    }


    // =========================================================
    // 회피 공격
    // =========================================================

    private void AttackToBoss()
    {
        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (boss == null)
        {
            Debug.LogError(
                "HitableObject 없음"
            );

            AvoidEnd();

            return;
        }

        if (_rigidbody == null)
        {
            Debug.LogError(
                "Rigidbody2D 없음"
            );

            AvoidEnd();

            return;
        }


        Vector2 startPosition =
            _rigidbody.position;

        Vector2 bossPosition =
            boss.transform.position;


        // =========================
        // 현재 위치 → 보스 방향
        // =========================

        Vector2 attackDirection =
            (
                bossPosition -
                startPosition
            ).normalized;


        // =========================
        // 기존 원형 Focus 종료
        // =========================

        if (_focusScreenController != null)
        {
            _focusScreenController
                .EndFocusScreenSetting();
        }


        // =========================
        // 일자 Focus 시작
        // =========================

        if (_dashScreenController != null)
        {
            _dashScreenController
                .StartDashScreenSetting(
                    attackDirection
                );
        }


        // =========================
        // 공격 애니메이션
        // =========================

        _animator.SetTrigger(
            "Attack"
        );


        // =========================
        // 보스에게 이동
        // =========================

        _attackMoveTween?.Kill();

        _attackMoveTween =
            _rigidbody
                .DOMove(
                    bossPosition,
                    _avoidAttackDuration
                )
                .SetEase(Ease.InQuad)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(() =>
                {
                    AttackToBossComplete();
                });
    }


    // =========================================================
    // 회피 공격 완료
    // =========================================================

    private void AttackToBossComplete()
    {
        // 일자 Focus 종료
        if (_dashScreenController != null)
        {
            _dashScreenController
                .EndDashScreenSetting();
        }

        AvoidEnd();
    }


    // =========================================================
    // 회피 실패
    // =========================================================

    private void AvoidFail()
    {
        Debug.Log(
            "회피 실패"
        );


        if (_avoidCaller != null)
        {
            IAvoidInteraction avoidInteraction =
                _avoidCaller.GetComponent<IAvoidInteraction>();

            avoidInteraction?.AvoidFailure();
        }


        AvoidEnd();
    }


    // =========================================================
    // 회피 종료
    // =========================================================

    private void AvoidEnd()
    {
        if (!_isAvoidInteraction)
            return;


        // =========================
        // 상태 초기화
        // =========================

        _isAvoidInteraction = false;
        _isAvoidAttacking = false;


        // =========================
        // 애니메이션
        // =========================

        _animator.SetBool(
            "AvoidReady",
            false
        );


        // =========================
        // 타이머 제거
        // =========================

        _avoidTimer?.Kill();
        _avoidTimer = null;


        // =========================
        // 키 UI 제거
        // =========================

        _playerAvoidKeyGenerator
            .ClearAlarmKeyPad();


        // =========================
        // 타임스케일 복구
        // =========================

        Time.timeScale = 1f;


        // =========================
        // 줌 복구
        // =========================

        _objectZoom.ZoomOut();


        // =========================
        // 원형 Focus 종료
        // =========================

        if (_focusScreenController != null)
        {
            _focusScreenController
                .EndFocusScreenSetting();
        }


        // =========================
        // 일자 Focus 종료
        // =========================

        if (_dashScreenController != null)
        {
            _dashScreenController
                .EndDashScreenSetting();
        }


        // =========================
        // 플레이어 조작 복구
        // =========================

        _playerMove.enabled = true;
        _playerAttack.enabled = true;


        Debug.Log(
            $"회피 종료 : {_avoidKey}"
        );
    }


    private void OnDestroy()
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();
        _attackMoveTween?.Kill();
    }
}
