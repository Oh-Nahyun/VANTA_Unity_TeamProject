using UnityEngine;

// 근거리 공격 회피(QTE) 판정. PlayerAvoid가 caller를 플레이어 위치로 옮기므로
// 보스 루트가 아니라 전용 빈 자식 오브젝트에 붙인다.
public class Boss1MeleeAvoid : MonoBehaviour, IAvoidInteraction
{
    [SerializeField] private Animator _animator; // The Tarnished Widow

    [Header("회피 결과 최대 대기 시간(실시간, PlayerAvoid 2초보다 길게)")]
    [SerializeField] private float _maxWaitTime = 3f;

    [Header("회피 발동 확률 (0~1)")]
    [SerializeField] private float _avoidChance = 0.3f;

    [Header("플레이어 피격 무적 시간 (Player._hitCoolTime과 같게)")]
    [SerializeField] private float _playerHitCoolTime = 2f;

    private Player _targetPlayer;
    private float _waitStartTime;
    private float _lastDamageTime = -999f;

    public bool IsWaiting { get; private set; }

    // 히트박스가 플레이어에 닿았을 때 호출. QTE를 띄우거나, 그냥 데미지를 준다.
    public void HandleHit(Player player, bool isCounter)
    {
        if (IsWaiting)
        {
            return;
        }

        // 원래도 데미지가 안 들어갔을 상황이면 아무것도 안 함
        if (!CanTakeDamage(player))
        {
            return;
        }

        PlayerAvoid playerAvoid = player.GetComponent<PlayerAvoid>();

        if (playerAvoid == null || Random.value >= _avoidChance)
        {
            DealDamage(player);
            return;
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
    }

    private bool CanTakeDamage(Player player)
    {
        // 보스1 근거리가 준 데미지 기준 쿨타임 (Player 쿨타임도 실시간이라 맞춤)
        if (Time.realtimeSinceStartup - _lastDamageTime < _playerHitCoolTime)
        {
            return false;
        }

        SkillManager skillManager = player.GetComponentInChildren<SkillManager>();
        if (skillManager != null && skillManager.IsSkillUse)
        {
            return false;
        }

        return true;
    }

    private void DealDamage(Player player)
    {
        _lastDamageTime = Time.realtimeSinceStartup;
        player.TakeDamage();
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
            DealDamage(target);
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
