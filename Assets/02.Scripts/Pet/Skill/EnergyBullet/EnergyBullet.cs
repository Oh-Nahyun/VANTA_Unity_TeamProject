using UnityEngine;

public class EnergyBullet : MonoBehaviour
{
    [Header("펫 에너지 탄 베지어 이동 속도")]
    [SerializeField] private float _bezierMoveSpeed = 1f;

    [Header("펫 에너지 탄 유도 이동 속도")]
    [SerializeField] private float _homingMoveSpeed = 5f;

    [Header("펫 에너지 탄 공격력")]
    [SerializeField] private float _damage = 10f;

    [Header("펫 에너지 탄 피격 이펙트 프리팹")]
    [SerializeField] private GameObject _energyBulletHitVFXPrefab;

    [Header("펫 에너지 탄 피격 이펙트 삭제 시간")]
    [SerializeField] private float _energyBulletHitVFXLifetime = 1f;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    public GameObject AttackTarget;

    /// <summary>
    /// 펫 에너지 탄 이동 곡선 클래스
    /// </summary>
    public BezierCurve BezierCurve;

    /// <summary>
    /// 펫 에너지 탄 스폰 시 이동 전 대기 시간 계산용 변수
    /// </summary>
    private float _timer;

    /// <summary>
    /// 펫 에너지 탄 이동 전 필요한 대기 시간
    /// </summary>
    private float _timeBeforeMove;

    /// <summary>
    /// 펫 에너지 탄 곡선 이동 진행률
    /// </summary>
    private float _bezierProgress;

    /// <summary>
    /// 펫 에너지 탄 유도 이동 여부 확인용 변수
    /// </summary>
    private bool _isHoming;

    private void Awake()
    {
        BezierCurve = GetComponent<BezierCurve>();
    }

    private void Start()
    {
        if (BezierCurve == null) return;
        if (AttackTarget == null) return;

        _timer = 0f;
        _timeBeforeMove = 0.6f;
        _bezierProgress = 0f;
        _isHoming = false;

        SetBezierPoints();
    }

    private void Update()
    {
        if (BezierCurve == null) return;
        if (AttackTarget == null)
        {
            SpawnEnergyBulletHitVFX();
            Destroy(gameObject);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeMove)
        {
            Move();
        }
    }

    private void SetBezierPoints()
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint = AttackTarget.transform.position;

        BezierCurve.SetAllPoints(startPoint, endPoint);
    }

    private void Move()
    {
        if (_isHoming)
        {
            HomingMove();
        }
        else
        {
            BezierMove();
        }
    }

    private void BezierMove()
    {
        _bezierProgress += Time.deltaTime * _bezierMoveSpeed;
        _bezierProgress = Mathf.Clamp01(_bezierProgress);

        Vector3 targetPosition = BezierCurve.GetPointOnCurve(_bezierProgress);
        Vector3 direction = BezierCurve.GetDirectionOnCurve(_bezierProgress);

        SetRotation(direction);
        transform.position = targetPosition;

        if (_bezierProgress >= 1f)
        {
            _isHoming = true;
        }
    }

    private void HomingMove()
    {
        Vector3 direction = AttackTarget.transform.position - transform.position;

        direction.Normalize();
        SetRotation(direction);
        transform.position += (Vector3)(direction * _homingMoveSpeed) * Time.deltaTime;
    }

    private void SetRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Hit()
    {
        HitableObject hitTarget = AttackTarget.GetComponent<HitableObject>();
        hitTarget.ExecuteHitEvent(_damage, DamageIndicatorType.Normal);
    }

    private void SpawnEnergyBulletHitVFX()
    {
        if (_energyBulletHitVFXPrefab == null) return;

        GameObject hitVFX = Instantiate(_energyBulletHitVFXPrefab, transform.position, Quaternion.identity);
        Destroy(hitVFX, _energyBulletHitVFXLifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Boss")) return;

        SpawnEnergyBulletHitVFX();
        Hit();
        Destroy(gameObject);
    }
}
