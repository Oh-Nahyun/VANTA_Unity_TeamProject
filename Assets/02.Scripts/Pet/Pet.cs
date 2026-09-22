using System;
using System.Timers;
using UnityEngine;

public class Pet : MonoBehaviour
{
    [Header("추적할 플레이어 클래스")]
    [SerializeField] private Player _player;

    [Header("펫 이동 속도")]
    [SerializeField] private float _moveSpeed = 3f;

    /// <summary>
    /// 펫 스폰 시 이동 전 대기 시간 계산용 변수
    /// </summary>
    private float _timer = 0f;

    /// <summary>
    /// 펫이 이동하기 전에 필요한 대기 시간
    /// </summary>
    private float _timeBeforeRun = 1.7f;
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeRun)
        {
            Chase();
        }
    }

    private void Chase()
    {
        Vector2 direction = _player.transform.position - transform.position;
        direction.Normalize();
        transform.Translate((Vector3)(direction * _moveSpeed) * Time.deltaTime);
    }
}
