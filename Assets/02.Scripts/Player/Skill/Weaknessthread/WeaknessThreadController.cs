using UnityEngine;

public class WeaknessThreadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material _threadMaterial;
    [SerializeField] private Transform _boss;
    [SerializeField] private Camera _targetCamera;

    [Header("Thread")]
    [SerializeField] private float _threadWidth = 0.008f;

    private Vector2 _threadDirection;
    private bool _threadRunning;
    private static readonly int ThreadCenterID =
        Shader.PropertyToID("_ThreadCenter");

    private static readonly int ThreadNormalID =
        Shader.PropertyToID("_ThreadNormal");

    private static readonly int ThreadWidthID =
        Shader.PropertyToID("_ThreadWidth");

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        FadeOutThread();
    }

    public void StartWeaknessThread()
    {
        // 직선이므로 0~180도면 모든 방향 표현 가능
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
    }

    public void EndWeaknessThread()
    {
        _threadRunning = false;
        FadeOutThread();
    }
    private void LateUpdate()
    {
        if (_threadRunning)
        {
            UpdateThread();
        }
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

    private void FadeOutThread()
    {
        _threadMaterial.SetFloat(
            ThreadWidthID,
            0f
        );
    }
    
    public Vector2 GetThreadDirection()
    {
        return _threadDirection;
    }
}
