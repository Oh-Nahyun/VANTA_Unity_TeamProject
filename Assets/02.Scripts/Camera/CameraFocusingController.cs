using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFocusingController : MonoBehaviour
{
    private static CameraFocusingController _instance;
    public static CameraFocusingController Instance => _instance;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _camera;

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

    private void InitializeCamera()
    {
        if (_camera == null)
        {
            Debug.LogError(
                "CinemachineCamera가 없습니다."
            );

            return;
        }

        _follow =
            _camera.GetComponent<CinemachineFollow>();

        if (_follow == null)
        {
            _follow =
                _camera.gameObject
                    .AddComponent<CinemachineFollow>();
        }

        _defaultTarget =
            _camera.Follow;

        _defaultOrthographicSize =
            _camera.Lens.OrthographicSize;

        _defaultDamping =
            _follow
                .TrackerSettings
                .PositionDamping;
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

        _isFocusing = true;

        _camera.Follow = target;

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

        _isFocusing = true;

        _camera.Follow = target;

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

        _isFocusing = true;

        _originTarget.position =
            Vector3.zero;

        _camera.Follow =
            _originTarget;

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

        KillTweens();

        _camera.Follow =
            _defaultTarget;

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
