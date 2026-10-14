using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class NameActingController : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField]
    private List<TextMeshPro> _textMeshes;

    [Header("Fade In")]
    [SerializeField]
    private float _fadeInDuration = 0.5f;

    [SerializeField]
    private float _fadeInterval = 0.3f;

    [Header("Fade Out")]
    [SerializeField]
    private float _fadeOutDelay = 1f;

    private Sequence _fadeSequence;


    private void Awake()
    {
        SetAllTextAlpha(0f);
    }

    private void OnEnable()
    {
        if (!RetryStatus.IsRetry)
        {
            SetAllTextAlpha(0f);
            return;
        }

        StartTextFadeIn();
    }

 

    public void StartTextFadeIn()
    {
        if (!RetryStatus.IsRetry)
            return;


        KillSequence();

        SetAllTextAlpha(0f);

        _fadeSequence = DOTween.Sequence();

        for (int i = 0; i < _textMeshes.Count; i++)
        {
            TextMeshPro text = _textMeshes[i];

            if (text == null)
                continue;

            _fadeSequence.Insert(
                i * _fadeInterval,
                text.DOFade(
                        1f,
                        _fadeInDuration
                    )
                    .SetEase(Ease.OutSine)
            );
        }

        _fadeSequence.AppendInterval(_fadeOutDelay);

        _fadeSequence.AppendCallback(() =>
        {
            SetAllTextAlpha(0f);
            gameObject.SetActive(false);
        });
    }

    public void FadeOutText()
    {
        KillSequence();

        SetAllTextAlpha(0f);

        gameObject.SetActive(false);
    }

    private void SetAllTextAlpha(float alpha)
    {
        foreach (TextMeshPro text in _textMeshes)
        {
            if (text == null)
                continue;

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    private void KillSequence()
    {
        if (_fadeSequence != null)
        {
            _fadeSequence.Kill();
            _fadeSequence = null;
        }
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }
}
