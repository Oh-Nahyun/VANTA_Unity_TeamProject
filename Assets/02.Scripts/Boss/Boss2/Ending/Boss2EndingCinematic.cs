using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 보스2 사망(엔딩) 연출. 플레이어 피니셔가 끝나 Boss2.Death() 가 불리면 시작한다. (플레이어 쪽 연출은 PlayerFinisher 가 맡는다)
///   빛줄기 내려오기 → 주저앉기(Death_1→Death_2, 잠시 머묾) → 다시 일어난 자세(Idle 첫 프레임)로 정지
///   → 눈 위에 검은 불이 타오르다 꺼지며 눈빛이 사라짐
///   → 몸에서 검은 기운을 뿜으며 서서히 높이 떠올라 둥둥 → 남은 Death 애니메이션 + 검은 연기로 흩어지며 사라짐 → 암흑
/// 시간 배율과 상관없이 실시간으로 진행한다. 암흑에서 끝나고, 다음 연출은 _onEndingFinished 에 연결한다.
/// </summary>
public class Boss2EndingCinematic : MonoBehaviour
{
    private const string DeathStateName = "Boss2Death";
    // 다시 일어난 자세는 기본 Idle 의 첫 프레임에서 멈춰 보여 준다.
    private const string IdleStateName = "Boss2Idle";

    [Header("Death 애니메이션 시점 (초, Boss2Death 클립 기준)")]
    [Tooltip("서 있는 프레임 (Death_1)")]
    [SerializeField] private float _standTime = 0.084f;
    [Tooltip("주저앉은 프레임 (Death_2)")]
    [SerializeField] private float _crouchTime = 0.17f;
    [Tooltip("사라지기 시작하는 프레임 (Death_3)")]
    [SerializeField] private float _vanishStartTime = 0.25f;
    [SerializeField] private float _clipLength = 1.5f;

    [Header("카메라")]
    [SerializeField] private float _focusZoom = 1f;
    [SerializeField] private float _focusDuration = 1f;

    [Header("빛줄기")]
    [SerializeField] private GameObject _beamPrefab;
    [Tooltip("빛줄기 두께 배율. 텍스처 가장자리가 흐리므로 눈에 보이는 폭은 이 값의 절반 정도 (배경 가운데 원 지름에 맞춤)")]
    [SerializeField] private float _beamWidth = 7.5f;
    [Tooltip("빛줄기 윗끝이 보스2 위로 얼마나 높이 있는지")]
    [SerializeField] private float _beamTopHeight = 9f;
    [SerializeField] private float _beamDropDuration = 1.2f;

    [Header("타이밍 (초)")]
    [SerializeField] private float _beforeCrouch = 0.3f;
    [Tooltip("주저앉은 채로 있는 시간. 바로 일어나지 않고 잠시 머물러야 자연스럽다.")]
    [SerializeField] private float _crouchHold = 2f;
    [Tooltip("일어선 뒤 눈에 불이 붙기 전까지")]
    [SerializeField] private float _standHold = 0.5f;

    [Header("눈의 검은 불")]
    [Tooltip("눈 위에서 타오르는 검은 불 (EyeFlameVFX)")]
    [SerializeField] private GameObject _eyeFlamePrefab;
    [Tooltip("Idle 첫 프레임 기준 피벗에서 눈까지 (좌우 반전 시 x 는 뒤집는다)")]
    [SerializeField] private Vector2 _eyeOffset = new Vector2(0.03f, 0.33f);
    [Tooltip("불이 꺼지며 눈을 덮는 크기 (눈 3점: 가로 5px, 세로 2px @18ppu 에 여유)")]
    [SerializeField] private Vector2 _eyeCoverSize = new Vector2(0.3f, 0.18f);
    [Tooltip("눈을 덮는 색 = 몸 색")]
    [SerializeField] private Color _eyeCoverColor = new Color(35f / 255f, 41f / 255f, 49f / 255f, 1f);
    [SerializeField] private float _eyeFlameDuration = 1.4f;
    [Tooltip("불이 사그라들며 눈이 꺼지는 시간")]
    [SerializeField] private float _eyeOutDuration = 0.5f;
    [Tooltip("눈이 꺼진 뒤 떠오르기 전까지")]
    [SerializeField] private float _afterEyeOut = 0.5f;

    [Header("떠오르기")]
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

    [Header("연기로 흩어지기")]
    [Tooltip("사라질 때 뿜는 검은 연기 (DarkBurstVFX)")]
    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private float _smokeScale = 0.3f;
    [Tooltip("시작 순간 한꺼번에 뿜는 양 (각 층 초당 방출량의 몇 초 분량)")]
    [SerializeField] private float _smokeBurst = 0.8f;
    [Tooltip("이어서 계속 뿜는 시간")]
    [SerializeField] private float _smokeEmitDuration = 0.7f;
    [Tooltip("보스 몸 앞에 그려지도록 보스 정렬 순서에 더하는 값")]
    [SerializeField] private int _smokeSortingOffset = 5;

    [Header("암흑")]
    [SerializeField] private float _blackoutDuration = 0.6f;

    [Header("연결점")]
    [Tooltip("암흑까지 끝났을 때. 다음 연출(엔딩 씬 등)을 여기에 연결한다.")]
    [SerializeField] private UnityEvent _onEndingFinished;

    public bool IsPlaying { get; private set; }

    private Animator _animator;
    private Rigidbody2D _rigidbody2D;
    private GameObject _beamRoot;
    private Image _blackout;
    private GameObject _eyeCover;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    /// <summary>엔딩을 시작한다. 이미 진행 중이면 false. 끝나면(암흑 후) onFinished 를 부른다.</summary>
    public bool Play(Action onFinished)
    {
        if (IsPlaying)
            return false;

        IsPlaying = true;
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
        ShowDeathFrame(_standTime);

        if (CameraFocusingController.Instance != null)
        {
            CameraFocusingController.Instance.SlowFocus(transform, _focusZoom, _focusDuration);
        }

        // 1. 빛줄기 내려오기
        yield return DropBeam();

        // 2. 주저앉기 → 3. 다시 일어난 자세로 정지
        yield return new WaitForSecondsRealtime(_beforeCrouch);
        ShowDeathFrame(_crouchTime);
        yield return new WaitForSecondsRealtime(_crouchHold);
        ShowStateFrame(IdleStateName, 0f);
        yield return new WaitForSecondsRealtime(_standHold);

        // 4. 눈 위에 검은 불이 타오르다 꺼지며 눈빛이 사라진다
        yield return BurnOutEyes();

        // 5. 몸에서 검은 기운을 뿜으며 서서히 떠올라 둥둥
        GameObject aura = StartRiseAura();
        yield return FloatUp();
        StopRiseAura(aura);

        // 6. 남은 Death 애니메이션 재생 + 검은 연기로 흩어지며 사라짐
        if (_eyeCover != null)
        {
            Destroy(_eyeCover);
        }
        _animator.Play(DeathStateName, 0, _vanishStartTime / _clipLength);
        _animator.speed = 1f;
        _animator.Update(0f);
        StartCoroutine(BurstIntoSmoke());
        yield return new WaitForSecondsRealtime(_clipLength - _vanishStartTime);

        // 6. 암흑
        yield return Blackout();
        if (_beamRoot != null)
        {
            Destroy(_beamRoot);
        }

        IsPlaying = false;
        _onEndingFinished?.Invoke();
        onFinished?.Invoke();
    }

    // Boss2Death 의 time 초 프레임을 보여 주고 그 자리에 멈춘다.
    private void ShowDeathFrame(float time)
    {
        ShowStateFrame(DeathStateName, time / _clipLength);
    }

    // 상태의 normalizedTime 프레임을 보여 주고 그 자리에 멈춘다.
    private void ShowStateFrame(string stateName, float normalizedTime)
    {
        _animator.speed = 1f;
        _animator.Play(stateName, 0, normalizedTime);
        _animator.Update(0f);
        _animator.speed = 0f;
    }

    // 눈 위에서 검은 불이 타오르다 사그라들고, 불이 꺼지면서 눈빛도 몸 색으로 덮여 사라진다.
    private IEnumerator BurnOutEyes()
    {
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        Vector3 eyeLocal = new Vector3(body != null && body.flipX ? -_eyeOffset.x : _eyeOffset.x, _eyeOffset.y, 0f);

        ParticleSystem[] flames = Array.Empty<ParticleSystem>();
        GameObject flame = null;
        if (_eyeFlamePrefab != null)
        {
            flame = Instantiate(_eyeFlamePrefab, transform);
            flame.transform.localPosition = eyeLocal;
            flames = flame.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in flames)
            {
                // 눈 덮개(본체 +1)보다 위에 그린다. 층끼리의 순서는 유지.
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (body != null)
                {
                    renderer.sortingLayerID = body.sortingLayerID;
                    renderer.sortingOrder = body.sortingOrder + 2 + renderer.sortingOrder;
                }
                ps.Play();
            }
        }

        SpriteRenderer cover = CreateEyeCover(body, eyeLocal);
        yield return new WaitForSecondsRealtime(_eyeFlameDuration);

        // 불이 사그라들며(새 불꽃이 더 나오지 않음) 눈이 몸 색으로 덮인다.
        foreach (ParticleSystem ps in flames)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        float t = 0f;
        while (t < _eyeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            Color color = _eyeCoverColor;
            color.a *= Mathf.Clamp01(t / _eyeOutDuration);
            cover.color = color;
            yield return null;
        }
        cover.color = _eyeCoverColor;

        if (flame != null)
        {
            Destroy(flame, 1f);
        }
        yield return new WaitForSecondsRealtime(_afterEyeOut);
    }

    // 눈 3점을 덮는 몸 색 사각형. 보스를 따라 떠오르도록 자식으로 두고, 처음엔 투명하다.
    private SpriteRenderer CreateEyeCover(SpriteRenderer body, Vector3 eyeLocal)
    {
        _eyeCover = new GameObject("EyeCover");
        _eyeCover.transform.SetParent(transform, false);
        _eyeCover.transform.localPosition = eyeLocal;
        _eyeCover.transform.localScale = new Vector3(_eyeCoverSize.x, _eyeCoverSize.y, 1f);

        SpriteRenderer cover = _eyeCover.AddComponent<SpriteRenderer>();
        Texture2D white = Texture2D.whiteTexture;
        cover.sprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);
        Color clear = _eyeCoverColor;
        clear.a = 0f;
        cover.color = clear;
        if (body != null)
        {
            // 몸과 같은 재질을 써야 조명·글로우가 같아져 덮개가 몸 색과 어긋나 보이지 않는다.
            cover.sharedMaterial = body.sharedMaterial;
            cover.sortingLayerID = body.sortingLayerID;
            cover.sortingOrder = body.sortingOrder + 1;
        }
        return cover;
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

    // 사라지는 순간 보스 몸에서 검은 연기를 사방으로 뿜어, 연기가 되어 흩어지는 것처럼 보이게 한다.
    // 보스2는 곧 파괴되므로 연기는 보스와 상관없는 루트에 만들고 스스로 사라지게 한다.
    private IEnumerator BurstIntoSmoke()
    {
        if (_smokePrefab == null)
            yield break;

        GameObject smoke = Instantiate(_smokePrefab, transform.position, Quaternion.identity);
        smoke.transform.localScale = Vector3.one * _smokeScale;
        Destroy(smoke, 6f);

        SpriteRenderer body = GetComponent<SpriteRenderer>();
        ParticleSystem[] systems = Array.FindAll(smoke.GetComponentsInChildren<ParticleSystem>(true), ps => ps.emission.enabled);
        float[] baseRates = new float[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            baseRates[i] = ps.emission.rateOverTimeMultiplier;

            // 보스 몸 앞에 그려지도록 보스와 같은 정렬 레이어의 더 앞 순서로 옮긴다. (층끼리의 순서는 유지)
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (body != null)
            {
                renderer.sortingLayerID = body.sortingLayerID;
                renderer.sortingOrder = body.sortingOrder + _smokeSortingOffset + renderer.sortingOrder;
            }

            ps.Play();
            ps.Emit(Mathf.RoundToInt(baseRates[i] * _smokeBurst));
        }

        // 잠깐 더 뿜다가 멈춘다. 이미 나간 연기는 수명대로 흩어진다.
        yield return new WaitForSecondsRealtime(_smokeEmitDuration);
        foreach (ParticleSystem ps in systems)
        {
            if (ps != null)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
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
        beam.transform.localScale = new Vector3(_beamWidth, 1f, 1f);

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

    // 보스2가 파괴돼도 남도록 별도 루트에 검은 화면을 만든다.
    private IEnumerator Blackout()
    {
        GameObject canvasObject = new GameObject("Boss2EndingBlackout", typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _blackout = imageObject.GetComponent<Image>();
        _blackout.raycastTarget = false;
        _blackout.color = new Color(0f, 0f, 0f, 0f);

        float t = 0f;
        while (t < _blackoutDuration)
        {
            t += Time.unscaledDeltaTime;
            _blackout.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / _blackoutDuration));
            yield return null;
        }
        _blackout.color = Color.black;
    }
}
