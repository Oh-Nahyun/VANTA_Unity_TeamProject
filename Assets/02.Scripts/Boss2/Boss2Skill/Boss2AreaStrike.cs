using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광역기가 맵에 뿌리는 공격 더미. 공격 애니메이션을 재생하고,
/// 판정 프레임에서 반경 안의 Player 에게 직접 데미지를 준 뒤 사라진다.
/// </summary>
public class Boss2AreaStrike : MonoBehaviour
{
    [Tooltip("판정 반경 (월드 단위, 프리팹 스케일과 무관)")]
    [SerializeField] private float _radius = 1f;

    [Tooltip("더미 하나의 데미지. Player.TakeDamage 가 아직 데미지 값을 받지 않아 지금은 보관만 한다")]
    [SerializeField] private int _damage = 1;

    [Tooltip("애니메이션의 Finish 이벤트가 누락돼도 이 시간이 지나면 파괴한다")]
    [SerializeField] private float _maxLifeTime = 3f;

    private bool _hasStruck;

    public int Damage => _damage;

    private void Start()
    {
        Destroy(gameObject, _maxLifeTime);
    }

    // 애니메이션 이벤트: 폭발이 가장 큰 프레임
    public void Strike()
    {
        if (_hasStruck)
            return;

        _hasStruck = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        // Player 에 콜라이더가 여러 개여도 한 번만 맞도록
        HashSet<Player> damaged = new HashSet<Player>();

        foreach (Collider2D hit in hits)
        {
            Player player = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.GetComponent<Player>()
                : hit.GetComponent<Player>();

            if (player != null && damaged.Add(player))
            {
                player.TakeDamage();
            }
        }
    }

    // 애니메이션 이벤트: 마지막 프레임
    public void Finish()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
