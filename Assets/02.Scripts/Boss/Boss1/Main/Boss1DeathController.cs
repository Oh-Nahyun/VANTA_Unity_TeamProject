using System;
using System.Collections;
using UnityEngine;

public class Boss1DeathController : MonoBehaviour
{
    [Header("보스2 등장 연출")]
    [SerializeField] private Boss2IntroCinematic _boss2Intro;
    
    [Header("주저앉은 후 대기 시간")]
    [SerializeField] private float _collapseWaitDuration = 1.5f;
    
    // 임시: 폭발 연출 시간, 지금은 debug log만
    [Header("임시 폭발 연출 시간")]
    [SerializeField] private float _explosionDuration = 2f;
    
    [SerializeField] private Vector2 _center = new Vector2(0.42f, -0.74f);
    
    [SerializeField] private float _startMoveSpeed= 1f;
    [SerializeField] private float _endMoveSpeed = 0.6f;
    [SerializeField] private float _slowDownDistance = 4f;
    [SerializeField] private float _arrivalDistance = 0.1f;
    
    private Boss1Life _boss1Life;
    private Boss1AttackController _boss1AttackController;
    private Boss1Movement _boss1Movement;
    private Boss1ProjectileManager _boss1ProjectileManager;
    [SerializeField] private Animator _animator;

    private bool _isMovingToCenter;
    private bool _isExploding;

    public event Action OnCollapseFinished;
    public event Action OnExplosionFinished;
    
    private void Awake()
    {
        _boss1Life = GetComponent<Boss1Life>();
        _boss1AttackController = GetComponent<Boss1AttackController>();
        _boss1Movement = GetComponent<Boss1Movement>();
        _boss1ProjectileManager = GetComponent<Boss1ProjectileManager>();
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
    }
    
    public void EndDeathCollapse()
    {
        _animator.Play("DeathIdle", 0, 0f);

        StartCoroutine(CollapseWaitRoutine());
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
        Debug.Log("폭발 요청 폭발 시작 일단 2초");

        // 폭발 연출 넣기
        yield return new WaitForSecondsRealtime(_explosionDuration);

        Debug.Log("폭발 끝");
        OnExplosionFinished?.Invoke();

        Destroy(gameObject);
    }
}
