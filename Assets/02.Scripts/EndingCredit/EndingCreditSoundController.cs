using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingCreditSoundController : MonoBehaviour
{
    [Header("현재 재생 중인 BGM")]
    [SerializeField]
    private AudioSource _audioSource;

    [Header("볼륨 증가 설정")]
    [SerializeField, Range(0f, 1f)]
    private float _targetVolume = 1f;

    [SerializeField, Min(0f)]
    private float _fadeDuration = 3f;

    [Header("볼륨 감소 설정")]
    [SerializeField, Min(0f)]
    private float _fadeOutDuration = 3f;

    private Tween _volumeTween;

    private void Start()
    {
        IncreaseBgmVolume();
    }

    public void IncreaseBgmVolume()
    {
        if (_audioSource == null)
            return;

        _volumeTween?.Kill();

        _volumeTween = _audioSource
            .DOFade(
                _targetVolume,
                _fadeDuration
            )
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                _volumeTween = null;
            });
    }

    public void DecreaseBgmVolume()
    {
        if (_audioSource == null)
            return;

        _volumeTween?.Kill();

        _volumeTween = _audioSource
            .DOFade(
                0f,
                _fadeOutDuration
            )
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                _volumeTween = null;

                // 볼륨이 완전히 0이 된 후 호출
                LoadInGameScene();
            });
    }
    private void LoadInGameScene()
    {
        SceneManager.LoadScene("InGameScene");
    }
    private void OnDestroy()
    {
        _volumeTween?.Kill();
        _volumeTween = null;
    }
}
