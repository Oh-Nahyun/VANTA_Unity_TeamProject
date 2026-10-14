using DG.Tweening;
using UnityEngine;

public class PlayerAvoid : MonoBehaviour
{
    [Header("타임슬로우 량")]
    [SerializeField] private float _slowAmount = 0.2f;

    [Header("회피기 사용할때 소리")]
    [SerializeField] private AudioClip _playerAvoidSound;

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

    [Header("회피 공격 데미지 시간")]
    [SerializeField] private float _avoidDashDamage = 300f;

    [Header("Focus")]
    [SerializeField] private SkillFocusScreenController _focusScreenController;

    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private SkillAttack _skillAttack;
    private SkillManager _skillManager;
    private Rigidbody2D _rigidbody;

    private KeyCode _avoidKey;

    private bool _isAvoidInteraction;
    private bool _isAvoidAttacking;

    // 회피 성공 후 중복 입력 방지
    private bool _isAvoidSucceeded;

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

        _skillManager =
            GetComponentInChildren<SkillManager>();

        _animator =
            GetComponent<Animator>();

        _rigidbody =
            GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckInputSuccess();
    }

    private void CheckInputSuccess()
    {
        // 회피 중이 아니거나 이미 성공했다면 입력 무시
        if (!_isAvoidInteraction || _isAvoidSucceeded)
            return;

        if (Input.GetKeyDown(_avoidKey))
        {
            AvoidSuccess();
        }
    }

    public void StartAvoidInteraction(GameObject caller = null)
    {
        if (caller == null)
            return;

        // 이미 회피 진행 중이라면 중복 시작 방지
        if (_isAvoidInteraction)
            return;

        SoundManager.Instance.PlayEffectSound(_playerAvoidSound);

        caller.transform.position =
            transform.position + Vector3.up * 0.1f;

        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _playerMove.StopPlayerMovement();

        _avoidKey = KeyCode.Space;

        _isAvoidInteraction = true;
        _isAvoidAttacking = false;
        _isAvoidSucceeded = false;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        _skillManager.enabled = false;

        Time.timeScale = _slowAmount;

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(false);

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
        if (caller == null)
            return;

        // 이미 회피 진행 중이라면 중복 시작 방지
        if (_isAvoidInteraction)
            return;

        SoundManager.Instance.PlayEffectSound(_playerAvoidSound);

        caller.transform.position =
            transform.position + Vector3.up * 0.1f;

        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();

        _playerMove.StopPlayerMovement();

        _avoidKey = KeyCode.Mouse1;

        _isAvoidInteraction = true;
        _isAvoidAttacking = true;
        _isAvoidSucceeded = false;

        _avoidCaller = caller;

        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        _skillManager.enabled = false;

        Time.timeScale = _slowAmount;

        _objectZoom.ZoomIn(
            _avoidZoomAmount
        );

        _playerAvoidKeyGenerator
            .CreateAlarmKeyPad(true);

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
        // 이미 성공했다면 중복 실행 방지
        if (!_isAvoidInteraction || _isAvoidSucceeded)
            return;

        // 성공 즉시 추가 입력 차단
        _isAvoidSucceeded = true;

        _avoidTimer?.Kill();
        _avoidTimer = null;

        _avoidCaller?
            .GetComponent<IAvoidInteraction>()?
            .AvoidResultBehavior(true);

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

        if (!_isAvoidAttacking)
        {
            PlayerPetCaller.Instance
                .CallPetToAttackSkill();
        }
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

        DOVirtual.DelayedCall(0.3f, () =>
        {
            _skillAttack.SingleDash(
                targetPosition,
                AttackToBossComplete
            );
        });

        DOVirtual.DelayedCall(2f, () =>
        {
            boss.ExecuteHitEvent(
                _avoidDashDamage,
                DamageIndicatorType.Critical
            );
        });
    }

    private void AttackToBossComplete()
    {
        AvoidEnd();
    }

    private void AvoidFail()
    {
        if (!_isAvoidInteraction || _isAvoidSucceeded)
            return;

        _avoidTimer = null;

        if (_avoidCaller != null)
        {
            IAvoidInteraction avoidInteraction =
                _avoidCaller
                    .GetComponent<IAvoidInteraction>();

            avoidInteraction?
                .AvoidResultBehavior(false);
        }

        AvoidEnd();
    }

    private void AvoidEnd()
    {
        if (!_isAvoidInteraction)
            return;

        _isAvoidInteraction = false;
        _isAvoidAttacking = false;
        _isAvoidSucceeded = false;

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
        _skillManager.enabled = true;
    }

    private void OnDestroy()
    {
        _avoidTimer?.Kill();
        _avoidMoveTween?.Kill();
    }
}
