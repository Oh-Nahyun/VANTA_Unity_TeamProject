using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스2 공격의 회피(QTE) 판정 담당.
/// PlayerAvoid 는 보스 GameObject 에서 GetComponent&lt;IAvoidInteraction&gt;() 로 첫 구현체 하나만 찾으므로,
/// IAvoidInteraction 은 Boss2 에서 이 컴포넌트만 구현해야 한다. 스킬·공격 패턴은 이걸 호출만 한다.
/// </summary>
public class Boss2AvoidJudge : MonoBehaviour, IAvoidInteraction
{
    [Tooltip("회피 결과를 기다리는 최대 실시간(초). PlayerAvoid 의 입력 제한시간(2초)보다 길게")]
    [SerializeField] private float _maxWaitTime = 3f;

    private Animator _animator;
    private float _waitStartTime;
    private readonly List<GameObject> _participants = new List<GameObject>();

    // 마지막 Begin 에서 회피 입력을 띄운 플레이어들. 실패 시 데미지를 줄 대상을 공격 쪽에서 알 수 있게 한다.
    public IReadOnlyList<GameObject> Participants => _participants;

    // 회피 결과를 기다리며 보스 모션을 멈춘 상태인지
    public bool IsWaiting { get; private set; }

    // 이번 공격에서 회피에 성공했는지. 공격 시작 때 ResetResult 로 지운다.
    public bool LastResult { get; private set; }

    /// <summary>
    /// 같은 GameObject 의 판정 컴포넌트를 찾고, 프리팹에 빠져 있으면 붙여서 돌려준다.
    /// </summary>
    public static Boss2AvoidJudge GetOrAdd(GameObject owner)
    {
        Boss2AvoidJudge judge = owner.GetComponent<Boss2AvoidJudge>();
        if (judge == null)
        {
            judge = owner.AddComponent<Boss2AvoidJudge>();
        }
        return judge;
    }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void Update()
    {
        if (IsWaiting == false)
            return;

        // 결과가 끝내 오지 않으면(PlayerAvoid 이상 등) 실패로 보고 진행한다.
        // PlayerAvoid 가 timeScale 을 낮추므로 실시간 기준으로 잰다.
        if (Time.realtimeSinceStartup - _waitStartTime >= _maxWaitTime)
        {
            LastResult = false;
            Resume();
            PlayFailSpark();
        }
    }

    /// <summary>
    /// center 기준 radius 안의 플레이어에게 회피 입력(QTE)을 시작하고 보스 모션을 멈춘다.
    /// radius 가 0 이하이면 거리와 상관없이 모든 플레이어가 대상이다.
    /// 대상이 없으면 기다리지 않고 false 를 돌려준다.
    /// avoidType 에 따라 플레이어 쪽 회피 연출(피하기 / 반격)이 달라진다.
    /// </summary>
    public bool Begin(Vector2 center, float radius, Boss2AvoidType avoidType)
    {
        _participants.Clear();
        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerAvoid playerAvoid = playerObject.GetComponent<PlayerAvoid>();
            // Player 태그만 있고 PlayerAvoid 가 없는 오브젝트(테스트 더미 등)는 건너뛴다.
            if (playerAvoid == null)
                continue;

            if (radius > 0f && Vector2.Distance(center, playerObject.transform.position) > radius)
                continue;

            switch (avoidType)
            {
                case Boss2AvoidType.Dodge:
                    playerAvoid.StartAvoidInteraction(gameObject);
                    break;
                case Boss2AvoidType.Counter:
                    playerAvoid.StartAvoidAttackInteraction(gameObject);
                    break;
            }
            _participants.Add(playerObject);
        }

        if (_participants.Count == 0)
            return false;

        IsWaiting = true;
        _waitStartTime = Time.realtimeSinceStartup;
        if (_animator != null) _animator.speed = 0f;
        return true;
    }

    public void ResetResult()
    {
        LastResult = false;
        _participants.Clear();
    }

    /// <summary>
    /// 공격이 중간에 끊길 때 호출한다. 멈춘 모션을 풀어 보스가 정지한 채로 남지 않게 한다.
    /// </summary>
    public void Cancel()
    {
        if (IsWaiting == false)
            return;

        Resume();
    }

    // IAvoidInteraction: PlayerAvoid 가 입력 성공(true) / 시간 초과(false) 때 호출한다.
    public void AvoidResultBehavior(bool result)
    {
        // 기다리는 중이 아닐 때 온 결과(디버그 키 등)는 무시한다.
        if (IsWaiting == false)
            return;

        LastResult = result;
        Resume();
        if (result == false)
        {
            PlayFailSpark();
        }
    }

    // 회피에 실패하면 멈췄던 공격이 그대로 이어지며 검 쪽에서 불꽃이 튄다.
    private void PlayFailSpark()
    {
        Boss2BladeSpark spark = GetComponent<Boss2BladeSpark>();
        if (spark != null)
        {
            spark.Play();
        }
    }

    private void Resume()
    {
        IsWaiting = false;
        if (_animator != null) _animator.speed = 1f;
    }
}
