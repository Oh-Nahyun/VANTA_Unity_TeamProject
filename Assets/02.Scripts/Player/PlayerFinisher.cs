using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerFinisher : MonoBehaviour
{
    [Header("Screen Split - 최대 차징 기준")]
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
    [SerializeField] private GameObject _finisherVfx1;
    [SerializeField] private GameObject _finisherVfx2;
    [SerializeField] private GameObject _finisherVfx3;
    [SerializeField] private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    [SerializeField] private WeaknessThreadController _weaknessThreadController;
    [Header("Charging")]
    [SerializeField] private GameObject _chargingObject;
    [SerializeField] private float _chargingDuration = 2f;
    [SerializeField, Min(1)] private int _maxChargeCount = 10;
    [SerializeField] private float _chargeScaleDuration = 0.08f;
    [SerializeField] private float _chargingShrinkDuration = 0.08f;

    [Header("Charging Camera Shake")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private float _startShakeForce = 0.03f;
    [SerializeField] private float _maxShakeForce = 1f;
    [Header("Charging Camera Zoom")]
    [SerializeField] private float _startZoomAmount = 0.2f;
    [SerializeField] private float _zoomPerCharge = 0.08f;
    [SerializeField] private float _chargeZoomDuration = 0.08f;
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
        if (_isFinisherStarted)
            return;

        HitableObject hitableObject =
            FindAnyObjectByType<HitableObject>();

        if (hitableObject == null)
            return;

        _isFinisherStarted = true;

        PlayerMove playerMove =
            GetComponent<PlayerMove>();

        playerMove.StopPlayerMovement();
        playerMove.enabled = false;

        GetComponent<PlayerAttack>().enabled = false;

        _skillManager.enabled = false;

        _animator.SetTrigger("StartFinisher");

        ObjectFocus objectFocus =
            hitableObject.GetComponent<ObjectFocus>();

        if (objectFocus == null)
        {
            objectFocus =
                hitableObject.gameObject.AddComponent<ObjectFocus>();
        }

        objectFocus.StartFocus();

        IBossMovable bossMovable =
            hitableObject.GetComponent<IBossMovable>();

        bossMovable?.MoveBlock();

        _playerAvoidKeyGenerator.CreateFinisherKey();

        CameraFocusingController.Instance.SlowFocus(
            transform,
            _startZoomAmount,
            _chargeZoomDuration

        );

        _weaknessThreadController.MakeFinishWeaknessThread(transform.position,hitableObject.transform.position);
        _skillFocusScreenController.StartFocusScreenSetting(
            StartCharging
        );
    }

    private void StartCharging()
    {
        StopCharging();

        _currentChargeCount = 0;
        _finalChargeRatio = 0f;
        _currentShakeForce = _startShakeForce;

        _isCharging = true;

        if (_chargingObject != null)
        {
            _chargingObject.SetActive(true);
            _chargingObject.transform.localScale = Vector3.zero;
        }

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

        int maxChargeCount =
            Mathf.Max(1, _maxChargeCount);

        if (_currentChargeCount >= maxChargeCount)
            return;

        _currentChargeCount++;
        float currentZoomAmount =
            _startZoomAmount +
            (_zoomPerCharge * _currentChargeCount);

        CameraFocusingController.Instance.SlowFocus(
            transform,
            currentZoomAmount,
            _chargeZoomDuration
        );
        float chargeRatio =
            Mathf.Clamp01(
                (float)_currentChargeCount / maxChargeCount
            );

        UpdateChargeVisual(chargeRatio);

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
            $"Charge : {_currentChargeCount}/{maxChargeCount} " +
            $"({chargeRatio * 100f:0}%)"
        );

        // 최대 차징에 도달하면 즉시 완료하고 대시.
        if (_currentChargeCount >= maxChargeCount)
        {
            ChargingComplete();
        }
    }

    private void UpdateChargeVisual(float chargeRatio)
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
        // 타이머와 최대 차징이 겹쳐도 한 번만 실행.
        if (!_isCharging)
            return;

        CameraFocusingController.Instance.ReturnCamera();
        _weaknessThreadController.BrokenThread();
        
        _isCharging = false;

        _chargingTimerTween?.Kill();
        _chargingTimerTween = null;

        _chargingTween?.Kill();
        _chargingTween = null;

        _playerAvoidKeyGenerator.ClearAlarmKeyPad();

        _finalChargeRatio =
            Mathf.Clamp01(
                (float)_currentChargeCount /
                Mathf.Max(1, _maxChargeCount)
            );

        Debug.Log(
            $"Finisher Charge Complete : " +
            $"{_currentChargeCount}/{_maxChargeCount} " +
            $"({_finalChargeRatio * 100f:0}%)"
        );

        if (_chargingObject != null)
        {
            // 확정된 차징 크기에서 축소 시작.
            _chargingObject.transform.localScale =
                Vector3.one * _finalChargeRatio;

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
                    });
        }
        
        // 오브젝트 축소 완료를 기다리지 않고 즉시 대시.
        AttackToBoss();
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
            (bossPosition - startPosition).normalized;

        Vector2 targetPosition =
            bossPosition + attackDirection * 2f;

        _finisherDashStart = startPosition;

        Debug.Log(
            $"Final Charge Ratio : {_finalChargeRatio}"
        );

        if (_finalChargeRatio < 0.4)
        {
            Instantiate(_finisherVfx1, bossPosition, Quaternion.identity);
        }else if (_finalChargeRatio >= 0.4 && _finalChargeRatio < 0.8)
        {
            Instantiate(_finisherVfx2, bossPosition, Quaternion.identity);
        }
        else
        {
            Instantiate(_finisherVfx3, bossPosition, Quaternion.identity);

        }
        
        
        _skillAttack.FinishDash(
            targetPosition,
            _finalChargeRatio,
            FinisherEnd
        );
    }

    private void FinisherEnd()
    {
        Camera cam = Camera.main;

        if (cam == null || _screenSplitMaterial == null)
        {
            return;
        }

        // 이전 트윈을 먼저 중지하고 초기값 설정.
        _screenSplitTween?.Kill();
        _screenSplitTween = null;

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

        // 최종 차징 결과에 따라 화면 분리량과 선 두께 결정.
        float chargeRatio =
            Mathf.Clamp01(_finalChargeRatio);

        float targetSplitAmount =
            _splitAmount * chargeRatio;

        float targetLineWidth =
            _lineWidth * chargeRatio;

        Sequence sequence =
            DOTween.Sequence()
                .SetUpdate(true);

        
        sequence.Append(
            _screenSplitMaterial
                .DOFloat(
                    targetSplitAmount,
                    SplitAmountID,
                    _splitOpenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            _screenSplitMaterial
                .DOFloat(
                    targetLineWidth,
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

        _screenSplitTween = sequence;
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
