using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// 광역기가 맵에 뿌리는 공격 더미.
///   1. 테두리(Border)는 처음부터 보이고, 마스크가 커지며 안쪽(Fill)을 가운데부터 드러낸다 (예고)
///   2. 다 드러나면 인디케이터를 끄고 폭발 애니메이션(3종 중 하나)을 재생한다
///   3. 클립의 Strike 이벤트에서 판정을 켜고, 폭발 본체가 사라지는 EndStrike 이벤트에서 판정을 끈 뒤, Finish 이벤트에서 사라진다
/// 판정·폭발 크기는 _radius 에 맞춰지고, 인디케이터(Fill · Border)는 프리팹에서 조절한 크기를 그대로 쓴다.
/// Animator · SpriteRenderer · CircleCollider2D 는 이 스크립트와 같은 오브젝트에 있어야
/// 애니메이션 이벤트와 트리거 메시지를 받을 수 있다.
/// </summary>
public class Boss2AreaStrike : MonoBehaviour
{
    [Tooltip("판정 반경 (월드 단위). 콜라이더·폭발 크기가 이 값에 맞춰진다 (인디케이터는 프리팹 크기)")]
    [SerializeField] private float _radius = 1f;

    [Tooltip("더미 하나의 데미지. Player.TakeDamage 가 아직 데미지 값을 받지 않아 지금은 보관만 한다")]
    [SerializeField] private int _damage = 1;

    [Tooltip("폭발이 시작된 뒤 Finish 이벤트가 누락돼도 이 시간이 지나면 파괴한다")]
    [SerializeField] private float _maxLifeTime = 3f;

    [FormerlySerializedAs("_indicator")]
    [Header("인디케이터")]
    [Tooltip("커지면서 Fill 을 드러내는 마스크. 0 → 프리팹에서 정한 Fill 크기까지 커진다")]
    [SerializeField] private SpriteMask _indicatorMask;
    [Tooltip("겉 테두리. 프리팹에서 정한 크기 그대로 처음부터 보인다")]
    [SerializeField] private SpriteRenderer _indicatorBorder;
    [Tooltip("안쪽 채움. 프리팹에서 정한 크기가 최대 크기. Mask Interaction = Visible Inside Mask")]
    [SerializeField] private SpriteRenderer _indicatorFill;

    [SerializeField] private float _indicatorPlayTime = 1f;

    [Header("폭발")]
    [Tooltip("컨트롤러의 RandomStart 전이 조건(1, 2, 3)과 맞춘다. 0 은 기본값이라 쓰지 않는다")]
    [SerializeField] private int _strikeVariantMin = 1;
    [SerializeField] private int _strikeVariantMax = 3;

    [Tooltip("폭발 스프라이트에서 실제로 보이는 가로 지름 (루트 스케일 1 기준). Swing 클립 ≈ 0.89")]
    [SerializeField] private float _explosionVisualDiameter = 0.89f;

    [Tooltip("폭발이 시작되면 Sorting Group 을 이 정렬 레이어로 바꾼다 (Player 보다 앞)")]
    [SerializeField] private string _strikeSortingLayer = "Vfx";

    private const string RandomStartParameter = "RandomStart";

    [Header("사운드")]
    [Tooltip("폭발 그림(RandomStart 1·2·3)마다 짝지은 소리. 순서대로 Sword Attack 1·2·3")]
    [SerializeField] private AudioClip[] _strikeSounds;
    [Range(0f, 1f)]
    [SerializeField] private float _strikeSoundVolume = 1f;
    [Tooltip("여러 폭발이 거의 동시에 터질 때 소리가 겹쳐 너무 커지지 않게 하는 최소 간격(초, 실시간). 모든 폭발체가 함께 쓴다")]
    [SerializeField] private float _strikeSoundMinInterval = 0.04f;

    [Header("검흔 빛")]
    [Tooltip("폭발 스프라이트를 그대로 따라 그리는 빛 레이어 (가산 머티리얼 M_Boss2SlashGlow). 비우면 빛을 쓰지 않는다")]
    [SerializeField] private SpriteRenderer _slashGlow;
    [Tooltip("Strike 순간의 빛 세기 (빛 레이어 알파). 머티리얼의 HDR 색이 실제 밝기를 정한다")]
    [Range(0f, 1f)]
    [SerializeField] private float _glowPeak = 1f;
    [Tooltip("Strike 뒤 빛이 다 사라질 때까지 (초)")]
    [SerializeField] private float _glowFadeDuration = 0.35f;

    // 더미끼리 마스크가 섞이지 않게 묶는 그룹. 그룹 안의 스프라이트는 이 그룹의 정렬 레이어로 그려진다.
    private SortingGroup _sortingGroup;
    private Animator _animator;
    private CircleCollider2D _collider2D;
    [SerializeField] private ParticleSystem _explosionEffect;

    private SpriteRenderer _renderer;
    // StartStrike 에서 고른 폭발 그림 번호 (RandomStart 값, 1부터)
    private int _strikeVariant;
    private static float _lastStrikeSoundTime = float.NegativeInfinity;
    // Strike 뒤 흐른 시간. 0 보다 작으면 빛이 꺼져 있다.
    private float _glowTime = -1f;

    private float _elapsedTime = 0f;
    private float _indicatorEndScale;
    private bool _isIndicatorFinished = false;

    // Player 에 콜라이더가 여러 개여도 한 번만 맞도록
    private readonly HashSet<Player> _damagedPlayers = new HashSet<Player>();

    public int Damage => _damage;

    private void Awake()
    {
        // 루트 스케일을 바꾸기 전에, 프리팹에서 정한 인디케이터의 월드 크기를 기억해 둔다.
        float prefabRootScale = transform.localScale.x;

        // 폭발의 보이는 지름이 판정 지름(_radius * 2)과 같아지도록 루트 스케일을 정한다.
        // 아래 콜라이더는 이 스케일을 나눠 계산하므로 그대로 월드 반경 _radius 를 유지한다.
        float scale = _radius * 2f / Mathf.Max(_explosionVisualDiameter, 0.0001f);
        transform.localScale = new Vector3(scale, scale, 1f);

        _animator = GetComponent<Animator>();
        _sortingGroup = GetComponent<SortingGroup>();
        _renderer = GetComponent<SpriteRenderer>();
        if (_slashGlow != null)
        {
            _slashGlow.enabled = false;
        }

        _collider2D = GetComponent<CircleCollider2D>();
        _collider2D.enabled = false;
        // 콜라이더 반경은 로컬 단위라서 스케일을 나눠 월드 반경 _radius 에 맞춘다.
        _collider2D.radius = _radius / Mathf.Max(transform.lossyScale.x, 0.0001f);

        // Fill · Border 는 프리팹에서 조절한 크기를 그대로 쓴다.
        // 루트 스케일이 바뀐 만큼 자식 스케일을 되돌려, 화면에 보이는 크기가 프리팹과 같게 유지한다.
        float keepRatio = prefabRootScale / Mathf.Max(scale, 0.0001f);
        _indicatorFill.transform.localScale *= keepRatio;
        _indicatorBorder.transform.localScale *= keepRatio;

        // 마스크는 0 에서 시작해, 다 커졌을 때 Fill 의 보이는 부분을 정확히 덮는 크기까지 커진다.
        float fillWorldDiameter = GetVisibleDiameter(_indicatorFill.sprite) * _indicatorFill.transform.lossyScale.x;
        _indicatorEndScale = GetScaleForDiameter(GetVisibleDiameter(_indicatorMask.sprite), fillWorldDiameter);
        _indicatorMask.transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (_isIndicatorFinished)
            return;

        _elapsedTime += Time.deltaTime;
        float progress = _elapsedTime / _indicatorPlayTime;

        float size = Mathf.Lerp(0f, _indicatorEndScale, progress);
        _indicatorMask.transform.localScale = new Vector3(size, size, 1f);

        if (progress >= 1f)
        {
            StartStrike();
        }
    }

    // Fill 이 다 드러나면 인디케이터를 모두 끄고 폭발을 시작한다. 파괴 예약도 이때부터 센다.
    private void StartStrike()
    {
        _isIndicatorFinished = true;
        _indicatorMask.gameObject.SetActive(false);
        _indicatorFill.gameObject.SetActive(false);
        _indicatorBorder.gameObject.SetActive(false);

        // 인디케이터가 꺼졌으니 그룹 = 폭발뿐. 그룹 레이어를 바꿔 Player 앞에 그린다.
        if (_sortingGroup != null)
        {
            _sortingGroup.sortingLayerName = _strikeSortingLayer;
        }

        _strikeVariant = Random.Range(_strikeVariantMin, _strikeVariantMax + 1);
        _animator.SetInteger(RandomStartParameter, _strikeVariant);
        Destroy(gameObject, _maxLifeTime);
    }

    // 스케일 1 에서 localDiameter(유닛)인 것이 월드 지름 worldDiameter 가 되는 로컬 스케일. 루트 스케일을 나눠 준다.
    private float GetScaleForDiameter(float localDiameter, float worldDiameter)
    {
        return worldDiameter / (Mathf.Max(transform.lossyScale.x, 0.0001f) * Mathf.Max(localDiameter, 0.0001f));
    }

    // 스프라이트에서 실제로 보이는 부분의 지름(유닛). 메시(Tight) 정점 범위를 쓰므로 투명 여백은 빠진다.
    // 기본 Circle 은 1, T_Orb_Fill 은 텍스처 폭의 약 65%.
    private static float GetVisibleDiameter(Sprite sprite)
    {
        if (sprite == null)
            return 1f;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }

        Vector2 size = max - min;
        return Mathf.Max(size.x, size.y);
    }

    // 애니메이션 이벤트: 폭발이 맞는 프레임
    public void Strike()
    {
        // 이미 원 안에 있던 Player 도 콜라이더가 켜지는 순간 OnTriggerEnter2D 를 받는다.
        _collider2D.enabled = true;

        if (_explosionEffect != null)
        {
            _explosionEffect.Play();
        }

        PlayStrikeSound();

        // 검흔이 가장 밝게 빛났다가 빠르게 사그라든다.
        if (_slashGlow != null)
        {
            _glowTime = 0f;
            _slashGlow.enabled = true;
            SyncGlow();
        }
    }

    // 지금 폭발 그림에 짝지은 소리를 낸다.
    private void PlayStrikeSound()
    {
        int index = _strikeVariant - 1;
        if (_strikeSounds == null || index < 0 || index >= _strikeSounds.Length || SoundManager.Instance == null)
            return;
        if (Time.realtimeSinceStartup - _lastStrikeSoundTime < _strikeSoundMinInterval)
            return;

        _lastStrikeSoundTime = Time.realtimeSinceStartup;
        SoundManager.Instance.PlayEffectSound(_strikeSounds[index], _strikeSoundVolume);
    }

    // 애니메이션이 스프라이트를 바꾼 뒤(LateUpdate) 빛 레이어가 같은 그림을 따라 그린다.
    private void LateUpdate()
    {
        if (_slashGlow == null || _glowTime < 0f)
            return;

        _glowTime += Time.deltaTime;
        if (_glowTime >= _glowFadeDuration)
        {
            _glowTime = -1f;
            _slashGlow.enabled = false;
            return;
        }
        SyncGlow();
    }

    private void SyncGlow()
    {
        _slashGlow.sprite = _renderer.sprite;
        _slashGlow.flipX = _renderer.flipX;
        _slashGlow.flipY = _renderer.flipY;

        // 처음엔 버티다가 끝으로 갈수록 빠르게 줄어든다.
        float t = Mathf.Clamp01(_glowTime / Mathf.Max(_glowFadeDuration, 0.0001f));
        Color color = _slashGlow.color;
        color.a = _glowPeak * (1f - t * t);
        _slashGlow.color = color;
    }

    // 애니메이션 이벤트: 폭발 본체가 사라지는 프레임. 남은 불똥만 보이는 동안에는 맞지 않도록 판정을 끈다.
    public void EndStrike()
    {
        _collider2D.enabled = false;
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
