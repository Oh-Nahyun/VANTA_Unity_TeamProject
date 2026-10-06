using UnityEngine;

public class ObjectShadowController : MonoBehaviour
{
    [Header("원본")]
    [SerializeField] private SpriteRenderer _targetRenderer;

    [Header("그림자")]
    [SerializeField] private SpriteRenderer _shadowRenderer;

    [Header("그림자 설정")]
    [SerializeField] private Vector2 _shadowOffset = new Vector2(0f, -0.1f);

    private Transform _targetTransform;

    private void Awake()
    {
        if (_targetRenderer != null)
        {
            _targetTransform = _targetRenderer.transform;
        }
    }

    private void LateUpdate()
    {
        if (_targetRenderer == null || _shadowRenderer == null)
            return;

        UpdateShadow();
    }

    private void UpdateShadow()
    {
        // 현재 애니메이션에서 출력 중인 Sprite 그대로 복사
        _shadowRenderer.sprite = _targetRenderer.sprite;

        // 원본 Flip 상태 복사
        _shadowRenderer.flipX = _targetRenderer.flipX;

        // 세로 방향은 반전
        _shadowRenderer.flipY = !_targetRenderer.flipY;

        // 원본 위치 추적
        transform.position =
            _targetTransform.position +
            (Vector3)_shadowOffset;
    }

    public void SetTarget(SpriteRenderer targetRenderer)
    {
        _targetRenderer = targetRenderer;

        if (_targetRenderer != null)
        {
            _targetTransform = _targetRenderer.transform;
        }
    }
}
