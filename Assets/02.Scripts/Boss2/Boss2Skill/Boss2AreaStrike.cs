using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 광역기가 맵에 뿌리는 공격 더미.
///   1. 인디케이터 원이 판정 반경까지 커진다 (예고)
///   2. 다 차면 원을 끄고 폭발 애니메이션(3종 중 하나)을 재생한다
///   3. 클립의 Strike 이벤트에서 판정을 켜고, Finish 이벤트에서 사라진다
/// 판정·인디케이터·폭발 크기는 모두 _radius 하나에 맞춰진다.
/// Animator · SpriteRenderer · CircleCollider2D 는 이 스크립트와 같은 오브젝트에 있어야
/// 애니메이션 이벤트와 트리거 메시지를 받을 수 있다.
/// </summary>
public class Boss2AreaStrike : MonoBehaviour
{
    [Tooltip("판정 반경 (월드 단위). 콜라이더·인디케이터·폭발 크기가 모두 이 값에 맞춰진다")]
    [SerializeField] private float _radius = 1f;

    [Tooltip("더미 하나의 데미지. Player.TakeDamage 가 아직 데미지 값을 받지 않아 지금은 보관만 한다")]
    [SerializeField] private int _damage = 1;

    [Tooltip("폭발이 시작된 뒤 Finish 이벤트가 누락돼도 이 시간이 지나면 파괴한다")]
    [SerializeField] private float _maxLifeTime = 3f;

    [Header("인디케이터")]
    [Tooltip("자식 CircleSkillIndicator. 태그로 찾으면 다른 더미의 원을 잡으므로 직접 연결한다")]
    [SerializeField] private Transform _indicator;
    [SerializeField] private float _indicatorPlayTime = 1f;

    [Header("폭발")]
    [Tooltip("컨트롤러의 RandomStart 전이 조건(1, 2, 3)과 맞춘다. 0 은 기본값이라 쓰지 않는다")]
    [SerializeField] private int _strikeVariantMin = 1;
    [SerializeField] private int _strikeVariantMax = 3;

    [Tooltip("폭발 스프라이트에서 실제로 보이는 가로 지름 (루트 스케일 1 기준). Swing 클립 ≈ 0.89")]
    [SerializeField] private float _explosionVisualDiameter = 0.89f;

    private const string RandomStartParameter = "RandomStart";

    private Animator _animator;
    private CircleCollider2D _collider2D;

    private float _elapsedTime = 0f;
    private float _indicatorEndScale;
    private bool _isIndicatorFinished = false;

    // Player 에 콜라이더가 여러 개여도 한 번만 맞도록
    private readonly HashSet<Player> _damagedPlayers = new HashSet<Player>();

    public int Damage => _damage;

    private void Awake()
    {
        // 폭발의 보이는 지름이 판정 지름(_radius * 2)과 같아지도록 루트 스케일을 정한다.
        // 아래 콜라이더·인디케이터는 이 스케일을 나눠 계산하므로 그대로 월드 반경 _radius 를 유지한다.
        float scale = _radius * 2f / Mathf.Max(_explosionVisualDiameter, 0.0001f);
        transform.localScale = new Vector3(scale, scale, 1f);

        _animator = GetComponent<Animator>();

        _collider2D = GetComponent<CircleCollider2D>();
        _collider2D.enabled = false;
        // 콜라이더 반경은 로컬 단위라서 스케일을 나눠 월드 반경 _radius 에 맞춘다.
        _collider2D.radius = _radius / Mathf.Max(transform.lossyScale.x, 0.0001f);

        _indicatorEndScale = GetIndicatorScaleForRadius();
        _indicator.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (_isIndicatorFinished)
            return;

        _elapsedTime += Time.deltaTime;
        float progress = _elapsedTime / _indicatorPlayTime;

        float size = Mathf.Lerp(0f, _indicatorEndScale, progress);
        _indicator.localScale = new Vector3(size, size, 1f);

        if (progress >= 1f)
        {
            StartStrike();
        }
    }

    // 원이 다 차면 원을 끄고 폭발을 시작한다. 파괴 예약도 이때부터 센다.
    private void StartStrike()
    {
        _isIndicatorFinished = true;
        _indicator.gameObject.SetActive(false);

        _animator.SetInteger(RandomStartParameter, Random.Range(_strikeVariantMin, _strikeVariantMax + 1));
        Destroy(gameObject, _maxLifeTime);
    }

    // 원 스프라이트의 월드 지름이 _radius * 2 가 되는 로컬 스케일
    private float GetIndicatorScaleForRadius()
    {
        SpriteRenderer indicatorRenderer = _indicator.GetComponent<SpriteRenderer>();
        float spriteDiameter = indicatorRenderer != null && indicatorRenderer.sprite != null
            ? indicatorRenderer.sprite.bounds.size.x
            : 1f;

        return _radius * 2f / (Mathf.Max(transform.lossyScale.x, 0.0001f) * spriteDiameter);
    }

    // 애니메이션 이벤트: 폭발이 맞는 프레임
    public void Strike()
    {
        // 이미 원 안에 있던 Player 도 콜라이더가 켜지는 순간 OnTriggerEnter2D 를 받는다.
        _collider2D.enabled = true;
    }

    // 애니메이션 이벤트: 마지막 프레임
    public void Finish()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<Player>()
            : other.GetComponent<Player>();

        if (player != null && _damagedPlayers.Add(player))
        {
            player.TakeDamage();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
