using System.Collections;
using UnityEngine;

public class Boss1HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _bodyRenderers;
    
    [SerializeField] private float _flashDuration = 0.01f;
    
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    private static readonly int Phase2Amount = Shader.PropertyToID("_Phase2Amount");

    private Color[] _originalColors;
    private Coroutine _flashCoroutine;
    
    public float CurrentPhase2Amount { get; private set; }

    public void PlayHitFlash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(HitFlashCoroutine(_flashDuration));
    }
    
    public void PlayHitFlash(float duration)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

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
}
