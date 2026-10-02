using UnityEngine;

public class SkillIndicatorOrbit : MonoBehaviour
{
    [Header("공전 설정")]
    [SerializeField] private float _orbitSpeed = 30f;
    [SerializeField] private float _orbitRadius = 4f;

    [Header("Boss1 설정")]
    [SerializeField] private float _boss1YOffset = 1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebug = true;

    private Transform _target;
    private float _currentAngle;

    private bool _isOrbiting;
    private bool _isBoss1;

    public void Initialize()
    {
        HitableObject hitableObject =
            FindAnyObjectByType<HitableObject>();

        if (hitableObject == null)
            return;

        _target = hitableObject.transform;

        _isBoss1 = hitableObject is Boss1Life;

        Vector2 orbitCenter = GetOrbitCenter();

        Vector2 offset =
            (Vector2)transform.position - orbitCenter;

        if (offset.sqrMagnitude <= 0.000001f)
        {
            offset = Vector2.right;
        }

        _currentAngle =
            Mathf.Atan2(
                offset.y,
                offset.x
            ) * Mathf.Rad2Deg;

        _isOrbiting = true;

        UpdateOrbitPosition();
    }

    private void LateUpdate()
    {
        if (!_isOrbiting || _target == null)
            return;

        _currentAngle -=
            _orbitSpeed * Time.deltaTime;

        UpdateOrbitPosition();
    }

    private Vector2 GetOrbitCenter()
    {
        Vector2 center = _target.position;

        if (_isBoss1)
        {
            center.y += _boss1YOffset;
        }

        return center;
    }

    private void UpdateOrbitPosition()
    {
        Vector2 orbitCenter = GetOrbitCenter();

        float rad =
            _currentAngle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        ) * _orbitRadius;

        transform.position =
            orbitCenter + offset;
    }

    public void StopOrbit()
    {
        _isOrbiting = false;
    }

    private void OnDrawGizmos()
    {
        if (!_showDebug || _target == null)
            return;

        Vector2 orbitCenter = GetOrbitCenter();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            orbitCenter,
            0.15f
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            orbitCenter,
            transform.position
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            orbitCenter,
            _orbitRadius
        );
    }
}
