using TMPro;
using UnityEngine;

public abstract class DamageIndicator : MonoBehaviour
{
    protected TextMeshProUGUI _text;

    protected virtual void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    public void Init(float damage)
    {
        _text.text = damage.ToString();
        IndicatorCreate();
    }

    protected abstract void IndicatorCreate();
}
