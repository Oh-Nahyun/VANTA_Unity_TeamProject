using System;
using UnityEngine;

public class Boss1DeathController : MonoBehaviour
{
    [SerializeField] private Vector2 _center = new Vector2(0.42f, -0.74f);
    
    [SerializeField] private float _startMoveSpeed= 1f;
    [SerializeField] private float _endMoveSpeed = 0.6f;
    [SerializeField] private float _slowDownDistance = 4f;
    [SerializeField] private float _arrivalDistance = 0.1f;
    
    private Boss1Life _boss1Life;
    private Boss1AttackController _boss1AttackController;
    private Boss1Movement _boss1Movement;
    private Boss1ProjectileManager _boss1ProjectileManager;

    private bool _isMovingToCenter;

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
        
        float moveSpeed = Mathf.Lerp(_startMoveSpeed, _endMoveSpeed, distanceRatio);
        
        _boss1Movement.MoveInDirection(direction, moveSpeed);
    }

    private void ArriveAtCenter()
    {
        _isMovingToCenter = false;
        _boss1Movement.Stop();
    }
}
