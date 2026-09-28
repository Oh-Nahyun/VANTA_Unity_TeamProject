using DG.Tweening;

using UnityEngine;

public class MiddleDamageIndicator : DamageIndicator
{
    protected override void IndicatorCreate()
    {
        _text.alpha = 0f;

        Sequence sequence = DOTween.Sequence();


        sequence.Append(
            transform.DOMoveY(transform.position.y + 0.7f, 0.2f)
        );
        sequence.Join(
            _text.DOFade(1f, 0.2f)
        );


        sequence.AppendInterval(0.5f);

        sequence.Append(
            _text.DOFade(0f, 0.3f)
        );


        sequence.OnComplete(() => Destroy(gameObject));
    }
}
