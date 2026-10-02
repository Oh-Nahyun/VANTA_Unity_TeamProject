using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BrokenWidowScreenController : MonoBehaviour
{
    [SerializeField]
    private FullScreenPassRendererFeature _brokenFullScreenFeature;

    [Header("Broken Setting")]
    [SerializeField] private float _crackIntensity = 0.02f;
    [SerializeField] private float _crackDistortion = 0.01f;
    [SerializeField] private float _brokenDuration = 0.2f;

    private Material _originalMaterial;
    private Material _runtimeMaterial;

    private Tween _intensityTween;
    private Tween _distortionTween;

    private static readonly int CrackIntensityID =
        Shader.PropertyToID("_CrackIntensity");

    private static readonly int CrackRotationID =
        Shader.PropertyToID("_CrackRotation");

    private static readonly int CrackDistortionID =
        Shader.PropertyToID("_CrackDistortion");

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

        _intensityTween?.Kill();
        _distortionTween?.Kill();

        // 매번 다른 방향으로 Crack 회전
        float randomRotation =
            Random.Range(0f, 360f);

        _runtimeMaterial.SetFloat(
            CrackRotationID,
            randomRotation
        );

        // 0부터 시작
        _runtimeMaterial.SetFloat(
            CrackIntensityID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackDistortionID,
            0f
        );

        // Crack 표시
        _intensityTween =
            _runtimeMaterial
                .DOFloat(
                    _crackIntensity,
                    CrackIntensityID,
                    _brokenDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

        // 화면 왜곡
        _distortionTween =
            _runtimeMaterial
                .DOFloat(
                    _crackDistortion,
                    CrackDistortionID,
                    _brokenDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
    }

    public void ResetBroken()
    {
        if (_runtimeMaterial == null)
            return;

        _intensityTween?.Kill();
        _distortionTween?.Kill();

        _runtimeMaterial.SetFloat(
            CrackIntensityID,
            0f
        );

        _runtimeMaterial.SetFloat(
            CrackDistortionID,
            0f
        );
    }

    private void OnDestroy()
    {
        _intensityTween?.Kill();
        _distortionTween?.Kill();

        if (_brokenFullScreenFeature != null &&
            _originalMaterial != null)
        {
            _brokenFullScreenFeature.passMaterial =
                _originalMaterial;
        }

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}
