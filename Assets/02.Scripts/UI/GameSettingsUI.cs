using UnityEngine;
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

    [Header("버튼에 마우스를 올렸을 때 효과음")]
    [SerializeField] private AudioClip _buttonHoverSound;

    [Header("뒤로 가기 버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonBackSound;

    [Header("설정 창이 열릴 때 효과음")]
    [SerializeField] private AudioClip _settingsOpenSound;

    [Header("설정 창이 닫힐 때 효과음")]
    [SerializeField] private AudioClip _settingsCloseSound;

    [Header("슬라이더에서 손을 뗐을 때 효과음")]
    [SerializeField] private AudioClip _sliderTickSound;

    private void OnEnable()
    {
        SoundManager.Instance.PlayEffectSound(_settingsOpenSound);
    }

    private void Start()
    {
        _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxVolumeSlider.value = PlayerPrefs.GetFloat("EffectVolume", 1f);

        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        _goBackButton.onClick.AddListener(OnClickGoBackButton);

        SoundGenerator.AddHoverSound(_goBackButton, _buttonHoverSound);
        SoundGenerator.AddReleaseSound(_masterVolumeSlider, _sliderTickSound);
        SoundGenerator.AddReleaseSound(_bgmVolumeSlider, _sliderTickSound);
        SoundGenerator.AddReleaseSound(_sfxVolumeSlider, _sliderTickSound);
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

    private void OnClickGoBackButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonBackSound);
        SoundManager.Instance.PlayEffectSound(_settingsCloseSound);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
