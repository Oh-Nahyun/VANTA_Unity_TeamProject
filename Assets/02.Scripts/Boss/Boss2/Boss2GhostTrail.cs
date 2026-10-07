using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스가 움직이는 동안 지금 그림을 일정 간격으로 복사해 흐려지는 잔상(고스트 트레일)을 남긴다.
/// 다른 보스 스크립트에 의존하지 않으므로 SpriteRenderer 가 있는 캐릭터에 붙이기만 하면 된다.
///
/// 켜는 경로는 세 가지이고, 하나라도 켜져 있으면 잔상을 남긴다.
///   1. 코드 / 애니메이션 이벤트: SetTrail(bool), TrailOn(), TrailOff()
///   2. 시간 지정: PlayFor(초)
///   3. FSM: Boss2Controller 가 행동(Boss2Action.GhostTrail)의 이동·공격 동안 SetFsmTrail 로 켠다
/// 컴포넌트 체크박스는 전체 스위치다. 끄면 위 요청과 상관없이 잔상을 남기지 않고, 남아 있던 잔상도 지운다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Boss2GhostTrail : MonoBehaviour
{
    [Tooltip("잔상을 남기는 간격(초)")]
    [SerializeField] private float _spawnInterval = 0.04f;

    [Tooltip("마지막 잔상 이후 이 거리 이상 움직였을 때만 남긴다. 제자리에 잔상이 겹쳐 쌓이지 않게 한다")]
    [SerializeField] private float _minMoveDistance = 0.05f;

    [Tooltip("잔상 하나가 완전히 사라지는 데 걸리는 시간(초)")]
    [SerializeField] private float _lifeTime = 0.3f;

    [Tooltip("잔상의 시작 색. 알파가 시작 투명도다")]
    [SerializeField] private Color _color = new Color(1f, 0f, 0f, 0.5f);

    [Tooltip("미리 만들어 두는 잔상 수. 모자라면 가장 오래된 잔상을 다시 쓴다")]
    [SerializeField] private int _poolSize = 20;

    [Tooltip("잔상 머티리얼. 비워 두면 기본 스프라이트 머티리얼.\n광역기 어둠 연출 중에도 보이게 하려면 Unlit 머티리얼을 넣는다")]
    [SerializeField] private Material _ghostTrailMaterial;

    [Tooltip("보스 그림의 알파가 이 값보다 낮으면(Dissolve 로 사라진 중 등) 잔상을 남기지 않는다")]
    [SerializeField] private float _minSourceAlpha = 0.05f;

    private struct Ghost
    {
        public SpriteRenderer Renderer;
        public float BornTime;
    }

    private SpriteRenderer _source;
    private Transform _ghostRoot;
    private readonly Queue<SpriteRenderer> _idleGhosts = new Queue<SpriteRenderer>();
    private readonly List<Ghost> _aliveGhosts = new List<Ghost>();

    private bool _manualRequest;
    private bool _fsmRequest;
    private float _timedUntil = float.NegativeInfinity;

    private float _spawnTimer;
    private Vector2 _lastSpawnPosition;

    /// <summary>지금 잔상을 남기는 중인지</summary>
    public bool IsEmitting => enabled && (_manualRequest || _fsmRequest || Time.time < _timedUntil);

    private void Awake()
    {
        _source = GetComponent<SpriteRenderer>();

        // 잔상은 보스를 따라 움직이면 안 되므로 씬 루트에 따로 둔다.
        _ghostRoot = new GameObject($"{name}_GhostTrail").transform;
        for (int i = 0; i < Mathf.Max(_poolSize, 1); i++)
        {
            _idleGhosts.Enqueue(CreateGhost(i));
        }
    }

    private void OnDisable()
    {
        // 전체 스위치를 끄면 남은 잔상이 멈춘 채로 보이지 않게 바로 지운다.
        for (int i = _aliveGhosts.Count - 1; i >= 0; i--)
        {
            Release(i);
        }
    }

    private void OnDestroy()
    {
        if (_ghostRoot != null)
        {
            Destroy(_ghostRoot.gameObject);
        }
    }

    private void Update()
    {
        UpdateSpawn();
        UpdateFade();
    }

    // ---- 켜고 끄기 ----

    /// <summary>코드에서 켜고 끈다. FSM·시간 지정 요청과는 따로 기억한다.</summary>
    public void SetTrail(bool on)
    {
        if (on && IsEmitting == false)
        {
            ResetSpawn();
        }
        _manualRequest = on;
    }

    /// <summary>seconds 동안만 켰다가 저절로 끈다.</summary>
    public void PlayFor(float seconds)
    {
        if (IsEmitting == false)
        {
            ResetSpawn();
        }
        _timedUntil = Mathf.Max(_timedUntil, Time.time + seconds);
    }

    /// <summary>Boss2Controller 전용. 행동의 이동·공격 동안 켠다.</summary>
    public void SetFsmTrail(bool on)
    {
        if (on && IsEmitting == false)
        {
            ResetSpawn();
        }
        _fsmRequest = on;
    }

    // 애니메이션 이벤트용
    public void TrailOn() => SetTrail(true);
    public void TrailOff() => SetTrail(false);

    [ContextMenu("Test/Ghost Trail 2s")]
    private void TestPlayFor() => PlayFor(2f);

    // 켜는 순간 바로 하나를 남기고, 그 자리부터 이동 거리를 잰다.
    private void ResetSpawn()
    {
        _spawnTimer = _spawnInterval;
        _lastSpawnPosition = new Vector2(float.MaxValue, float.MaxValue);
    }

    // ---- 생성 ----

    private void UpdateSpawn()
    {
        if (IsEmitting == false)
            return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < _spawnInterval)
            return;

        Vector2 position = transform.position;
        if ((position - _lastSpawnPosition).sqrMagnitude < _minMoveDistance * _minMoveDistance)
            return;

        if (_source.sprite == null || _source.enabled == false || _source.color.a < _minSourceAlpha)
            return;

        _spawnTimer = 0f;
        _lastSpawnPosition = position;
        Spawn();
    }

    private void Spawn()
    {
        // 풀이 비었으면 가장 오래된 잔상을 다시 쓴다.
        if (_idleGhosts.Count == 0)
        {
            Release(0);
        }

        SpriteRenderer ghost = _idleGhosts.Dequeue();
        Transform ghostTransform = ghost.transform;
        ghostTransform.SetPositionAndRotation(transform.position, transform.rotation);
        ghostTransform.localScale = transform.lossyScale;

        ghost.sprite = _source.sprite;
        ghost.flipX = _source.flipX;
        ghost.flipY = _source.flipY;
        ghost.sortingLayerID = _source.sortingLayerID;
        ghost.sortingOrder = _source.sortingOrder - 1;
        ghost.color = _color;
        ghost.gameObject.SetActive(true);

        _aliveGhosts.Add(new Ghost { Renderer = ghost, BornTime = Time.time });
    }

    // ---- 페이드 ----

    private void UpdateFade()
    {
        // 앞쪽이 오래된 잔상이라 뒤에서부터 지워도 순서가 유지된다.
        for (int i = _aliveGhosts.Count - 1; i >= 0; i--)
        {
            Ghost ghost = _aliveGhosts[i];
            float t = _lifeTime > 0f ? (Time.time - ghost.BornTime) / _lifeTime : 1f;
            if (t >= 1f)
            {
                Release(i);
                continue;
            }

            Color color = _color;
            color.a = _color.a * (1f - t);
            ghost.Renderer.color = color;
        }
    }

    private void Release(int aliveIndex)
    {
        SpriteRenderer ghost = _aliveGhosts[aliveIndex].Renderer;
        _aliveGhosts.RemoveAt(aliveIndex);

        if (ghost == null)
            return;

        ghost.gameObject.SetActive(false);
        _idleGhosts.Enqueue(ghost);
    }

    private SpriteRenderer CreateGhost(int index)
    {
        GameObject ghostObject = new GameObject($"Ghost_{index}");
        ghostObject.transform.SetParent(_ghostRoot, false);
        ghostObject.SetActive(false);

        // 보스 머티리얼(핏빛·Dissolve 셰이더)은 잔상에 맞지 않아 기본 또는 지정 머티리얼을 쓴다.
        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        if (_ghostTrailMaterial != null)
        {
            ghostRenderer.sharedMaterial = _ghostTrailMaterial;
        }
        return ghostRenderer;
    }
}
