using UnityEngine;

public class PetShadow : MonoBehaviour
{
    [Header("펫 본체 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer _bodySpriteRenderer;

    [Header("펫 그림자 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer _shadowSpriteRenderer;

    [Header("펫 그림자 색상")]
    [SerializeField] private Color _shadowColor = Color.black;

    private void LateUpdate()
    {
        SetShadowSprite();
    }

    private void SetShadowSprite()
    {
        _shadowSpriteRenderer.sprite = _bodySpriteRenderer.sprite;
        _shadowSpriteRenderer.flipX = _bodySpriteRenderer.flipX;
        _shadowSpriteRenderer.color = _shadowColor;
    }
}
