using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PlayerEmotionContoller : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshPro _text;

    [Header("Emotions")]
    [SerializeField] private List<string> _emotions = new List<string>();

    [Header("Typing")]
    [SerializeField] private float _typingInterval = 0.15f;

    private Sequence _emotionSequence;

    private void Awake()
    {
        _text.text = "";
    }

    public void PlayEmotion(int emotionIndex, float duration)
    {
        if (emotionIndex < 0 || emotionIndex >= _emotions.Count)
            return;

        KillSequence();

        _text.text = "";

        string emotion = _emotions[emotionIndex];

        _emotionSequence = DOTween.Sequence();

        for (int i = 0; i < emotion.Length; i++)
        {
            int index = i;

            _emotionSequence.AppendCallback(() =>
            {
                _text.text = emotion.Substring(0, index + 1);
            });

            if (i < emotion.Length - 1)
            {
                _emotionSequence.AppendInterval(_typingInterval);
            }
        }

        _emotionSequence.AppendInterval(duration);

        _emotionSequence.AppendCallback(() =>
        {
            _text.text = "";
        });
    }

    public void ClearEmotion()
    {
        KillSequence();
        _text.text = "";
    }

    private void KillSequence()
    {
        _emotionSequence?.Kill();
        _emotionSequence = null;
    }

    private void OnDisable()
    {
        KillSequence();
        _text.text = "";
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    public void PlayBoss1DiscoverEmotion()
    {
        PlayEmotion(0,1);
    }
   
    public void PlayBoss1RoarEmotion()
    {
        PlayEmotion(1,2);
    }
    public void PlayBoss1DeathEmotion()
    {
        PlayEmotion(2,1);
    }
    public void PlayPhaseChangeEmotion()
    {
        PlayEmotion(3,1);
    }
    public void PlayBoss2DiscoverEmotion()
    {
        PlayEmotion(4,1);
    }
}
