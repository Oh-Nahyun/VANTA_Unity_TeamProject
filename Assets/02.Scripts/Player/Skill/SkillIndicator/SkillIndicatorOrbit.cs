using UnityEngine;

public class SkillIndicatorOrbit : MonoBehaviour
{
    [Header("공전 설정")] [SerializeField] private float _orbitSpeed = 30f;
    [SerializeField] private float _orbitRadius = 4f;
    [SerializeField] private float _orbitEnterDuration = 0.25f;

    [Header("Boss1 설정")] [SerializeField] private float _boss1YOffset = 1f;

    [Header("Debug")] [SerializeField] private bool _showDebug = true;

    private Transform _target;
    private float _currentAngle;

    private bool _isOrbiting;
    private bool _isEnteringOrbit;
    private bool _isBoss1;
    private float _orbitEnterStartRadius;
    private float _orbitEnterStartAngle;
    private float _orbitEnterTargetAngle;
    private float _orbitEnterElapsed;

    public void Initialize(
        int index,
        int totalCount,
        float startAngle = 0f)
    {
        HitableObject hitableObject =
            FindAnyObjectByType<HitableObject>();

        if (hitableObject == null ||
            totalCount <= 0)
        {
            return;
        }

        _target = hitableObject.transform;

        _isBoss1 = hitableObject is Boss1Life;

        float angleStep =
            360f / totalCount;

        _orbitEnterTargetAngle =
            startAngle +
            angleStep * index;

        Vector2 offset =
            (Vector2)transform.position -
            GetOrbitCenter();

        if (offset.sqrMagnitude <= 0.000001f)
        {
            offset = Vector2.right;
        }

        _orbitEnterStartAngle =
            Mathf.Atan2(
                offset.y,
                offset.x
            ) * Mathf.Rad2Deg;

        _currentAngle = _orbitEnterStartAngle;

        _isOrbiting = false;
        _isEnteringOrbit = true;
        _orbitEnterStartRadius =
            offset.magnitude;
        _orbitEnterElapsed = 0f;

        if (_orbitEnterDuration <= 0f)
        {
            _isEnteringOrbit = false;
            _isOrbiting = true;
            UpdateOrbitPosition();
        }
    }

    private void Update()
    {
        if (_target == null)
            return;

        if (_isEnteringOrbit)
        {
            UpdateEnterOrbitPosition();
            return;
        }

        if (!_isOrbiting)
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

    private Vector2 GetOrbitPosition()
    {
        return GetOrbitPosition(_orbitRadius);
    }

    private Vector2 GetOrbitPosition(float radius)
    {
        Vector2 orbitCenter = GetOrbitCenter();

        float rad =
            _currentAngle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        ) * radius;

        return orbitCenter + offset;
    }

    private void UpdateEnterOrbitPosition()
    {
        _orbitEnterElapsed += Time.deltaTime;

        float normalizedTime =
            Mathf.Clamp01(
                _orbitEnterElapsed /
                _orbitEnterDuration
            );

        float t = Mathf.SmoothStep(
            0f,
            1f,
            normalizedTime
        );

        float targetAngle =
            _orbitEnterTargetAngle -
            _orbitSpeed * _orbitEnterElapsed;

        _currentAngle =
            Mathf.LerpAngle(
                _orbitEnterStartAngle,
                targetAngle,
                t
            );

        transform.position =
            GetOrbitPosition(
                Mathf.Lerp(
                    _orbitEnterStartRadius,
                    _orbitRadius,
                    t
                )
            );

        if (_orbitEnterElapsed < _orbitEnterDuration)
            return;

        _isEnteringOrbit = false;
        _isOrbiting = true;
        _currentAngle = targetAngle;

        UpdateOrbitPosition();
    }

    private void UpdateOrbitPosition()
    {
        transform.position = GetOrbitPosition();
    }

    public void StopOrbit()
    {
        _isOrbiting = false;
        _isEnteringOrbit = false;
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
