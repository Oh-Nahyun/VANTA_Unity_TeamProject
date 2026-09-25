using DG.Tweening;
using UnityEngine;

public class WeaknessThreadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material _threadMaterial;
    [SerializeField] private Transform _boss;
    [SerializeField] private Camera _targetCamera;

    [Header("Thread")]
    [SerializeField] private float _threadWidth = 0.008f;

    [Header("Thread Fade")]
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Header("Thread Movement")]
    [SerializeField] private float _rotationSpeed = 3f;

    private Vector2 _threadDirection;
    private bool _threadRunning;

    private Tween _fadeTween;

    private static readonly int ThreadCenterID =
        Shader.PropertyToID("_ThreadCenter");

    private static readonly int ThreadNormalID =
        Shader.PropertyToID("_ThreadNormal");

    private static readonly int ThreadWidthID =
        Shader.PropertyToID("_ThreadWidth");

    private static readonly int ThreadOpacityID =
        Shader.PropertyToID("_ThreadOpacity");

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        _threadMaterial.SetFloat(ThreadOpacityID, 0f);
    }

    public void StartWeaknessThread()
    {
        float randomAngle = Random.Range(0f, 180f);
        float radian = randomAngle * Mathf.Deg2Rad;

        _threadDirection = new Vector2(
            Mathf.Cos(radian),
            Mathf.Sin(radian)
        ).normalized;

        _threadMaterial.SetFloat(
            ThreadWidthID,
            _threadWidth
        );

        _threadRunning = true;

        // 위치/방향부터 설정하고 등장
        UpdateThread();

        FadeInThread();
    }

    public void EndWeaknessThread()
    {
        FadeOutThread();
    }

    private void LateUpdate()
    {
        if (!_threadRunning)
            return;

        UpdateThreadDirection();
        UpdateThread();
    }

    private void UpdateThreadDirection()
    {
        float angle =
            _rotationSpeed * Time.unscaledDeltaTime;

        _threadDirection =
            Quaternion.Euler(0f, 0f, angle)
            * _threadDirection;

        _threadDirection.Normalize();
    }

    private void UpdateThread()
    {
        if (_boss == null || _targetCamera == null)
            return;

        Vector3 bossScreen =
            _targetCamera.WorldToViewportPoint(_boss.position);

        Vector2 center = new Vector2(
            bossScreen.x,
            bossScreen.y
        );

        Vector3 directionPoint =
            _boss.position +
            new Vector3(
                _threadDirection.x,
                _threadDirection.y,
                0f
            );

        Vector3 directionScreen =
            _targetCamera.WorldToViewportPoint(directionPoint);

        Vector2 screenDirection = new Vector2(
            directionScreen.x - bossScreen.x,
            directionScreen.y - bossScreen.y
        );

        if (screenDirection.sqrMagnitude < 0.000001f)
            return;

        screenDirection.Normalize();

        Vector2 normal = new Vector2(
            -screenDirection.y,
            screenDirection.x
        );

        _threadMaterial.SetVector(
            ThreadCenterID,
            center
        );

        _threadMaterial.SetVector(
            ThreadNormalID,
            normal
        );
    }

    private void FadeInThread()
    {
        _fadeTween?.Kill();

        _threadMaterial.SetFloat(
            ThreadOpacityID,
            0f
        );

        _fadeTween = _threadMaterial
            .DOFloat(
                1f,
                ThreadOpacityID,
                _fadeInDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void FadeOutThread()
    {
        _fadeTween?.Kill();

        _fadeTween = _threadMaterial
            .DOFloat(
                0f,
                ThreadOpacityID,
                _fadeOutDuration
            )
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _threadRunning = false;
            });
    }

    public Vector2 GetThreadDirection()
    {
        return _threadDirection;
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
    }
}
