using UnityEngine;

public class EdgeColliderGenerator : MonoBehaviour
{
    [Header("가로길이")][SerializeField] private float _width = 10f;
    [Header("세로길이")][SerializeField] private float _height = 6f;
    [Header("세그 먼트 수")][SerializeField] private int _segments = 32;
    
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
