using System;
using UnityEngine;
using Random = UnityEngine.Random;


public class ObjectRandomSpawnSet : MonoBehaviour
{
    [SerializeField] private float _randomRadius = 1f;

    private void OnEnable()
    {
        Vector2 randomOffset =
            Random.insideUnitCircle * _randomRadius;

        transform.position +=
            new Vector3(randomOffset.x, randomOffset.y, 0f);
    }
}
