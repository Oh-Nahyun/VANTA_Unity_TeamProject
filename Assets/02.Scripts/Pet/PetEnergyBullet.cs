using UnityEngine;

public class PetEnergyBullet : MonoBehaviour
{
    [Header("펫 에너지 탄 이동 속도")]
    [SerializeField] private float _moveSpeed = 1f;
    
    [Header("펫 에너지 탄 공격력")]
    [SerializeField] private float _damage = 10f;
    
    [Header("펫 에너지 탄 이동 곡선 정도")]
    [SerializeField] private float _curveAmount = 2f;
    
    [Header("펫 에너지 탄 이동 곡선의 제어점 위치 변경용 변수 (0과 가까울수록 시작점과 가까워짐)")]
    [Range(0f, 1f)]
    [SerializeField] private float _controlPointPositionRatio = 0.2f;
    
    [Header("펫 에너지 탄 이동 곡선 클래스")]
    [SerializeField] private BezierCurve _bezierCurve;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    private GameObject _monster;
    
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

    private void Start()
    {
        if (_bezierCurve == null) return;
        if (_monster == null)
        {
            _monster = GameObject.FindGameObjectWithTag("Monster");
            if (_monster == null) return;
        }
        
        _timer = 0f;
        _timeBeforeMove = 0.6f;
        _bezierProgress = 0f;

        SetBezierPoints();
    }

    private void Update()
    {
        if (_bezierCurve == null) return;
        if (_monster == null) return;
        
        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeMove)
        {
            Move();
        }
    }
    
    private void SetBezierPoints()
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint = _monster.transform.position;
        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3 vertical = new Vector3(-direction.y, direction.x, 0f);
        Vector3 controlBasePoint = Vector3.Lerp(startPoint, endPoint, _controlPointPositionRatio);
        Vector3 controlPoint = controlBasePoint + vertical * _curveAmount;

        _bezierCurve.SetPoints(startPoint, controlPoint, endPoint);
    }

    private void Move()
    {
        _bezierProgress += Time.deltaTime * _moveSpeed;
        _bezierProgress = Mathf.Clamp01(_bezierProgress);
        _bezierCurve.SetEndPoint(_monster.transform.position);
        
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = _bezierCurve.GetPointOnCurve(_bezierProgress);
        Vector3 direction = targetPosition - currentPosition;

        if (direction != Vector3.zero)
        {
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        transform.position = targetPosition;
    }

    private void Hit()
    {
        // TODO : [펫] 발동 조건(플레이어 상태) 설정 및 몬스터 데미지 처리하기
        // _monster.TakeDamage(_energyBulletDamage);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Monster")) return;
        
        Hit();
    }
}
