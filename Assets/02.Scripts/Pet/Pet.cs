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
    /// 스프라이트 렌더러
    /// </summary>
    private SpriteRenderer _spriteRenderer;
    /// <summary>
    /// 펫 스폰 시 이동 전 대기 시간 계산용 변수
    /// </summary>
    private float _timer;
    /// <summary>
    /// 펫이 이동하기 전에 필요한 대기 시간
    /// </summary>
    private float _timeBeforeRun;

    /// <summary>
    /// 펫이 추적 가능한 상태인지 확인용 변수
    /// </summary>
    private bool _canChase;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeRun = 1.7f;
        _canChase = true;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_canChase && _timer >= _timeBeforeRun)
        {
            Chase();
        }
    }

    private void Chase()
    {
        Vector2 direction = _player.transform.position - transform.position;
        direction.Normalize();

        if (direction.x < 0)
        {
            _spriteRenderer.flipX = true;
        }
        else
        {
            _spriteRenderer.flipX = false;
        }
        transform.Translate((Vector3)(direction * _moveSpeed) * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        _canChase = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        _canChase = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        _canChase = true;
    }
}
