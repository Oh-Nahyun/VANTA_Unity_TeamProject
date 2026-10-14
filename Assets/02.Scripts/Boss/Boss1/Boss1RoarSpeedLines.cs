using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Boss1RoarSpeedLines : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _fps = 20f;
    [SerializeField] private float _maxAlpha = 0.5f;
    [SerializeField] private float _fadeInDuration = 0.08f;
    [SerializeField] private float _fadeOutDuration = 0.3f;

    private Tween _fadeTween;
    private bool _isPlaying;
    private float _timer;
    private int _frameIndex;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = -1;
        SetAlpha(0f);
        _image.enabled = false;
    }

    public void Play(float strength = 1f)
    {
        _isPlaying = true;
        _image.enabled = true;
        
        _fadeTween?.Kill();
        _fadeTween = _image.DOFade(Mathf.Clamp01(_maxAlpha * strength), _fadeInDuration).SetLink(gameObject);
    }

    public void Stop()
    {
        if (!_isPlaying)
        {
            return;
        }
        
        _isPlaying = false;
        _fadeTween?.Kill();
        _fadeTween = _image.DOFade(0f, _fadeOutDuration).SetLink(gameObject).OnComplete(()=> _image.enabled = false);
    }

    private void Update()
    {
        if (!_image.enabled || _frames.Length == 0)
        {
            return;
        }
        
        _timer += Time.deltaTime;
        if (_timer < 1f / _fps)
        {
            return;
        }

        _timer = 0f;
        _frameIndex = (_frameIndex + 1 + Random.Range(0, _frames.Length - 1)) % _frames.Length;
        _image.sprite = _frames[_frameIndex];
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
    
}
