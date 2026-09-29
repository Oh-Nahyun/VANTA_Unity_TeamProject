using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WeaknessThreadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FullScreenPassRendererFeature _threadFullScreenFeature;
    [SerializeField] private FullScreenPassRendererFeature _rayFullScreenFeature;
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

    private Material _originalThreadMaterial;
    private Material _originalRayMaterial;

    private Material _runtimeThreadMaterial;
    private Material _runtimeRayMaterial;

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

        InitializeMaterials();
    }


    private void InitializeMaterials()
    {
        // =========================
        // Thread Material
        // =========================

        if (_threadFullScreenFeature != null &&
            _threadFullScreenFeature.passMaterial != null)
        {
            _originalThreadMaterial =
                _threadFullScreenFeature.passMaterial;

            _runtimeThreadMaterial =
                new Material(_originalThreadMaterial);

            _runtimeThreadMaterial.name =
                $"{_originalThreadMaterial.name} (Runtime)";

            _threadFullScreenFeature.passMaterial =
                _runtimeThreadMaterial;

            _runtimeThreadMaterial.SetFloat(
                ThreadOpacityID,
                0f
            );

            _runtimeThreadMaterial.SetFloat(
                ThreadGlowIntensityID,
                _normalGlowIntensity
            );
        }
        else
        {
            Debug.LogError(
                "Thread Full Screen Pass 또는 Material이 없습니다."
            );
        }


        // =========================
        // Ray Material
        // =========================

        if (_rayFullScreenFeature != null &&
            _rayFullScreenFeature.passMaterial != null)
        {
            _originalRayMaterial =
                _rayFullScreenFeature.passMaterial;

            _runtimeRayMaterial =
                new Material(_originalRayMaterial);

            _runtimeRayMaterial.name =
                $"{_originalRayMaterial.name} (Runtime)";

            _rayFullScreenFeature.passMaterial =
                _runtimeRayMaterial;

            _runtimeRayMaterial.SetFloat(
                RayOpacityID,
                0f
            );
        }
        else
        {
            Debug.LogError(
                "Ray Full Screen Pass 또는 Material이 없습니다."
            );
        }
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
        if (_runtimeThreadMaterial == null ||
            _runtimeRayMaterial == null)
        {
            return;
        }

        _fadeTween?.Kill();
        _rayFadeTween?.Kill();
        _glowSequence?.Kill();

        float randomAngle =
            Random.Range(0f, 180f);

        float radian =
            randomAngle * Mathf.Deg2Rad;

        _threadDirection = new Vector2(
            Mathf.Cos(radian),
            Mathf.Sin(radian)
        ).normalized;

        _runtimeThreadMaterial.SetFloat(
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
        if (_boss == null ||
            _targetCamera == null ||
            _runtimeThreadMaterial == null)
        {
            return;
        }

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

        _runtimeThreadMaterial.SetVector(
            ThreadCenterID,
            center
        );

        _runtimeThreadMaterial.SetVector(
            ThreadNormalID,
            normal
        );
    }


    private void FadeInThread()
    {
        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _runtimeThreadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );


        _fadeTween = _runtimeThreadMaterial
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
            _runtimeThreadMaterial
                .DOFloat(
                    _spawnGlowIntensity,
                    ThreadGlowIntensityID,
                    _glowFlashInDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _glowSequence.Append(
            _runtimeThreadMaterial
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
        if (_runtimeThreadMaterial == null)
            return;

        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _fadeTween = _runtimeThreadMaterial
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

                _runtimeThreadMaterial.SetFloat(
                    ThreadGlowIntensityID,
                    _normalGlowIntensity
                );
            });
    }


    private void FadeInRay()
    {
        if (_runtimeRayMaterial == null)
            return;

        _rayFadeTween?.Kill();

        _runtimeRayMaterial.SetFloat(
            RayOpacityID,
            0f
        );

        _rayFadeTween = _runtimeRayMaterial
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
        if (_runtimeRayMaterial == null)
            return;

        _rayFadeTween?.Kill();

        _rayFadeTween = _runtimeRayMaterial
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


        // =========================
        // 원본 Material 복구
        // =========================

        if (_threadFullScreenFeature != null &&
            _originalThreadMaterial != null)
        {
            _threadFullScreenFeature.passMaterial =
                _originalThreadMaterial;
        }

        if (_rayFullScreenFeature != null &&
            _originalRayMaterial != null)
        {
            _rayFullScreenFeature.passMaterial =
                _originalRayMaterial;
        }


        // =========================
        // Runtime Material 제거
        // =========================

        if (_runtimeThreadMaterial != null)
        {
            Destroy(_runtimeThreadMaterial);
            _runtimeThreadMaterial = null;
        }

        if (_runtimeRayMaterial != null)
        {
            Destroy(_runtimeRayMaterial);
            _runtimeRayMaterial = null;
        }
    }
}
