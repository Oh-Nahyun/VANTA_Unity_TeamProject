using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WeaknessMainLineControl : MonoBehaviour
{
    [SerializeField]
    private FullScreenPassRendererFeature _threadFullScreenFeature;

    [Header("Thread")]
    [SerializeField] private float _threadWidth = 0.008f;

    [Header("Thread Fade")]
    [SerializeField] private float _fadeInDuration = 0.25f;
    [SerializeField] private float _fadeOutDuration = 0.25f;

    [Header("Thread Spawn Glow")]
    [SerializeField] private float _normalGlowIntensity = 1.5f;
    [SerializeField] private float _spawnGlowIntensity = 6f;
    [SerializeField] private float _glowFlashInDuration = 0.08f;
    [SerializeField] private float _glowFlashOutDuration = 0.4f;

    [Header("Thread Curve Start")]
    [SerializeField] private float _startCurveFrequency = 100f;
    [SerializeField] private float _normalCurveFrequency = 10f;

    [SerializeField] private float _startCurveStrength = 50f;
    [SerializeField] private float _normalCurveStrength = 0.03f;

    [SerializeField] private float _curveSetDuration = 0.35f;

    [Header("Thread Width Start")]
    [SerializeField] private float _normalCoreWidth = 0.01f;
    [SerializeField] private float _normalGlowWidth = 0.03f;

    [SerializeField] private float _widthStartDelay = 0.05f;
    [SerializeField] private float _widthGrowDuration = 0.45f;

    [Header("Thread Random")]
    [SerializeField] private Vector2 _strengthRange =
        new Vector2(0f, 0.5f);

    [SerializeField] private Vector2 _coreWidthRange =
        new Vector2(0.005f, 0.01f);

    [SerializeField] private Vector2 _glowWidthRange =
        new Vector2(0.01f, 0.03f);

    [SerializeField] private float _randomDuration = 0.5f;

    [Header("Thread Broken")]
    [SerializeField] private float _brokenMultiplier = 50f;
    [SerializeField] private float _brokenDuration = 0.2f;

    private Material _originalThreadMaterial;
    private Material _runtimeThreadMaterial;

    private Sequence _threadSequence;
    private Sequence _brokenSequence;

    private bool _isThreadRunning;

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

    private static readonly int ThreadCoreWidthID =
        Shader.PropertyToID("_ThreadCoreWidth");

    private static readonly int ThreadGlowWidthID =
        Shader.PropertyToID("_ThreadGlowWidth");

    private void Awake()
    {
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (_threadFullScreenFeature == null ||
            _threadFullScreenFeature.passMaterial == null)
        {
            Debug.LogError(
                "Thread Full Screen Pass 또는 Material이 없습니다."
            );

            return;
        }

        _originalThreadMaterial =
            _threadFullScreenFeature.passMaterial;

        _runtimeThreadMaterial =
            new Material(_originalThreadMaterial);

        _runtimeThreadMaterial.name =
            $"{_originalThreadMaterial.name} (Runtime)";

        _threadFullScreenFeature.passMaterial =
            _runtimeThreadMaterial;

        ResetThread();
    }

    public void UpdateMainThreadDirection(
        Vector2 center,
        Vector2 normal)
    {
        if (_runtimeThreadMaterial == null)
            return;

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

        KillSequences();

        _isThreadRunning = true;

        _runtimeThreadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _runtimeThreadMaterial.SetFloat(
            CurveFrequencyID,
            _startCurveFrequency
        );

        _runtimeThreadMaterial.SetFloat(
            CurveStrengthID,
            _startCurveStrength
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadCoreWidthID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowWidthID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );

        _threadSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        // =========================
        // Fade In
        // =========================

        _threadSequence.Insert(
            0f,
            _runtimeThreadMaterial
                .DOFloat(
                    1f,
                    ThreadOpacityID,
                    _fadeInDuration
                )
                .SetEase(Ease.OutCubic)
        );

        // =========================
        // Frequency
        // 100 -> 10
        // =========================

        _threadSequence.Insert(
            0f,
            _runtimeThreadMaterial
                .DOFloat(
                    _normalCurveFrequency,
                    CurveFrequencyID,
                    _curveSetDuration
                )
                .SetEase(Ease.OutCubic)
        );

        // =========================
        // Strength
        // 50 -> 0.03
        // =========================

        _threadSequence.Insert(
            0f,
            _runtimeThreadMaterial
                .DOFloat(
                    _normalCurveStrength,
                    CurveStrengthID,
                    _curveSetDuration
                )
                .SetEase(Ease.OutCubic)
        );

        // =========================
        // Core Width
        // 살짝 늦게 0 -> 0.01
        // =========================

        _threadSequence.Insert(
            _widthStartDelay,
            _runtimeThreadMaterial
                .DOFloat(
                    _normalCoreWidth,
                    ThreadCoreWidthID,
                    _widthGrowDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // =========================
        // Glow Width
        // 살짝 늦게 0 -> 0.03
        // =========================

        _threadSequence.Insert(
            _widthStartDelay,
            _runtimeThreadMaterial
                .DOFloat(
                    _normalGlowWidth,
                    ThreadGlowWidthID,
                    _widthGrowDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // =========================
        // Spawn Glow
        // =========================

        _threadSequence.Insert(
            0f,
            _runtimeThreadMaterial
                .DOFloat(
                    _spawnGlowIntensity,
                    ThreadGlowIntensityID,
                    _glowFlashInDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _threadSequence.Insert(
            _glowFlashInDuration,
            _runtimeThreadMaterial
                .DOFloat(
                    _normalGlowIntensity,
                    ThreadGlowIntensityID,
                    _glowFlashOutDuration
                )
                .SetEase(Ease.OutExpo)
        );

        // =========================
        // 등장 완료
        // =========================

        _threadSequence.OnComplete(() =>
        {
            _threadSequence = null;

            if (_isThreadRunning)
            {
                StartRandomAnimation();
            }
        });
    }

    private void StartRandomAnimation()
    {
        if (!_isThreadRunning ||
            _runtimeThreadMaterial == null)
        {
            return;
        }

        _threadSequence?.Kill();

        float targetStrength =
            Random.Range(
                _strengthRange.x,
                _strengthRange.y
            );

        float targetCoreWidth =
            Random.Range(
                _coreWidthRange.x,
                _coreWidthRange.y
            );

        float targetGlowWidth =
            Random.Range(
                _glowWidthRange.x,
                _glowWidthRange.y
            );

        _threadSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        // 현재 Strength -> 새로운 랜덤 Strength
        _threadSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    targetStrength,
                    CurveStrengthID,
                    _randomDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // 현재 CoreWidth -> 새로운 랜덤 CoreWidth
        _threadSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    targetCoreWidth,
                    ThreadCoreWidthID,
                    _randomDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // 현재 GlowWidth -> 새로운 랜덤 GlowWidth
        _threadSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    targetGlowWidth,
                    ThreadGlowWidthID,
                    _randomDuration
                )
                .SetEase(Ease.InOutSine)
        );

        _threadSequence.OnComplete(() =>
        {
            _threadSequence = null;

            if (_isThreadRunning)
            {
                StartRandomAnimation();
            }
        });
    }

    public void FadeOutThread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _isThreadRunning = false;

        KillSequences();

        _threadSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        _threadSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    0f,
                    ThreadOpacityID,
                    _fadeOutDuration
                )
                .SetEase(Ease.InCubic)
        );

        _threadSequence.OnComplete(() =>
        {
            _threadSequence = null;

            ResetThread();
        });
    }

    public void Breakthread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _isThreadRunning = false;

        KillSequences();

        float currentFrequency =
            _runtimeThreadMaterial.GetFloat(
                CurveFrequencyID
            );

        float currentStrength =
            _runtimeThreadMaterial.GetFloat(
                CurveStrengthID
            );

        _brokenSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        _brokenSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    currentFrequency *
                    _brokenMultiplier,
                    CurveFrequencyID,
                    _brokenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _brokenSequence.Join(
            _runtimeThreadMaterial
                .DOFloat(
                    currentStrength *
                    _brokenMultiplier,
                    CurveStrengthID,
                    _brokenDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _brokenSequence.OnComplete(() =>
        {
            _brokenSequence = null;

            FadeOutThread();
        });
    }

    public void StartThreadDirectionSet()
    {
    }

    private void ResetThread()
    {
        if (_runtimeThreadMaterial == null)
            return;

        _runtimeThreadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowIntensityID,
            _normalGlowIntensity
        );

        _runtimeThreadMaterial.SetFloat(
            CurveFrequencyID,
            _normalCurveFrequency
        );

        _runtimeThreadMaterial.SetFloat(
            CurveStrengthID,
            _normalCurveStrength
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadCoreWidthID,
            0f
        );

        _runtimeThreadMaterial.SetFloat(
            ThreadGlowWidthID,
            0f
        );
    }

    private void KillSequences()
    {
        _threadSequence?.Kill();
        _brokenSequence?.Kill();

        _threadSequence = null;
        _brokenSequence = null;
    }

    private void OnDestroy()
    {
        _isThreadRunning = false;

        KillSequences();

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
