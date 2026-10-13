using System;
using UnityEngine;

public class Boss1LaserAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Boss1Laser _laser;
    [SerializeField] private Transform _laserHeadPivot;

    private Transform _playerTransform;

    [Serializable]
    private class PhaseSettings
    {
        [Header("조준 시간 (경고선)")]
        [SerializeField] private float _aimDuration = 1f;

        [Header("발사 지속 시간")]
        [SerializeField] private float _fireDuration = 3f;

        [Header("발사 직후 경고선 자리에 고정되는 시간")]
        [SerializeField] private float _trackDelay = 0.2f;

        [Header("추적 회전 속도 (초당 각도): 시작 → 최대")]
        [SerializeField] private float _turnSpeedStart = 20f;
        [SerializeField] private float _turnSpeedMax = 45f;

        [Header("최대 회전 속도까지 걸리는 시간")]
        [SerializeField] private float _turnAccelTime = 1.5f;

        public float AimDuration => _aimDuration;
        public float FireDuration => _fireDuration;
        public float TrackDelay => _trackDelay;
        public float TurnSpeedStart => _turnSpeedStart;
        public float TurnSpeedMax => _turnSpeedMax;
        public float TurnAccelTime => _turnAccelTime;
    }

    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;

    [Header("머리 인디케이터")]
    [SerializeField] private Boss1HeadIndicator _headIndicator;

    [Header("입 섬광")]
    [SerializeField] private Boss1MuzzleFlash _muzzleFlash;
    
    [Header("정반대편에서 회전 방향 유지 기준 각도")]
    [SerializeField] private float _keepTurnDirectionAngle = 150f;
    
    [Header("각도 차이가 클수록 빨리 돎")]
    [SerializeField] private float _catchUpStartAngle = 45f;  // 이 각도부터 빨라지기 시작
    [SerializeField] private float _catchUpMultiplier = 4f;

    private float _turnSign;

    private Boss1PhaseController _boss1PhaseController;
    private Boss1Sound _boss1Sound;

    private float _timer;
    private LaserState _currentState;

    private Vector2 _lockedDirection;
    private float _lockedAngle;
    private float _currentAngle;

    private bool _isPhase2;

    public bool IsFinished { get; private set; }

    private enum LaserState
    {
        Aim,
        Fire,
        End
    }

    private PhaseSettings CurrentPhaseSettings =>
        _isPhase2 ? _phase2Settings : _phase1Settings;

    private void Awake()
    {
        _boss1PhaseController = GetComponent<Boss1PhaseController>();
        _boss1Sound = GetComponent<Boss1Sound>();
    }

    private void OnEnable()
    {
        _boss1PhaseController.OnPhaseChanged += ChangePhase;
    }

    private void OnDisable()
    {
        _boss1PhaseController.OnPhaseChanged -= ChangePhase;
    }

    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        _laser.Hide();
    }

    private void ChangePhase(Boss1PhaseController.Boss1Phase phase)
    {
        _isPhase2 = phase == Boss1PhaseController.Boss1Phase.phase2;
    }

    public void EnterAttack()
    {
        IsFinished = false;

        _laser.Hide();
        ChangeState(LaserState.Aim);

        if (_headIndicator != null)
        {
            _headIndicator.ShowHeadIndicator();
        }
    }

    public void UpdateAttack()
    {
        switch (_currentState)
        {
            case LaserState.Aim:
                UpdateAim();
                break;

            case LaserState.Fire:
                UpdateFire();
                break;

            case LaserState.End:
                IsFinished = true;
                break;
        }
    }

    public void ExitAttack()
    {
        _boss1Sound.Stop(Boss1SoundType.LaserCharge);
        _boss1Sound.Stop(Boss1SoundType.LaserChargeP2);
        _boss1Sound.Stop(Boss1SoundType.LaserFire);

        _laser.Hide();

        if (_muzzleFlash != null)
        {
            _muzzleFlash.StopHold();
        }

        if (_headIndicator != null)
        {
            _headIndicator.HideHeadIndicator();
        }
    }

    private void UpdateAim()
    {
        // Aim 첫 프레임에만 플레이어 방향을 저장
        if (_timer == 0f)
        {
            _lockedDirection = (_playerTransform.position - _laserHeadPivot.position).normalized;
            _lockedAngle = DirectionToAngle(_lockedDirection);
            _boss1Sound.Play(_isPhase2 ? Boss1SoundType.LaserChargeP2 : Boss1SoundType.LaserCharge);
        }

        _timer += Time.deltaTime;

        // 데미지 없는 경고선
        _laser.ShowAim(_lockedDirection, _timer / CurrentPhaseSettings.AimDuration);

        if (_timer < CurrentPhaseSettings.AimDuration)
        {
            return;
        }

        // 발사는 경고선이 보여 준 자리에서 시작
        _currentAngle = _lockedAngle;
        ChangeState(LaserState.Fire);
    }

    private void UpdateFire()
    {
        _timer += Time.deltaTime;

        // 잠깐 고정된 뒤부터 플레이어 쪽으로 천천히 회전
        if (_timer >= CurrentPhaseSettings.TrackDelay)
        {
            float trackTime = _timer - CurrentPhaseSettings.TrackDelay;
            float accel = CurrentPhaseSettings.TurnAccelTime > 0f
                ? Mathf.Clamp01(trackTime / CurrentPhaseSettings.TurnAccelTime)
                : 1f;

            float turnSpeed = Mathf.Lerp(CurrentPhaseSettings.TurnSpeedStart,
                                         CurrentPhaseSettings.TurnSpeedMax, accel);

            float delta = Mathf.DeltaAngle(_currentAngle, GetAngleToPlayer());

            if (_turnSign != 0f && Mathf.Abs(delta) > _keepTurnDirectionAngle && Mathf.Sign(delta) != _turnSign)
            {
                delta = _turnSign * (360f - Mathf.Abs(delta));
            }
            
            float catchUp = Mathf.InverseLerp(_catchUpStartAngle, 180f, Mathf.Abs(delta));
            float finalSpeed = turnSpeed * Mathf.Lerp(1f, _catchUpMultiplier, catchUp);

            float maxStep = finalSpeed * Time.deltaTime;
            
            float step = Mathf.Clamp(delta, -maxStep, maxStep);
            _currentAngle += step;

            if (Mathf.Abs(step) > 0.0001f)
            {
                _turnSign = Mathf.Sign(step);
            }
        }

        // 실제 레이저 표시 + 피격 검사
        _laser.Fire(AngleToDirection(_currentAngle));

        if (_timer < CurrentPhaseSettings.FireDuration)
        {
            return;
        }

        ChangeState(LaserState.End);
    }

    private void ChangeState(LaserState newState)
    {
        _currentState = newState;
        _timer = 0f;

        if (_currentState == LaserState.Fire)
        {
            _turnSign = 0f; 
            _laser.HideAim();

            _boss1Sound.Play(Boss1SoundType.LaserFire);

            if (_muzzleFlash != null)
            {
                _muzzleFlash.StartHold();
            }
        }

        if (_currentState == LaserState.End)
        {
            _laser.Hide();

            if (_muzzleFlash != null)
            {
                _muzzleFlash.StopHold();
            }
        }
    }

    private float GetAngleToPlayer()
    {
        Vector2 direction = (_playerTransform.position - _laserHeadPivot.position).normalized;
        return DirectionToAngle(direction);
    }

    private static float DirectionToAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private static Vector2 AngleToDirection(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
