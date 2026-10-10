using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스2 공격 예고 인디케이터를 만든다. Boss2 루트에 붙이고, 공격·이동 패턴이 예고 시간 동안 띄운다.
///   - ShowLine  : 검기 경로·돌진 경로 같은 직선 (시작점 → 방향 · 길이 · 두께)
///   - ShowCircle: 제자리 회전참 같은 원형 범위 (광역기와 같은 그림)
/// 인디케이터는 보스와 상관없는 루트에 만들어 보스가 움직여도 그 자리에 남는다.
/// 공격이 끊기면 Clear 로 이 보스가 띄운 것을 모두 지운다.
/// </summary>
public class Boss2IndicatorSpawner : MonoBehaviour
{
    [Header("직선")]
    [Tooltip("겉 테두리 (9-slice, 피벗 = 왼쪽 가운데)")]
    [SerializeField] private Sprite _lineBorderSprite;
    [Tooltip("안쪽 채움 (9-slice, 피벗 = 왼쪽 가운데)")]
    [SerializeField] private Sprite _lineFillSprite;
    [Tooltip("채움 두께 = 두께 × 이 값 (테두리 안쪽에 들어가게)")]
    [SerializeField, Range(0.3f, 1f)] private float _lineFillWidthRatio = 0.8f;
    [Tooltip("직선 채움 색. 텍스처가 이미 붉어서 밝게 둔다 (원 채움은 광역기와 같은 _fillColor)")]
    [SerializeField] private Color _lineFillColor = new Color(1f, 1f, 1f, 0.7f);

    [Header("원")]
    [SerializeField] private Sprite _circleBorderSprite;
    [SerializeField] private Sprite _circleFillSprite;

    [Header("공통")]
    [Tooltip("테두리 색. 너무 선명하지 않게 알파 절반")]
    [SerializeField] private Color _borderColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color _fillColor = new Color(0.46f, 0f, 0f, 0.53f);
    [Tooltip("광역기 인디케이터와 같은 정렬 레이어")]
    [SerializeField] private string _sortingLayer = "Zone";
    [Tooltip("조명 영향 없이 그대로 보이도록 광역기 인디케이터와 같은 Sprite-Unlit-Default")]
    [SerializeField] private Material _material;

    private readonly List<Boss2Indicator> _active = new List<Boss2Indicator>();

    /// <summary>origin 에서 direction 으로 length 만큼 뻗는 직선 인디케이터. duration 동안 채움이 끝까지 차오른다.</summary>
    public Boss2Indicator ShowLine(Vector2 origin, Vector2 direction, float length, float width, float duration)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        GameObject root = new GameObject("Boss2LineIndicator");
        root.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        SpriteRenderer border = CreateRenderer(root.transform, "Border", _lineBorderSprite, _borderColor, 1);
        border.drawMode = SpriteDrawMode.Sliced;
        border.size = new Vector2(length, width);

        SpriteRenderer fill = CreateRenderer(root.transform, "Fill", _lineFillSprite, _lineFillColor, 0);
        fill.drawMode = SpriteDrawMode.Sliced;

        Boss2Indicator indicator = root.AddComponent<Boss2Indicator>();
        indicator.InitLine(fill, length, width * _lineFillWidthRatio, duration);
        Track(indicator);
        return indicator;
    }

    /// <summary>center 를 중심으로 반경 radius 의 원형 인디케이터. duration 동안 채움이 가운데부터 커진다.</summary>
    public Boss2Indicator ShowCircle(Vector2 center, float radius, float duration)
    {
        GameObject root = new GameObject("Boss2CircleIndicator");
        root.transform.position = center;

        SpriteRenderer border = CreateRenderer(root.transform, "Border", _circleBorderSprite, _borderColor, 1);
        float borderScale = radius * 2f / GetVisibleDiameter(_circleBorderSprite);
        border.transform.localScale = new Vector3(borderScale, borderScale, 1f);

        SpriteRenderer fill = CreateRenderer(root.transform, "Fill", _circleFillSprite, _fillColor, 0);
        float fillScale = radius * 2f / GetVisibleDiameter(_circleFillSprite);

        Boss2Indicator indicator = root.AddComponent<Boss2Indicator>();
        indicator.InitCircle(fill, fillScale, duration);
        Track(indicator);
        return indicator;
    }

    /// <summary>이 보스가 띄운 인디케이터를 모두 지운다. (공격 취소·얼음 등)</summary>
    public void Clear()
    {
        foreach (Boss2Indicator indicator in _active)
        {
            if (indicator != null)
            {
                indicator.Cancel();
            }
        }
        _active.Clear();
    }

    private void Track(Boss2Indicator indicator)
    {
        _active.RemoveAll(item => item == null);
        _active.Add(indicator);
    }

    private SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        if (_material != null)
        {
            renderer.sharedMaterial = _material;
        }
        renderer.sortingLayerName = _sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    // 스프라이트에서 실제로 보이는 부분의 지름(유닛). 투명 여백은 빠진다. (Boss2AreaStrike 와 같은 방식)
    private static float GetVisibleDiameter(Sprite sprite)
    {
        if (sprite == null)
            return 1f;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        Vector2 size = max - min;
        return Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
    }

    private void OnDisable()
    {
        Clear();
    }
}
