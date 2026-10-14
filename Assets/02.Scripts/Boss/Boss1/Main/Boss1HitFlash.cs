using System.Collections;
using UnityEngine;

public class Boss1HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _bodyRenderers;
    
    [SerializeField] private float _flashDuration = 0.01f;
    
    private static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    private static readonly int Phase2Amount = Shader.PropertyToID("_Phase2Amount");

    private Color[] _originalColors;
    private Coroutine _flashCoroutine;
    
    public float CurrentPhase2Amount { get; private set; }

    public void PlayHitFlash()
    {
        PlayHitFlash(_flashDuration, Color.white);
    }
    
    public void PlayHitFlash(float duration)
    {
        PlayHitFlash(duration, Color.white);
    }
    
    public void PlayHitFlash(float duration, Color color)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        SetColorProperty(FlashColor, color);
        _flashCoroutine = StartCoroutine(HitFlashCoroutine(duration));
    }
    
    private IEnumerator HitFlashCoroutine(float duration)
    {
        SetFlashAmount(1f);
        
        yield return new WaitForSeconds(duration);
        
        SetFlashAmount(0f);
        
        _flashCoroutine = null;
    }
    
    private void SetFlashAmount(float amount)
    {
        SetFloatProperty(FlashAmount, amount);
    }

    public void SetPhase2Amount(float amount)
    {
        CurrentPhase2Amount = amount;
        SetFloatProperty(Phase2Amount, amount);
    }

    private void SetFloatProperty(int propertyId, float value)
    {
        foreach (SpriteRenderer bodyRenderer in _bodyRenderers)
        {
            if (bodyRenderer == null)
            {
                continue;
            }
            
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(propertyId, value);
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }
    }
    
    private void SetColorProperty(int propertyId, Color value)
    {
        foreach (SpriteRenderer bodyRenderer in _bodyRenderers)
        {
            if (bodyRenderer == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(propertyId, value);
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
