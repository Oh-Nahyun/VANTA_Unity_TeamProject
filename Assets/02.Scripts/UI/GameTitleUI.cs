using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameTitleUI : MonoBehaviour
{
    [Header("게임 시작 버튼")]
    [SerializeField] private Button _gameStartButton;

    [Header("게임 설정 버튼")]
    [SerializeField] private Button _gameSettingsButton;

    [Header("게임 종료 버튼")]
    [SerializeField] private Button _gameExitButton;

    [Header("게임 설정 패널")]
    [SerializeField] private GameObject _gameSettingPanel;

    [Header("타이틀 디졸브 효과용 클래스")]
    [SerializeField] TitleDissolveEffect _titleDissolveEffect;

    [Header("타이틀 페이드 아웃 효과용 클래스")]
    [SerializeField] TitleFadeInOutEffect _titleFadeInOutEffect;

    [Header("버튼에 마우스를 올렸을 때 효과음")]
    [SerializeField] private AudioClip _buttonHoverSound;

    [Header("버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonClickSound;

    [Header("게임 시작 버튼 클릭 효과음")]
    [SerializeField] private AudioClip _titleStartSound;

    [Header("인게임에서 설정 창을 열 때 재생할 타이틀 배경음악")]
    [SerializeField] private AudioClip _settingsBgm;

    /// <summary>
    /// 게임 재도전 시 타이틀 UI 건너뛰기를 위한 확인용 변수
    /// </summary>
    private bool _isSkipTitleEffect;

    /// <summary>
    /// 시작 버튼을 눌러 게임이 시작되었는지 확인용 변수
    /// </summary>
    private bool _isGameStarted;

    /// <summary>
    /// 게임 설정 패널 클래스
    /// </summary>
    private GameSettingsUI _gameSettingsUI;

    /// <summary>
    /// 인게임에서 설정 창을 열어 게임이 정지된 상태인지 확인용 변수
    /// </summary>
    private bool _isPaused;

    /// <summary>
    /// 게임 정지 직전의 타임 스케일
    /// </summary>
    private float _timeScaleBeforePause;

    /// <summary>
    /// 게임 정지 시 함께 멈춘 트윈 목록
    /// </summary>
    private readonly List<Tween> _pausedTweens = new List<Tween>();

    /// <summary>
    /// 타이틀 버튼 키보드 조작 가능 상태 확인용 변수
    /// </summary>
    private bool _isTitleNavigable;

    /// <summary>
    /// 키보드 조작 대상 타이틀 버튼 목록
    /// </summary>
    private Button[] _titleButtons;

    /// <summary>
    /// 메뉴 조작이 키보드 모드인지 여부 확인용 변수
    /// </summary>
    public static bool IsKeyboardMode { get; set; }

    /// <summary>
    /// 입력 모드를 마지막으로 갱신한 프레임
    /// </summary>
    private static int _inputModeFrame = -1;

    /// <summary>
    /// 이번 프레임에 키보드 모드로 막 전환되었는지 확인용 변수
    /// </summary>
    private static bool _isKeyboardModeJustSwitched;

    private void Start()
    {
        IsKeyboardMode = false;
        _gameSettingsUI = _gameSettingPanel.GetComponent<GameSettingsUI>();
        _titleButtons = new[] { _gameStartButton, _gameSettingsButton, _gameExitButton };

        foreach (Button button in _titleButtons)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        _gameStartButton.onClick.AddListener(OnClickStartButton);
        _gameSettingsButton.onClick.AddListener(OnClickSettingsButton);
        _gameExitButton.onClick.AddListener(OnClickExitButton);

        SoundGenerator.AddHoverSound(_gameStartButton, _buttonHoverSound);
        SoundGenerator.AddHoverSound(_gameSettingsButton, _buttonHoverSound);
        SoundGenerator.AddHoverSound(_gameExitButton, _buttonHoverSound);

        if (InGameSceneManager.IsRetry)
        {
            InGameSceneManager.IsRetry = false;
            StartCoroutine(StartGameWithoutTitleCoroutine());
        }
    }
    
    public static bool UpdateInputMode()
    {
        if (_inputModeFrame == Time.frameCount) return _isKeyboardModeJustSwitched;

        _inputModeFrame = Time.frameCount;
        _isKeyboardModeJustSwitched = false;

        if (IsNavigationKeyPressed())
        {
            _isKeyboardModeJustSwitched = !IsKeyboardMode;
            IsKeyboardMode = true;
        }
        else if (IsMouseActive())
        {
            IsKeyboardMode = false;
        }

        return _isKeyboardModeJustSwitched;
    }

    private static bool IsNavigationKeyPressed()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)
            || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
            || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        return keyboard != null
            && (keyboard.wKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame
                || keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame
                || keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame
                || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
    }

    private static bool IsMouseActive()
    {
        if (Input.mousePositionDelta.sqrMagnitude > 0f || Input.GetMouseButtonDown(0)) return true;

        var mouse = UnityEngine.InputSystem.Mouse.current;

        return mouse != null
            && (mouse.delta.ReadValue().sqrMagnitude > 0f || mouse.leftButton.wasPressedThisFrame);
    }

    private void LateUpdate()
    {
        if (_isGameStarted) return;

        if (transform.GetChild(0).gameObject.activeSelf)
        {
            Cursor.visible = !IsKeyboardMode;
        }
    }

    private void UpdateTitleNavigation()
    {
        bool isNavigable = !_isGameStarted
                           && transform.GetChild(0).gameObject.activeSelf
                           && !_gameSettingPanel.activeSelf;

        if (isNavigable != _isTitleNavigable)
        {
            _isTitleNavigable = isNavigable;

            if (isNavigable && IsKeyboardMode)
            {
                EventSystem.current.SetSelectedGameObject(_gameStartButton.gameObject);
            }
        }

        if (!isNavigable) return;

        bool isJustSwitched = UpdateInputMode();
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (!IsKeyboardMode)
        {
            if (selected != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return;
        }

        if (isJustSwitched || selected == null)
        {
            EventSystem.current.SetSelectedGameObject(_gameStartButton.gameObject);
            return;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        bool isUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                    || (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame));
        bool isDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)
                      || (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame));

        if (!isUp && !isDown) return;

        int index = System.Array.FindIndex(_titleButtons, button => button.gameObject == selected);
        index = Mathf.Clamp(index + (isDown ? 1 : -1), 0, _titleButtons.Length - 1);

        EventSystem.current.SetSelectedGameObject(_titleButtons[index].gameObject);
    }

    private void Update()
    {
        UpdateTitleNavigation();
        UpdatePause();

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_gameSettingPanel.activeSelf)
        {
            _gameSettingsUI.CloseSettings();
            UpdatePause();
            return;
        }

        if (transform.GetChild(0).gameObject.activeSelf) return;

        if (GameResultUI.IsResultShown) return;

        IsKeyboardMode = false;
        _gameSettingPanel.SetActive(true);
        PauseGame();
    }

    private void PauseGame()
    {
        _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        _isPaused = true;
        AudioListener.pause = true;
        SoundManager.Instance.PlaySettingsBGM(_settingsBgm);

        _pausedTweens.Clear();
        DOTween.PlayingTweens(_pausedTweens);

        foreach (Tween tween in _pausedTweens)
        {
            tween.Pause();
        }
    }

    private void ResumeGame()
    {
        Time.timeScale = _timeScaleBeforePause;
        _isPaused = false;
        AudioListener.pause = false;
        SoundManager.Instance.StopSettingsBGM();

        foreach (Tween tween in _pausedTweens)
        {
            if (tween.active)
            {
                tween.Play();
            }
        }

        _pausedTweens.Clear();
    }

    private void OnDestroy()
    {
        if (_isPaused)
        {
            ResumeGame();
        }
    }

    private void UpdatePause()
    {
        if (!_isPaused) return;

        if (!_gameSettingPanel.activeSelf)
        {
            ResumeGame();
            return;
        }

        if (Time.timeScale != 0f)
        {
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void OnClickStartButton()
    {
        if (_isSkipTitleEffect || _isGameStarted) return;

        _isGameStarted = true;
        SoundManager.Instance.PlayEffectSound(_titleStartSound);
        StartCoroutine(DestroyTitleUICoroutine());
    }

    private void OnClickSettingsButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        _gameSettingPanel.active = true;
    }

    public void OnClickExitButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        StartCoroutine(ExitAfterFadeOutCoroutine());
    }

    private IEnumerator StartGameWithoutTitleCoroutine()
    {
        transform.GetChild(1).gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(false);

        yield return null;

        _isSkipTitleEffect = true;
        _gameStartButton.onClick.Invoke();
        _isSkipTitleEffect = false;
    }

    private IEnumerator DestroyTitleUICoroutine()
    {
        _titleDissolveEffect.PlayButtonDissolve();
        yield return new WaitForSeconds(_titleDissolveEffect.ButtonDissolveDuration);
        transform.GetChild(1).gameObject.SetActive(false);

        _titleDissolveEffect.PlayTitleDissolve();
        yield return new WaitForSeconds(_titleDissolveEffect.TitleDissolveDuration);
        transform.GetChild(0).gameObject.SetActive(false);
    }

    private IEnumerator ExitAfterFadeOutCoroutine()
    {
        _titleFadeInOutEffect.FadeOut();

        yield return new WaitForSecondsRealtime(_titleFadeInOutEffect.FadeDuration);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
