

using System;
using DG.Tweening;
using UnityEngine;

public class SkillDashScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material _focusMaterial;
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _targetCamera;
   
    [Header("Dash Focus")]
    [SerializeField] private float _dashWidth = 0.08f;
    [SerializeField] private float _focusInDuration = 0.5f;
    [SerializeField] private float _focusOutDuration = 0.5f;

    private static readonly int FocusModeID =
        Shader.PropertyToID("_FocusMode");

    private static readonly int DashCenterID =
        Shader.PropertyToID("_DashCenter");

    private static readonly int DashNormalID =
        Shader.PropertyToID("_DashNormal");

    private static readonly int DashWidthID =
        Shader.PropertyToID("_DashWidth");

    private Tween _widthTween;
    
    public void StartDashScreenSetting(Vector3 dashDirection)
    {
        Vector3 playerScreen =
            _targetCamera.WorldToViewportPoint(_player.position);

        Vector2 center = new Vector2(
            playerScreen.x,
            playerScreen.y
        );

        Vector3 endScreen =
            _targetCamera.WorldToViewportPoint(
                _player.position + dashDirection
            );

        Vector2 screenDirection = new Vector2(
            endScreen.x - playerScreen.x,
            endScreen.y - playerScreen.y
        ).normalized;

        Vector2 normal = new Vector2(
            -screenDirection.y,
            screenDirection.x
        );

        _focusMaterial.SetFloat(FocusModeID, 1f);
        _focusMaterial.SetVector(DashCenterID, center);
        _focusMaterial.SetVector(DashNormalID, normal);

        _widthTween?.Kill();

        _focusMaterial.SetFloat(DashWidthID, 0f);

        _widthTween = _focusMaterial
            .DOFloat(
                _dashWidth,
                DashWidthID,
                _focusInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }


    public void EndDashScreenSetting()
    {
        _widthTween?.Kill();

        _widthTween = _focusMaterial
            .DOFloat(
                0f,
                DashWidthID,
                _focusOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _focusMaterial.SetFloat(FocusModeID, 0f);
            });
    }
}

