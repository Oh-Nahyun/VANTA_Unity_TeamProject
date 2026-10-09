using System;
using UnityEngine;

public class Boss1SplitBulletAttack : MonoBehaviour, IBoss1RangedAttack
{
    [SerializeField] private Transform _splitHeadPivot;
    private Transform _playerTransform;
    [SerializeField] private Boss1SplitBullet _splitBulletPrefab;

    [Serializable]
    private class PhaseSettings
    {
        [Header("분열탄 발사 간격")]
        [SerializeField] private float _splitFireInterval = 0.5f;
        
        [Header("분열탄 발사 횟수")]
        [SerializeField] private int _splitFireCount = 8;
        
        public float SplitFireInterval => _splitFireInterval;
        public int SplitFireCount => _splitFireCount;
    }
    
    [Header("페이즈별 난이도 세팅")]
    [SerializeField] private PhaseSettings _phase1Settings;
    [SerializeField] private PhaseSettings _phase2Settings;
    
    [Header("머리 인디케이터")]
    [SerializeField] private Boss1HeadIndicator _headIndicator;
    
    private Boss1PhaseController _boss1PhaseController;
    private Boss1ProjectileManager _projectileManager;
    
    private float _timer;
    private int _currentFireCount;
    
    private bool _isPhase2;
    
    public bool IsFinished { get; private set; }
    
    private PhaseSettings CurrentPhaseSettings => _isPhase2 ? _phase2Settings : _phase1Settings;

    private void Awake()
    {
        _boss1PhaseController = GetComponent<Boss1PhaseController>();
        _projectileManager = GetComponent<Boss1ProjectileManager>();
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
        if (_timer < CurrentPhaseSettings.SplitFireInterval)
        {
            return;
        }

        FireSplitBullet();
        _timer = 0f;
        _currentFireCount++;
        if (_currentFireCount >= CurrentPhaseSettings.SplitFireCount)
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

    
    private void FireSplitBullet()
    {
        Vector2 direction = (_playerTransform.position - _splitHeadPivot.position).normalized;
        Boss1SplitBullet splitBullet = Instantiate(_splitBulletPrefab, _splitHeadPivot.position, Quaternion.identity);
        splitBullet.Initialize(direction, _projectileManager);
    }
}
