using UnityEngine;

public class EdgeColliderGenerator : MonoBehaviour
{
    [Header("가로길이")][SerializeField] private float _width = 10f;
    [Header("세로길이")][SerializeField] private float _height = 6f;
    [Header("세그 먼트 수")][SerializeField] private int _segments = 32;

    // 맵 타원의 월드 중심과 월드 크기(가로·세로 전체 길이). 맵 범위가 필요한 다른 스크립트(보스 광역기 등)가 읽는다.
    public Vector2 Center => transform.position;
    public Vector2 Size => new Vector2(_width * transform.lossyScale.x, _height * transform.lossyScale.y);

    private void Awake()
    {
        EdgeCollider2D edge = GetComponent<EdgeCollider2D>();

        Vector2[] points = new Vector2[_segments + 1];

        for (int i = 0; i <= _segments; i++)
        {
            float angle = (float)i / _segments * Mathf.PI * 2f;

            points[i] = new Vector2(
                Mathf.Cos(angle) * _width / 2f,
                Mathf.Sin(angle) * _height / 2f
            );
        }

        edge.points = points;
    }
}
