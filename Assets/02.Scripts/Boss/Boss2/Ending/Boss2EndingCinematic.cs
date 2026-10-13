using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 보스2 사망(엔딩) 연출. 플레이어 피니셔가 끝나 Boss2.Death() 가 불리면 시작한다. (플레이어 쪽 연출은 PlayerFinisher 가 맡는다)
///   서 있다가 서서히 주저앉음(사이 프레임) + 동시에 포커스(카메라 + 포커스 화면의 밝은 원)가 플레이어에서 보스로 천천히 옮겨감
///   → 보스2만 보이는 스포트라이트 + 빛줄기 → 주저앉은 채 눈에서 검은 기운이 나오다 눈을 감음(Death_1 → Death_2)
///   → 주저앉은 자세 그대로 떠올라 둥둥 → 검은 구름을 뿜으며 디졸브 + 눈꺼풀이 닫히듯 위아래부터 암전
/// 시간 배율과 상관없이 실시간으로 진행한다. 암흑에서 끝나고, 다음 연출은 _onEndingFinished 에 연결한다.
/// </summary>
public class Boss2EndingCinematic : MonoBehaviour
{
    private const string DeathStateName = "Boss2Death";

    // 포커스 화면(SG_SkillFocus) 전역 값. 피니셔 중 SkillFocusScreenController 가 쓰는 값을 엔딩 동안 이어받는다.
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int HiddenActiveID = Shader.PropertyToID("_HiddenActive");

    [Header("서서히 주저앉기")]
    [Tooltip("서 있는 자세 → 앉은 자세 순서의 프레임 (Idle_0, Crouch_A, Death_0, Crouch_B, Death_1). Tools/Boss2/Build Crouch Frames 가 채운다")]
    [SerializeField] private Sprite[] _crouchFrames;
    [Tooltip("서 있다가 다 주저앉기까지 걸리는 시간")]
    [SerializeField] private float _crouchDuration = 3.5f;
    [Tooltip("주저앉은 채 눈을 감은 프레임 (Death_2)")]
    [SerializeField] private Sprite _eyesClosedSprite;

    [Header("Death 애니메이션 시점 (초, Boss2Death 클립 기준)")]
    [Tooltip("사라지기 시작하는 프레임 (Death_3)")]
    [SerializeField] private float _vanishStartTime = 0.25f;
    [SerializeField] private float _clipLength = 1.5f;
    [Tooltip("검은 구름 속에서 사라지는(디졸브) 속도 배율")]
    [SerializeField] private float _vanishSpeed = 2f;

    [Header("포커스 이동 (플레이어 → 보스)")]
    [SerializeField] private float _focusZoom = 1f;
    [Tooltip("카메라와 포커스 원이 플레이어에서 보스로 천천히 옮겨 가는 시간")]
    [SerializeField] private float _focusMoveDuration = 3.5f;
    [Tooltip("옮겨 간 뒤 보스2만 보이도록 줄이는 포커스 원 반경 (포커스 화면 기준, 플레이어 스킬 때 0.3)")]
    [SerializeField] private float _spotlightRadius = 0.2f;
    [SerializeField] private float _spotlightShrinkDuration = 0.4f;

    [Header("빛줄기")]
    [SerializeField] private GameObject _beamPrefab;
    [Tooltip("빛줄기가 비추는 폭 = 포커스 타원의 가로 길이 × 이 값")]
    [SerializeField] private float _beamWidthScale = 1f;
    [Tooltip("포커스 타원의 눈에 보이는 가장자리 = 반경 + 이 값 (화면 비율 기준, 포커스 화면의 _Feather 로 퍼지는 부분)")]
    [SerializeField] private float _spotlightEdge = 0.05f;
    [Tooltip("카메라를 못 찾을 때 쓰는 빛줄기 폭")]
    [SerializeField] private float _beamWidthFallback = 3f;
    [Tooltip("빛줄기 윗끝이 보스2 위로 얼마나 높이 있는지")]
    [SerializeField] private float _beamTopHeight = 9f;
    [SerializeField] private float _beamDropDuration = 1.2f;
    [Tooltip("검은 구름이 뿜어질 때 빛줄기가 가늘어지며 꺼지는 시간")]
    [SerializeField] private float _beamCloseDuration = 0.4f;
    [Tooltip("빛줄기가 내려온 뒤 눈에서 검은 기운이 나오기 전까지")]
    [SerializeField] private float _afterBeam = 0.5f;

    [Header("눈의 검은 기운 → 눈 감기")]
    [Tooltip("눈에서 뿜어져 나오는 검은 기운 (EyeFlameVFX)")]
    [SerializeField] private GameObject _eyeFlamePrefab;
    [Tooltip("앉은 프레임(Death_1) 의 눈 3점 가운데, 피벗 기준 (좌우 반전 시 x 는 뒤집는다)")]
    [SerializeField] private Vector2 _crouchEyeOffset = new Vector2(0.19f, 0.17f);
    [Tooltip("눈을 뜬 채 검은 기운이 나오는 시간")]
    [SerializeField] private float _eyeFlameDuration = 1.4f;
    [Tooltip("눈을 감은 뒤 떠오르기 전까지")]
    [SerializeField] private float _afterEyeClose = 0.5f;

    [Header("떠오르기 (주저앉은 자세 그대로)")]
    [SerializeField] private float _floatHeight = 3f;
    [SerializeField] private float _floatDuration = 2f;
    [SerializeField] private float _hoverDuration = 1.5f;
    [SerializeField] private float _hoverAmplitude = 0.06f;
    [SerializeField] private float _hoverPeriod = 1.2f;
    [Tooltip("떠오르는 동안 몸에서 뿜어져 나오는 검은 기운 (보스2 등장 연출의 DarkBurstVFX)")]
    [SerializeField] private GameObject _riseAuraPrefab;
    [SerializeField] private float _riseAuraScale = 0.35f;
    [Tooltip("프리팹에 설정된 방출량의 몇 배로 뿜을지")]
    [SerializeField] private float _riseAuraRate = 0.5f;

    [Header("검은 구름")]
    [Tooltip("사라질 때 뿜는 검은 구름 (DarkBurstVFX)")]
    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private float _smokeScale = 0.55f;
    [Tooltip("시작 순간 한꺼번에 뿜는 양 (각 층 초당 방출량의 몇 초 분량)")]
    [SerializeField] private float _smokeBurst = 2.5f;
    [Tooltip("이어서 계속 뿜는 양 (프리팹 방출량의 몇 배)")]
    [SerializeField] private float _smokeRate = 2f;
    [Tooltip("이어서 계속 뿜는 시간")]
    [SerializeField] private float _smokeEmitDuration = 1f;
    [Tooltip("구름 색에 곱하는 색. 어두울수록 구름이 진하고 검게 보인다 (알파는 그대로)")]
    [SerializeField] private Color _smokeTint = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("보스 몸 앞에 그려지도록 보스 정렬 순서에 더하는 값")]
    [SerializeField] private int _smokeSortingOffset = 5;

    [Header("눈꺼풀 암전")]
    [Tooltip("구름이 뿜어진 뒤 눈꺼풀이 닫히기 시작할 때까지")]
    [SerializeField] private float _eyelidDelay = 0.6f;
    [Tooltip("위아래에서 검은 화면이 닫혀 들어오는 시간")]
    [SerializeField] private float _eyelidDuration = 0.35f;

    [Header("엔딩 배경음")]
    [Tooltip("엔딩이 시작될 때 재생할 곡. 비우면 재생하지 않는다")]
    [SerializeField] private AudioClip _endingMusic;
    [Tooltip("엔딩 시작 후 곡이 시작될 때까지 (초, 실제 시간)")]
    [SerializeField] private float _endingMusicDelay = 0f;
    [Tooltip("곡 볼륨 배율. 옵션의 마스터·BGM 볼륨이 함께 곱해진다")]
    [SerializeField, Range(0f, 1f)] private float _endingMusicVolume = 1f;
    [SerializeField] private bool _loopEndingMusic = false;
    [Tooltip("엔딩 시작 때 지금 나오는 스테이지 BGM 을 끈다")]
    [SerializeField] private bool _stopStageBgmOnEnding = true;
    [Tooltip("암전까지 끝났을 때 곡을 줄이며 끄는 시간 (초). 0 이면 끄지 않고 계속 재생한다 (다음 연출로 이어짐)")]
    [SerializeField] private float _endingMusicFadeOut = 0f;

    [Header("연결점")]
    [Tooltip("암흑까지 끝났을 때. 다음 연출(엔딩 씬 등)을 여기에 연결한다.")]
    [SerializeField] private UnityEvent _onEndingFinished;
    [SerializeField] private UnityEvent _onEnding;

    
    public bool IsPlaying { get; private set; }

    private Animator _animator;
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _body;
    private GameObject _beamRoot;

    // 포커스 원: 엔딩이 직접 움직이는 동안 SkillFocusScreenController 는 꺼 둔다.
    private SkillFocusScreenController _focusScreen;
    private bool _isDrivingFocus;
    private Transform _focusFrom;
    private Transform _focusTarget;

    // 떠오르는 동안 숨긴 플레이어 렌더러 (원래 그려지던 것만 기억해 되돌린다)
    private readonly System.Collections.Generic.List<Renderer> _hiddenPlayerRenderers = new System.Collections.Generic.List<Renderer>();
    private float _focusBlend;
    private float _focusRadius;

    // 엔딩 곡은 보스가 꺼진 뒤에도 이어지도록 보스와 따로 둔 오브젝트에서 재생하고, 지연·페이드도 DOTween 으로 돌린다.
    private AudioSource _endingMusicSource;
    private Tween _endingMusicTween;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _body = GetComponent<SpriteRenderer>();
    }

    /// <summary>엔딩을 시작한다. 이미 진행 중이면 false. 끝나면(암흑 후) onFinished 를 부른다.</summary>
    public bool Play(Action onFinished)
    {
        if (IsPlaying)
            return false;

        IsPlaying = true;
        _onEnding?.Invoke();
        StartEndingMusic();
        StartCoroutine(EndingRoutine(onFinished));
        return true;
    }

    private IEnumerator EndingRoutine(Action onFinished)
    {
        // 피니셔 동안 멈춰 있던 애니메이터를 연출이 직접 프레임 단위로 다룬다.
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // 치명타 때 걸린 Hit/Death 트리거가 남아 있으면 전이가 일어나 프레임 제어를 덮으므로 지운다.
        _animator.ResetTrigger("Hit");
        _animator.ResetTrigger("Death");

        // 1~2. 피니셔가 끝나자마자 서 있다가 서서히 주저앉고, 그동안 카메라와 포커스 원이 플레이어에서 보스로 천천히 옮겨 간다.
        //      주저앉기·눈 감기는 프레임을 직접 바꾸므로 그동안 애니메이터를 끈다. (사라질 때 다시 켠다)
        _animator.enabled = false;
        Coroutine crouch = StartCoroutine(CrouchSlowly());
        yield return MoveFocusToBoss();
        yield return crouch;

        // 3. 보스2만 보이는 스포트라이트 + 빛줄기
        yield return ShrinkSpotlight();
        yield return DropBeam();
        yield return new WaitForSecondsRealtime(_afterBeam);

        // 4. 주저앉은 채 눈에서 검은 기운이 나오다 눈을 감는다.
        yield return EyesExhaleAndClose();

        // 5. 주저앉은 자세 그대로 검은 기운을 뿜으며 떠올라 둥둥
        // 카메라가 보스를 따라 올라가면 근처의 플레이어가 스포트라이트 안으로 들어오므로 숨긴다.
        // (지금은 플레이어가 스포트라이트 밖 어둠 속이라 사라지는 순간이 보이지 않는다)
        HidePlayer();
        GameObject aura = StartRiseAura();
        yield return FloatUp();
        StopRiseAura(aura);

        // 6. 검은 구름을 뿜으며 구름 속에서 디졸브, 곧바로 눈꺼풀이 닫히듯 암전
        StartCoroutine(BurstIntoSmoke());
        StartCoroutine(CloseBeam());
        _animator.enabled = true;
        _animator.Play(DeathStateName, 0, _vanishStartTime / _clipLength);
        _animator.speed = _vanishSpeed;
        _animator.Update(0f);
        yield return new WaitForSecondsRealtime(_eyelidDelay);
        yield return CloseEyelids();

        if (_beamRoot != null)
        {
            Destroy(_beamRoot);
        }
        ReleaseFocus();
        // 암흑 뒤라 다시 보여도 화면에는 나오지 않는다. 다음 연출을 위해 원래대로 돌려 둔다.
        ShowPlayer();

        if (_endingMusicFadeOut > 0f)
        {
            StopEndingMusic(_endingMusicFadeOut);
        }

        IsPlaying = false;
        _onEndingFinished?.Invoke();
        onFinished?.Invoke();
    }

    // 서 있는 자세에서 앉은 자세까지 프레임을 하나씩 넘기며 천천히 주저앉는다.
    // 처음엔 버티듯 느리게, 끝으로 갈수록 힘이 빠져 내려앉도록 시간을 SmoothStep 으로 나눈다.
    private IEnumerator CrouchSlowly()
    {
        if (_crouchFrames == null || _crouchFrames.Length == 0 || _body == null)
            yield break;

        int last = _crouchFrames.Length - 1;
        float t = 0f;
        while (t < _crouchDuration)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / _crouchDuration));
            _body.sprite = _crouchFrames[Mathf.Min(last, Mathf.FloorToInt(k * last + 0.0001f))];
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        _body.sprite = _crouchFrames[last];
    }

    #region 포커스

    // 포커스 원의 중심을 플레이어 → 보스로 옮기고, 카메라도 같은 시간 동안 보스로 옮긴다.
    // CameraFocusingController 는 대상을 바꾸면 카메라를 그 자리로 바로 옮기므로, 플레이어 자리에서 출발하는 임시 대상을 따라가게 한다.
    private IEnumerator MoveFocusToBoss()
    {
        _focusScreen = FindAnyObjectByType<SkillFocusScreenController>();
        if (_focusScreen != null)
        {
            // 매 프레임 포커스 원을 플레이어에게 되돌리지 않도록 잠시 끈다. (끝나면 다시 켠다)
            _focusScreen.enabled = false;
        }

        PlayerFinisher player = FindAnyObjectByType<PlayerFinisher>();
        _focusFrom = player != null ? player.transform : transform;
        _focusRadius = Shader.GetGlobalFloat(RadiusID);
        _focusBlend = 0f;
        _isDrivingFocus = true;

        _focusTarget = new GameObject("Boss2EndingFocusTarget").transform;
        _focusTarget.position = _focusFrom.position;
        if (CameraFocusingController.Instance != null)
        {
            CameraFocusingController.Instance.SlowFocus(_focusTarget, _focusZoom, _focusMoveDuration);
        }

        float t = 0f;
        while (t < _focusMoveDuration)
        {
            t += Time.unscaledDeltaTime;
            _focusBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / _focusMoveDuration));
            yield return null;
        }
        _focusBlend = 1f;
    }

    // 보스에 옮겨 간 포커스 원을 좁혀 보스2만 비춘다.
    private IEnumerator ShrinkSpotlight()
    {
        float from = _focusRadius;
        float t = 0f;
        while (t < _spotlightShrinkDuration)
        {
            t += Time.unscaledDeltaTime;
            _focusRadius = Mathf.Lerp(from, _spotlightRadius, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / _spotlightShrinkDuration)));
            yield return null;
        }
        _focusRadius = _spotlightRadius;
    }

    // 다른 스크립트의 Update·트윈보다 늦게 써서 엔딩 동안 포커스 화면 값을 유지한다.
    // 카메라 임시 대상도 여기서 옮긴다. 이동이 끝나면(_focusBlend = 1) 보스를 계속 따라간다.
    private void LateUpdate()
    {
        if (_isDrivingFocus == false)
            return;

        Vector3 from = _focusFrom != null ? _focusFrom.position : transform.position;
        Vector3 center = Vector3.Lerp(from, transform.position, _focusBlend);
        if (_focusTarget != null)
        {
            _focusTarget.position = center;
        }

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 viewport = cam.WorldToViewportPoint(center);
        Shader.SetGlobalVector(CenterID, new Vector4(viewport.x, viewport.y, 0f, 0f));
        Shader.SetGlobalFloat(RadiusID, _focusRadius);
        Shader.SetGlobalFloat(HiddenActiveID, 1f);
    }

    private void ReleaseFocus()
    {
        _isDrivingFocus = false;
        if (_focusScreen != null)
        {
            _focusScreen.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (_isDrivingFocus)
        {
            ReleaseFocus();
        }
    }

    // 보스2가 파괴될 때 카메라 임시 대상도 지운다. (암전 뒤라 카메라가 기본 대상으로 돌아가도 보이지 않는다)
    private void OnDestroy()
    {
        if (_focusTarget != null)
        {
            Destroy(_focusTarget.gameObject);
        }
    }

    #endregion

    #region 눈

    // 앉은 자세(Death_1)의 눈에서 검은 기운이 뿜어져 나오다 사그라들면, 눈 감은 프레임(Death_2)으로 바꾼다.
    private IEnumerator EyesExhaleAndClose()
    {
        Vector3 eyeLocal = new Vector3(_body != null && _body.flipX ? -_crouchEyeOffset.x : _crouchEyeOffset.x, _crouchEyeOffset.y, 0f);

        ParticleSystem[] flames = Array.Empty<ParticleSystem>();
        GameObject flame = null;
        if (_eyeFlamePrefab != null)
        {
            flame = Instantiate(_eyeFlamePrefab, transform);
            flame.transform.localPosition = eyeLocal;
            flames = flame.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in flames)
            {
                // 몸보다 위에 그린다. 층끼리의 순서는 유지.
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (_body != null)
                {
                    renderer.sortingLayerID = _body.sortingLayerID;
                    renderer.sortingOrder = _body.sortingOrder + 2 + renderer.sortingOrder;
                }
                ps.Play();
            }
        }

        yield return new WaitForSecondsRealtime(_eyeFlameDuration);

        // 기운이 사그라들며(새로 나오지 않음) 눈을 감는다.
        foreach (ParticleSystem ps in flames)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (_eyesClosedSprite != null && _body != null)
        {
            _body.sprite = _eyesClosedSprite;
        }

        if (flame != null)
        {
            Destroy(flame, 1.5f);
        }
        yield return new WaitForSecondsRealtime(_afterEyeClose);
    }

    #endregion

    #region 떠오르기

    // 플레이어(스프라이트·이펙트·트레일 등 자식 렌더러 전부)를 그리지 않는다. 오브젝트·스크립트는 그대로 둔다.
    private void HidePlayer()
    {
        PlayerFinisher player = FindAnyObjectByType<PlayerFinisher>();
        if (player == null)
            return;

        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.forceRenderingOff)
                continue;

            renderer.forceRenderingOff = true;
            _hiddenPlayerRenderers.Add(renderer);
        }
    }

    private void ShowPlayer()
    {
        foreach (Renderer renderer in _hiddenPlayerRenderers)
        {
            if (renderer != null)
            {
                renderer.forceRenderingOff = false;
            }
        }
        _hiddenPlayerRenderers.Clear();
    }

    // 떠오르는 동안 몸에서 검은 기운이 뿜어져 나온다. 보스를 따라가도록 자식으로 붙인다.
    private GameObject StartRiseAura()
    {
        if (_riseAuraPrefab == null)
            return null;

        GameObject aura = Instantiate(_riseAuraPrefab, transform);
        aura.transform.localPosition = Vector3.zero;
        aura.transform.localScale = Vector3.one * _riseAuraScale;

        foreach (ParticleSystem ps in aura.GetComponentsInChildren<ParticleSystem>(true))
        {
            // 자식을 묶기만 하는 루트(방출 모듈 꺼짐)는 건드리지 않는다.
            if (ps.emission.enabled == false)
                continue;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier *= _riseAuraRate;
            ps.Play();
        }
        return aura;
    }

    // 뿜기를 멈추고, 이미 나간 기운은 보스가 사라져도 남아 흩어지도록 떼어 낸다.
    private void StopRiseAura(GameObject aura)
    {
        if (aura == null)
            return;

        foreach (ParticleSystem ps in aura.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        aura.transform.SetParent(null, true);
        Destroy(aura, 5f);
    }

    // 감속하며 떠오른 뒤 사인파로 살짝 오르내린다. 피니셔의 이동 봉쇄 중이어도 위치를 직접 옮긴다.
    private IEnumerator FloatUp()
    {
        Vector3 start = transform.position;
        Vector3 top = start + Vector3.up * _floatHeight;

        float t = 0f;
        while (t < _floatDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / _floatDuration), 3f);
            SetPosition(Vector3.LerpUnclamped(start, top, k));
            yield return null;
        }

        t = 0f;
        while (t < _hoverDuration)
        {
            t += Time.unscaledDeltaTime;
            float offset = Mathf.Sin(t / _hoverPeriod * Mathf.PI * 2f) * _hoverAmplitude;
            SetPosition(top + Vector3.up * offset);
            yield return null;
        }
    }

    private void SetPosition(Vector3 position)
    {
        if (_rigidbody2D != null)
        {
            _rigidbody2D.position = position;
        }
        transform.position = position;
    }

    #endregion

    #region 사라지기

    // 보스 몸에서 검은 구름을 사방으로 뿜어 몸을 덮는다.
    // 보스2는 곧 파괴되므로 구름은 보스와 상관없는 루트에 만들고 스스로 사라지게 한다.
    private IEnumerator BurstIntoSmoke()
    {
        if (_smokePrefab == null)
            yield break;

        GameObject smoke = Instantiate(_smokePrefab, transform.position, Quaternion.identity);
        smoke.transform.localScale = Vector3.one * _smokeScale;
        Destroy(smoke, 6f);

        ParticleSystem[] systems = Array.FindAll(smoke.GetComponentsInChildren<ParticleSystem>(true), ps => ps.emission.enabled);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];

            // 보스 몸 앞에 그려지도록 보스와 같은 정렬 레이어의 더 앞 순서로 옮긴다. (층끼리의 순서는 유지)
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (_body != null)
            {
                renderer.sortingLayerID = _body.sortingLayerID;
                renderer.sortingOrder = _body.sortingOrder + _smokeSortingOffset + renderer.sortingOrder;
            }

            // 더 진하게: 색을 어둡게 하고, 계속 뿜는 양도 늘린다.
            ParticleSystem.MainModule main = ps.main;
            main.startColor = Tint(main.startColor, _smokeTint);
            ParticleSystem.EmissionModule emission = ps.emission;
            float baseRate = emission.rateOverTimeMultiplier;
            emission.rateOverTimeMultiplier = baseRate * _smokeRate;

            ps.Play();
            ps.Emit(Mathf.RoundToInt(baseRate * _smokeBurst));
        }

        // 잠깐 더 뿜다가 멈춘다. 이미 나간 구름은 수명대로 흩어진다.
        yield return new WaitForSecondsRealtime(_smokeEmitDuration);
        foreach (ParticleSystem ps in systems)
        {
            if (ps != null)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // 시작 색에 tint 를 곱한다. (단색·두 색 모드. 그라디언트 모드는 그대로 둔다)
    private static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient color, Color tint)
    {
        switch (color.mode)
        {
            case ParticleSystemGradientMode.Color:
                return new ParticleSystem.MinMaxGradient(color.color * tint);
            case ParticleSystemGradientMode.TwoColors:
                return new ParticleSystem.MinMaxGradient(color.colorMin * tint, color.colorMax * tint);
            default:
                return color;
        }
    }

    // 윗끝을 기준으로 세로로 자라며 보스2 위로 빛이 내려온다.
    private IEnumerator DropBeam()
    {
        if (_beamPrefab == null)
            yield break;

        _beamRoot = new GameObject("Boss2EndingBeam");
        _beamRoot.transform.position = transform.position + Vector3.up * _beamTopHeight;

        GameObject beam = Instantiate(_beamPrefab, _beamRoot.transform);
        beam.transform.localPosition = Vector3.zero;
        beam.transform.localScale = new Vector3(GetSpotlightWorldWidth() * _beamWidthScale, 1f, 1f);

        float t = 0f;
        while (t < _beamDropDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / _beamDropDuration));
            _beamRoot.transform.localScale = new Vector3(1f, Mathf.Max(0.001f, k), 1f);
            yield return null;
        }
        _beamRoot.transform.localScale = Vector3.one;
    }

    // 검은 구름이 뿜어질 때 빛줄기가 가늘어지며 꺼진다. (빛이 구름 위에 더해져 구름이 옅어 보이지 않도록)
    private IEnumerator CloseBeam()
    {
        if (_beamRoot == null)
            yield break;

        float t = 0f;
        while (t < _beamCloseDuration && _beamRoot != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / _beamCloseDuration);
            _beamRoot.transform.localScale = new Vector3(Mathf.Max(0.001f, 1f - k * k), 1f, 1f);
            yield return null;
        }
        if (_beamRoot != null)
        {
            _beamRoot.SetActive(false);
        }
    }

    // 포커스 타원의 가로 길이(월드). 포커스 화면은 화면 비율 좌표로 원을 그려서, 가로 반경 = 반경 × 화면 가로 폭이 된다.
    private float GetSpotlightWorldWidth()
    {
        Camera cam = Camera.main;
        if (cam == null || cam.orthographic == false)
            return _beamWidthFallback;

        float screenWorldWidth = cam.orthographicSize * 2f * cam.aspect;
        return 2f * (_spotlightRadius + _spotlightEdge) * screenWorldWidth;
    }

    // 눈을 감듯 화면 위아래에서 검은 막이 닫혀 들어온다. 보스2가 파괴돼도 남도록 별도 루트에 만든다.
    private IEnumerator CloseEyelids()
    {
        GameObject canvasObject = new GameObject("Boss2EndingEyelids", typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        RectTransform top = CreateEyelid(canvasObject.transform, "Top");
        RectTransform bottom = CreateEyelid(canvasObject.transform, "Bottom");

        float t = 0f;
        while (t < _eyelidDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / _eyelidDuration);
            SetEyelids(top, bottom, k * k);
            yield return null;
        }
        SetEyelids(top, bottom, 1f);
    }

    private static RectTransform CreateEyelid(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    // closed 0 = 활짝 뜸, 1 = 위아래가 화면 가운데에서 만남
    private static void SetEyelids(RectTransform top, RectTransform bottom, float closed)
    {
        float half = 0.5f * closed;
        top.anchorMin = new Vector2(0f, 1f - half);
        top.anchorMax = Vector2.one;
        bottom.anchorMin = Vector2.zero;
        bottom.anchorMax = new Vector2(1f, half);
    }

    #endregion

    #region 엔딩 배경음

    private void StartEndingMusic()
    {
        if (_stopStageBgmOnEnding && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        if (_endingMusic == null)
            return;

        if (_endingMusicSource == null)
        {
            GameObject go = new GameObject("Boss2EndingMusic");
            _endingMusicSource = go.AddComponent<AudioSource>();
            _endingMusicSource.playOnAwake = false;
            _endingMusicSource.spatialBlend = 0f;
        }

        KillEndingMusicTween();
        _endingMusicSource.Stop();
        _endingMusicSource.clip = _endingMusic;
        _endingMusicSource.loop = _loopEndingMusic;
        _endingMusicSource.volume = GetEndingMusicVolume();

        if (_endingMusicDelay > 0f)
        {
            AudioSource source = _endingMusicSource;
            _endingMusicTween = DOVirtual.DelayedCall(_endingMusicDelay, () => source.Play(), true).SetLink(source.gameObject);
        }
        else
        {
            _endingMusicSource.Play();
        }
    }

    // fadeDuration 이 0 이면 바로 멈춘다.
    private void StopEndingMusic(float fadeDuration)
    {
        KillEndingMusicTween();
        if (_endingMusicSource == null || _endingMusicSource.isPlaying == false)
            return;

        if (fadeDuration <= 0f)
        {
            _endingMusicSource.Stop();
            return;
        }

        AudioSource source = _endingMusicSource;
        _endingMusicTween = DOTween.To(() => source.volume, value => source.volume = value, 0f, fadeDuration)
            .SetUpdate(true)
            .SetLink(source.gameObject)
            .OnComplete(() => source.Stop());
    }

    private void KillEndingMusicTween()
    {
        if (_endingMusicTween != null)
        {
            _endingMusicTween.Kill();
            _endingMusicTween = null;
        }
    }

    // SoundManager 와 같은 저장 값(마스터·BGM 볼륨)을 따른다.
    private float GetEndingMusicVolume()
    {
        return _endingMusicVolume * PlayerPrefs.GetFloat("MasterVolume", 1f) * PlayerPrefs.GetFloat("BGMVolume", 1f);
    }

    #endregion
}
