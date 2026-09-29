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

    [Header("회피 공격 관통 거리")]
    [SerializeField] private float _attackPassDistance = 1.5f;

    [Header("Focus")]
    [SerializeField] private SkillFocusScreenController _focusScreenController;

    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private SkillAttack _skillAttack;
    private Rigidbody2D _rigidbody;

    private KeyCode _avoidKey;

    private bool _isAvoidInteraction;
    private bool _isAvoidAttacking;

    private GameObject _avoidCaller;

    private Tween _avoidTimer;
    private Tween _avoidMoveTween;

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

        _skillAttack =
            GetComponentInChildren<SkillAttack>();

        _animator =
            GetComponent<Animator>();

        _rigidbody =
            GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckInputSuccess();

        if (Input.GetKeyDown(KeyCode.K))
        {
            StartAvoidInteraction();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartAvoidAttackInteraction();
        }
    }

    private void CheckInputSuccess()
    {
        if (!_isAvoidInteraction)
            return;

        if (Input.GetKeyDown(_avoidKey))
        {
            AvoidSuccess();
        }
     
    }

    public void StartAvoidInteraction(GameObject caller = null)
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _avoidKey = KeyCode.Space;

        _isAvoidInteraction = true;
        _isAvoidAttacking = false;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;

        Time.timeScale = _slowAmount;

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(
                _avoidKey
            );

        _animator.SetBool(
            "AvoidReady",
            true
        );

        if (_focusScreenController != null)
        {
            _focusScreenController
                .StartFocusScreenSetting(null);
        }

        _avoidTimer =
            DOVirtual
                .DelayedCall(
                    _avoidDuration,
                    AvoidFail
                )
                .SetUpdate(true);
    }

    public void StartAvoidAttackInteraction(GameObject caller = null)
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _avoidKey = KeyCode.Mouse1;

        _isAvoidInteraction = true;
        _isAvoidAttacking = true;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;

        Time.timeScale = _slowAmount;

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(
                _avoidKey
            );

        _animator.SetBool(
            "AvoidReady",
            true
        );

        if (_focusScreenController != null)
        {
            _focusScreenController
                .StartFocusScreenSetting(null);
        }

        _avoidTimer =
            DOVirtual
                .DelayedCall(
                    _avoidDuration,
                    AvoidFail
                )
                .SetUpdate(true);
    }

    private void AvoidSuccess()
    {
        if (!_isAvoidInteraction)
            return;

        _avoidTimer?.Kill();
        _avoidTimer = null;

        _playerAvoidKeyGenerator
            .ClearAlarmKeyPad();

        _animator.SetBool(
            "AvoidReady",
            false
        );

        _animator.SetTrigger(
            "Avoid"
        );

        DashToReverseSide();
    }

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

        Vector2 direction =
            (
                (Vector2)transform.position -
                (Vector2)boss.transform.position
            ).normalized;

        Vector2 targetPosition =
            _rigidbody.position +
            direction * _dashDistance;

        _avoidMoveTween?.Kill();

        _avoidMoveTween =
            _rigidbody
                .DOMove(
                    targetPosition,
                    _avoidDashDuration
                )
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _avoidMoveTween = null;

                    if (_isAvoidAttacking)
                    {
                        AttackToBoss();
                    }
                    else
                    {
                        AvoidEnd();
                    }
                });
    }

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

        if (_skillAttack == null)
        {
            Debug.LogError(
                "SkillAttack 없음"
            );

            AvoidEnd();
            return;
        }

        Vector2 startPosition =
            _rigidbody.position;

        Vector2 bossPosition =
            boss.transform.position;

        Vector2 attackDirection =
            (
                bossPosition -
                startPosition
            ).normalized;

        Vector2 targetPosition =
            bossPosition +
            attackDirection * _attackPassDistance;

        if (_focusScreenController != null)
        {
            _focusScreenController
                .EndFocusScreenSetting();
        }

        _skillAttack.SingleDash(
            targetPosition,
            AttackToBossComplete
        );
    }

    private void AttackToBossComplete()
    {
        AvoidEnd();
    }

    private void AvoidFail()
    {
        if (!_isAvoidInteraction)
            return;

        _avoidTimer = null;

        if (_avoidCaller != null)
        {
            IAvoidInteraction avoidInteraction =
                _avoidCaller.GetComponent<IAvoidInteraction>();

            avoidInteraction?.AvoidFailure();
        }

        AvoidEnd();
    }

    private void AvoidEnd()
    {
        if (!_isAvoidInteraction)
            return;

        _isAvoidInteraction = false;
        _isAvoidAttacking = false;

        _animator.SetBool(
            "AvoidReady",
            false
        );

        _avoidTimer?.Kill();
        _avoidTimer = null;

        _playerAvoidKeyGenerator
            .ClearAlarmKeyPad();

        Time.timeScale = 1f;

        _objectZoom.ZoomOut();

        if (_focusScreenController != null)
        {
            _focusScreenController
                .EndFocusScreenSetting();
        }

        _playerMove.enabled = true;
        _playerAttack.enabled = true;
    }

    private void OnDestroy()
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();
    }
}
