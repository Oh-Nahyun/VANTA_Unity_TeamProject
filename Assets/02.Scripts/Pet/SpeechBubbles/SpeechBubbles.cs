using System.Collections;
using UnityEngine;

public class SpeechBubbles : MonoBehaviour
{
    [Header("펫 기본 공격 말풍선 스프라이트")]
    [SerializeField] private Sprite _energyBulletSprite;

    [Header("펫 공격 스킬 말풍선 스프라이트")]
    [SerializeField] private Sprite _crystalPrisonSprite;

    [Header("펫 유틸 스킬 말풍선 스프라이트")]
    [SerializeField] private Sprite _crystalSanctuarySprite;

    [Header("펫 기본 공격 및 스킬 말풍선 스프라이트 출력 시간")]
    [SerializeField] private float _speechBubblesTimer = 1.5f;

    /// <summary>
    /// 펫 말풍선 스프라이트 랜더러
    /// </summary>
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _spriteRenderer.sprite = null;
    }

    public void ChangeSpriteToEnergyBullet()
    {
        _spriteRenderer.sprite = _energyBulletSprite;

        StopAllCoroutines();
        StartCoroutine(ChangeSpriteToNull());
    }

    public void ChangeSpriteToCrystalPrison()
    {
        _spriteRenderer.sprite = _crystalPrisonSprite;

        StopAllCoroutines();
        StartCoroutine(ChangeSpriteToNull());
    }

    public void ChangeSpriteToCrystalSanctuary()
    {
        _spriteRenderer.sprite = _crystalSanctuarySprite;

        StopAllCoroutines();
        StartCoroutine(ChangeSpriteToNull());
    }

    IEnumerator ChangeSpriteToNull()
    {
        yield return new WaitForSeconds(_speechBubblesTimer);

        _spriteRenderer.sprite = null;
    }
}
