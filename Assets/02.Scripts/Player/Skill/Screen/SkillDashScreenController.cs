using DG.Tweening;
using UnityEngine;

public class SkillDashScreenController : MonoBehaviour
{
    [Header("References")]
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

    private float _currentWidth;

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        // Global Shader 값 초기화
        _currentWidth = 0f;

        Shader.SetGlobalFloat(
            FocusModeID,
            0f
        );

        Shader.SetGlobalFloat(
            DashWidthID,
            0f
        );

        Shader.SetGlobalVector(
            DashCenterID,
            Vector4.zero
        );

        Shader.SetGlobalVector(
            DashNormalID,
            Vector4.zero
        );
    }

    public void StartDashScreenSetting(Vector3 dashDirection)
    {
        if (_player == null || _targetCamera == null)
            return;

        // 플레이어의 화면상 위치
        Vector3 playerScreen =
            _targetCamera.WorldToViewportPoint(
                _player.position
            );

        Vector2 center = new Vector2(
            playerScreen.x,
            playerScreen.y
        );

        // Dash 방향의 화면상 방향 계산
        Vector3 endScreen =
            _targetCamera.WorldToViewportPoint(
                _player.position + dashDirection
            );

        Vector2 screenDirection = new Vector2(
            endScreen.x - playerScreen.x,
            endScreen.y - playerScreen.y
        );

        if (screenDirection.sqrMagnitude < 0.000001f)
            return;

        screenDirection.Normalize();

        // Dash 방향에 수직인 Normal
        Vector2 normal = new Vector2(
            -screenDirection.y,
            screenDirection.x
        );

        // Dash Focus 활성화
        Shader.SetGlobalFloat(
            FocusModeID,
            1f
        );

        Shader.SetGlobalVector(
            DashCenterID,
            new Vector4(
                center.x,
                center.y,
                0f,
                0f
            )
        );

        Shader.SetGlobalVector(
            DashNormalID,
            new Vector4(
                normal.x,
                normal.y,
                0f,
                0f
            )
        );

        _widthTween?.Kill();

        _currentWidth = 0f;

        Shader.SetGlobalFloat(
            DashWidthID,
            0f
        );

        // 0 → DashWidth
        _widthTween = DOTween.To(
                () => _currentWidth,
                value =>
                {
                    _currentWidth = value;

                    Shader.SetGlobalFloat(
                        DashWidthID,
                        value
                    );
                },
                _dashWidth,
                _focusInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void EndDashScreenSetting()
    {
        _widthTween?.Kill();

        // DashWidth → 0
        _widthTween = DOTween.To(
                () => _currentWidth,
                value =>
                {
                    _currentWidth = value;

                    Shader.SetGlobalFloat(
                        DashWidthID,
                        value
                    );
                },
                0f,
                _focusOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                Shader.SetGlobalFloat(
                    FocusModeID,
                    0f
                );
            });
    }

    private void OnDestroy()
    {
        _widthTween?.Kill();

        // Global 값 정리
        Shader.SetGlobalFloat(
            FocusModeID,
            0f
        );

        Shader.SetGlobalFloat(
            DashWidthID,
            0f
        );

        Shader.SetGlobalVector(
            DashCenterID,
            Vector4.zero
        );

        Shader.SetGlobalVector(
            DashNormalID,
            Vector4.zero
        );
    }
}
