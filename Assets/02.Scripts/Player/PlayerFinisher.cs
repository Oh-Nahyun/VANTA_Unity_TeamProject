using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerFinisher : MonoBehaviour
{
    [Header("Screen Split")]
    [SerializeField] private Material _screenSplitMaterial;
    [SerializeField] private float _splitAmount = 0.05f;
    [SerializeField] private float _lineWidth = 0.003f;
    [SerializeField] private float _splitOpenDuration = 0.08f;
    [SerializeField] private float _splitHoldDuration = 0.1f;
    [SerializeField] private float _splitCloseDuration = 0.2f;

    [Header("Reference")]
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private SkillAttack _skillAttack;
    [SerializeField] private SkillFocusScreenController _skillFocusScreenController;
    [SerializeField] private GameObject _finisherVfx;

    [Header("Charging")]
    [SerializeField] private GameObject _chargingObject;
    [SerializeField] private float _chargingDuration = 2f;
    [SerializeField] private int _maxChargeCount = 10;
    [SerializeField] private float _chargeScaleDuration = 0.08f;
    [SerializeField] private float _chargingShrinkDuration = 0.08f;

    [Header("Charging Camera Shake")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private float _startShakeForce = 0.03f;
    [SerializeField] private float _maxShakeForce = 1f;

    private static readonly int DashStartID =
        Shader.PropertyToID("_DashStart");

    private static readonly int DashEndID =
        Shader.PropertyToID("_DashEnd");

    private static readonly int SplitAmountID =
        Shader.PropertyToID("_SplitAmount");

    private static readonly int SplitActiveID =
        Shader.PropertyToID("_SplitActive");

    private static readonly int LineWidthID =
        Shader.PropertyToID("_LineWidth");

    private Vector2 _finisherDashStart;

    private Tween _screenSplitTween;
    private Tween _chargingTween;
    private Tween _chargingTimerTween;

    private float _currentShakeForce;

    private int _currentChargeCount;
    private float _finalChargeRatio;

    private bool _isFinisherStarted;
    private bool _isCharging;

    private void Awake()
    {
        ResetScreenSplit();
    }

    private void Update()
    {
        if (!Input.GetButtonDown("Fire1"))
            return;

        if (_isCharging)
        {
            AddCharge();
            return;
        }

        if (_isFinisherStarted)
            return;

        StartFinisher();
    }

    public void StartFinisher()
    {
        _isFinisherStarted = true;

        PlayerMove playerMove =
            GetComponent<PlayerMove>();

        playerMove.StopPlayerMovement();
        playerMove.enabled = false;

        GetComponent<PlayerAttack>().enabled = false;

        _skillManager.enabled = false;

        _animator.SetTrigger("StartFinisher");

        HitableObject hitableObject =
            FindAnyObjectByType<HitableObject>();

        if (hitableObject == null)
        {
            _isFinisherStarted = false;
            return;
        }

        ObjectFocus objectFocus =
            hitableObject.GetComponent<ObjectFocus>();

        if (objectFocus == null)
        {
            objectFocus =
                hitableObject.gameObject
                    .AddComponent<ObjectFocus>();
        }

        objectFocus.StartFocus();

        IBossMovable bossMovable =
            hitableObject.GetComponent<IBossMovable>();

        bossMovable?.MoveBlock();

        CameraFocusingController.Instance.SlowFocus(
            transform,
            0.8f,
            _chargingDuration
        );

        _skillFocusScreenController
            .StartFocusScreenSetting(
                StartCharging
            );
    }

    private void StartCharging()
    {
        StopCharging();

        _currentChargeCount = 0;
        _finalChargeRatio = 0f;

        _currentShakeForce =
            _startShakeForce;

        _isCharging = true;

        _chargingObject.SetActive(true);

        _chargingObject.transform.localScale =
            Vector3.zero;

        _chargingTimerTween =
            DOVirtual.DelayedCall(
                    _chargingDuration,
                    ChargingComplete
                )
                .SetUpdate(true);
    }

    private void AddCharge()
    {
        if (!_isCharging)
            return;

        if (_currentChargeCount >= _maxChargeCount)
            return;

        _currentChargeCount++;

        float chargeRatio =
            Mathf.Clamp01(
                (float)_currentChargeCount /
                _maxChargeCount
            );

        UpdateChargeVisual(
            chargeRatio
        );

        _currentShakeForce =
            Mathf.Lerp(
                _startShakeForce,
                _maxShakeForce,
                chargeRatio
            );

        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulse(
                _currentShakeForce
            );
        }

        Debug.Log(
            $"Charge : {_currentChargeCount}/{_maxChargeCount} " +
            $"({chargeRatio * 100f:0}%)"
        );
    }

    private void UpdateChargeVisual(
        float chargeRatio)
    {
        if (_chargingObject == null)
            return;

        Vector3 targetScale =
            Vector3.one * chargeRatio;

        _chargingTween?.Kill();

        _chargingTween =
            _chargingObject.transform
                .DOScale(
                    targetScale,
                    _chargeScaleDuration
                )
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
    }

    private void ChargingComplete()
    {
        if (!_isCharging)
            return;

        _isCharging = false;

        _finalChargeRatio =
            Mathf.Clamp01(
                (float)_currentChargeCount /
                _maxChargeCount
            );

        Debug.Log(
            $"Finisher Charge Complete : " +
            $"{_currentChargeCount}/{_maxChargeCount} " +
            $"({_finalChargeRatio * 100f:0}%)"
        );

        _chargingTimerTween?.Kill();
        _chargingTimerTween = null;

        _chargingTween?.Kill();

        _chargingTween =
            _chargingObject.transform
                .DOScale(
                    Vector3.zero,
                    _chargingShrinkDuration
                )
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _chargingObject.SetActive(false);

                    _chargingTween = null;

                    AttackToBoss();
                });
    }

    private void AttackToBoss()
    {
        HitableObject boss =
            FindAnyObjectByType<HitableObject>();

        if (boss == null)
            return;

        Vector2 startPosition =
            transform.position;

        Vector2 bossPosition =
            boss.transform.position;

        Vector2 attackDirection =
            (
                bossPosition -
                startPosition
            ).normalized;

        Vector2 targetPosition =
            bossPosition +
            attackDirection * 2f;

        _finisherDashStart =
            startPosition;

        Debug.Log(
            $"Final Charge Ratio : {_finalChargeRatio}"
        );

        _skillAttack.SingleDash(
            targetPosition,
            FinisherEnd
        );
    }

    private void FinisherEnd()
    {
        Camera cam =
            Camera.main;

        if (
            cam == null ||
            _screenSplitMaterial == null
        )
        {
            return;
        }

        Vector2 finisherDashEnd =
            transform.position;

        Vector3 startViewport =
            cam.WorldToViewportPoint(
                _finisherDashStart
            );

        Vector3 endViewport =
            cam.WorldToViewportPoint(
                finisherDashEnd
            );

        Vector2 dashStart =
            new Vector2(
                startViewport.x,
                startViewport.y
            );

        Vector2 dashEnd =
            new Vector2(
                endViewport.x,
                endViewport.y
            );

        _screenSplitMaterial.SetVector(
            DashStartID,
            dashStart
        );

        _screenSplitMaterial.SetVector(
            DashEndID,
            dashEnd
        );

        _screenSplitMaterial.SetFloat(
            SplitAmountID,
            0f
        );

        _screenSplitMaterial.SetFloat(
            LineWidthID,
            0f
        );

        _screenSplitMaterial.SetFloat(
            SplitActiveID,
            1f
        );

        _screenSplitTween?.Kill();

        Sequence sequence =
            DOTween.Sequence()
                .SetUpdate(true);

        sequence.Append(
            _screenSplitMaterial
                .DOFloat(
                    _splitAmount,
                    SplitAmountID,
                    _splitOpenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            _screenSplitMaterial
                .DOFloat(
                    _lineWidth,
                    LineWidthID,
                    _splitOpenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.AppendInterval(
            _splitHoldDuration
        );

        sequence.Append(
            _screenSplitMaterial
                .DOFloat(
                    0f,
                    SplitAmountID,
                    _splitCloseDuration
                )
                .SetEase(Ease.InOutQuad)
        );

        sequence.Join(
            _screenSplitMaterial
                .DOFloat(
                    0f,
                    LineWidthID,
                    _splitCloseDuration
                )
                .SetEase(Ease.InOutQuad)
        );

        sequence.OnComplete(() =>
        {
            _screenSplitMaterial.SetFloat(
                SplitAmountID,
                0f
            );

            _screenSplitMaterial.SetFloat(
                LineWidthID,
                0f
            );

            _screenSplitMaterial.SetFloat(
                SplitActiveID,
                0f
            );

            _screenSplitTween = null;

            _animator.SetTrigger("Start");

            _isFinisherStarted = false;
        });

        _screenSplitTween =
            sequence;
    }

    private void StopCharging()
    {
        _chargingTween?.Kill();
        _chargingTimerTween?.Kill();

        _chargingTween = null;
        _chargingTimerTween = null;

        _isCharging = false;

        _currentChargeCount = 0;
        _currentShakeForce = 0f;
    }

    private void ResetScreenSplit()
    {
        if (_screenSplitMaterial == null)
            return;

        _screenSplitTween?.Kill();

        _screenSplitTween = null;

        _screenSplitMaterial.SetFloat(
            SplitAmountID,
            0f
        );

        _screenSplitMaterial.SetFloat(
            LineWidthID,
            0f
        );

        _screenSplitMaterial.SetFloat(
            SplitActiveID,
            0f
        );
    }

    private void OnDestroy()
    {
        StopCharging();

        _screenSplitTween?.Kill();

        ResetScreenSplit();
    }
}
