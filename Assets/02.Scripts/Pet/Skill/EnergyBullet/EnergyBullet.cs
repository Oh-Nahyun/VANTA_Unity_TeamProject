using UnityEngine;

public class EnergyBullet : MonoBehaviour
{
    [Header("펫 에너지 탄 베지어 이동 속도")]
    [SerializeField] private float _bezierMoveSpeed = 1f;

    [Header("펫 에너지 탄 유도 이동 속도")]
    [SerializeField] private float _homingMoveSpeed = 5f;

    [Header("펫 에너지 탄 공격력")]
    [SerializeField] private float _damage = 10f;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    private GameObject _attackTarget;

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
        _attackTarget = GameObject.FindGameObjectWithTag("Boss");

        if (BezierCurve == null)
            return;
        if (_attackTarget == null)
            return;

        _timer = 0f;
        _timeBeforeMove = 0.6f;
        _bezierProgress = 0f;
        _isHoming = false;

        SetBezierPoints();
    }

    private void Update()
    {
        if (BezierCurve == null)
            return;
        if (_attackTarget == null)
            return;

        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeMove)
        {
            Move();
        }
    }

    private void SetBezierPoints()
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint = _attackTarget.transform.position;

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
        Vector3 direction = _attackTarget.transform.position - transform.position;

        direction.Normalize();
        SetRotation(direction);
        transform.position += (Vector3)(direction * _homingMoveSpeed) * Time.deltaTime;
    }

    private void SetRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Hit()
    {
        // TODO : [펫] 발동 조건(플레이어 상태) 설정 및 몬스터 데미지 처리하기
        // _monster.TakeDamage(_energyBulletDamage);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Monster"))
            return;

        Hit();
    }
}
