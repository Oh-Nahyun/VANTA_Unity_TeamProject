using DG.Tweening;
using UnityEngine;

public class PlayerAfterImage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Tween _fadeTween;

    public void Show(
        Sprite sprite,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        bool flipX,
        bool flipY,
        float lifeTime)
    {
        _fadeTween?.Kill();

        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.flipX = flipX;
        _spriteRenderer.flipY = flipY;

        Color color = _spriteRenderer.color;
        color.a = 1f;
        _spriteRenderer.color = color;

        gameObject.SetActive(true);

        _fadeTween =
            _spriteRenderer
                .DOFade(0f, lifeTime)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    _fadeTween = null;
                });
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
    }
}
