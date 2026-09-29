using UnityEngine;

public class SkillIndicatorOrbit : MonoBehaviour
{
    [Header("공전 설정")]
    [SerializeField] private float _orbitSpeed = 30f;

    private Transform _target;
    private float _radius;
    private float _currentAngle;

    private bool _isOrbiting;

    public void Initialize()
    {
        _target = FindAnyObjectByType<HitableObject>().transform;

        Vector2 offset =
            transform.position - _target.position;

        _radius = offset.magnitude;

        _currentAngle =
            Mathf.Atan2(offset.y, offset.x)
            * Mathf.Rad2Deg;

        _isOrbiting = true;
    }

    private void Update()
    {
        if (!_isOrbiting || _target == null)
            return;

        // 시계 방향으로 회전
        _currentAngle -=
            _orbitSpeed * Time.deltaTime;

        float rad =
            _currentAngle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        ) * _radius;

        transform.position =
            _target.position + (Vector3)offset;
    }

    public void StopOrbit()
    {
        _isOrbiting = false;
    }
}
