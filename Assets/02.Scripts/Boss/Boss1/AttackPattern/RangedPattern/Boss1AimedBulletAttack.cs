using System;
using UnityEngine;

public class Boss1AimedBulletAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Transform _aimedHeadPivot;
    private Transform _playerTransform;
    [SerializeField] private Boss1Bullet _bulletPrefab;
    
    [Serializable]
    private class PhaseSettings
    {
        [Header("조준탄 발사 간격")]
        [SerializeField] private float _aimedFireInterval = 0.5f;
        
        [Header("조준탄 발사 횟수")]
        [SerializeField] private int _aimedFireCount = 8;
        
        public float AimedFireInterval => _aimedFireInterval;
        public int AimedFireCount => _aimedFireCount;
    }
    
    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;
    
    [Header("머리 인디케이터")]
    [SerializeField] private Boss1HeadIndicator _headIndicator;
    
    [Header("발사 섬광")]
    [SerializeField] private Boss1MuzzleFlash _muzzleFlash;
    
    private Boss1PhaseController _boss1PhaseController;
    private Boss1ProjectileManager _projectileManager;
    
    private float _timer;
    private int _currentFireCount;

    private bool _isPhase2;
    
    private Boss1Sound _boss1Sound;
    
    public bool IsFinished { get; private set; }
    
    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    private void Awake()
    {
        _boss1PhaseController = GetComponent<Boss1PhaseController>();
        _projectileManager = GetComponent<Boss1ProjectileManager>();
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

    private void ChangePhase(Boss1PhaseController.Boss1Phase phase)
    {
        _isPhase2 = phase == Boss1PhaseController.Boss1Phase.phase2;
    }
    
    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    public void EnterAttack()
    {
        _timer = 0f;
        _currentFireCount = 0;
        IsFinished = false;
        
        if (_headIndicator != null)
        {
            _headIndicator.ShowHeadIndicator();
        }
    }

    public void UpdateAttack()
    {
        if (IsFinished)
        {
            return;
        }
        
        _timer += Time.deltaTime;
        if (_timer < CurrentPhaseSettings.AimedFireInterval)
        {
            return;
        }

        FireAimedBullet();
        _timer = 0f;
        _currentFireCount++;
        if (_currentFireCount >= CurrentPhaseSettings.AimedFireCount)
        {
            IsFinished = true;
        }
    }

    public void ExitAttack()
    {
        if (_headIndicator != null)
        {
            _headIndicator.HideHeadIndicator();
        }
    }

    private void FireAimedBullet()
    {
        Vector2 direction = (_playerTransform.position - _aimedHeadPivot.position).normalized;
        Boss1Bullet bullet = Instantiate(_bulletPrefab, _aimedHeadPivot.position, Quaternion.identity);
        bullet.Initialize(direction, _projectileManager);
        _boss1Sound.Play(Boss1SoundType.ShotAimed);
        
        if (_muzzleFlash != null)
        {
            _muzzleFlash.Play();
        }
    }
}
