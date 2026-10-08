using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFocusingController : MonoBehaviour
{
    private static CameraFocusingController _instance;
    public static CameraFocusingController Instance => _instance;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineBrain _brain;

    [Header("Focus Setting")]
    [SerializeField] private float _focusDamping = 0.3f;
    [SerializeField] private float _zoomInDuration = 0.25f;
    [SerializeField] private float _zoomOutDuration = 0.3f;
    [SerializeField] private float _targetChangeDuration = 0.15f;

    [Header("Show Origin")]
    [SerializeField] private float _originOrthographicSize = 5f;
    [SerializeField] private float _originDuration = 1f;

    private CinemachineFollow _follow;

    private Transform _defaultTarget;
    private Transform _originTarget;
    private Transform _currentFocusTarget;

    private float _defaultOrthographicSize;
    private Vector3 _defaultDamping;

    private Tween _zoomTween;
    private Tween _dampingTween;

    private bool _isFocusing;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeCamera();
        CreateOriginTarget();
    }

    private void Start()
    {
        StartCoroutine(
            RefreshCameraBindingAfterStart()
        );
    }

    private void InitializeCamera()
    {
        if (_camera == null)
        {
            Debug.LogError(
                "CinemachineCamera가 없습니다."
            );

            return;
        }

        EnsureCameraObjectIsActive();
        ResolveBrain();

        if (_brain != null)
        {
            ConfigureBrain();
        }

        ResolveFollowComponent();

        _defaultTarget =
            _camera.Follow;

        ResolveDefaultTarget();
        RefreshCameraState(
            true
        );

        _defaultOrthographicSize =
            _camera.Lens.OrthographicSize;

        _defaultDamping =
            _follow
                .TrackerSettings
                .PositionDamping;
    }

    private IEnumerator RefreshCameraBindingAfterStart()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return null;

            RefreshCameraState(
                true
            );
        }
    }

    private void EnsureCameraObjectIsActive()
    {
        if (_camera == null)
            return;

        if (!_camera.gameObject.activeSelf)
        {
            _camera.gameObject.SetActive(
                true
            );
        }

        if (!_camera.enabled)
        {
            _camera.enabled =
                true;
        }
    }

    private void ResolveBrain()
    {
        if (_brain == null)
        {
            _brain =
                Camera.main != null
                    ? Camera.main.GetComponent<CinemachineBrain>()
                    : FindAnyObjectByType<CinemachineBrain>();
        }
    }

    private void ConfigureBrain()
    {
        if (_brain == null)
            return;

        _brain.enabled = true;
        _brain.UpdateMethod =
            CinemachineBrain.UpdateMethods.SmartUpdate;
        _brain.BlendUpdateMethod =
            CinemachineBrain.BrainUpdateMethods.LateUpdate;
    }

    private void ResolveFollowComponent()
    {
        _follow =
            _camera.GetComponent<CinemachineFollow>();

        if (_follow == null)
        {
            _follow =
                _camera.gameObject
                    .AddComponent<CinemachineFollow>();
        }
    }

    private void CreateOriginTarget()
    {
        GameObject originObject =
            new GameObject(
                "CameraOriginTarget"
            );

        originObject.transform.position =
            Vector3.zero;

        _originTarget =
            originObject.transform;
    }

    public void Focus(
        Transform target,
        float zoomAmount)
    {
        if (target == null ||
            _camera == null)
        {
            return;
        }

        KillTweens();
        RefreshCameraState(
            false
        );
        ResolveDefaultTarget();

        _isFocusing = true;
        _currentFocusTarget = target;

        _camera.Follow = target;
        _camera.PreviousStateIsValid =
            false;

        ZoomIn(
            zoomAmount
        );
    }

    public void SlowFocus(
        Transform target,
        float zoomAmount,
        float duration)
    {
        if (target == null ||
            _camera == null)
        {
            return;
        }

        KillTweens();
        RefreshCameraState(
            false
        );
        ResolveDefaultTarget();

        _isFocusing = true;
        _currentFocusTarget = target;

        _camera.Follow = target;
        _camera.PreviousStateIsValid =
            false;

        float targetSize =
            _defaultOrthographicSize -
            zoomAmount;

        targetSize =
            Mathf.Max(
                0.1f,
                targetSize
            );

        _zoomTween =
            DOTween.To(
                    () =>
                        _camera
                            .Lens
                            .OrthographicSize,
                    value =>
                        _camera
                            .Lens
                            .OrthographicSize =
                            value,
                    targetSize,
                    duration
                )
                .SetEase(
                    Ease.InOutSine
                )
                .SetUpdate(true);

        Vector3 focusDamping =
            new Vector3(
                _focusDamping,
                _focusDamping,
                _defaultDamping.z
            );

        _dampingTween =
            DOTween.To(
                    () =>
                        _follow
                            .TrackerSettings
                            .PositionDamping,
                    value =>
                        _follow
                            .TrackerSettings
                            .PositionDamping =
                            value,
                    focusDamping,
                    duration
                )
                .SetEase(
                    Ease.InOutSine
                )
                .SetUpdate(true)
                .OnComplete(
                    () =>
                    {
                        _dampingTween = null;

                        if (zoomAmount == 0)
                        {
                            ReturnCamera();
                        }
                    }
                );
    }

    public void ShowOrigin()
    {
        if (_camera == null ||
            _follow == null ||
            _originTarget == null)
        {
            return;
        }

        KillTweens();
        RefreshCameraState(
            false
        );

        _isFocusing = true;
        _currentFocusTarget = _originTarget;

        _originTarget.position =
            Vector3.zero;

        _camera.Follow =
            _originTarget;
        _camera.PreviousStateIsValid =
            false;

        Vector3 originDamping =
            new Vector3(
                _focusDamping,
                _focusDamping,
                _defaultDamping.z
            );

        _zoomTween =
            DOTween.To(
                    () =>
                        _camera
                            .Lens
                            .OrthographicSize,
                    value =>
                        _camera
                            .Lens
                            .OrthographicSize =
                            value,
                    _originOrthographicSize,
                    _originDuration
                )
                .SetEase(
                    Ease.InOutSine
                )
                .SetUpdate(true)
                .OnComplete(
                    () =>
                    {
                        _zoomTween = null;
                    }
                );

        _dampingTween =
            DOTween.To(
                    () =>
                        _follow
                            .TrackerSettings
                            .PositionDamping,
                    value =>
                        _follow
                            .TrackerSettings
                            .PositionDamping =
                            value,
                    originDamping,
                    _originDuration
                )
                .SetEase(
                    Ease.InOutSine
                )
                .SetUpdate(true)
                .OnComplete(
                    () =>
                    {
                        _dampingTween = null;
                    }
                );
    }

    private void ZoomIn(
        float zoomAmount)
    {
        float targetSize =
            _defaultOrthographicSize -
            zoomAmount;

        targetSize =
            Mathf.Max(
                0.1f,
                targetSize
            );

        _zoomTween =
            DOTween.To(
                    () =>
                        _camera
                            .Lens
                            .OrthographicSize,
                    value =>
                        _camera
                            .Lens
                            .OrthographicSize =
                            value,
                    targetSize,
                    _zoomInDuration
                )
                .SetEase(
                    Ease.OutQuad
                )
                .SetUpdate(true);

        Vector3 focusDamping =
            new Vector3(
                _focusDamping,
                _focusDamping,
                _defaultDamping.z
            );

        _dampingTween =
            DOTween.To(
                    () =>
                        _follow
                            .TrackerSettings
                            .PositionDamping,
                    value =>
                        _follow
                            .TrackerSettings
                            .PositionDamping =
                            value,
                    focusDamping,
                    _targetChangeDuration
                )
                .SetEase(
                    Ease.OutQuad
                )
                .SetUpdate(true);
    }

    public void ReturnCamera()
    {
        if (!_isFocusing)
            return;

        _isFocusing = false;
        _currentFocusTarget = null;

        KillTweens();

        ResolveDefaultTarget();

        if (_defaultTarget == null)
        {
            Debug.LogWarning(
                "카메라 기본 Follow 타겟을 찾지 못했습니다."
            );

            return;
        }

        _camera.Follow =
            _defaultTarget;
        _camera.PreviousStateIsValid =
            false;

        _zoomTween =
            DOTween.To(
                    () =>
                        _camera
                            .Lens
                            .OrthographicSize,
                    value =>
                        _camera
                            .Lens
                            .OrthographicSize =
                            value,
                    _defaultOrthographicSize,
                    _zoomOutDuration
                )
                .SetEase(
                    Ease.OutQuad
                )
                .SetUpdate(true);

        _dampingTween =
            DOTween.To(
                    () =>
                        _follow
                            .TrackerSettings
                            .PositionDamping,
                    value =>
                        _follow
                            .TrackerSettings
                            .PositionDamping =
                            value,
                    _defaultDamping,
                    _targetChangeDuration
                )
                .SetEase(
                    Ease.OutQuad
                )
                .SetUpdate(true);
    }

    private void KillTweens()
    {
        _zoomTween?.Kill();
        _dampingTween?.Kill();

        _zoomTween = null;
        _dampingTween = null;
    }

    private void ResolveDefaultTarget()
    {
        if (_defaultTarget != null)
            return;

        if (_camera != null &&
            _camera.Follow != null &&
            _camera.Follow != _originTarget)
        {
            _defaultTarget =
                _camera.Follow;

            return;
        }

        GameObject player =
            null;

        try
        {
            player =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );
        }
        catch (UnityException)
        {
            player =
                null;
        }

        if (player == null)
        {
            Player playerComponent =
                FindAnyObjectByType<Player>();

            if (playerComponent != null)
            {
                player =
                    playerComponent.gameObject;
            }
        }

        if (player == null)
            return;

        _defaultTarget =
            player.transform;

        if (_camera != null &&
            _camera.Follow == null)
        {
            _camera.Follow =
                _defaultTarget;
        }
    }

    private void LateUpdate()
    {
        if (_camera == null)
            return;

        RefreshCameraState(
            false
        );

        if (_camera.Follow != null)
            return;

        ResolveDefaultTarget();

        if (_defaultTarget == null)
            return;

        _camera.Follow =
            _isFocusing && _currentFocusTarget != null
                ? _currentFocusTarget
                : _defaultTarget;
    }

    private void RefreshCameraState(
        bool resetPreviousState)
    {
        if (_camera == null)
            return;

        EnsureCameraObjectIsActive();
        ResolveBrain();
        ConfigureBrain();
        ResolveFollowComponent();
        ResolveDefaultTarget();

        Transform target =
            _isFocusing && _currentFocusTarget != null
                ? _currentFocusTarget
                : _defaultTarget;

        if (target != null &&
            _camera.Follow != target)
        {
            _camera.Follow =
                target;
        }

        if (resetPreviousState)
        {
            _camera.PreviousStateIsValid =
                false;
        }
    }

    private void OnDestroy()
    {
        KillTweens();

        if (_originTarget != null)
        {
            Destroy(
                _originTarget.gameObject
            );
        }

        if (Instance == this)
        {
            _instance = null;
        }
    }
}
