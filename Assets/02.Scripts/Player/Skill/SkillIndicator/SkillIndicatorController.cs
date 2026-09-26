using System.Collections.Generic;
using UnityEngine;

public class SkillIndicatorController : MonoBehaviour
{
    [SerializeField]private Transform _player;
    [SerializeField]private GameObject _skillIndicatorPrefab;

    private readonly List<SkillIndicatorMove> _pets = new();

    public void AddPet()
    {
        Transform target;

        if (_pets.Count == 0)
        {
            target = _player;
        }
        else
        {
            target = _pets[^1].transform;
        }

        Vector3 spawnPosition =
            target.position - target.right * 0.1f;

        GameObject spawned = Instantiate(
            _skillIndicatorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        SkillIndicatorMove indicatorMove =
            spawned.GetComponent<SkillIndicatorMove>();

        indicatorMove.SetTarget(target);

        _pets.Add(indicatorMove);
    }

    public void MoveToPoint(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            _pets[i].MoveToPoint(points[i]);
        }
    }

    public void ClearPet()
    {
        _pets.Clear();
    }
}
