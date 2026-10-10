using System.Collections;
using UnityEngine;

public class Boss1HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _bodyRenderers;
    
    [SerializeField] private float _flashDuration = 0.01f;
    
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

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
        foreach (SpriteRenderer bodyRenderer in _bodyRenderers)
        {
            if (bodyRenderer == null)
            {
                continue;
            }
            
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FlashAmount, amount);
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
