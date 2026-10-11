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

    [Header("펫 에너지 탄 비행 효과음")]
    [SerializeField] private AudioClip _energyBulletFlySound;

    [Header("펫 에너지 탄 피격 효과음")]
    [SerializeField] private AudioClip _energyBulletHitSound;

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

    /// <summary>
    /// 펫 에너지 탄 비행 효과음 재생 여부 확인용 변수
    /// </summary>
    private bool _isFlySoundPlayed;

    /// <summary>
    /// 펫 에너지 탄이 비행 효과음을 재생해도 되는지 확인용 변수
    /// </summary>
    public bool IsPlayFlySound;

    /// <summary>
    /// 펫 에너지 탄 타겟 상실 연출 클래스
    /// </summary>
    private EnergyBulletDissolveVFX _energyBulletDissolveVFX;

    private void Awake()
    {
        BezierCurve = GetComponent<BezierCurve>();
        _energyBulletDissolveVFX = GetComponent<EnergyBulletDissolveVFX>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeMove = 0.6f;
        _bezierProgress = 0f;
        _isHoming = false;
        _isFlySoundPlayed = false;
        IsPlayFlySound = true;

        if (BezierCurve == null) return;
        if (AttackTarget == null) return;

        SetBezierPoints();
    }

    private void Update()
    {
        if (BezierCurve == null) return;
        if (_energyBulletDissolveVFX != null && _energyBulletDissolveVFX.IsDissolving) return;

        if (AttackTarget == null)
        {
            if (_energyBulletDissolveVFX != null)
            {
                _energyBulletDissolveVFX.StartDissolve();
            }
            else
            {
                Destroy(gameObject);
            }

            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeMove)
        {
            PlayFlySound();
            Move();
        }
    }

    private void PlayFlySound()
    {
        if (_isFlySoundPlayed) return;

        _isFlySoundPlayed = true;

        if (!IsPlayFlySound) return;

        SoundManager.Instance.PlayEffectSound(_energyBulletFlySound);
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
        if (_energyBulletDissolveVFX != null && _energyBulletDissolveVFX.IsDissolving) return;
        if (!other.CompareTag("Boss")) return;

        SoundManager.Instance.PlayEffectSound(_energyBulletHitSound);
        SpawnEnergyBulletHitVFX();
        Hit();
        Destroy(gameObject);
    }
}
