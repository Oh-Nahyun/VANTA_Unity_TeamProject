using System;
using UnityEngine;

public class Pet : MonoBehaviour
{
    [Header("추적할 플레이어 클래스")]
    [SerializeField] private Player _player;

    [Header("펫 이동 속도")]
    [SerializeField] private float _moveSpeed = 3f;

    private void Update()
    {
        Vector2 direction = _player.transform.position - transform.position;
        direction.Normalize();
        transform.Translate((Vector3)(direction * _moveSpeed) * Time.deltaTime);
    }
}
