using System.Collections.Generic;
using UnityEngine;

public class SkillDashPointCreator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _boss;
    [SerializeField] private Transform _player;

    [Header("대쉬포인트 설정")]
    [SerializeField] private float _spawnRadius = 5f;

    /// <summary>
    /// 플레이어 반대편부터 보스 중심 원 위에 동일한 각도 간격으로 배치합니다.
    /// </summary>
    public List<Vector3> CreateDashPoint(int count)
    {
        List<Vector3> positions = new();

        if (count <= 0)
            return positions;

        if (_boss == null)
        {
            HitableObject hitableObject = FindAnyObjectByType<HitableObject>();
            _boss = hitableObject.transform;
        }
        Vector3 center = _boss.position;
        Vector2 direction = (Vector2)(center - _player.position);

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.right;

        float startAngle = Mathf.Atan2(direction.y, direction.x);
        float angleStep = Mathf.PI * 2f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f
            ) * _spawnRadius;

            positions.Add(center + offset);
        }

        return positions;
    }
}
