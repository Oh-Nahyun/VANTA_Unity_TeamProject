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

    private void Start()
    {
        _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxVolumeSlider.value = PlayerPrefs.GetFloat("EffectVolume", 1f);

        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        _goBackButton.onClick.AddListener(OnClickGoBackButton);
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
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
