using DG.Tweening;
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
    /// 직전 프레임에 선택되어있던 UI
    /// </summary>
    private GameObject _lastSelected;

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
        Cursor.visible = true;

        SelectTab(_tabButtons[0], false);
        MoveSelectedImage(_tabButtons[0], true);
        SoundManager.Instance.PlayEffectSound(_settingsOpenSound);
    }

    private void MoveSelectedImage(Button tab, bool isInstant)
    {
        _selectedImageTween?.Kill();

        float targetY = tab.transform.position.y;

        if (isInstant)
        {
            Vector3 position = _selectedImage.position;
            position.y = targetY;
            _selectedImage.position = position;
            return;
        }

        _selectedImageTween = _selectedImage
            .DOMoveY(targetY, _selectedImageMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void SelectTab(Button tab, bool playSound)
    {
        EventSystem.current.SetSelectedGameObject(tab.gameObject);
        _lastSelected = tab.gameObject;

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
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
            {
                SelectTab(_tabButtons[0], true);
            }

            _lastSelected = null;
            return;
        }

        if (selected == _lastSelected) return;

        _lastSelected = selected;

        if (Input.GetMouseButton(0)) return;

        foreach (Button tab in _tabButtons)
        {
            if (tab.gameObject == selected)
            {
                tab.onClick.Invoke();
                return;
            }
        }
    }

    private void LateUpdate()
    {
        Cursor.visible = true;
    }

    private void Start()
    {
        _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxVolumeSlider.value = PlayerPrefs.GetFloat("EffectVolume", 1f);

        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        _goBackButton.onClick.AddListener(CloseSettings);

        SoundGenerator.AddHoverSound(_goBackButton, _buttonHoverSound);

        foreach (Button button in _tabButtons)
        {
            SoundGenerator.AddHoverSound(button, _buttonHoverSound);
            button.onClick.AddListener(PlayClickSound);

            Button tab = button;
            button.onClick.AddListener(() => MoveSelectedImage(tab, false));
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

    public void SetMasterVolume(float volume)
    {
        SoundManager.Instance.SetMasterVolume(volume);
    }

    public void SetBGMVolume(float volume)
    {
        SoundManager.Instance.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        SoundManager.Instance.SetEffectVolume(volume);
    }

    public void CloseSettings()
    {
        SoundManager.Instance.PlayEffectSound(_buttonBackSound);
        SoundManager.Instance.PlayEffectSound(_settingsCloseSound);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
