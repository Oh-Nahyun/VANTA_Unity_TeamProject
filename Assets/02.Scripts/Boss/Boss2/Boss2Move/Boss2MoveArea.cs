using UnityEngine;

/// <summary>
/// 보스2 가 서 있을 수 있는 영역. 순간이동(즉사기·회전베기)할 자리가 맵 밖이거나 벽과 겹치면 다른 자리를 고른다.
/// 영역은 다음 순서로 정한다.
///   1. 인스펙터의 _bounds
///   2. 씬의 EdgeColliderGenerator 가 만든 맵 벽 (Backgrounds 프리팹의 타원형 EdgeCollider2D)
///   3. 씬의 CameraMoveBound (벽이 없는 테스트 씬용)
/// </summary>
public class Boss2MoveArea : MonoBehaviour
{
    [Tooltip("보스가 있을 수 있는 맵 범위. 비워 두면 맵 벽(EdgeColliderGenerator) → CameraMoveBound 순으로 찾아 쓴다")]
    [SerializeField] private Collider2D _bounds;

    [Tooltip("맵 벽이 없을 때 찾을 범위 오브젝트 이름")]
    [SerializeField] private string _fallbackBoundsName = "CameraMoveBound";

    [Tooltip("보스가 겹치면 안 되는 추가 장애물 레이어. 비워 두면 장애물 검사를 하지 않는다")]
    [SerializeField] private LayerMask _blockLayers = 0;

    [Tooltip("원래 거리에서 막히면 이 비율까지 거리를 줄여 가며 같은 방향을 다시 시도한다")]
    [Range(0f, 1f)]
    [SerializeField] private float _minDistanceRatio = 0.5f;

    [Tooltip("줄여도 막히면 이 비율까지 거리를 늘려 가며 다시 시도한다. 맵 구석처럼 보스 그림이 큰데 자리가 좁을 때 필요하다")]
    [Min(1f)]
    [SerializeField] private float _maxDistanceRatio = 2.5f;

    // 대상 기준으로 시도할 방향. 위에서 나타나는 연출을 우선하고, 막히면 아래 → 왼쪽 → 오른쪽,
    // 맵 구석처럼 네 방향이 다 막히면 대각선까지 시도한다.
    private static readonly Vector2[] AppearDirections =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(-1f, 1f).normalized, new Vector2(1f, 1f).normalized,
        new Vector2(-1f, -1f).normalized, new Vector2(1f, -1f).normalized
    };

    // 거리를 줄이거나 늘려 다시 시도하는 횟수 (각각)
    private const int ShrinkSteps = 4;
    private const int ExtendSteps = 6;

    private Collider2D _body;
    // 영역이 닫힌 EdgeCollider2D(맵 벽)일 때의 월드 좌표 꼭짓점. 다각형 안쪽 판정에 쓴다.
    private Vector2[] _areaPolygon;

    private void Awake()
    {
        // 몸통(피격) 콜라이더 크기로 맵 경계·벽과 겹치는지 본다. 공격 박스(자식)는 쓰지 않는다.
        _body = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // 맵 벽은 EdgeColliderGenerator.Awake 에서 점이 만들어지므로 Start 에서 찾는다.
        if (_bounds == null)
        {
            EdgeColliderGenerator wall = FindAnyObjectByType<EdgeColliderGenerator>();
            if (wall != null)
            {
                _bounds = wall.GetComponent<EdgeCollider2D>();
            }
        }

        if (_bounds == null && string.IsNullOrEmpty(_fallbackBoundsName) == false)
        {
            GameObject boundsObject = GameObject.Find(_fallbackBoundsName);
            if (boundsObject != null)
            {
                _bounds = boundsObject.GetComponent<Collider2D>();
            }
        }

        CacheAreaPolygon();
    }

    /// <summary>
    /// target 에서 distance 만큼 떨어진 자리 중 맵 안이면서 벽과 겹치지 않는 첫 자리를 돌려준다.
    /// 위 → 아래 → 왼쪽 → 오른쪽 → 대각선 순으로 시도하고, 각 방향에서 막히면 거리를 줄여 다시 시도한다.
    /// 그래도 막히면 거리를 늘려(최대 _maxDistanceRatio 배) 맵 안쪽으로 비켜 선다.
    /// 모두 막히면 대상 자리(플레이어는 맵 안에 있으므로 항상 갈 수 있는 곳)를 돌려준다.
    /// </summary>
    public Vector2 ResolveAppearPosition(Vector2 target, float distance)
    {
        GetBodyBox(Vector2.zero, out Vector2 bodyOffset, out Vector2 bodySize);
        return ResolveAppearPosition(target, distance, bodyOffset, bodySize);
    }

    /// <summary>
    /// 몸통 대신 "보이는 그림" 박스로 판정하는 버전. 그림이 피벗에서 떨어져 그려지는 공격(회전베기 등)에 쓴다.
    /// visualOffset 은 피벗에서 그림 중심까지, visualSize 는 그림 크기다.
    /// distance 는 대상에서 "그림 중심"까지 거리이며, 돌려주는 값은 보스 피벗 위치다.
    /// </summary>
    public Vector2 ResolveAppearPosition(Vector2 target, float distance, Vector2 visualOffset, Vector2 visualSize)
    {
        // 1차: 대상 가까이에 나타나도록 원래 거리 → 줄인 거리 순으로 모든 방향을 본다.
        foreach (Vector2 direction in AppearDirections)
        {
            for (int step = 0; step <= ShrinkSteps; step++)
            {
                float ratio = Mathf.Lerp(1f, _minDistanceRatio, (float)step / ShrinkSteps);
                if (TryCandidate(target, direction, distance * ratio, visualSize, out Vector2 visualCenter))
                    return visualCenter - visualOffset;
            }
        }

        // 2차: 맵 구석처럼 가까운 자리가 다 막히면 거리를 늘려 맵 안쪽으로 비켜 선다.
        foreach (Vector2 direction in AppearDirections)
        {
            for (int step = 1; step <= ExtendSteps; step++)
            {
                float ratio = Mathf.Lerp(1f, _maxDistanceRatio, (float)step / ExtendSteps);
                if (TryCandidate(target, direction, distance * ratio, visualSize, out Vector2 visualCenter))
                    return visualCenter - visualOffset;
            }
        }

        // 모두 막히면 그림 중심을 대상 자리에 둔다. (플레이어는 맵 안에 있으므로 항상 갈 수 있는 곳)
        return target - visualOffset;
    }

    private bool TryCandidate(Vector2 target, Vector2 direction, float distance, Vector2 visualSize, out Vector2 visualCenter)
    {
        visualCenter = target + direction * distance;
        return IsValidBox(visualCenter, visualSize);
    }

    // 보스 피벗이 position 에 있을 때 몸통이 맵 안에 다 들어오고 장애물과 겹치지 않는지
    public bool IsValidPosition(Vector2 position)
    {
        GetBodyBox(position, out Vector2 center, out Vector2 size);
        return IsValidBox(center, size);
    }

    // center·size 박스가 맵 안에 다 들어오고 장애물과 겹치지 않는지
    public bool IsValidBox(Vector2 center, Vector2 size)
    {
        Vector2 half = size * 0.5f;

        if (_bounds != null)
        {
            // 몸통 박스의 네 모서리가 모두 영역 안이어야 한다. (맵 벽 타원은 볼록하므로 모서리만 보면 충분)
            if (IsInsideArea(center + new Vector2(-half.x, -half.y)) == false
                || IsInsideArea(center + new Vector2(half.x, -half.y)) == false
                || IsInsideArea(center + new Vector2(-half.x, half.y)) == false
                || IsInsideArea(center + new Vector2(half.x, half.y)) == false)
            {
                return false;
            }
        }

        if (_blockLayers != 0 && Physics2D.OverlapBox(center, size, 0f, _blockLayers) != null)
        {
            return false;
        }

        return true;
    }

    // 점 하나가 영역 안인지. 영역이 없으면 어디든 안으로 본다. (잔상처럼 몸통 크기를 따지지 않는 연출용)
    public bool IsInsideArea(Vector2 point)
    {
        if (_bounds == null)
            return true;

        if (_areaPolygon != null)
            return IsInsidePolygon(point, _areaPolygon);

        Bounds area = _bounds.bounds;
        return point.x >= area.min.x && point.x <= area.max.x
            && point.y >= area.min.y && point.y <= area.max.y;
    }

    // 맵 벽(EdgeCollider2D)의 점을 월드 좌표로 옮겨 둔다. 맵 벽은 움직이지 않는다고 본다.
    private void CacheAreaPolygon()
    {
        _areaPolygon = null;

        if (_bounds is EdgeCollider2D edge && edge.pointCount >= 3)
        {
            Vector2[] localPoints = edge.points;
            _areaPolygon = new Vector2[localPoints.Length];
            for (int i = 0; i < localPoints.Length; i++)
            {
                _areaPolygon[i] = edge.transform.TransformPoint(localPoints[i] + edge.offset);
            }
        }
    }

    // 짝수-홀수 규칙의 다각형 안쪽 판정
    private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
    {
        bool isInside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool crosses = (a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
            if (crosses)
            {
                isInside = !isInside;
            }
        }
        return isInside;
    }

    // 피벗이 position 에 있을 때의 몸통 박스. 몸통 콜라이더가 없으면 점으로 본다.
    private void GetBodyBox(Vector2 position, out Vector2 center, out Vector2 size)
    {
        if (_body == null)
        {
            center = position;
            size = Vector2.zero;
            return;
        }

        Bounds body = _body.bounds;
        center = position + ((Vector2)body.center - (Vector2)transform.position);
        size = body.size;
    }
}
