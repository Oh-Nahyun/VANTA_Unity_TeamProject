using UnityEngine;

/// <summary>
/// 보스2 컨트롤러가 고르는 행동 하나. 이동 패턴이 끝나면(도착하면) 공격 패턴이 이어진다.
/// 조건(거리·체력·쿨다운)을 모두 만족하는 행동 중에서 가중치 비율로 뽑힌다.
/// </summary>
[System.Serializable]
public class Boss2Action
{
    public string Name;

    [Tooltip("None 이면 이동 없이 제자리에서 바로 공격")]
    public Boss2MovePatternType Move;

    [Tooltip("None 이면 이동만 하고 공격하지 않음")]
    public Boss2AttackPatternType Attack;

    [Tooltip("ComboAttack 일 때 타수 범위")]
    public int ComboMin = 1;
    public int ComboMax = 1;

    [Tooltip("BladeAuraSlash 일 때 발사 수 (Single / Triple). None 이면 공격 패턴의 기본값")]
    public SlashProjectileType ProjectileType = SlashProjectileType.None;

    [Header("조건")]
    [Tooltip("플레이어와의 거리가 이 범위일 때만 후보")]
    public float DistanceMin = 0f;
    public float DistanceMax = 999f;

    [Tooltip("보스 체력 비율(0~1)이 이 범위일 때만 후보")]
    [Range(0f, 1f)] public float HealthMin = 0f;
    [Range(0f, 1f)] public float HealthMax = 1f;

    [Tooltip("한 번 쓰고 나서 다시 후보가 되기까지 초")]
    public float Cooldown = 0f;

    [Tooltip("후보끼리의 선택 가중치. 0 이면 쓰지 않음")]
    public float Weight = 1f;
    
    [Header("상태/버프")]
    [Tooltip("이 행동의 이동 + 공격 동안 무적 (Recover 부터 해제)")]
    public bool IsInvincible = false;

    // 런타임 기록 (저장하지 않음)
    [System.NonSerialized] public float LastUsedTime = float.NegativeInfinity;

    public bool IsAvailable(float distance, float healthRatio, float now)
    {
        return Weight > 0f
            && distance >= DistanceMin && distance <= DistanceMax
            && healthRatio >= HealthMin && healthRatio <= HealthMax
            && now - LastUsedTime >= Cooldown;
    }

    public int PickComboCount()
    {
        return Random.Range(ComboMin, Mathf.Max(ComboMin, ComboMax) + 1);
    }
}
