using DG.Tweening;


using UnityEngine;
using UnityEngine.Rendering.Universal;
public class WeaknessMainLineControl : MonoBehaviour
{
    [SerializeField] private FullScreenPassRendererFeature _threadFullScreenFeature;
    [Header("Thread")]
    [SerializeField] private float _threadWidth = 0.008f;



    [Header("Thread Fade")]
    [SerializeField] private float _fadeInDuration = 0.25f;
    [SerializeField] private float _fadeOutDuration = 0.25f;
    
    [Header("Thread Spawn Flash")]
    [SerializeField] private float _normalGlowIntensity = 1.5f;
    [SerializeField] private float _spawnGlowIntensity = 6f;
    [SerializeField] private float _glowFlashInDuration = 0.08f;
    [SerializeField] private float _glowFlashOutDuration = 0.4f;
    
    [Header("Thread Broken")]
    [SerializeField] private float _brokenMultiplier = 50f;
    [SerializeField] private float _brokenDuration = 0.2f;
    
    private Material _originalThreadMaterial;
    private Material _runtimeThreadMaterial;


    
    private static readonly int CurveFrequencyID =
        Shader.PropertyToID("_CurveFrequency");

    private static readonly int CurveStrengthID =
        Shader.PropertyToID("_CurveStrength");

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

    private Tween _fadeTween;
    private Sequence _glowSequence;
    private Sequence _brokenSequence;
    
    private void Awake()
    {
        InitializeMaterials();
    }

    
    private void InitializeMaterials()
    {
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

            _runtimeThreadMaterial.SetFloat(
                ThreadWidthID,
                _threadWidth
            );
        }
        else
        {
            Debug.LogError(
                "Thread Full Screen Pass 또는 Material이 없습니다."
            );
        }
    }

    public void UpdateMainThreadDirection(Vector2 center,Vector2 normal)
    {
        _runtimeThreadMaterial.SetVector(
            ThreadCenterID,
            center
        );

        _runtimeThreadMaterial.SetVector(
            ThreadNormalID,
            normal
        );
    }
    public void FadeInThread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _runtimeThreadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );
        
        _runtimeThreadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _fadeTween =
            _runtimeThreadMaterial
                .DOFloat(
                    1f,
                    ThreadOpacityID,
                    _fadeInDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

        _glowSequence =
            DOTween.Sequence()
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
    
    
    public void FadeOutThread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _fadeTween?.Kill();
        _glowSequence?.Kill();

        _fadeTween =
            _runtimeThreadMaterial
                .DOFloat(
                    0f,
                    ThreadOpacityID,
                    _fadeOutDuration
                )
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _runtimeThreadMaterial.SetFloat(
                        ThreadGlowIntensityID,
                        _normalGlowIntensity
                    );

                    _runtimeThreadMaterial.SetFloat(
                        ThreadWidthID,
                        _threadWidth
                    );
                });
    }

    public void StartThreadDirectionSet()
    {    
        
    }
    public void Breakthread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _fadeTween?.Kill();
        _glowSequence?.Kill();
        _brokenSequence?.Kill();

        float originalFrequency =
            _runtimeThreadMaterial.GetFloat(
                CurveFrequencyID
            );

        float originalStrength =
            _runtimeThreadMaterial.GetFloat(
                CurveStrengthID
            );

        _brokenSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        _brokenSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    originalFrequency * _brokenMultiplier,
                    CurveFrequencyID,
                    _brokenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _brokenSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    originalStrength * _brokenMultiplier,
                    CurveStrengthID,
                    _brokenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _brokenSequence.OnComplete(() =>
        {
            _runtimeThreadMaterial.SetFloat(
                CurveFrequencyID,
                originalFrequency
            );

            _runtimeThreadMaterial.SetFloat(
                CurveStrengthID,
                originalStrength
            );

            _brokenSequence = null;
            
            FadeOutThread();
        });
    }
    
    private void OnDestroy()
    {
        _fadeTween?.Kill();
        _glowSequence?.Kill();
        _brokenSequence?.Kill();
        
        if (_threadFullScreenFeature != null &&
            _originalThreadMaterial != null)
        {
            _threadFullScreenFeature.passMaterial =
                _originalThreadMaterial;
        }
        if (_runtimeThreadMaterial != null)
        {
            Destroy(
                _runtimeThreadMaterial
            );

            _runtimeThreadMaterial = null;
        }
    }
}
