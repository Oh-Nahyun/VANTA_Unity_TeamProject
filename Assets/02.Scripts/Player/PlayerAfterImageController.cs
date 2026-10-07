using DG.Tweening;
using UnityEngine;

public class PlayerAfterImageController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private SpriteRenderer _playerRenderer;

    [Header("After Images")]
    [SerializeField] private PlayerAfterImage[] _afterImages;

    [Header("Setting")]
    [SerializeField] private float _spawnInterval = 0.04f;
    [SerializeField] private float _lifeTime = 0.2f;

    private bool _isActive;

    private int _currentIndex;

    private Tween _spawnTween;

    private void Awake()
    {
        foreach (PlayerAfterImage afterImage in _afterImages)
        {
            if (afterImage != null)
            {
                afterImage.gameObject.SetActive(false);
            }
        }
    }

    public void OnAfterImage()
    {
        if (_isActive)
            return;

        _isActive = true;

        SpawnAfterImage();

        _spawnTween =
            DOVirtual
                .DelayedCall(
                    _spawnInterval,
                    SpawnLoop
                )
                .SetLoops(-1)
                .SetUpdate(true);
    }

    public void OffAfterImage()
    {
        _isActive = false;

        _spawnTween?.Kill();
        _spawnTween = null;
    }

    private void SpawnLoop()
    {
        if (!_isActive)
            return;

        SpawnAfterImage();
    }

    private void SpawnAfterImage()
    {
        if (_afterImages == null ||
            _afterImages.Length == 0 ||
            _playerRenderer == null)
        {
            return;
        }

        PlayerAfterImage afterImage =
            _afterImages[_currentIndex];

        afterImage.Show(
            _playerRenderer.sprite,
            transform.position,
            transform.rotation,
            transform.localScale,
            _playerRenderer.flipX,
            _playerRenderer.flipY,
            _lifeTime
        );

        _currentIndex++;

        if (_currentIndex >= _afterImages.Length)
        {
            _currentIndex = 0;
        }
    }

    private void OnDestroy()
    {
        _spawnTween?.Kill();
    }
}