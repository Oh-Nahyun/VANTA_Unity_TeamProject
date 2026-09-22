using UnityEngine;
using DG.Tweening;

public class SkillScreenController : MonoBehaviour
{
    [Header("Focus Screen Material")]
    [SerializeField] private Material _focusMaterial;

    [SerializeField] private float normalRadius = 1.5f;
    [SerializeField] private float skillRadius = 0.3f;
    
    [Header("포커싱 시간")]
    [SerializeField] private float focusInDuration = 0.25f;
    [SerializeField] private float focusOutDuration = 0.2f;

    private static readonly int RadiusID = Shader.PropertyToID("_Radius");

    private Tween _radiusTween;


    private void Start()
    {
        _focusMaterial.SetFloat(RadiusID, normalRadius);

    }
    public void SkillScreenSettingStart()
    {
        _radiusTween?.Kill();

        _radiusTween = _focusMaterial
            .DOFloat(skillRadius, RadiusID, focusInDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void SkillScreenSettingEnd()
    {
        _radiusTween?.Kill();

        _radiusTween = _focusMaterial
            .DOFloat(normalRadius, RadiusID, focusOutDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
    }


}
