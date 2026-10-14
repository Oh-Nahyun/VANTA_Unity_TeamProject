using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("게임 마스터 볼륨 설정용 슬라이더")]
    [SerializeField] private Slider _masterVolumeSlider;

    [Header("게임 BGM 볼륨 설정용 슬라이더")]
    [SerializeField] private Slider _bgmVolumeSlider;

    [Header("게임 SFX 볼륨 설정용 슬라이더")]
    [SerializeField] private Slider _sfxVolumeSlider;

    [Header("마스터 볼륨 퍼센트 텍스트")]
    [SerializeField] private TMP_Text _masterVolumeText;

    [Header("BGM 볼륨 퍼센트 텍스트")]
    [SerializeField] private TMP_Text _bgmVolumeText;

    [Header("SFX 볼륨 퍼센트 텍스트")]
    [SerializeField] private TMP_Text _sfxVolumeText;

    [Header("게임 설정 패널 나가리 버튼")]
    [SerializeField] private Button _goBackButton;

    [Header("설정 창 패널 버튼")]
    [SerializeField] private Button[] _tabButtons;

    [Header("설정 창 게임 패널 버튼")]
    [SerializeField] private Button[] _gamePanelButtons;

    [Header("현재 선택된 패널 표시 이미지")]
    [SerializeField] private RectTransform _selectedImage;

    [Header("선택 표시 이미지 이동 시간")]
    [SerializeField, Min(0f)] private float _selectedImageMoveDuration = 0.2f;

    [Header("버튼에 마우스를 올렸을 때 효과음")]
    [SerializeField] private AudioClip _buttonHoverSound;

    [Header("버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonClickSound;

    [Header("뒤로 가기 버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonBackSound;

    [Header("설정 창이 열릴 때 효과음")]
    [SerializeField] private AudioClip _settingsOpenSound;

    [Header("설정 창이 닫힐 때 효과음")]
    [SerializeField] private AudioClip _settingsCloseSound;

    [Header("슬라이더에서 손을 뗐을 때 효과음")]
    [SerializeField] private AudioClip _sliderTickSound;

    /// <summary>
    /// 설정 창을 열기 직전의 마우스 커서 표시 여부
    /// </summary>
    private bool _cursorVisibleBeforeOpen;

    /// <summary>
    /// 현재 표시 중인 탭 버튼 (키보드 모드 진입 시 선택 복구용)
    /// </summary>
    private Button _currentTab;

    /// <summary>
    /// 탭 전환 시 클릭 효과음 생략 여부 확인용 변수
    /// </summary>
    private bool _isClickSoundMuted;

    /// <summary>
    /// 선택 표시 이미지 이동 트윈
    /// </summary>
    private Tween _selectedImageTween;

    private void OnEnable()
    {
        _cursorVisibleBeforeOpen = Cursor.visible;
        Cursor.visible = !GameTitleUI.IsKeyboardMode;

        _currentTab = _tabButtons[0];
        ShowTab(_currentTab, false);

        EventSystem.current.SetSelectedGameObject(GameTitleUI.IsKeyboardMode ? _currentTab.gameObject : null);

        SoundManager.Instance.PlayEffectSound(_settingsOpenSound);

        _selectedImageTween?.Kill();
        StartCoroutine(SnapSelectedImageNextFrame());
    }

    private IEnumerator SnapSelectedImageNextFrame()
    {
        yield return null;

        MoveSelectedImage(_tabButtons[0], true);
    }

    private void MoveSelectedImage(Button tab, bool isInstant)
    {
        _selectedImageTween?.Kill();

        float targetY = _selectedImage.parent.InverseTransformPoint(tab.transform.position).y;

        if (isInstant)
        {
            Vector3 position = _selectedImage.localPosition;
            position.y = targetY;
            _selectedImage.localPosition = position;
            return;
        }

        _selectedImageTween = _selectedImage
            .DOLocalMoveY(targetY, _selectedImageMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void ShowTab(Button tab, bool playSound)
    {
        _isClickSoundMuted = !playSound;
        tab.onClick.Invoke();
        _isClickSoundMuted = false;
    }

    private void OnDisable()
    {
        _selectedImageTween?.Kill();
        Cursor.visible = _cursorVisibleBeforeOpen;
    }

    private void Update()
    {
        bool isJustSwitched = GameTitleUI.UpdateInputMode();
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (!GameTitleUI.IsKeyboardMode)
        {
            if (selected != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return;
        }

        if (isJustSwitched || selected == null)
        {
            EventSystem.current.SetSelectedGameObject(_currentTab.gameObject);
            return;
        }

        int direction = GameTitleUI.GetVerticalInput();

        if (direction == 0) return;

        int index = System.Array.IndexOf(_tabButtons, _currentTab);
        int nextIndex = Mathf.Clamp(index + direction, 0, _tabButtons.Length - 1);

        if (nextIndex == index) return;

        EventSystem.current.SetSelectedGameObject(_tabButtons[nextIndex].gameObject);
        ShowTab(_tabButtons[nextIndex], true);
    }

    private void LateUpdate()
    {
        Cursor.visible = !GameTitleUI.IsKeyboardMode;
    }

    private void Start()
    {
        _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxVolumeSlider.value = PlayerPrefs.GetFloat("EffectVolume", 1f);

        UpdateVolumeText(_masterVolumeText, _masterVolumeSlider.value);
        UpdateVolumeText(_bgmVolumeText, _bgmVolumeSlider.value);
        UpdateVolumeText(_sfxVolumeText, _sfxVolumeSlider.value);

        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        _goBackButton.onClick.AddListener(CloseSettings);

        SoundGenerator.AddHoverSound(_goBackButton, _buttonHoverSound);

        foreach (Button button in _tabButtons)
        {
            SoundGenerator.AddHoverSound(button, _buttonHoverSound);
            button.onClick.AddListener(PlayClickSound);

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Button tab = button;
            button.onClick.AddListener(() =>
            {
                _currentTab = tab;
                MoveSelectedImage(tab, false);
            });
        }

        foreach (Button button in _gamePanelButtons)
        {
            SoundGenerator.AddHoverSound(button, _buttonHoverSound);
        }

        SoundGenerator.AddReleaseSound(_masterVolumeSlider, _sliderTickSound);
        SoundGenerator.AddReleaseSound(_bgmVolumeSlider, _sliderTickSound);
        SoundGenerator.AddReleaseSound(_sfxVolumeSlider, _sliderTickSound);
    }

    private void PlayClickSound()
    {
        if (_isClickSoundMuted) return;

        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
    }

    private void UpdateVolumeText(TMP_Text text, float volume)
    {
        text.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    public void SetMasterVolume(float volume)
    {
        SoundManager.Instance.SetMasterVolume(volume);
        UpdateVolumeText(_masterVolumeText, volume);
    }

    public void SetBGMVolume(float volume)
    {
        SoundManager.Instance.SetBGMVolume(volume);
        UpdateVolumeText(_bgmVolumeText, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SoundManager.Instance.SetEffectVolume(volume);
        UpdateVolumeText(_sfxVolumeText, volume);
    }

    public void CloseSettings()
    {
        SoundManager.Instance.PlayEffectSound(_buttonBackSound);
        SoundManager.Instance.PlayEffectSound(_settingsCloseSound);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
