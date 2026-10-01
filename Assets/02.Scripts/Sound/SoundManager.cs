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
    [SerializeField] private float _bgmVolume = 1f;
    [SerializeField] private float _fadeDuration = 1f;

    [Header("Effect Pitch")]
    [SerializeField] private float _minPitch = 0.95f;
    [SerializeField] private float _maxPitch = 1.05f;

    private Tween _bgmFadeTween;

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
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
            return;

        _bgmFadeTween?.Kill();

        if (_bgmSource.isPlaying)
        {
            _bgmFadeTween = _bgmSource
                .DOFade(0f, _fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _bgmSource.clip = clip;
                    _bgmSource.loop = true;
                    _bgmSource.volume = 0f;
                    _bgmSource.Play();

                    _bgmFadeTween = _bgmSource
                        .DOFade(_bgmVolume, _fadeDuration)
                        .SetUpdate(true);
                });

            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        _bgmFadeTween = _bgmSource
            .DOFade(_bgmVolume, _fadeDuration)
            .SetUpdate(true);
    }

    public void StopBGM()
    {
        if (!_bgmSource.isPlaying)
            return;

        _bgmFadeTween?.Kill();

        _bgmFadeTween = _bgmSource
            .DOFade(0f, _fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
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
}
