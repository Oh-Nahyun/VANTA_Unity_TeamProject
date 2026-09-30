using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SkillDashSlashScreenController : MonoBehaviour
{
    [SerializeField]
    private FullScreenPassRendererFeature _slashFullScreenFeature;

    [SerializeField]
    private Camera _targetCamera;

    [Header("Slash")]
    [SerializeField]
    private float _slashDuration = 0.3f;

    private Material _originalSlashMaterial;
    private Material _runtimeSlashMaterial;

    private Tween _slashTween;

    private static readonly int SlashProgressID =
        Shader.PropertyToID("_SlashProgress");

    private static readonly int SlashCenterID =
        Shader.PropertyToID("_SlashCenter");

    private static readonly int SlashNormalID =
        Shader.PropertyToID("_SlashNormal");

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        if (_slashFullScreenFeature == null ||
            _slashFullScreenFeature.passMaterial == null)
        {
            Debug.LogError(
                "Slash Full Screen Pass 또는 Material이 없습니다."
            );

            return;
        }

        _originalSlashMaterial =
            _slashFullScreenFeature.passMaterial;

        _runtimeSlashMaterial =
            new Material(_originalSlashMaterial);

        _runtimeSlashMaterial.name =
            $"{_originalSlashMaterial.name} (Runtime)";

        _slashFullScreenFeature.passMaterial =
            _runtimeSlashMaterial;

        _runtimeSlashMaterial.SetFloat(
            SlashProgressID,
            0f
        );
    }

    public void PlaySlash(
        Vector2 worldStart,
        Vector2 worldEnd)
    {
        if (_runtimeSlashMaterial == null ||
            _targetCamera == null)
        {
            return;
        }

        Vector3 startViewport3 =
            _targetCamera.WorldToViewportPoint(
                worldStart
            );

        Vector3 endViewport3 =
            _targetCamera.WorldToViewportPoint(
                worldEnd
            );

        Vector2 startViewport =
            new Vector2(
                startViewport3.x,
                startViewport3.y
            );

        Vector2 endViewport =
            new Vector2(
                endViewport3.x,
                endViewport3.y
            );

        Vector2 direction =
            endViewport - startViewport;

        if (direction.sqrMagnitude <= 0f)
            return;

        direction.Normalize();

        Vector2 normal =
            new Vector2(
                -direction.y,
                direction.x
            );

        Vector2 center =
            (startViewport + endViewport) * 0.5f;

        _runtimeSlashMaterial.SetVector(
            SlashCenterID,
            center
        );

        _runtimeSlashMaterial.SetVector(
            SlashNormalID,
            normal
        );

        _slashTween?.Kill();

        _runtimeSlashMaterial.SetFloat(
            SlashProgressID,
            0f
        );

        _slashTween =
            _runtimeSlashMaterial
                .DOFloat(
                    1f,
                    SlashProgressID,
                    _slashDuration
                )
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _runtimeSlashMaterial.SetFloat(
                        SlashProgressID,
                        0f
                    );

                    _slashTween = null;
                });
    }

    private void OnDestroy()
    {
        _slashTween?.Kill();

        if (_slashFullScreenFeature != null &&
            _originalSlashMaterial != null)
        {
            _slashFullScreenFeature.passMaterial =
                _originalSlashMaterial;
        }

        if (_runtimeSlashMaterial != null)
        {
            Destroy(_runtimeSlashMaterial);

            _runtimeSlashMaterial = null;
        }
    }
}
