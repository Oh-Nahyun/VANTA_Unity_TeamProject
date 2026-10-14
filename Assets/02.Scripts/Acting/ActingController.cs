using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(ActingScreenFade))]
public class ActingController : MonoBehaviour
{
    [SerializeField] private ActingScreenFade _screenFade;
    [Header("연출 중 표시할 텍스트")]
    [SerializeField] private GameObject _skipGuideText;
    [SerializeField] private Camera _skipGuideCamera;
    [Tooltip("카메라 화면 비율 좌표. (0, 0)은 왼쪽 아래, (1, 1)은 오른쪽 위")]
    [SerializeField] private Vector2 _skipGuideViewportPosition = new Vector2(0.5f, 0.08f);
    [Tooltip("화면 높이 1080 기준 글자 크기")]
    [SerializeField, Min(1f)] private float _skipGuideFontSize = 28f;
    [Tooltip("월드 TMP를 카메라 앞에 배치할 거리")]
    [SerializeField, Min(0.1f)] private float _skipGuideCameraDistance = 10f;
    [Header("보스 참조 (미지정 시 씬에서 검색)")]
    [SerializeField] private Boss1IntroController _boss1Intro;
    [SerializeField] private Boss1DeathController _boss1Death;
    [SerializeField] private Boss2 _boss2;
    [SerializeField] private BossHealthFadeInEffect _bossHealth;

    [Header("컷신 시작시 호출될 이벤트")]

    [SerializeField] private UnityEvent _skipCutScene;

    private GameStartActing _gameStart;
    private Phase1ToPhase2Acting _phaseChange;
    private Coroutine _skipRoutine;
    private Boss2Controller _boss2Controller;
    private bool _boss2ControllerWasEnabled;
    private TMP_Text _skipGuideLabel;

    public bool IsSkipping { get; private set; }

    private void Awake()
    {
        SetSkipGuideVisible(false);
        if (_screenFade == null)
            _screenFade = GetComponent<ActingScreenFade>();
        if (_screenFade == null)
            _screenFade = gameObject.AddComponent<ActingScreenFade>();
    }

    private void Update()
    {
        if (!RetryStatus.IsRetry)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartShortCut();
        }
    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        Canvas.willRenderCanvases += UpdateSkipGuideLayout;
    }


    public void SetRetryStatus()
    {
        RetryStatus.SetRetry();
    }
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        Camera target = _skipGuideCamera != null ? _skipGuideCamera : Camera.main;
        if (camera == target)
            UpdateSkipGuideLayout();
    }

    private void LateUpdate()
    {
        if (!IsSkipping)
            return;
        GetComponent<PlayerStatusManager>().PlayerControlOff();
        GetComponent<Player>()?.OffPlayerHittable();
    }

    public void BeginGameStart(GameStartActing acting)
    {
        if (IsSkipping)
            return;

        _gameStart = acting;
        _phaseChange = null;
        SetSkipGuideVisible(acting != null);
    }

    public void BeginPhaseChange(Phase1ToPhase2Acting acting)
    {
        if (IsSkipping)
            return;

        _phaseChange = acting;
        _gameStart = null;
        SetSkipGuideVisible(acting != null);
    }

    public void StopActing()
    {
        if (IsSkipping)
            return;

        _gameStart = null;
        _phaseChange = null;
        SetSkipGuideVisible(false);
    }

    public void StartShortCut()
    {
        
        if (!RetryStatus.IsRetry)
            return;

        if (IsSkipping ||
            (_gameStart == null && _phaseChange == null))
        {
            return;
        }

        if (_phaseChange != null && _phaseChange.BossIntro == null)
        {
            Debug.LogWarning("보스2 인트로 참조가 없어 스킵할 수 없습니다.", this);
            return;
        }

        _skipCutScene.Invoke();
        IsSkipping = true;
        SetSkipGuideVisible(false);

        _gameStart?.CancelForSkip();
        _phaseChange?.CancelForSkip();

        _skipRoutine = StartCoroutine(SkipRoutine());
    }

    private IEnumerator SkipRoutine()
    {
        yield return _screenFade.FadeOut();

        Time.timeScale = 1f;
        ClearActingLabels();
        if (_phaseChange != null)
            PreparePhase2();
        else
            PrepareGameStart();

        // 카메라와 렌더러가 갱신된 뒤 화면을 연다.
        yield return null;
        if (_bossHealth == null)
            _bossHealth = FindAnyObjectByType<BossHealthFadeInEffect>();
        _bossHealth?.ShowImmediately();
        yield return _screenFade.FadeIn();

        if (_phaseChange != null)
        {
            RestoreBossController();
            _phaseChange.BossIntro.StartBattle();
            _phaseChange.CompleteAfterSkip();
        }
        else
        {
            _gameStart.CompleteAfterSkip();
        }

        GetComponent<Player>()?.OnPlayerHittable();

        _gameStart = null;
        _phaseChange = null;
        IsSkipping = false;
        _skipRoutine = null;
    }

    private void PrepareGameStart()
    {
        if (_boss1Intro == null)
            _boss1Intro = FindAnyObjectByType<Boss1IntroController>();

        if (_boss1Intro != null)
        {
            _boss1Intro.StopAllCoroutines();
            _boss1Intro.RemoveWarningZone();
            _boss1Intro.EndRoar();
            _boss1Intro.EndIntroClawEnd();
            foreach (SpriteRenderer sprite in _boss1Intro.GetComponentsInChildren<SpriteRenderer>(true))
                sprite.forceRenderingOff = false;
        }

        _gameStart.ApplySkippedState();
    }

    private void PreparePhase2()
    {
        Boss2IntroCinematic intro = _phaseChange.BossIntro;
        // 현재 Replay는 재생하지 않고 Timeline·소개·왕좌를 정리한다.
        intro.Replay();

        if (_boss1Death == null)
            _boss1Death = FindAnyObjectByType<Boss1DeathController>();
        if (_boss2 == null)
            _boss2 = FindAnyObjectByType<Boss2>(FindObjectsInactive.Include);

        if (_boss2 != null)
        {
            _boss2Controller = _boss2.GetComponent<Boss2Controller>();
            if (_boss2Controller != null)
            {
                _boss2ControllerWasEnabled = _boss2Controller.enabled;
                _boss2Controller.enabled = false;
            }
        }

        if (_boss1Death != null)
        {
            if (_boss2 != null)
                _boss2.transform.position = _boss1Death.transform.position;

            // 지연된 사망 이벤트가 인트로를 다시 시작하지 않게 한다.
            _boss1Death.gameObject.SetActive(false);
            Destroy(_boss1Death.gameObject);
        }

        intro.RevealBoss2();
        _boss2Controller?.StopFsm();
        _phaseChange.ApplySkippedState();
    }

    private void RestoreBossController()
    {
        if (_boss2Controller != null)
            _boss2Controller.enabled = _boss2ControllerWasEnabled;
        _boss2Controller = null;
    }

    private void ClearActingLabels()
    {
        foreach (NameActingController name in FindObjectsByType<NameActingController>(FindObjectsSortMode.None))
            name.FadeOutText();

        GetComponent<PlayerEmotionContoller>()?.ClearEmotion();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Canvas.willRenderCanvases -= UpdateSkipGuideLayout;
        SetSkipGuideVisible(false);
        if (_skipRoutine != null)
            StopCoroutine(_skipRoutine);

        _skipRoutine = null;
        RestoreBossController();
        _gameStart = null;
        _phaseChange = null;
        if (IsSkipping)
            Time.timeScale = 1f;
        IsSkipping = false;
        _screenFade?.Clear();
    }

    private void SetSkipGuideVisible(bool visible)
    {
        
        visible = visible && RetryStatus.IsRetry;

        
        if (_skipGuideText != null && _skipGuideText.activeSelf != visible)
        {
            _skipGuideText.SetActive(visible);
        }

        if (visible)
        {
            UpdateSkipGuideLayout();
        }
    }

    private void UpdateSkipGuideLayout()
    {
        if (_skipGuideText == null || !_skipGuideText.activeInHierarchy)
            return;
        if (_skipGuideLabel == null)
            _skipGuideLabel = _skipGuideText.GetComponentInChildren<TMP_Text>(true);
        Camera camera = _skipGuideCamera != null ? _skipGuideCamera : Camera.main;
        if (_skipGuideLabel == null || camera == null || camera.pixelHeight <= 0)
            return;

        RectTransform rect = _skipGuideLabel.rectTransform;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Vector3 viewport = new Vector3(Mathf.Clamp01(_skipGuideViewportPosition.x),
            Mathf.Clamp01(_skipGuideViewportPosition.y),
            Mathf.Clamp(_skipGuideCameraDistance, camera.nearClipPlane + 0.01f, camera.farClipPlane));
        Camera projectionCamera = camera;

        if (_skipGuideLabel is TextMeshProUGUI)
        {
            Canvas canvas = _skipGuideLabel.canvas;
            if (canvas == null || !(rect.parent is RectTransform parent))
                return;
            Canvas root = canvas.rootCanvas;
            projectionCamera = root.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : (root.worldCamera != null ? root.worldCamera : camera);
            Vector2 screenPoint = camera.ViewportToScreenPoint(viewport);
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, screenPoint, projectionCamera, out Vector3 position))
            {
                return;
            }

            rect.position = position;
        }
        else
        {
            rect.position = camera.ViewportToWorldPoint(viewport);
            rect.rotation = camera.transform.rotation;
        }

        // 실제 투영 크기를 사용해 CanvasScaler, 부모 스케일, 카메라 줌을 함께 보정한다.
        Vector2 origin = RectTransformUtility.WorldToScreenPoint(projectionCamera, rect.position);
        float pixelsPerUnitY = Vector2.Distance(origin,
            RectTransformUtility.WorldToScreenPoint(projectionCamera, rect.TransformPoint(Vector3.up)));
        float pixelsPerUnitX = Vector2.Distance(origin,
            RectTransformUtility.WorldToScreenPoint(projectionCamera, rect.TransformPoint(Vector3.right)));
        if (pixelsPerUnitY < 0.001f || pixelsPerUnitX < 0.001f)
            return;

        float fontMultiplier = _skipGuideLabel.isOrthographic ? 1f : 0.1f;
        _skipGuideLabel.enableAutoSizing = false;
        _skipGuideLabel.fontSize = _skipGuideFontSize * camera.pixelHeight / 1080f
            / pixelsPerUnitY / fontMultiplier;
        _skipGuideLabel.alignment = TextAlignmentOptions.Center;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, camera.pixelWidth * 0.8f / pixelsPerUnitX);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, camera.pixelHeight * 0.1f / pixelsPerUnitY);
    }
}
