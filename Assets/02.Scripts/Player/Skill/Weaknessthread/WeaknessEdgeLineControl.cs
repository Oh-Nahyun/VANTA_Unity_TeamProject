using System;
using DG.Tweening;


using UnityEngine;
using UnityEngine.Rendering.Universal;
public class WeaknessEdgeLineControl : MonoBehaviour
{    
    
    [SerializeField] private FullScreenPassRendererFeature _rayFullScreenFeature;

    [Header("Ray Fade")]
    [SerializeField] private float _rayMaxOpacity = 1f;
    [SerializeField] private float _rayFadeInDuration = 0.4f;
    [SerializeField] private float _rayFadeOutDuration = 0.3f;
    
   
    private Material _originalRayMaterial;

    private Material _runtimeRayMaterial;
    
    private static readonly int RayOpacityID =
        Shader.PropertyToID("_EdgeOpacity");
    
    private Tween _rayFadeTween;


    private void Awake()
    {
        InitializeMaterials();
    }

    
    private void InitializeMaterials()
    {
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
    public void FadeInRay()
    {
        if (_runtimeRayMaterial == null)
            return;

        _rayFadeTween?.Kill();

        _runtimeRayMaterial.SetFloat(
            RayOpacityID,
            0f
        );

        _rayFadeTween =
            _runtimeRayMaterial
                .DOFloat(
                    _rayMaxOpacity,
                    RayOpacityID,
                    _rayFadeInDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
    }
    
    public void FadeOutRay()
    {
        if (_runtimeRayMaterial == null)
            return;

        _rayFadeTween?.Kill();

        _rayFadeTween =
            _runtimeRayMaterial
                .DOFloat(
                    0f,
                    RayOpacityID,
                    _rayFadeOutDuration
                )
                .SetEase(Ease.InCubic)
                .SetUpdate(true);
    }
    
    
    public void BreakRay()
    {
        _rayFadeTween?.Kill();
        FadeOutRay();
    }
    
    
    private void OnDestroy()
    {
        _rayFadeTween?.Kill();
        if (_rayFullScreenFeature != null &&
            _originalRayMaterial != null)
        {
            _rayFullScreenFeature.passMaterial =
                _originalRayMaterial;
        }
        if (_runtimeRayMaterial != null)
        {
            Destroy(
                _runtimeRayMaterial
            );

            _runtimeRayMaterial = null;
        }
    }
}
