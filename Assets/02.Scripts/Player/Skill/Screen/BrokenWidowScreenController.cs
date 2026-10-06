using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BrokenWidowScreenController : MonoBehaviour
{
    [SerializeField]
    private FullScreenPassRendererFeature _brokenFullScreenFeature;

    [SerializeField]
    private Material _hurtMaterial;

    [Header("Broken Setting")]
    [SerializeField] private float _crackIntensity = 0.02f;
    [SerializeField] private float _crackDistortion = 0.01f;
    [SerializeField] private float _crackGlowIntensity = 0.6f;
    [SerializeField] private float _brokenDuration = 0.2f;

    [Header("Death Broken Setting")]
    [SerializeField] private float _deathCrackIntensity = 0.05f;
    [SerializeField] private float _deathCrackDistortion = 0.03f;
    [SerializeField] private float _deathCrackGlowIntensity = 1.2f;
    [SerializeField] private float _deathBrokenDuration = 0.5f;

    private Material _originalMaterial;
    private Material _runtimeMaterial;

    private Sequence _brokenSequence;

    private static readonly int CrackIntensityID =
        Shader.PropertyToID("_CrackIntensity");

    private static readonly int CrackRotationID =
        Shader.PropertyToID("_CrackRotation");

    private static readonly int CrackDistortionID =
        Shader.PropertyToID("_CrackDistortion");

    private static readonly int CrackGlowIntensityID =
        Shader.PropertyToID("_CrackGlowIntensity");

    private static readonly int IsHurtID =
        Shader.PropertyToID("_IsHurt");
    private static readonly int CrackScaleID =
        Shader.PropertyToID("_CrackScale");

    private void Awake()
    {
        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        if (_brokenFullScreenFeature == null ||
            _brokenFullScreenFeature.passMaterial == null)
        {
            Debug.LogError("Broken Full Screen Pass 또는 Material이 없습니다.");
            return;
        }

        _originalMaterial =
            _brokenFullScreenFeature.passMaterial;

        _runtimeMaterial =
            new Material(_originalMaterial);

        _runtimeMaterial.name =
            $"{_originalMaterial.name} (Runtime)";

        _brokenFullScreenFeature.passMaterial =
            _runtimeMaterial;

        ResetBroken();
    }

    public void ApplyBroken()
    {
        if (_runtimeMaterial == null)
            return;

        _brokenSequence?.Kill();

        if (_hurtMaterial != null)
        {
            _hurtMaterial.SetFloat(
                IsHurtID,
                1f
            );
        }

        float randomRotation =
            Random.Range(0f, 360f);

        _runtimeMaterial.SetFloat(
            CrackRotationID,
            randomRotation
        );

        _runtimeMaterial.SetFloat(
            CrackIntensityID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackDistortionID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackGlowIntensityID,
            0f
        );

        _brokenSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _crackIntensity,
                    CrackIntensityID,
                    _brokenDuration
                )
                .SetEase(Ease.OutCubic)
        );

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _crackDistortion,
                    CrackDistortionID,
                    _brokenDuration
                )
                .SetEase(Ease.OutCubic)
        );

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _crackGlowIntensity,
                    CrackGlowIntensityID,
                    _brokenDuration
                )
                .SetEase(Ease.OutCubic)
        );
    }

    public void ApplyDeathBroken()
    {
        if (_runtimeMaterial == null)
            return;

        _brokenSequence?.Kill();

        if (_hurtMaterial != null)
        {
            _hurtMaterial.SetFloat(
                IsHurtID,
                1f
            );
        }

        float randomRotation =
            Random.Range(0f, 360f);

        _runtimeMaterial.SetFloat(
            CrackRotationID,
            randomRotation
        );

        _runtimeMaterial.SetFloat(
            CrackScaleID,
            0.7f
        );

        _runtimeMaterial.SetFloat(
            CrackIntensityID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackDistortionID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackGlowIntensityID,
            0f
        );

        _brokenSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _deathCrackIntensity,
                    CrackIntensityID,
                    _deathBrokenDuration
                )
                .SetEase(Ease.OutCubic)
        );

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _deathCrackDistortion,
                    CrackDistortionID,
                    _deathBrokenDuration
                )
                .SetEase(Ease.OutCubic)
        );

        _brokenSequence.Join(
            _runtimeMaterial
                .DOFloat(
                    _deathCrackGlowIntensity,
                    CrackGlowIntensityID,
                    _deathBrokenDuration
                )
                .SetEase(Ease.OutCubic)
        );
    }

    public void ResetBroken()
    {
        if (_runtimeMaterial == null)
            return;

        _brokenSequence?.Kill();
        _brokenSequence = null;

        _runtimeMaterial.SetFloat(
            CrackIntensityID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackDistortionID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackGlowIntensityID,
            0f
        );

        if (_hurtMaterial != null)
        {
            _hurtMaterial.SetFloat(
                IsHurtID,
                0f
            );
        }
    }

    private void OnDestroy()
    {
        _brokenSequence?.Kill();

        if (_hurtMaterial != null)
        {
            _hurtMaterial.SetFloat(
                IsHurtID,
                0f
            );
        }

        if (_brokenFullScreenFeature != null &&
            _originalMaterial != null)
        {
            _brokenFullScreenFeature.passMaterial =
                _originalMaterial;
        }

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }
}
