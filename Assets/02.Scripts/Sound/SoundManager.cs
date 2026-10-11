using System.Collections.Generic;
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
    private readonly Dictionary<AudioSource, float> _loopVolumeScales = new Dictionary<AudioSource, float>();
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
        PlayEffectSound(clip, 1f);
    }

    public void PlayEffectSound(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        _effectSource.pitch = Random.Range(_minPitch, _maxPitch);
        _effectSource.PlayOneShot(clip, volumeScale);
    }

    public AudioSource PlayLoopSound(AudioClip clip, float volumeScale, float fadeInDuration)
    {
        if (clip == null) return null;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.Play();

        _loopVolumeScales[source] = volumeScale;

        DOTween
            .To(() => source.volume, value => source.volume = value, GetLoopVolume(volumeScale), fadeInDuration)
            .SetTarget(source);

        return source;
    }

    public void StopLoopSound(AudioSource source, float fadeOutDuration)
    {
        if (source == null)
            return;

        DOTween.Kill(source);
        _loopVolumeScales.Remove(source);

        if (fadeOutDuration <= 0f)
        {
            Destroy(source);
            return;
        }

        DOTween
            .To(() => source.volume, value => source.volume = value, 0f, fadeOutDuration)
            .SetTarget(source)
            .OnComplete(() => Destroy(source));
    }

    private float GetLoopVolume(float volumeScale)
    {
        return _effectVolume * _masterVolume * volumeScale;
    }

    private void RefreshLoopVolumes()
    {
        foreach (KeyValuePair<AudioSource, float> loop in _loopVolumeScales)
        {
            if (loop.Key == null) continue;

            loop.Key.volume = GetLoopVolume(loop.Value);
        }
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
        RefreshLoopVolumes();

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
        RefreshLoopVolumes();

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
