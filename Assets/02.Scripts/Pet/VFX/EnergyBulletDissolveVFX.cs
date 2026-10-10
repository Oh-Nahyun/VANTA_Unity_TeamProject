using UnityEngine;

public class EnergyBulletDissolveVFX : MonoBehaviour
{
    [Header("펫 에너지 탄 타겟 상실 시 이동 시간")]
    [SerializeField] private float _dissolveMoveDuration = 0.5f;

    [Header("펫 에너지 탄 타겟 상실 시 이동 속도")]
    [SerializeField] private float _dissolveMoveSpeed = 3f;

    [Header("펫 에너지 탄 타겟 상실 시 진행 방향 기준 흩어지는 최대 각도")]
    [SerializeField] private float _dissolveScatterAngle = 70f;

    [Header("펫 에너지 탄 타겟 상실 시 꼬불거림 세기")]
    [SerializeField] private float _dissolveWobbleStrength = 1.5f;

    [Header("펫 에너지 탄 타겟 상실 시 꼬불거림 빠르기")]
    [SerializeField] private Vector2 _dissolveWobbleFrequencyRange = new Vector2(8f, 16f);

    [Header("펫 에너지 탄 타겟 상실 시 소멸 이펙트 프리팹")]
    [SerializeField] private GameObject _energyBulletDissolveVFXPrefab;

    [Header("펫 에너지 탄 타겟 상실 시 소멸 이펙트 삭제 시간")]
    [SerializeField] private float _energyBulletDissolveVFXLifetime = 1f;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 연출 진행 여부
    /// </summary>
    public bool IsDissolving
    {
        get;
        private set;
    }

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 연출 경과 시간
    /// </summary>
    private float _elapsed;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 시 흩어지는 방향
    /// </summary>
    private Vector3 _direction;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 시 꼬불거림 빠르기
    /// </summary>
    private float _wobbleFrequency;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 시 꼬불거림 시작 위상
    /// </summary>
    private float _wobblePhase;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 시 크기 축소 전 원래 크기
    /// </summary>
    private Vector3 _startScale;

    public void StartDissolve()
    {
        if (IsDissolving) return;

        IsDissolving = true;
        _elapsed = 0f;
        _startScale = transform.localScale;

        float angleOffset = Random.Range(-_dissolveScatterAngle, _dissolveScatterAngle);
        _direction = Quaternion.Euler(0f, 0f, angleOffset) * transform.right;
        _wobbleFrequency = Random.Range(_dissolveWobbleFrequencyRange.x, _dissolveWobbleFrequencyRange.y);
        _wobblePhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (!IsDissolving) return;

        _elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(_elapsed / _dissolveMoveDuration);
        float speed = _dissolveMoveSpeed * (1f - progress);

        Vector3 perpendicular = new Vector3(-_direction.y, _direction.x, 0f);
        float wobble = Mathf.Sin(_elapsed * _wobbleFrequency + _wobblePhase) * _dissolveWobbleStrength;
        Vector3 velocity = _direction * speed + perpendicular * wobble;

        transform.position += velocity * Time.deltaTime;
        SetRotation(velocity);
        transform.localScale = _startScale * (1f - progress * progress);

        if (progress >= 1f)
        {
            SpawnEnergyBulletDissolveVFX();
            Destroy(gameObject);
        }
    }

    private void SetRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SpawnEnergyBulletDissolveVFX()
    {
        if (_energyBulletDissolveVFXPrefab == null) return;

        GameObject dissolveVFX = Instantiate(_energyBulletDissolveVFXPrefab, transform.position, Quaternion.identity);
        Destroy(dissolveVFX, _energyBulletDissolveVFXLifetime);
    }
}
