using UnityEngine;
using DG.Tweening;

public class SkillScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material _focusMaterial;
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _targetCamera;

    [Header("스크린 설정")]
    [SerializeField] private float _normalRadius = 1.5f;
    [SerializeField] private float _skillRadius = 0.3f;

    [SerializeField] private float _focusInDuration = 0.25f;
    [SerializeField] private float _focusOutDuration = 0.2f;

    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int CenterID = Shader.PropertyToID("_Center");

    private Tween _radiusTween;
    private bool _isSkillActive;

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;
    }

    private void Start()
    {
        _focusMaterial.SetFloat(RadiusID, _normalRadius);
    }

    private void LateUpdate()
    {
        if (!_isSkillActive)
            return;

        UpdateFocusPosition();
    }

    private void UpdateFocusPosition()
    {
        Vector3 viewportPos =
            _targetCamera.WorldToViewportPoint(_player.position);

        _focusMaterial.SetVector(
            CenterID,
            new Vector4(viewportPos.x, viewportPos.y, 0f, 0f)
        );
    }

    public void SkillScreenSettingStart()
    {
        _isSkillActive = true;

        // 스킬을 누른 그 프레임부터 위치 적용
        UpdateFocusPosition();

        _radiusTween?.Kill();

        _radiusTween = _focusMaterial
            .DOFloat(_skillRadius, RadiusID, _focusInDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void SkillScreenSettingEnd()
    {
        _isSkillActive = false;

        _radiusTween?.Kill();

        _radiusTween = _focusMaterial
            .DOFloat(_normalRadius, RadiusID, _focusOutDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _radiusTween?.Kill();
    }
}
