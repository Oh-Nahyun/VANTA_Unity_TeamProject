using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class Boss1BulletHellAttack : MonoBehaviour
{
    [SerializeField] private Transform _aimedHeadPivot;
    [SerializeField] private Transform _splitHeadPivot;
    [SerializeField] private Transform _playerTransform;
    
    [SerializeField] private Boss1Bullet _bulletPrefab;
    [SerializeField] private Boss1SplitBullet _splitBulletPrefab;
    
    [SerializeField] private float _aimedFireInteval = 0.5f;
    [SerializeField] private int _aimedFireCount = 8;

    [SerializeField] private float _splitFireInteval = 1f;
    [SerializeField] private int _splitFireCount = 5;
    
    [SerializeField] private float _patternInterval = 0.5f;
    
    private Coroutine _splitAttackCoroutiine;

    private BulletHellState _currentState;
    
    private float _timer;
    private int _currentFireCount;
    private int _currentSplitFireCount;
    
    public bool IsFinished { get; private set; }

    private enum BulletHellState
    {
        Aimed,
        Split,
        End
    }

    public void EnterAttack()
    {
        IsFinished = false;
        _timer = 0f;
        _currentFireCount = 0;
        ChangeState(BulletHellState.Aimed);

        Debug.Log("BulletHell 패턴 시작");
    }

    public void UpdateAttack()
    {
        switch (_currentState)
        {
            case BulletHellState.Aimed:
                UpdateAimedAttack();
                break;
            case BulletHellState.Split:
                UpdateSplitAttack();
                break;
            case BulletHellState.End:
                IsFinished = true;
                break;
        }
    }

    public void ExitAttack()
    {
        Debug.Log("BulleHell 패턴 종료");
    }

    private void UpdateAimedAttack()
    {
        _timer += Time.deltaTime;
        if (_timer < _aimedFireInteval)
        {
            return;
        }

        FireAimedBullet();
        _timer = 0f;
        _currentFireCount++;
        if (_currentFireCount >= _aimedFireCount)
        {
            ChangeState(BulletHellState.Split);
        }
    }

    private void UpdateSplitAttack()
    {
        _timer += Time.deltaTime;
        if (_timer < _splitFireInteval)
        {
            return;
        }

        _currentSplitFireCount = 0;
        FireSplitBullet();
        _timer = 0f;
        _currentSplitFireCount++;
        if (_currentSplitFireCount >= _aimedFireCount)
        {
            ChangeState(BulletHellState.End);
        }
    }
    

    private void FireAimedBullet()
    {
        Vector2 direction = (_playerTransform.position - _aimedHeadPivot.position).normalized;
        Boss1Bullet bullet = Instantiate(_bulletPrefab, _aimedHeadPivot.position, Quaternion.identity);
        bullet.Initialize(direction);
    }

    private void FireSplitBullet()
    {
        Vector2 direction = (_playerTransform.position - _splitHeadPivot.position).normalized;
        Boss1SplitBullet splitBullet = Instantiate(_splitBulletPrefab, _splitHeadPivot.position, Quaternion.identity);
        splitBullet.Initialize(direction);
    }

    private void ChangeState(BulletHellState newState)
    {
        _currentState = newState;
        _timer = 0f;
        _currentFireCount = 0;
        Debug.Log($"BulletHell 현재 패턴 : {_currentState}");
    }
}
