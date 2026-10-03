using System;
using DG.Tweening;
using UnityEngine;

public class SkillFocusScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _targetCamera;

    [Header("스크린 설정")]
    [SerializeField] private float _normalRadius = 1.5f;
    [SerializeField] private float _skillRadius = 0.3f;

    [SerializeField] private float _focusInDuration = 0.25f;
    [SerializeField] private float _focusOutDuration = 0.2f;

    private static readonly int RadiusID =
        Shader.PropertyToID("_Radius");

    private static readonly int CenterID =
        Shader.PropertyToID("_Center");

    private static readonly int HiddenActiveID =
        Shader.PropertyToID("_HiddenActive");

    private Tween _radiusTween;
    private Tween _hiddenTween;

    private bool _isSkillActive;

    private float _currentRadius;
    private float _currentHiddenActive;

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        _currentRadius = _normalRadius;
        _currentHiddenActive = 0f;

        Shader.SetGlobalFloat(
            RadiusID,
            _normalRadius
        );

        Shader.SetGlobalVector(
            CenterID,
            Vector4.zero
        );

        Shader.SetGlobalFloat(
            HiddenActiveID,
            0f
        );
    }

    private void LateUpdate()
    {
        if (!_isSkillActive)
            return;

        UpdateFocusPosition();
    }

    private void UpdateFocusPosition()
    {
        if (_player == null || _targetCamera == null)
            return;

        Vector3 viewportPos =
            _targetCamera.WorldToViewportPoint(
                _player.position
            );

        Shader.SetGlobalVector(
            CenterID,
            new Vector4(
                viewportPos.x,
                viewportPos.y,
                0f,
                0f
            )
        );
        
   
    }

    public void StartFocusScreenSetting(Action onComplete)
    {
        _isSkillActive = true;

        UpdateFocusPosition();

        _radiusTween?.Kill();
        _hiddenTween?.Kill();

        _radiusTween = DOTween.To(
                () => _currentRadius,
                value =>
                {
                    _currentRadius = value;

                    Shader.SetGlobalFloat(
                        RadiusID,
                        value
                    );
                },
                _skillRadius,
                _focusInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });

        _hiddenTween = DOTween.To(
                () => _currentHiddenActive,
                value =>
                {
                    _currentHiddenActive = value;

                    Shader.SetGlobalFloat(
                        HiddenActiveID,
                        value
                    );
                },
                1f,
                _focusInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void EndFocusScreenSetting()
    {
        _isSkillActive = false;

        _radiusTween?.Kill();
        _hiddenTween?.Kill();

        _radiusTween = DOTween.To(
                () => _currentRadius,
                value =>
                {
                    _currentRadius = value;

                    Shader.SetGlobalFloat(
                        RadiusID,
                        value
                    );
                },
                _normalRadius,
                _focusOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true);

        _hiddenTween = DOTween.To(
                () => _currentHiddenActive,
                value =>
                {
                    _currentHiddenActive = value;

                    Shader.SetGlobalFloat(
                        HiddenActiveID,
                        value
                    );
                },
                0f,
                _focusOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _radiusTween?.Kill();
        _hiddenTween?.Kill();

        _currentRadius = _normalRadius;
        _currentHiddenActive = 0f;

        Shader.SetGlobalFloat(
            RadiusID,
            _normalRadius
        );

        Shader.SetGlobalVector(
            CenterID,
            Vector4.zero
        );

        Shader.SetGlobalFloat(
            HiddenActiveID,
            0f
        );
    }
}
