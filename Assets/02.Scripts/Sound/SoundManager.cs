using DG.Tweening;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource _bgmSource;

    [SerializeField] private AudioSource _effectSource;

    [Header("BGM")]
    [SerializeField] private float _fadeDuration = 1f;

    [Header("Effect Pitch")]
    [SerializeField] private float _minPitch = 0.95f;

    [SerializeField] private float _maxPitch = 1.05f;

    [Header("Volume")]
    [SerializeField] private float _masterVolume = 1f;

    [SerializeField] private float _bgmVolume = 1f;
    [SerializeField] private float _effectVolume = 1f;

    private Tween _bgmFadeTween;
    private float _bgmFadeValue = 1f;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadVolume();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        _bgmFadeTween?.Kill();

        if (_bgmSource.isPlaying)
        {
            _bgmFadeTween = DOTween
                .To(
                    () => _bgmFadeValue,
                    value =>
                    {
                        _bgmFadeValue = value;
                        _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
                    },
                    0f,
                    _fadeDuration
                )
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _bgmSource.clip = clip;
                    _bgmSource.loop = true;
                    _bgmFadeValue = 0f;
                    _bgmSource.Play();
                    _bgmFadeTween = DOTween
                        .To(
                            () => _bgmFadeValue,
                            value =>
                            {
                                _bgmFadeValue = value;
                                _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
                            },
                            1f,
                            _fadeDuration
                        )
                        .SetUpdate(true);
                });

            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmFadeValue = 0f;
        _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
        _bgmSource.Play();
        _bgmFadeTween = DOTween
            .To(
                () => _bgmFadeValue,
                value =>
                {
                    _bgmFadeValue = value;
                    _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
                },
                1f,
                _fadeDuration
            )
            .SetUpdate(true);
    }

    public void StopBGM()
    {
        if (!_bgmSource.isPlaying) return;

        _bgmFadeTween?.Kill();
        _bgmFadeTween = DOTween
            .To(
                () => _bgmFadeValue,
                value =>
                {
                    _bgmFadeValue = value;
                    _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
                },
                0f,
                _fadeDuration
            )
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
                _bgmFadeValue = 1f;
            });
    }

    public void PlayEffectSound(AudioClip clip)
    {
        if (clip == null)
            return;

        _effectSource.pitch = Random.Range(
            _minPitch,
            _maxPitch
        );

        _effectSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        _bgmFadeTween?.Kill();
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = volume;
        _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;
        _effectSource.volume = _effectVolume * _masterVolume;

        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = volume;
        _bgmSource.volume = _bgmVolume * _masterVolume * _bgmFadeValue;

        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    public void SetEffectVolume(float volume)
    {
        _effectVolume = volume;
        _effectSource.volume = _effectVolume * _masterVolume;

        PlayerPrefs.SetFloat("EffectVolume", volume);
    }

    private void LoadVolume()
    {
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _effectVolume = PlayerPrefs.GetFloat("EffectVolume", 1f);

        _bgmFadeValue = 1f;
        _bgmSource.volume = _bgmVolume * _masterVolume;
        _effectSource.volume = _effectVolume * _masterVolume;
    }
}
