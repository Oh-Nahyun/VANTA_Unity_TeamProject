using UnityEngine;

// 근거리 공격 회피(QTE) 판정. PlayerAvoid가 caller를 플레이어 위치로 옮기므로
// 보스 루트가 아니라 전용 빈 자식 오브젝트에 붙인다.
public class Boss1MeleeAvoid : MonoBehaviour, IAvoidInteraction
{
    [SerializeField] private Animator _animator; // The Tarnished Widow

    [Header("회피 결과 최대 대기 시간(실시간, PlayerAvoid 2초보다 길게)")]
    [SerializeField] private float _maxWaitTime = 3f;

    private Player _targetPlayer;
    private float _waitStartTime;

    public bool IsWaiting { get; private set; }

    // 시작했으면 true. false면 호출한 쪽에서 그냥 데미지를 준다.
    public bool Begin(Player player, bool isCounter)
    {
        if (IsWaiting)
        {
            return false;
        }

        PlayerAvoid playerAvoid = player.GetComponent<PlayerAvoid>();
        if (playerAvoid == null)
        {
            return false;
        }

        _targetPlayer = player;
        IsWaiting = true;
        _waitStartTime = Time.realtimeSinceStartup;
        _animator.speed = 0f; // 공격 모션 멈춤

        if (isCounter)
        {
            playerAvoid.StartAvoidAttackInteraction(gameObject);
        }
        else
        {
            playerAvoid.StartAvoidInteraction(gameObject);
        }

        return true;
    }

    private void Update()
    {
        if (!IsWaiting)
        {
            return;
        }

        // PlayerAvoid가 timeScale을 0.2로 낮추니까 실시간 기준으로 잰다
        if (Time.realtimeSinceStartup - _waitStartTime >= _maxWaitTime)
        {
            AvoidResultBehavior(false);
        }
    }

    // PlayerAvoid가 입력 성공(true) / 시간 초과(false) 때 호출
    public void AvoidResultBehavior(bool result)
    {
        if (!IsWaiting)
        {
            return;
        }

        Player target = _targetPlayer;
        Resume();

        if (!result && target != null)
        {
            target.TakeDamage();
        }
    }

    // 공격이 중간에 끊길 때(2페이즈 전환, 사망) 모션을 풀어 준다
    public void Cancel()
    {
        if (IsWaiting)
        {
            Resume();
        }
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void Resume()
    {
        IsWaiting = false;
        _targetPlayer = null;
        _animator.speed = 1f;
    }
}
