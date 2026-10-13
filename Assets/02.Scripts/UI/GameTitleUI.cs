using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
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

    private void Start()
    {
        _gameSettingsUI = _gameSettingPanel.GetComponent<GameSettingsUI>();

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

    private void Update()
    {
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
        if (_isSkipTitleEffect) return;

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
        yield return new WaitForSeconds(_titleFadeInOutEffect.FadeDuration);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
