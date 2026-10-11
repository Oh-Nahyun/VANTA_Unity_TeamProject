using System;
using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Boss1DeathController : MonoBehaviour
{
    [Header("보스2 등장 연출")]
    [SerializeField] private Boss2IntroCinematic _boss2Intro;
    
    [Header("주저앉은 후 대기 시간")]
    [SerializeField] private float _collapseWaitDuration = 1.5f;
    
    [Header("폭발 연출")]
    [SerializeField] private float _explosionDuration = 2f;      // 폭발 시작 ~ 보스2 인트로에 신호 보낼 때까지
    [SerializeField] private float _explosionImpactTime = 0.1f;  // Death 애니에서 터지는 프레임

    [SerializeField] private CinemachineImpulseSource _explosionImpulseSource;
    [SerializeField] private float _explosionImpulseForce = 0.6f;

    [SerializeField] private Light2D _explosionLight;
    [SerializeField] private float _explosionLightIntensity = 4f;
    [SerializeField] private float _explosionLightDuration = 0.6f;

    [SerializeField] private GameObject _explosionParticlePrefab;
    [SerializeField] private Vector2 _explosionOffset = new Vector2(0f, 1f);
    
    [SerializeField] private Vector2 _center = new Vector2(0.42f, -0.74f);
    
    [SerializeField] private Boss1DeathBloodOverlay _bloodOverlayPrefab;
    
    [Header("중앙으로 걸어가는 속도")]
    [SerializeField] private float _startMoveSpeed= 1f;
    [SerializeField] private float _endMoveSpeed = 0.6f;
    [SerializeField] private float _slowDownDistance = 4f;
    [SerializeField] private float _arrivalDistance = 0.1f;
    
    [Header("사망 후 색 변화")]
    [SerializeField] private float _colorRestoreDuration = 1.2f; // _collapseWaitDuration(1.5)보다 짧게
    [SerializeField] private Boss1Shadow _boss1Shadow;
    private Boss1HitFlash _boss1HitFlash;
    
    private Boss1Life _boss1Life;
    private Boss1AttackController _boss1AttackController;
    private Boss1Movement _boss1Movement;
    private Boss1ProjectileManager _boss1ProjectileManager;
    [SerializeField] private Animator _animator;

    private bool _isMovingToCenter;
    private bool _isExploding;
    
    private Boss1Sound _boss1Sound;

    public event Action OnCollapseFinished;
    public event Action OnExplosionFinished;
    
    private void Awake()
    {
        _boss1Life = GetComponent<Boss1Life>();
        _boss1AttackController = GetComponent<Boss1AttackController>();
        _boss1Movement = GetComponent<Boss1Movement>();
        _boss1ProjectileManager = GetComponent<Boss1ProjectileManager>();
        _boss1HitFlash = GetComponent<Boss1HitFlash>();
        _boss1Sound = GetComponent<Boss1Sound>();
        
        if (_explosionLight != null)
        {
            _explosionLight.intensity = 0f;
        }
    }

    private void OnEnable()
    {
        _boss1Life.OnDeath += StartDeathSequence;
    }
    
    private void OnDisable()
    {
        _boss1Life.OnDeath -= StartDeathSequence;
    }
    
    private void FixedUpdate()
    {
        if (!_isMovingToCenter)
        {
            return;
        }

        MoveToCenter();
    }

    private void StartDeathSequence()
    {
        foreach (Collider2D collider in GetComponents<Collider2D>())
        {
            collider.enabled = false;
        }
        
        _boss1AttackController.LockAttack();
        _boss1Movement.Stop();
        _boss1ProjectileManager.DestroyAllProjectiles();
        
        _boss1Movement.StartControlled();
        
        _boss1Movement.FaceTarget(_center);
        _boss1Movement.LockFacing();
        
        _isMovingToCenter = true;
    }

    private void MoveToCenter()
    {
        Vector2 currentPos = transform.position;
        Vector2 targetPos = _center;
        
        Vector2 direction = targetPos - currentPos;
        float distance = direction.magnitude;

        if (distance <= _arrivalDistance)
        {
            ArriveAtCenter();
            return;
        }

        float distanceRatio = Mathf.Clamp01(distance / _slowDownDistance);
        
        float moveSpeed = Mathf.Lerp(_endMoveSpeed, _startMoveSpeed, distanceRatio);
        
        _boss1Movement.MoveInDirection(direction, moveSpeed);
    }

    private void ArriveAtCenter()
    {
        _isMovingToCenter = false;
        _boss1Movement.Stop();
        _animator.Play("DeathCollapse", 0, 0f);
        _boss1Sound.Play(Boss1SoundType.DeathCollapse);
    }
    
    public void EndDeathCollapse()
    {
        _animator.Play("DeathIdle", 0, 0f);
        RestoreColor();

        StartCoroutine(CollapseWaitRoutine());
    }
    
    private void RestoreColor()
    {
        if (_boss1HitFlash == null)
        {
            return;
        }

        DOVirtual.Float(_boss1HitFlash.CurrentPhase2Amount, 0f, _colorRestoreDuration, value =>
            {
                _boss1HitFlash.SetPhase2Amount(value);
                if (_boss1Shadow != null)
                {
                    _boss1Shadow.SetPhase2Amount(value);
                }
            })
            .SetEase(Ease.InQuad)
            .SetLink(gameObject);
    }

    private IEnumerator CollapseWaitRoutine()
    {
        yield return new WaitForSeconds(_collapseWaitDuration);
        
        OnCollapseFinished?.Invoke();
        
        if (_boss2Intro != null)
        {
            _boss2Intro.Play(null);
        }
        
        else
        {
            Debug.LogWarning("_boss2Intro가 연결이 안됨");
            PlayDeathExplosion();
        }
    }
    
    public void PlayDeathExplosion()
    {
        if (_isExploding)
        {
            return;
        }

        _isExploding = true;
        StartCoroutine(DeathExplosionRoutine());
    }
    
    private IEnumerator DeathExplosionRoutine()
    {
        _animator.Play("Death", 0, 0f);

        yield return new WaitForSeconds(_explosionImpactTime);
        PlayExplosionImpact();

        yield return new WaitForSeconds(_explosionDuration - _explosionImpactTime);

        OnExplosionFinished?.Invoke();
        Destroy(gameObject);
    }
    
    private void PlayExplosionImpact()
    {
        _boss1Sound.Play(Boss1SoundType.DeathExplosion);
        
        if (_explosionImpulseSource != null)
        {
            _explosionImpulseSource.GenerateImpulseWithVelocity(Vector3.down * _explosionImpulseForce);
        }

        if (_explosionLight != null)
        {
            _explosionLight.intensity = _explosionLightIntensity;
            DOTween.To(() => _explosionLight.intensity, x => _explosionLight.intensity = x, 0f, _explosionLightDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(_explosionLight.gameObject);
        }

        if (_explosionParticlePrefab != null)
        {
            Vector3 position = transform.position + (Vector3)_explosionOffset;
            GameObject gore = Instantiate(_explosionParticlePrefab, position, Quaternion.identity);
            gore.GetComponent<ParticleSystem>().Play();
            Destroy(gore, 3f);
        }
        
        if (_bloodOverlayPrefab != null)
        {
            Instantiate(_bloodOverlayPrefab).Play();
            _boss1Sound.Play(Boss1SoundType.DeathSplat);
        }
    }
}
