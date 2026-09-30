using DG.Tweening;
using UnityEngine;

public class SkillIndicatorSelectIndicator : MonoBehaviour
{
    [Header("타겟 오브젝트")]
    [SerializeField] private GameObject _targetSprite;

    [Header("Dissolve")]
    [SerializeField] private float _dissolveInDuration = 0.25f;
    [SerializeField] private float _dissolveOutDuration = 0.2f;

    [Header("Color")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = Color.darkBlue;
    [SerializeField] private float _colorChangeDuration = 0.2f;

    private ObjectRotator _objectRotator;
    private ObjectSquash _objectSquash;
    private Material _material;

    private Tween _dissolveTween;
    private Tween _colorTween;

    private static readonly int DissolveAmountID =
        Shader.PropertyToID("_DissolveAmount");

    private static readonly int IndicatorColorID =
        Shader.PropertyToID("_IndicatorColor");

    private void Awake()
    {
        _objectRotator =
            _targetSprite.GetComponent<ObjectRotator>();

        _objectSquash =
            _targetSprite.GetComponent<ObjectSquash>();

        _material =
            _targetSprite.GetComponent<Renderer>().material;

        _material.SetFloat(
            DissolveAmountID,
            1f
        );

        _material.SetColor(
            IndicatorColorID,
            _normalColor
        );

        _targetSprite.SetActive(false);
    }

    public void IndicatingStart()
    {
        _dissolveTween?.Kill();
        _colorTween?.Kill();

        _targetSprite.SetActive(true);

        _objectRotator.enabled = true;

        _material.SetFloat(
            DissolveAmountID,
            1f
        );

        _material.SetColor(
            IndicatorColorID,
            _normalColor
        );

        _dissolveTween =
            _material
                .DOFloat(
                    0f,
                    DissolveAmountID,
                    _dissolveInDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
    }

    public void IndicatingStop()
    {
        _dissolveTween?.Kill();
        _colorTween?.Kill();

        _dissolveTween =
            _material
                .DOFloat(
                    1f,
                    DissolveAmountID,
                    _dissolveOutDuration
                )
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _targetSprite.SetActive(false);
                });
    }

    public void IndicateSelected()
    {
        _objectRotator.enabled = false;

        _objectSquash.PlaySquash();

        _colorTween?.Kill();

        _colorTween =
            _material
                .DOColor(
                    _selectedColor,
                    IndicatorColorID,
                    _colorChangeDuration
                )
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _dissolveTween?.Kill();
        _colorTween?.Kill();

        if (_material != null)
        {
            Destroy(_material);
        }
    }
}
