using System;
using DG.Tweening;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectZoom : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CinemachineCamera _camera;

    [Header("줌")]
    [SerializeField] private float _zoomInDuration = 0.3f;
    [SerializeField] private float _zoomOutDuration = 0.3f;

    [Header("줌 중 카메라 추적")]
    [SerializeField] private float _zoomDamping = 0.5f;
    [SerializeField] private float _dampingChangeDuration = 0.2f;

    private CinemachineFollow _follow;

    private float _defaultOrthographicSize;
    private Vector3 _defaultDamping;

    private Tween _zoomTween;
    private Tween _dampingTween;

    private void Awake()
    {
        
        _follow = _camera.GetComponent<CinemachineFollow>();
        if (_follow == null)
        {
            _follow=_camera.AddComponent<CinemachineFollow>();
        }

    }

    public void OnEnable()
    {
        _defaultOrthographicSize = _camera.Lens.OrthographicSize;
        _defaultDamping = _follow.TrackerSettings.PositionDamping;
    }

    public void ZoomIn(float zoomAmount)
    {
        _zoomTween?.Kill();
        _dampingTween?.Kill();

        float targetSize = _defaultOrthographicSize - zoomAmount;

        _zoomTween = DOTween.To(
                () => _camera.Lens.OrthographicSize,
                value => _camera.Lens.OrthographicSize = value,
                targetSize,
                _zoomInDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        Vector3 fastDamping = new Vector3(
            _zoomDamping,
            _zoomDamping,
            _defaultDamping.z
        );

        _dampingTween = DOTween.To(
                () => _follow.TrackerSettings.PositionDamping,
                value => _follow.TrackerSettings.PositionDamping = value,
                fastDamping,
                _dampingChangeDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void ZoomOut()
    {
        _zoomTween?.Kill();
        _dampingTween?.Kill();

        // 원래 줌으로
        _zoomTween = DOTween.To(
                () => _camera.Lens.OrthographicSize,
                value => _camera.Lens.OrthographicSize = value,
                _defaultOrthographicSize,
                _zoomOutDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // 원래 Damping으로
        _dampingTween = DOTween.To(
                () => _follow.TrackerSettings.PositionDamping,
                value => _follow.TrackerSettings.PositionDamping = value,
                _defaultDamping,
                _dampingChangeDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _zoomTween?.Kill();
        _dampingTween?.Kill();
    }
}
