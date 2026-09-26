using DG.Tweening;
using UnityEngine;

public class WeaknessThreadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material _threadMaterial;
    [SerializeField] private Material _rayMaterial;
    [SerializeField] private Transform _boss;
    [SerializeField] private Camera _targetCamera;

    [Header("Thread")]
    [SerializeField] private float _threadWidth = 0.008f;

    [Header("Thread Movement")]
    [SerializeField] private float _rotationSpeed = 3f;

    [Header("Thread Fade")]
    [SerializeField] private float _fadeInDuration = 0.25f;
    [SerializeField] private float _fadeOutDuration = 0.25f;

    [Header("Thread Spawn Flash")]
    [SerializeField] private float _normalGlowIntensity = 1.5f;
    [SerializeField] private float _spawnGlowIntensity = 6f;
    [SerializeField] private float _glowFlashInDuration = 0.08f;
    [SerializeField] private float _glowFlashOutDuration = 0.4f;

    [Header("Ray Fade")]
    [SerializeField] private float _rayMaxOpacity = 1f;
    [SerializeField] private float _rayFadeInDuration = 0.4f;
    [SerializeField] private float _rayFadeOutDuration = 0.3f;

    private Vector2 _threadDirection;
    private bool _threadRunning;

    private Tween _fadeTween;
    private Tween _rayFadeTween;
    private Sequence _glowSequence;

    private static readonly int ThreadCenterID =
        Shader.PropertyToID("_ThreadCenter");

    private static readonly int ThreadNormalID =
        Shader.PropertyToID("_ThreadNormal");

    private static readonly int ThreadWidthID =
        Shader.PropertyToID("_ThreadWidth");

    private static readonly int ThreadOpacityID =
        Shader.PropertyToID("_ThreadOpacity");

    private static readonly int ThreadGlowIntensityID =
        Shader.PropertyToID("_ThreadGlowIntensity");

    private static readonly int RayOpacityID =
        Shader.PropertyToID("_EdgeOpacity");

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        _threadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _threadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );

        _rayMaterial.SetFloat(
            RayOpacityID,
            0f
        );
    }

    private void LateUpdate()
    {
        if (!_threadRunning)
            return;

        UpdateThreadDirection();
        UpdateThread();
    }

    public void StartWeaknessThread()
    {
        _fadeTween?.Kill();
        _rayFadeTween?.Kill();
        _glowSequence?.Kill();

        float randomAngle = Random.Range(0f, 180f);
        float radian = randomAngle * Mathf.Deg2Rad;

        _threadDirection = new Vector2(
            Mathf.Cos(radian),
            Mathf.Sin(radian)
        ).normalized;

        _threadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _threadRunning = true;

        UpdateThread();

        FadeInThread();
        FadeInRay();
    }

    public void EndWeaknessThread()
    {
        FadeOutThread();
        FadeOutRay();
    }

    private void UpdateThreadDirection()
    {
        float angle =
            _rotationSpeed * Time.unscaledDeltaTime;

        _threadDirection =
            Quaternion.Euler(0f, 0f, angle) *
            _threadDirection;

        _threadDirection.Normalize();
    }

    private void UpdateThread()
    {
        if (_boss == null || _targetCamera == null)
            return;

        Vector3 bossScreen =
            _targetCamera.WorldToViewportPoint(
                _boss.position
            );

        Vector2 center = new Vector2(
            bossScreen.x,
            bossScreen.y
        );

        Vector3 directionPoint =
            _boss.position +
            new Vector3(
                _threadDirection.x,
                _threadDirection.y,
                0f
            );

        Vector3 directionScreen =
            _targetCamera.WorldToViewportPoint(
                directionPoint
            );

        Vector2 screenDirection = new Vector2(
            directionScreen.x - bossScreen.x,
            directionScreen.y - bossScreen.y
        );

        if (screenDirection.sqrMagnitude < 0.000001f)
            return;

        screenDirection.Normalize();

        Vector2 normal = new Vector2(
            -screenDirection.y,
            screenDirection.x
        );

        _threadMaterial.SetVector(
            ThreadCenterID,
            center
        );

        _threadMaterial.SetVector(
            ThreadNormalID,
            normal
        );
    }

    private void FadeInThread()
    {
        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _threadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _threadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );

        _fadeTween = _threadMaterial
            .DOFloat(
                1f,
                ThreadOpacityID,
                _fadeInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        _glowSequence = DOTween.Sequence()
            .SetUpdate(true);

        _glowSequence.Append(
            _threadMaterial
                .DOFloat(
                    _spawnGlowIntensity,
                    ThreadGlowIntensityID,
                    _glowFlashInDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _glowSequence.Append(
            _threadMaterial
                .DOFloat(
                    _normalGlowIntensity,
                    ThreadGlowIntensityID,
                    _glowFlashOutDuration
                )
                .SetEase(Ease.OutExpo)
        );
    }

    private void FadeOutThread()
    {
        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _fadeTween = _threadMaterial
            .DOFloat(
                0f,
                ThreadOpacityID,
                _fadeOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _threadRunning = false;

                _threadMaterial.SetFloat(
                    ThreadGlowIntensityID,
                    _normalGlowIntensity
                );
            });
    }

    private void FadeInRay()
    {
        _rayFadeTween?.Kill();

        _rayMaterial.SetFloat(
            RayOpacityID,
            0f
        );

        _rayFadeTween = _rayMaterial
            .DOFloat(
                _rayMaxOpacity,
                RayOpacityID,
                _rayFadeInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void FadeOutRay()
    {
        _rayFadeTween?.Kill();

        _rayFadeTween = _rayMaterial
            .DOFloat(
                0f,
                RayOpacityID,
                _rayFadeOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
    }

    public Vector2 GetThreadDirection()
    {
        return _threadDirection;
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        _rayFadeTween?.Kill();
        _glowSequence?.Kill();
    }
}
