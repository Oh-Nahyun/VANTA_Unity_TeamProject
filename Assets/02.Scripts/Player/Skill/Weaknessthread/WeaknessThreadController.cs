
using UnityEngine;

public class WeaknessThreadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _boss;
    [SerializeField] private Camera _targetCamera;
    [Header("Thread Movement")]
    [SerializeField] private float _rotationSpeed = 3f;
    [SerializeField] private Vector2 _boss1ThreadOffset;

    private WeaknessEdgeLineControl _weaknessEdgeLineControl;
    private WeaknessMainLineControl _weaknessMainLineControl;
    
    private Vector2 _threadDirection;
    private bool _isBoss1;
    private bool _isFinisher;
    private bool _threadRunning;
    

    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;

        _weaknessEdgeLineControl = GetComponent<WeaknessEdgeLineControl>();
        _weaknessMainLineControl = GetComponent<WeaknessMainLineControl>();

       
    }

    

    private void LateUpdate()
    {
        if (!_threadRunning)
            return;

        if (_isFinisher)
            return;
        
        UpdateThreadDirection();
        UpdateThread();
    }

    public void StartWeaknessThread()
    {
        float randomAngle =
            Random.Range(0f, 180f);

        float radian =
            randomAngle * Mathf.Deg2Rad;

        _threadDirection = new Vector2(
            Mathf.Cos(radian),
            Mathf.Sin(radian)
        ).normalized;
        

        
        _threadRunning = true;

        HitableObject hitableObject =
            FindAnyObjectByType<HitableObject>();

        if (hitableObject != null)
        {
            _boss = hitableObject.transform;
            _isBoss1 = hitableObject is Boss1Life;
        }
        
        UpdateThread();

        _weaknessMainLineControl.FadeInThread();
        _weaknessEdgeLineControl.FadeInRay();
    }
    public void MakeFinishWeaknessThread(
        Vector2 start,
        Vector2 end)
    {
        _isFinisher = true;
        Vector2 direction = end - start;

        if (direction.sqrMagnitude <= 0f)
            return;

        _threadDirection = direction.normalized;

        _threadRunning = true;
        HitableObject hitableObject =FindAnyObjectByType<HitableObject>();
        _boss = hitableObject.transform;
        
        UpdateThread();

        _weaknessMainLineControl.FadeInThread();
        _weaknessEdgeLineControl.FadeInRay();
    }
    public void EndWeaknessThread()
    {
        _threadRunning = false;

        _weaknessMainLineControl.FadeOutThread();
        _weaknessEdgeLineControl.FadeOutRay();
    }

    private void UpdateThreadDirection()
    {
        float angle =
            _rotationSpeed * Time.unscaledDeltaTime;

        _threadDirection =
            Quaternion.Euler(
                0f,
                0f,
                angle
            ) * _threadDirection;

        _threadDirection.Normalize();
    }

    private void UpdateThread()
    {
        if (_boss == null ||
            _targetCamera == null)
        {
            return;
        }

        Vector3 bossPosition = _boss.position;

        if (_isBoss1)
        {
            bossPosition += new Vector3(
                _boss1ThreadOffset.x,
                _boss1ThreadOffset.y,
                0f
            );
        }

        Vector3 bossScreen =
            _targetCamera.WorldToViewportPoint(
                bossPosition
            );

        Vector2 center =
            new Vector2(
                bossScreen.x,
                bossScreen.y
            );

        Vector3 directionPoint =
            bossPosition +
            new Vector3(
                _threadDirection.x,
                _threadDirection.y,
                0f
            );

        Vector3 directionScreen =
            _targetCamera.WorldToViewportPoint(
                directionPoint
            );

        Vector2 screenDirection =
            new Vector2(
                directionScreen.x - bossScreen.x,
                directionScreen.y - bossScreen.y
            );

        if (screenDirection.sqrMagnitude < 0.000001f)
            return;

        screenDirection.Normalize();

        Vector2 normal =
            new Vector2(
                -screenDirection.y,
                screenDirection.x
            );

        _weaknessMainLineControl.UpdateMainThreadDirection(
            center,
            normal
        );
    }
    

    public Vector2 GetThreadDirection()
    {
        return _threadDirection;
    }

    public void BrokenThread()
    {
        _threadRunning = false;
        _weaknessEdgeLineControl.BreakRay();
        _weaknessMainLineControl.Breakthread();
    }
}
