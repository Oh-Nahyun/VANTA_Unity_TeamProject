using System.Collections.Generic;
using UnityEngine;

public class SkillDashPointCreator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _dashPointPrefab;
    [SerializeField] private Transform _boss;
    [SerializeField] private Transform _player;

    [Header("대쉬포인트 설정")]
    [SerializeField] private float _spawnRadius = 5f;

    [Tooltip("포인트 사이의 최소 거리")]
    [SerializeField] private float _minPointDistance = 2f;

    [Tooltip("랜덤 후보를 몇 개 만들어 비교할지")]
    [SerializeField] private int _candidateCount = 30;

    private readonly List<GameObject> _createdPoints = new();

    public void CreateDashPoint(int count)
    {
        if (count <= 0)
            return;

        ClearDashPoints();

        List<Vector3> selectedPositions = new();

        // 1. 첫 번째 포인트
        // Player -> Boss 방향으로 Boss를 넘어간 위치
        Vector3 firstPosition = GetOppositePosition();

        CreatePoint(firstPosition);
        selectedPositions.Add(firstPosition);

        // 2. 나머지 포인트
        for (int i = 1; i < count; i++)
        {
            Vector3 position = FindBestPosition(selectedPositions);

            CreatePoint(position);
            selectedPositions.Add(position);
        }
    }

    /// <summary>
    /// 플레이어 기준으로 보스를 관통한 반대편 위치
    /// </summary>
    private Vector3 GetOppositePosition()
    {
        Vector2 direction = (Vector2)(_boss.position - _player.position);

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.right;

        direction.Normalize();

        return _boss.position + (Vector3)(direction * _spawnRadius);
    }

    /// <summary>
    /// 랜덤 후보들 중 기존 포인트들과 가장 멀리 떨어진 후보 선택
    /// </summary>
    private Vector3 FindBestPosition(List<Vector3> selectedPositions)
    {
        Vector3 bestPosition = Vector3.zero;
        float bestMinDistance = -1f;

        for (int i = 0; i < _candidateCount; i++)
        {
            Vector3 candidate = GetRandomPosition();

            float minDistance = GetMinDistance(
                candidate,
                selectedPositions
            );

            // 최소 거리 조건을 만족하면서
            // 기존 포인트들과 가장 멀리 떨어진 후보 선택
            if (minDistance >= _minPointDistance &&
                minDistance > bestMinDistance)
            {
                bestMinDistance = minDistance;
                bestPosition = candidate;
            }
        }

        // 모든 후보가 최소 거리 조건을 만족하지 못했다면
        // 그냥 가장 멀었던 후보를 다시 탐색
        if (bestMinDistance < 0f)
        {
            bestPosition = GetFarthestCandidate(selectedPositions);
        }

        return bestPosition;
    }

    private Vector3 GetFarthestCandidate(
        List<Vector3> selectedPositions)
    {
        Vector3 bestPosition = Vector3.zero;
        float bestMinDistance = -1f;

        for (int i = 0; i < _candidateCount; i++)
        {
            Vector3 candidate = GetRandomPosition();

            float minDistance = GetMinDistance(
                candidate,
                selectedPositions
            );

            if (minDistance > bestMinDistance)
            {
                bestMinDistance = minDistance;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }

    /// <summary>
    /// 보스 중심 원 위의 랜덤 위치
    /// </summary>
    private Vector3 GetRandomPosition()
    {
        float angle = Random.Range(0f, 360f);

        float rad = angle * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        );

        return _boss.position + (Vector3)(direction * _spawnRadius);
    }

    /// <summary>
    /// 후보와 기존 포인트들 사이에서 가장 가까운 거리
    /// </summary>
    private float GetMinDistance(
        Vector3 candidate,
        List<Vector3> positions)
    {
        float minDistance = float.MaxValue;

        foreach (Vector3 position in positions)
        {
            float distance = Vector2.Distance(candidate, position);

            if (distance < minDistance)
                minDistance = distance;
        }

        return minDistance;
    }

    private void CreatePoint(Vector3 position)
    {
        GameObject point = Instantiate(
            _dashPointPrefab,
            position,
            Quaternion.identity
        );

        _createdPoints.Add(point);
    }

    public void ClearDashPoints()
    {
        foreach (GameObject point in _createdPoints)
        {
            if (point != null)
                Destroy(point);
        }

        _createdPoints.Clear();
    }
}
