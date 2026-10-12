using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스2 공격 예고 인디케이터를 만든다. Boss2 루트에 붙이고, 공격·이동 패턴이 예고 시간 동안 띄운다.
///   - ShowLine  : 검기 경로·돌진 경로 같은 직선 (시작점 → 방향 · 길이 · 두께)
///   - ShowCircle: 제자리 회전참 같은 원형 범위 (광역기와 같은 그림)
///   - ShowFan   : 콤보 공격 같은 앞쪽 부채꼴 범위 (반경 · 각도)
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

    [Header("부채꼴")]
    [Tooltip("처음부터 보이는 옅은 바탕의 진하기 (채움 색 알파 × 이 값)")]
    [SerializeField, Range(0f, 1f)] private float _fanAreaAlpha = 0.35f;
    [Tooltip("테두리 선 두께")]
    [SerializeField] private float _fanBorderWidth = 0.04f;

    [Header("공통")]
    [Tooltip("테두리 색. 너무 선명하지 않게 알파 절반")]
    [SerializeField] private Color _borderColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color _fillColor = new Color(0.46f, 0f, 0f, 0.53f);
    [Tooltip("광역기 인디케이터와 같은 정렬 레이어")]
    [SerializeField] private string _sortingLayer = "Zone";
    [Tooltip("조명 영향 없이 그대로 보이도록 광역기 인디케이터와 같은 Sprite-Unlit-Default")]
    [SerializeField] private Material _material;

    private readonly List<Boss2Indicator> _active = new List<Boss2Indicator>();
    private Material _fallbackFanMaterial;

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

    /// <summary>
    /// origin 에서 direction 쪽으로 펼쳐지는 부채꼴(반경 radius, 전체 각도 angle 도) 인디케이터.
    /// 옅은 바탕 + 테두리 선은 처음부터 보이고, 채움이 duration 동안 안쪽에서 바깥으로 차오른다.
    /// </summary>
    public Boss2Indicator ShowFan(Vector2 origin, Vector2 direction, float radius, float angle, float duration)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        GameObject root = new GameObject("Boss2FanIndicator");
        root.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        Color area = _fillColor;
        area.a *= _fanAreaAlpha;
        CreateFanMesh(root.transform, "Area", radius, angle, area, 0);
        CreateFanOutline(root.transform, radius, angle);
        MeshRenderer fill = CreateFanMesh(root.transform, "Fill", radius, angle, _fillColor, 1);

        Boss2Indicator indicator = root.AddComponent<Boss2Indicator>();
        indicator.InitFan(fill.transform, duration);
        Track(indicator);
        return indicator;
    }

    // 부채꼴 면: 꼭짓점(0,0) + 호 위의 점들. +x 가 가운데 방향이다.
    private MeshRenderer CreateFanMesh(Transform parent, string name, float radius, float angle, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        int segments = Mathf.Max(2, Mathf.CeilToInt(angle / 5f));
        var vertices = new Vector3[segments + 2];
        var colors = new Color[vertices.Length];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = (-angle * 0.5f + angle * i / segments) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
        }
        for (int i = 0; i < vertices.Length; i++)
        {
            colors[i] = color;
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        var mesh = new Mesh { name = "Boss2Fan" };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = FanMaterial;
        renderer.sortingLayerName = _sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    // 부채꼴 테두리: 꼭짓점 → 호 → 꼭짓점으로 닫힌 선
    private void CreateFanOutline(Transform parent, float radius, float angle)
    {
        GameObject go = new GameObject("Border");
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = _fanBorderWidth;
        line.numCornerVertices = 2;
        line.sharedMaterial = FanMaterial;
        line.startColor = _borderColor;
        line.endColor = _borderColor;
        line.sortingLayerName = _sortingLayer;
        line.sortingOrder = 2;

        int segments = Mathf.Max(2, Mathf.CeilToInt(angle / 5f));
        line.positionCount = segments + 2;
        line.SetPosition(0, Vector3.zero);
        for (int i = 0; i <= segments; i++)
        {
            float a = (-angle * 0.5f + angle * i / segments) * Mathf.Deg2Rad;
            line.SetPosition(i + 1, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }

    // 메시·선은 정점 색을 쓰므로 스프라이트용 언릿 머티리얼을 그대로 쓴다. 비어 있으면 기본 스프라이트 셰이더로 만든다.
    private Material FanMaterial
    {
        get
        {
            if (_material != null)
                return _material;
            if (_fallbackFanMaterial == null)
            {
                _fallbackFanMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            return _fallbackFanMaterial;
        }
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
