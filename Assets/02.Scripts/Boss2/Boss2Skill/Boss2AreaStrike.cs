using UnityEngine;

/// <summary>
/// 광역기가 맵에 뿌리는 공격 더미. 공격 애니메이션을 재생하고, 판정 프레임에서 한 번 때린 뒤 사라진다.
/// 실제 판정(범위·데미지·대상 필터)은 생성한 스킬이 넘겨준 콜백이 처리한다.
/// </summary>
public class Boss2AreaStrike : MonoBehaviour
{
    [Tooltip("애니메이션의 Finish 이벤트가 누락돼도 이 시간이 지나면 파괴한다")]
    [SerializeField] private float _maxLifeTime = 3f;

    private System.Action<Vector2> _onStrike;
    private bool _hasStruck;

    /// <summary>
    /// 생성 직후 스킬이 호출한다. onStrike 는 판정 순간에 자기 위치로 한 번 불린다.
    /// </summary>
    public void Init(System.Action<Vector2> onStrike)
    {
        _onStrike = onStrike;
        Destroy(gameObject, _maxLifeTime);
    }

    // 애니메이션 이벤트: 폭발이 가장 큰 프레임
    public void Strike()
    {
        if (_hasStruck)
            return;

        _hasStruck = true;
        _onStrike?.Invoke(transform.position);
    }

    // 애니메이션 이벤트: 마지막 프레임
    public void Finish()
    {
        Destroy(gameObject);
    }
}
