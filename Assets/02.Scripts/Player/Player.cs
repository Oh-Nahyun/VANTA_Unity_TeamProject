using DG.Tweening;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("플레이어 피격 가능 횟수")]
    [SerializeField] private int _health = 2;

    [Header("플레이어 피격시 무적 시간")]
    [SerializeField] private float _hitCoolTime = 2f;

    [Header("플레이어 피격시 VFX")]
    [SerializeField] private GameObject _hitVfx;

    [Header("플레이어 피격시 타임 슬로우")]
    [SerializeField] private float _damagedSlowAmount = 0.1f;
    [SerializeField] private float _damagedSlowInDuration = 3f;
    [SerializeField] private float _damagedSlowHoldDuration = 0.15f;
    [SerializeField] private float _damagedSlowRecoverDuration = 1f;

    
    private float _curTime;

    private bool _isHitCoolTime;
    private bool _isHittable = true;

    private Animator _animator;
    private BrokenWidowScreenController _brokenWidowScreenController;

    private Sequence _damagedSlowSequence;

    private void Awake()
    {
        _brokenWidowScreenController =
            GetComponent<BrokenWidowScreenController>();

        _animator =
            GetComponent<Animator>();
    }

    private void Update()
    {
        CalculateHitCoolTime();

        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage();
        }
    }

    private void CalculateHitCoolTime()
    {
        if (!_isHitCoolTime)
            return;

        _curTime += Time.unscaledDeltaTime;

        if (_curTime >= _hitCoolTime)
        {
            _curTime = 0f;
            _isHitCoolTime = false;
        }
    }

    public void OnPlayerHittable()
    {
        _isHittable = true;
    }

    public void OffPlayerHittable()
    {
        _isHittable = false;
    }

    public void TakeDamage()
    {
        if (_isHitCoolTime || !_isHittable)
            return;

        _isHitCoolTime = true;
        _curTime = 0f;

        _health--;

        DamagedEffect();
        GetComponent<PlayerMove>().DashToReverseSideByHit();
        Debug.Log("데미지입음");

        if (_animator != null)
        {
            _animator.SetTrigger("Hurt");
        }

        if (_hitVfx != null)
        {
            Instantiate(
                _hitVfx,
                transform.position,
                Quaternion.identity
            );
        }

        if (_brokenWidowScreenController != null)
        {
            _brokenWidowScreenController.ApplyBroken();
        }

        if (_health <= 0)
        {
            Death();
        }
    }

    private void DamagedEffect()
    {
        _damagedSlowSequence?.Kill();

        Time.timeScale = 1f;

        _damagedSlowSequence = DOTween.Sequence()
            .SetUpdate(UpdateType.Normal, true);

        _damagedSlowSequence.Append(
            DOTween.To(
                    () => Time.timeScale,
                    value => Time.timeScale = value,
                    0.05f,
                    0.7f
                )
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, true)
        );

        _damagedSlowSequence.AppendInterval(2f);

        _damagedSlowSequence.Append(
            DOTween.To(
                    () => Time.timeScale,
                    value => Time.timeScale = value,
                    1f,
                    1f
                )
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Normal, true)
        );

        _damagedSlowSequence.OnComplete(() =>
        {
            Time.timeScale = 1f;
            _damagedSlowSequence = null;
        });
    }

    private void Death()
    {
    }

    private void OnDestroy()
    {
        _damagedSlowSequence?.Kill();
        _damagedSlowSequence = null;

        Time.timeScale = 1f;
    }
}
