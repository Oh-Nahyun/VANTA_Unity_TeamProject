using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 보스1 → 보스2 페이즈 전환 등장 시네마틱.
/// Timeline(Boss2Intro.playable)의 애니메이션 트랙이 아래 "Timeline 이 움직이는 값" 들을 커브로 움직이고,
/// 시그널 트랙이 베기·보스1 폭발 요청·보스2 등장·카메라 복귀·전투 시작 메서드를 호출한다.
/// 이 컴포넌트는 그 값들을 매 프레임 실제 오브젝트(가스·조명·구름·오버레이·인트로 카메라)에 반영한다.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class Boss2IntroCinematic : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private Boss2 _boss2;
    [Tooltip("가스가 나오고 보스2가 서게 될 기준. 비어 있거나 이미 사라졌으면 보스2 위치를 쓴다.")]
    [SerializeField] private Transform _boss1;

    [Header("전투 시작")]
    [Tooltip("시네마틱이 끝나고 플레이어가 준비할 시간. 이 시간 동안 보스2는 idle 로 멈춰 있다가 FSM 을 시작한다.")]
    [SerializeField] private float _battleStartDelay = 1f;

    [Header("연출 오브젝트")]
    [SerializeField] private ParticleSystem _gas;
    [SerializeField] private ParticleSystem _cloud;
    [SerializeField] private Boss2IntroSlash[] _slashes;
    [SerializeField] private Transform _slashRoot;
    [SerializeField] private CinemachineImpulseSource _slashImpulse;
    [SerializeField] private Image _flashOverlay;
    [SerializeField] private Image _blackOverlay;
    [SerializeField] private CinemachineCamera _introCamera;

    [Header("가스 (연막탄처럼 점점 많이·빠르게)")]
    [Tooltip("_gasAmount = 1 일 때 프리팹에 설정된 방출량의 몇 배로 뿜을지 (모든 자식 파티클에 적용)")]
    [SerializeField] private float _gasRateScale = 1f;
    [Tooltip("_gasAmount 0 → 1 사이 분출 속도 배율 (프리팹 속도 기준)")]
    [SerializeField] private float _gasMinSpeedScale = 0.35f;
    [SerializeField] private float _gasMaxSpeedScale = 1.2f;
    [Tooltip("시작 순간 한꺼번에 터져 나오는 양 (각 층 초당 방출량의 몇 초 분량). 0 이면 시작 분출 없음")]
    [SerializeField] private float _gasStartBurst = 0.5f;
    [Tooltip("시작 분출의 속도 배율 (프리팹 속도 기준)")]
    [SerializeField] private float _gasStartBurstSpeedScale = 1.3f;

    [Header("화면 어둡게")]
    [Tooltip("최대로 어두울 때 글로벌 라이트 강도 배율")]
    [SerializeField, Range(0f, 1f)] private float _darkIntensityScale = 0.25f;

    [Header("인트로 카메라")]
    [Tooltip("보스2 위치 기준 얼굴 위치")]
    [SerializeField] private Vector2 _faceOffset = new Vector2(-0.05f, 1.15f);
    [SerializeField] private float _faceOrthographicSize = 1.1f;
    [Tooltip("카메라가 뚝 떨어지는 방향·거리 (오른쪽 아래)")]
    [SerializeField] private Vector2 _dropOffset = new Vector2(0.45f, -0.45f);
    [SerializeField] private float _shakeAmplitude = 0.06f;
    [SerializeField] private float _shakeFrequency = 90f;
    [SerializeField] private float _zoomOutDuration = 1.2f;

    [Header("보스1 폭발 (팀원 연결점)")]
    [Tooltip("켜져 있으면 임시 폭발 이펙트 + 고정 시간 대기. 끄면 아래 이벤트로 폭발을 요청하고 NotifyBoss1ExplosionFinished() 를 기다린다.")]
    [SerializeField] private bool _useTempExplosion = true;
    [SerializeField] private GameObject _tempExplosionPrefab;
    [SerializeField] private float _tempExplosionScale = 0.45f;
    [SerializeField] private float _tempExplosionDuration = 0.8f;
    [Tooltip("폭발 끝 알림이 안 와도 이 시간이 지나면 진행한다.")]
    [SerializeField] private float _explosionTimeout = 8f;
    [SerializeField] private UnityEvent _onRequestBoss1Explosion;

    [Header("Timeline 이 움직이는 값")]
    [SerializeField, Range(0f, 1f)] private float _gasAmount;
    [SerializeField, Range(0f, 1f)] private float _darkness;
    [SerializeField, Range(0f, 1f)] private float _cloudAlpha;
    [SerializeField, Range(0f, 1f)] private float _flashAlpha;
    [SerializeField, Range(0f, 1f)] private float _blackout;
    [SerializeField] private float _cameraDrop;
    [SerializeField] private float _cameraShake;

    /// <summary>보스1 폭발을 코드로 연결할 때 구독. 폭발이 끝나면 NotifyBoss1ExplosionFinished() 호출.</summary>
    public event Action Boss1ExplosionRequested;

    public bool IsPlaying { get; private set; }

    private bool IsBoss1Present => _boss1 != null && _boss1.gameObject.activeInHierarchy;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private PlayableDirector _director;
    private Action _onFinished;

    private Boss2Controller _boss2Controller;
    private Animator _boss2Animator;
    private SpriteRenderer _boss2Body;
    private readonly List<Renderer> _boss2Renderers = new List<Renderer>();
    private readonly List<Renderer> _boss2RenderersAlreadyOff = new List<Renderer>();
    private bool _isBoss2Concealed;
    private int _boss2SortingLayer;
    private int _boss2SortingOrder;
    private bool _isBoss2SortingChanged;
    private bool _isBoss2FsmStopped;

    private Vector3 _anchor;
    private Vector3 _faceBase;

    private readonly List<Light2D> _globalLights = new List<Light2D>();
    private readonly List<float> _globalLightIntensities = new List<float>();

    private ParticleSystem[] _gasSystems = Array.Empty<ParticleSystem>();
    private float[] _gasBaseRates;
    private float[] _gasBaseSpeeds;
    private ParticleSystemRenderer _cloudRenderer;
    private MaterialPropertyBlock _cloudBlock;
    private Color _cloudBaseColor = Color.white;

    private CinemachineBrain _brain;
    private CinemachineBlendDefinition _brainDefaultBlend;
    private Coroutine _restoreBlendRoutine;

    private Coroutine _battleStartRoutine;
    private bool _isWaitingExplosion;
    private Coroutine _explosionRoutine;
    private Boss1DeathController _boss1Death;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
        _director.playOnAwake = false;
        _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        if (_boss2 != null)
        {
            _boss2Controller = _boss2.GetComponent<Boss2Controller>();
            _boss2Animator = _boss2.GetComponent<Animator>();
            _boss2Body = _boss2.GetComponent<SpriteRenderer>();
            _boss2.GetComponentsInChildren(true, _boss2Renderers);
            ConcealBoss2();
        }

        if (_gas != null)
        {
            // 가스 프리팹은 여러 층(자식 파티클)으로 되어 있을 수 있다. 각 층의 원래 방출량·속도를 기준으로 비율 조절한다.
            // 방출 모듈이 꺼진 시스템(자식을 묶기만 하는 루트 등)은 제외한다. Emit() 은 방출 모듈이 꺼져 있어도 입자를 만들기 때문.
            _gasSystems = Array.FindAll(_gas.GetComponentsInChildren<ParticleSystem>(true), ps => ps.emission.enabled);
            _gasBaseRates = new float[_gasSystems.Length];
            _gasBaseSpeeds = new float[_gasSystems.Length];
            for (int i = 0; i < _gasSystems.Length; i++)
            {
                _gasBaseRates[i] = _gasSystems[i].emission.rateOverTimeMultiplier;
                _gasBaseSpeeds[i] = _gasSystems[i].main.startSpeedMultiplier;
            }
        }

        if (_cloud != null)
        {
            _cloudRenderer = _cloud.GetComponent<ParticleSystemRenderer>();
            _cloudBlock = new MaterialPropertyBlock();
            if (_cloudRenderer.sharedMaterial != null && _cloudRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _cloudBaseColor = _cloudRenderer.sharedMaterial.GetColor(BaseColorId);
            }
        }

        if (_introCamera != null)
        {
            _introCamera.gameObject.SetActive(false);
        }

        ResetAnimatedValues();
        ApplyOverlays();
        StopEffects(true);
    }

    /// <summary>인트로 재생. 끝나고 전투가 시작될 때 onFinished 를 부른다.</summary>
    public void Play(Action onFinished)
    {
        if (IsPlaying)
            return;

        IsPlaying = true;
        _onFinished = onFinished;
        _isWaitingExplosion = false;

        _anchor = IsBoss1Present ? _boss1.position : (_boss2 != null ? _boss2.transform.position : transform.position);
        _anchor.z = 0f;
        _boss1Death = IsBoss1Present ? _boss1.GetComponent<Boss1DeathController>() : null;
        if (_boss1Death != null)
        {
            _boss1Death.OnExplosionFinished -= NotifyBoss1ExplosionFinished;
            _boss1Death.OnExplosionFinished += NotifyBoss1ExplosionFinished;
        }

        ConcealBoss2();
        CacheGlobalLights();
        CacheBrain();

        if (_gas != null)
        {
            _gas.transform.position = _anchor;
            _gas.Clear();
            _gas.Play();
            EmitGasStartBurst();
        }
        if (_cloud != null)
        {
            _cloud.transform.position = _anchor;
            _cloud.Clear();
            _cloud.Play();
        }
        if (_slashRoot != null)
        {
            _slashRoot.position = _anchor;
        }

        _director.time = 0;
        _director.Play();
        SetDirectorSpeed(1);
    }

    /// <summary>테스트용: 진행 중이던 인트로·전투를 멈추고 처음부터 다시 재생한다.</summary>
    [ContextMenu("Test/Play Intro")]
    public void Replay()
    {
        if (Application.isPlaying == false)
        {
            Debug.LogWarning("[Boss2Intro] 플레이 모드에서 실행하세요.");
            return;
        }

        if (_director.state == PlayState.Playing)
        {
            _director.Stop();
        }
        if (_explosionRoutine != null)
        {
            StopCoroutine(_explosionRoutine);
            _explosionRoutine = null;
        }
        if (_battleStartRoutine != null)
        {
            StopCoroutine(_battleStartRoutine);
            _battleStartRoutine = null;
        }
        if (_introCamera != null)
        {
            _introCamera.gameObject.SetActive(false);
        }
        RestoreGlobalLights();
        RestoreBoss2Sorting();
        IsPlaying = false;
        _onFinished = null;
        ResetAnimatedValues();
        ApplyOverlays();

        if (_boss2 != null && _boss2.gameObject.activeInHierarchy && _boss2Controller != null)
        {
            _boss2Controller.StopFsm();
        }
        Play(null);
    }

    #region Timeline 시그널

    public void SlashFirst() => PlaySlash(0);

    public void SlashSecond() => PlaySlash(1);

    private void PlaySlash(int index)
    {
        if (_slashes != null && index < _slashes.Length && _slashes[index] != null)
        {
            _slashes[index].Play();
        }
        if (_slashImpulse != null)
        {
            _slashImpulse.GenerateImpulse();
        }
    }

    /// <summary>보스1 폭발 요청. 폭발이 끝날 때까지 Timeline 을 멈춰 둔다.</summary>
    public void RequestBoss1Explosion()
    {
        if (_isWaitingExplosion)
            return;

        _isWaitingExplosion = true;
        SetDirectorSpeed(0);

        if (_explosionRoutine != null)
        {
            StopCoroutine(_explosionRoutine);
        }

        if (_useTempExplosion)
        {
            _explosionRoutine = StartCoroutine(TempExplosionRoutine());
        }
        else
        {
            _onRequestBoss1Explosion?.Invoke();
            Boss1ExplosionRequested?.Invoke();
            _explosionRoutine = StartCoroutine(ExplosionTimeoutRoutine());
        }
    }

    /// <summary>보스1 폭발 애니메이션이 끝났을 때 호출. 바로 암전으로 넘어간다.</summary>
    public void NotifyBoss1ExplosionFinished()
    {
        if (_isWaitingExplosion == false)
            return;

        _isWaitingExplosion = false;
        if (_explosionRoutine != null)
        {
            StopCoroutine(_explosionRoutine);
            _explosionRoutine = null;
        }
        SetDirectorSpeed(1);
    }

    /// <summary>완전 암전 중에 호출: 보스2를 보스1 자리에 idle 첫 프레임으로 세우고, 얼굴 줌 카메라로 컷.</summary>
    public void RevealBoss2()
    {
        StopEffects(true);

        if (_boss2 == null)
            return;

        if (_boss2.gameObject.activeSelf == false)
        {
            _boss2.gameObject.SetActive(true);
        }

        if (IsPlaying)
        {
            Vector3 position = _anchor;
            position.z = _boss2.transform.position.z;
            _boss2.transform.position = position;
        }

        ShowBoss2();

        if (_boss2Animator != null)
        {
            _boss2Animator.Rebind();
            _boss2Animator.Update(0f);
            _boss2Animator.speed = IsPlaying ? 0f : 1f;
        }

        PutBoss2BehindBoss1();

        if (IsPlaying && _introCamera != null)
        {
            _faceBase = _boss2.transform.position + (Vector3)_faceOffset;
            _faceBase.z = _introCamera.transform.position.z;
            _introCamera.transform.position = _faceBase;
            _introCamera.Lens.OrthographicSize = _faceOrthographicSize;

            SetBrainBlend(new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f), 0.1f);
            _introCamera.gameObject.SetActive(true);
            _introCamera.PreviousStateIsValid = false;
        }
    }

    /// <summary>얼굴 줌 카메라 해제 → 메인 카메라로 줌아웃 블렌드.</summary>
    public void EndIntroCamera()
    {
        if (_introCamera == null || _introCamera.gameObject.activeSelf == false)
            return;

        SetBrainBlend(new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, _zoomOutDuration), _zoomOutDuration + 0.1f);
        _introCamera.gameObject.SetActive(false);
    }

    /// <summary>시네마틱 종료: 보스2 idle 재생, 플레이어 준비 시간(_battleStartDelay) 뒤 무적 해제·FSM 시작.</summary>
    public void StartBattle()
    {
        EndIntroCamera();
        RestoreGlobalLights();
        StopEffects(true);

        if (_boss2 != null)
        {
            ShowBoss2();
            RestoreBoss2Sorting();
            if (_boss2Animator != null)
            {
                _boss2Animator.speed = 1f;
            }
            if (_battleStartRoutine != null)
            {
                StopCoroutine(_battleStartRoutine);
            }
            _battleStartRoutine = StartCoroutine(StartFsmAfterDelay());
        }

        if (_boss1Death != null)
        {
            _boss1Death.OnExplosionFinished -= NotifyBoss1ExplosionFinished;
            _boss1Death = null;
        }

        bool wasPlaying = IsPlaying;
        IsPlaying = false;
        if (_director.state == PlayState.Playing)
        {
            _director.Stop();
        }
        ResetAnimatedValues();
        ApplyOverlays();

        Action finished = _onFinished;
        _onFinished = null;
        if (wasPlaying)
        {
            finished?.Invoke();
        }
    }

    #endregion

    private void LateUpdate()
    {
        // 인트로 전에 다른 곳(보스1 사망 처리)에서 보스2를 켜도 보이지 않게 막는다.
        if (_isBoss2Concealed && _boss2 != null)
        {
            HideBoss2Renderers();
            if (_isBoss2FsmStopped == false && _boss2.gameObject.activeInHierarchy && _boss2Controller != null)
            {
                _isBoss2FsmStopped = true;
                _boss2Controller.StopFsm();
            }
        }

        if (IsPlaying == false)
            return;

        ApplyGas();
        ApplyDarkness();
        ApplyCloud();
        ApplyOverlays();
        ApplyIntroCamera();
    }

    private void ApplyGas()
    {
        if (_gas == null)
            return;

        float speedScale = Mathf.Lerp(_gasMinSpeedScale, _gasMaxSpeedScale, _gasAmount);
        for (int i = 0; i < _gasSystems.Length; i++)
        {
            var emission = _gasSystems[i].emission;
            emission.rateOverTimeMultiplier = _gasBaseRates[i] * _gasRateScale * _gasAmount;
            var main = _gasSystems[i].main;
            main.startSpeedMultiplier = _gasBaseSpeeds[i] * speedScale;
        }
    }

    // 시작하자마자 검은 기운이 한 번에 터져 나오게 각 층에서 한꺼번에 뿜는다.
    private void EmitGasStartBurst()
    {
        if (_gasStartBurst <= 0f)
            return;

        for (int i = 0; i < _gasSystems.Length; i++)
        {
            int count = Mathf.RoundToInt(_gasBaseRates[i] * _gasStartBurst);
            if (count <= 0)
                continue;

            var main = _gasSystems[i].main;
            main.startSpeedMultiplier = _gasBaseSpeeds[i] * _gasStartBurstSpeedScale;
            _gasSystems[i].Emit(count);
        }
    }

    private void ApplyDarkness()
    {
        for (int i = 0; i < _globalLights.Count; i++)
        {
            if (_globalLights[i] == null)
                continue;
            _globalLights[i].intensity = _globalLightIntensities[i] * Mathf.Lerp(1f, _darkIntensityScale, _darkness);
        }
    }

    private void ApplyCloud()
    {
        if (_cloudRenderer == null)
            return;

        Color color = _cloudBaseColor;
        color.a *= _cloudAlpha;
        _cloudRenderer.GetPropertyBlock(_cloudBlock);
        _cloudBlock.SetColor(BaseColorId, color);
        _cloudRenderer.SetPropertyBlock(_cloudBlock);
    }

    private void ApplyOverlays()
    {
        SetImageAlpha(_flashOverlay, _flashAlpha);
        SetImageAlpha(_blackOverlay, _blackout);
    }

    private void ApplyIntroCamera()
    {
        if (_introCamera == null || _introCamera.gameObject.activeSelf == false)
            return;

        Vector3 shake = Vector3.zero;
        if (_cameraShake > 0f)
        {
            float t = Time.unscaledTime * _shakeFrequency;
            shake.x = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f;
            shake.y = (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f;
            shake *= _shakeAmplitude * _cameraShake;
        }

        _introCamera.transform.position = _faceBase + (Vector3)(_dropOffset * _cameraDrop) + shake;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
        image.enabled = alpha > 0.001f;
    }

    private void SetDirectorSpeed(double speed)
    {
        if (_director.playableGraph.IsValid() && _director.playableGraph.GetRootPlayableCount() > 0)
        {
            _director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }
    }

    private IEnumerator StartFsmAfterDelay()
    {
        if (_battleStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(_battleStartDelay);
        }
        _battleStartRoutine = null;

        if (_boss2 == null || _boss2.IsDead)
            yield break;

        _boss2.SetInvincible(false);
        if (_boss2Controller != null)
        {
            _boss2Controller.StartFsm();
        }
    }

    private IEnumerator TempExplosionRoutine()
    {
        if (_tempExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(_tempExplosionPrefab, _anchor, Quaternion.identity);
            explosion.transform.localScale *= _tempExplosionScale;
            foreach (ParticleSystem ps in explosion.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                ps.Play();
            }
            Destroy(explosion, _tempExplosionDuration + 3f);
        }

        yield return new WaitForSecondsRealtime(_tempExplosionDuration);
        _explosionRoutine = null;
        NotifyBoss1ExplosionFinished();
    }

    private IEnumerator ExplosionTimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(_explosionTimeout);
        _explosionRoutine = null;
        Debug.LogWarning("[Boss2Intro] 보스1 폭발 끝 알림이 오지 않아 시간 초과로 진행합니다.");
        NotifyBoss1ExplosionFinished();
    }

    private void StopEffects(bool clear)
    {
        if (_gas != null)
        {
            _gas.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
        if (_cloud != null)
        {
            _cloud.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void ResetAnimatedValues()
    {
        _gasAmount = 0f;
        _darkness = 0f;
        _cloudAlpha = 0f;
        _flashAlpha = 0f;
        _blackout = 0f;
        _cameraDrop = 0f;
        _cameraShake = 0f;
    }

    #region 보스2 숨김·정렬

    private void ConcealBoss2()
    {
        if (_boss2 == null || _isBoss2Concealed)
            return;

        _isBoss2Concealed = true;
        _boss2RenderersAlreadyOff.Clear();
        foreach (Renderer r in _boss2Renderers)
        {
            if (r != null && r.forceRenderingOff)
            {
                _boss2RenderersAlreadyOff.Add(r);
            }
        }
        HideBoss2Renderers();
        _boss2.SetInvincible(true);
        _isBoss2FsmStopped = false;
    }

    private void HideBoss2Renderers()
    {
        foreach (Renderer r in _boss2Renderers)
        {
            if (r != null)
            {
                r.forceRenderingOff = true;
            }
        }
    }

    private void ShowBoss2()
    {
        if (_isBoss2Concealed == false)
            return;

        _isBoss2Concealed = false;
        foreach (Renderer r in _boss2Renderers)
        {
            if (r != null && _boss2RenderersAlreadyOff.Contains(r) == false)
            {
                r.forceRenderingOff = false;
            }
        }
    }

    // 보스1이 아직 그려지고 있다면 보스2 본체를 보스1과 같은 정렬 레이어의 더 뒤 순서로 둔다.
    private void PutBoss2BehindBoss1()
    {
        if (_boss2Body == null)
            return;

        if (IsBoss1Present == false || _isBoss2SortingChanged)
            return;

        int minOrder = int.MaxValue;
        int layer = 0;
        int minLayerValue = int.MaxValue;
        foreach (SpriteRenderer sr in _boss1.GetComponentsInChildren<SpriteRenderer>())
        {
            int layerValue = SortingLayer.GetLayerValueFromID(sr.sortingLayerID);
            if (layerValue < minLayerValue || (layerValue == minLayerValue && sr.sortingOrder < minOrder))
            {
                minLayerValue = layerValue;
                layer = sr.sortingLayerID;
                minOrder = sr.sortingOrder;
            }
        }

        if (minOrder == int.MaxValue)
            return;

        _boss2SortingLayer = _boss2Body.sortingLayerID;
        _boss2SortingOrder = _boss2Body.sortingOrder;
        _isBoss2SortingChanged = true;
        _boss2Body.sortingLayerID = layer;
        _boss2Body.sortingOrder = minOrder - 1;
    }

    private void RestoreBoss2Sorting()
    {
        if (_boss2Body == null || _isBoss2SortingChanged == false)
            return;

        _isBoss2SortingChanged = false;
        _boss2Body.sortingLayerID = _boss2SortingLayer;
        _boss2Body.sortingOrder = _boss2SortingOrder;
    }

    #endregion

    #region 조명·카메라

    private void CacheGlobalLights()
    {
        _globalLights.Clear();
        _globalLightIntensities.Clear();
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType == Light2D.LightType.Global && light.isActiveAndEnabled)
            {
                _globalLights.Add(light);
                _globalLightIntensities.Add(light.intensity);
            }
        }
    }

    private void RestoreGlobalLights()
    {
        for (int i = 0; i < _globalLights.Count; i++)
        {
            if (_globalLights[i] != null)
            {
                _globalLights[i].intensity = _globalLightIntensities[i];
            }
        }
        _globalLights.Clear();
        _globalLightIntensities.Clear();
    }

    private void CacheBrain()
    {
        if (_brain != null)
            return;

        _brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : FindAnyObjectByType<CinemachineBrain>();
        if (_brain != null)
        {
            _brainDefaultBlend = _brain.DefaultBlend;
        }
    }

    private void SetBrainBlend(CinemachineBlendDefinition blend, float restoreAfter)
    {
        CacheBrain();
        if (_brain == null)
            return;

        if (_restoreBlendRoutine != null)
        {
            StopCoroutine(_restoreBlendRoutine);
        }
        _brain.DefaultBlend = blend;
        _restoreBlendRoutine = StartCoroutine(RestoreBlendRoutine(restoreAfter));
    }

    private IEnumerator RestoreBlendRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (_brain != null)
        {
            _brain.DefaultBlend = _brainDefaultBlend;
        }
        _restoreBlendRoutine = null;
    }

    #endregion

    private void OnDisable()
    {
        if (_brain != null && _restoreBlendRoutine != null)
        {
            _brain.DefaultBlend = _brainDefaultBlend;
        }
        RestoreGlobalLights();
    }
}
