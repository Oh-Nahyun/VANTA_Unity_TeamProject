using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _bgmSubSource;
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
    [Header("BGM Slow")]
    [SerializeField, Range(0.1f, 1f)]
    private float _slowPitch = 0.5f;

    [SerializeField, Min(0f)]
    private float _slowDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float _restoreDuration = 0.3f;

    private Tween _bgmSlowTween;
    private float _currentBgmPitch = 1f;
    private Tween _bgmFadeTween;

    private readonly Dictionary<AudioSource, float>
        _loopVolumeScales =
            new Dictionary<AudioSource, float>();

    private AudioSource _currentBgmSource;
    private AudioSource _nextBgmSource;

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

        _currentBgmSource = _bgmSource;
        _nextBgmSource = _bgmSubSource;

        LoadVolume();

        if (_bgmSubSource != null)
        {
            _bgmSubSource.volume = 0f;
            _bgmSubSource.loop = true;
        }
    }

    // =========================
    // BGM 최초 재생
    // =========================

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
            return;

        _bgmFadeTween?.Kill();

        // 이미 같은 음악이면 무시
        if (_currentBgmSource.clip == clip &&
            _currentBgmSource.isPlaying)
        {
            return;
        }

        _currentBgmSource.clip = clip;
        _currentBgmSource.loop = true;

        _bgmFadeValue = 0f;
        _currentBgmSource.volume = 0f;

        _currentBgmSource.Play();

        _bgmFadeTween = DOTween
            .To(
                () => _bgmFadeValue,
                value =>
                {
                    _bgmFadeValue = value;

                    _currentBgmSource.volume =
                        GetBGMVolume() *
                        _bgmFadeValue;
                },
                1f,
                _fadeDuration
            )
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmFadeTween = null;
            });
    }

    // =========================
    // BGM 크로스 페이드
    // =========================

    public void ChangeBGM(AudioClip clip)
    {
        if (clip == null)
            return;

        // 현재 같은 BGM 재생 중
        if (_currentBgmSource.clip == clip &&
            _currentBgmSource.isPlaying)
        {
            return;
        }

        _bgmFadeTween?.Kill();

        // 현재 BGM이 없다면 일반 재생
        if (!_currentBgmSource.isPlaying)
        {
            PlayBGM(clip);
            return;
        }

        AudioSource oldSource = _currentBgmSource;
        AudioSource newSource = _nextBgmSource;

        // 새 음악 준비
        newSource.Stop();
        newSource.clip = clip;
        newSource.loop = true;
        newSource.volume = 0f;
        newSource.Play();

        float fadeValue = 0f;

        _bgmFadeTween = DOTween
            .To(
                () => fadeValue,
                value =>
                {
                    fadeValue = value;

                    float volume =
                        GetBGMVolume();

                    // 기존 음악 감소
                    oldSource.volume =
                        volume * (1f - value);

                    // 새로운 음악 증가
                    newSource.volume =
                        volume * value;
                },
                1f,
                _fadeDuration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                oldSource.Stop();
                oldSource.clip = null;
                oldSource.volume = 0f;

                newSource.volume =
                    GetBGMVolume();

                // 두 AudioSource 역할 교환
                _currentBgmSource = newSource;
                _nextBgmSource = oldSource;

                _bgmFadeValue = 1f;
                _bgmFadeTween = null;
            });
    }

    // =========================
    // BGM 정지
    // =========================

    public void StopBGM()
    {
        _bgmFadeTween?.Kill();
        _bgmFadeTween = null;

        float currentStartVolume =
            _currentBgmSource != null
                ? _currentBgmSource.volume
                : 0f;

        float nextStartVolume =
            _nextBgmSource != null
                ? _nextBgmSource.volume
                : 0f;

        float fadeValue = 0f;

        _bgmFadeTween = DOTween
            .To(
                () => fadeValue,
                value =>
                {
                    fadeValue = value;

                    if (_currentBgmSource != null)
                    {
                        _currentBgmSource.volume =
                            Mathf.Lerp(
                                currentStartVolume,
                                0f,
                                value
                            );
                    }

                    if (_nextBgmSource != null)
                    {
                        _nextBgmSource.volume =
                            Mathf.Lerp(
                                nextStartVolume,
                                0f,
                                value
                            );
                    }
                },
                1f,
                _fadeDuration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_currentBgmSource != null)
                {
                    _currentBgmSource.Stop();
                    _currentBgmSource.clip = null;
                    _currentBgmSource.volume = 0f;
                }

                if (_nextBgmSource != null)
                {
                    _nextBgmSource.Stop();
                    _nextBgmSource.clip = null;
                    _nextBgmSource.volume = 0f;
                }

                _bgmFadeValue = 1f;
                _bgmFadeTween = null;
            });
    }

    // =========================
    // Effect
    // =========================

    public void PlayEffectSound(
        AudioClip clip)
    {
        PlayEffectSound(
            clip,
            1f
        );
    }
    public void StopBGMImmediate()
    {
        // 진행 중인 페이드 / 크로스페이드 종료
        _bgmFadeTween?.Kill();
        _bgmFadeTween = null;

        // 현재 BGM 즉시 정지
        if (_currentBgmSource != null)
        {
            _currentBgmSource.Stop();
            _currentBgmSource.clip = null;
            _currentBgmSource.volume = 0f;
        }

        // 크로스페이드 중일 수 있으므로
        // 반대쪽 Source도 즉시 정지
        if (_nextBgmSource != null)
        {
            _nextBgmSource.Stop();
            _nextBgmSource.clip = null;
            _nextBgmSource.volume = 0f;
        }

        _bgmFadeValue = 1f;
    }
    public void PlayEffectSound(
        AudioClip clip,
        float volumeScale)
    {
        if (clip == null)
            return;

        _effectSource.pitch =
            Random.Range(
                _minPitch,
                _maxPitch
            );

        _effectSource.PlayOneShot(
            clip,
            volumeScale
        );
    }

    // =========================
    // Loop Effect
    // =========================

    public AudioSource PlayLoopSound(
        AudioClip clip,
        float volumeScale,
        float fadeInDuration)
    {
        if (clip == null)
            return null;

        AudioSource source =
            gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;

        source.Play();

        _loopVolumeScales[source] =
            volumeScale;

        DOTween
            .To(
                () => source.volume,
                value =>
                    source.volume = value,
                GetLoopVolume(volumeScale),
                fadeInDuration
            )
            .SetTarget(source);

        return source;
    }

    public void StopLoopSound(
        AudioSource source,
        float fadeOutDuration)
    {
        if (source == null)
            return;

        DOTween.Kill(source);

        _loopVolumeScales.Remove(
            source
        );

        if (fadeOutDuration <= 0f)
        {
            Destroy(source);
            return;
        }

        DOTween
            .To(
                () => source.volume,
                value =>
                    source.volume = value,
                0f,
                fadeOutDuration
            )
            .SetTarget(source)
            .OnComplete(() =>
            {
                Destroy(source);
            });
    }

    // =========================
    // Volume
    // =========================

    private float GetBGMVolume()
    {
        return
            _bgmVolume *
            _masterVolume;
    }

    private float GetLoopVolume(
        float volumeScale)
    {
        return
            _effectVolume *
            _masterVolume *
            volumeScale;
    }

    private void RefreshLoopVolumes()
    {
        foreach (
            KeyValuePair<AudioSource, float> loop
            in _loopVolumeScales)
        {
            if (loop.Key == null)
                continue;

            loop.Key.volume =
                GetLoopVolume(loop.Value);
        }
    }

    public void SetMasterVolume(
        float volume)
    {
        _masterVolume = volume;

        RefreshBGMVolume();

        _effectSource.volume =
            _effectVolume *
            _masterVolume;

        RefreshLoopVolumes();

        PlayerPrefs.SetFloat(
            "MasterVolume",
            volume
        );
    }

    public void SetBGMVolume(
        float volume)
    {
        _bgmVolume = volume;

        RefreshBGMVolume();

        PlayerPrefs.SetFloat(
            "BGMVolume",
            volume
        );
    }

    public void SetEffectVolume(
        float volume)
    {
        _effectVolume = volume;

        _effectSource.volume =
            _effectVolume *
            _masterVolume;

        RefreshLoopVolumes();

        PlayerPrefs.SetFloat(
            "EffectVolume",
            volume
        );
    }

    private void RefreshBGMVolume()
    {
        if (_currentBgmSource != null)
        {
            _currentBgmSource.volume =
                GetBGMVolume();
        }

        if (_nextBgmSource != null &&
            !_nextBgmSource.isPlaying)
        {
            _nextBgmSource.volume = 0f;
        }
    }

    private void LoadVolume()
    {
        _masterVolume =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                1f
            );

        _bgmVolume =
            PlayerPrefs.GetFloat(
                "BGMVolume",
                1f
            );

        _effectVolume =
            PlayerPrefs.GetFloat(
                "EffectVolume",
                1f
            );

        _bgmFadeValue = 1f;

        if (_currentBgmSource != null)
        {
            _currentBgmSource.volume =
                GetBGMVolume();
        }

        if (_nextBgmSource != null)
        {
            _nextBgmSource.volume = 0f;
        }

        _effectSource.volume =
            _effectVolume *
            _masterVolume;
    }
    public void StartSlow()
    {
        _bgmSlowTween?.Kill();

        _bgmSlowTween = DOTween.To(
                () => _currentBgmPitch,
                value =>
                {
                    _currentBgmPitch = value;
                    ApplyBGMPitch();
                },
                _slowPitch,
                _slowDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmSlowTween = null;
            });
    }
    public void StopSlow()
    {
        _bgmSlowTween?.Kill();

        _bgmSlowTween = DOTween.To(
                () => _currentBgmPitch,
                value =>
                {
                    _currentBgmPitch = value;
                    ApplyBGMPitch();
                },
                1f,
                _restoreDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmSlowTween = null;
            });
    }
    private void ApplyBGMPitch()
    {
        if (_bgmSource != null)
        {
            _bgmSource.pitch = _currentBgmPitch;
        }

        if (_bgmSubSource != null)
        {
            _bgmSubSource.pitch = _currentBgmPitch;
        }
    }
    private void OnDestroy()
    {
        _bgmFadeTween?.Kill();
    }
}
