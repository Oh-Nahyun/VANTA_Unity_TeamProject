
using DG.Tweening;
using UnityEngine;

public class EndingCreditSoundController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioSource _subAudioSource;

    [Header("변경할 BGM")]
    [SerializeField] private AudioClip _bgmClip;

    [Header("Fade 설정")]
    [SerializeField] private float _fadeOutDuration = 2f;
    [SerializeField] private float _fadeInDuration = 3f;

    [SerializeField, Range(0f, 1f)]
    private float _targetVolume = 1f;

    private Sequence _fadeSequence;

    private void Awake()
    {
        if (_audioSource == null || _subAudioSource == null)
            return;

        // 초기 BGM Fade In
        _audioSource.volume = 0f;

        // 보조 AudioSource 초기화
        _subAudioSource.Stop();
        _subAudioSource.volume = 0f;
        _subAudioSource.playOnAwake = false;

        _fadeSequence = DOTween.Sequence();

        _fadeSequence.Append(
            _audioSource.DOFade(_targetVolume, _fadeInDuration)
        );
        
        ChangeBgm();
    }

    // 기존 BGM과 새로운 BGM 크로스페이드
    public void ChangeBgm()
    {
        if (_audioSource == null ||
            _subAudioSource == null ||
            _bgmClip == null)
            return;

        _fadeSequence?.Kill();

        // 기존 재생 중인 BGM
        AudioSource oldSource = _audioSource;

        // 새롭게 재생할 BGM
        AudioSource newSource = _subAudioSource;

        newSource.Stop();
        newSource.clip = _bgmClip;
        newSource.volume = 0f;
        newSource.Play();

        _fadeSequence = DOTween.Sequence();

        // 기존 BGM Fade Out
        _fadeSequence.Join(
            oldSource.DOFade(0f, _fadeOutDuration)
        );

        // 새로운 BGM Fade In (동시에 실행)
        _fadeSequence.Join(
            newSource.DOFade(_targetVolume, _fadeInDuration)
        );

        // 전환 완료 후 기존 BGM 정지
        _fadeSequence.AppendCallback(() =>
        {
            oldSource.Stop();
        });

        // AudioSource 역할 교체
        _audioSource = newSource;
        _subAudioSource = oldSource;
    }

    // 현재 BGM Fade Out 후 정지
    public void FadeOutBgm()
    {
        if (_audioSource == null)
            return;

        _fadeSequence?.Kill();

        // 크로스페이드 중이었다면 보조 BGM도 정지
        if (_subAudioSource != null)
        {
            _subAudioSource.Stop();
            _subAudioSource.volume = 0f;
        }

        _fadeSequence = DOTween.Sequence();

        _fadeSequence.Append(
            _audioSource.DOFade(0f, _fadeOutDuration)
        );

        _fadeSequence.AppendCallback(() =>
        {
            _audioSource.Stop();
        });
    }

    private void OnDestroy()
    {
        _fadeSequence?.Kill();
        _fadeSequence = null;
    }
}
