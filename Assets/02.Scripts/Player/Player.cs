using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Player : MonoBehaviour
{
    [Header("플레이어 피격 가능 횟수")]
    [SerializeField] private int _health = 2;

    [Header("플레이어 피격시 무적 시간")]
    [SerializeField] private float _hitCoolTime = 2f;

    [Header("플레이어 피격시 VFX")]
    [SerializeField] private GameObject _hitVfx;
    [SerializeField] private GameObject _deathVfx;

    [Header("플레이어 피격시 타임 슬로우")]
    [SerializeField] private float _damagedSlowAmount = 0.1f;
    [SerializeField] private float _damagedSlowInDuration = 3f;
    [SerializeField] private float _damagedSlowHoldDuration = 0.15f;
    [SerializeField] private float _damagedSlowRecoverDuration = 1f;

    [Header("사망 시 남길 조명")]
    [SerializeField] private Light2D _deathLight;
    
    [Header("사망 시 조명 반경")]
    [SerializeField] private float _deathLightRadius = 0.3f;
    [SerializeField] private float _deathLightShrinkDuration = 0.7f;

    [Header("사망 시 이벤트 바인딩")]
    [SerializeField] private UnityEvent _deathEvent;
    private float _curTime;

    private bool _isHitCoolTime;
    private bool _isHittable = true;

    private Animator _animator;
    private BrokenWidowScreenController _brokenWidowScreenController;
    private PlayerStatusManager _playerStatusManager;
    private Sequence _damagedSlowSequence;

    private void Awake()
    {
        _brokenWidowScreenController =
            GetComponent<BrokenWidowScreenController>();
        _playerStatusManager=GetComponent<PlayerStatusManager>();
        
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


    public void InitPlayer()
    {
        _playerStatusManager.StartPlayerOriginSetting();
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

        

   
        
      

        if (_health <= 0)
        {
            Death();
        }
        else
        {
            DamagedEffect();

        }
    }

    public void DamagedEffect()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Hurt");
        }
        GetComponent<PlayerMove>().DashToReverseSideByHit();

        if (_brokenWidowScreenController != null)
        {
            _brokenWidowScreenController.ApplyBroken();
        }
        
        _damagedSlowSequence?.Kill();

        if (_hitVfx != null)
        {
            Instantiate(
                _hitVfx,
                transform.position,
                Quaternion.identity
            );
        }
        
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
        DeathEffect();
    }

private void DeathEffect()
{
    if (_animator != null)
    {
        _animator.SetTrigger("DeathStart");
    }
        GetComponent<PlayerMove>().DashToReverseSideByHit();

        _playerStatusManager.PlayerControlOff();

        if (_brokenWidowScreenController != null)
        {
            _brokenWidowScreenController.ApplyDeathBroken();
        }
        
    // 기존 피격 슬로우 종료
    _damagedSlowSequence?.Kill();
    _damagedSlowSequence = null;

    // 사망 후 추가 피격 방지
    _isHittable = false;

    // 사망 VFX
    if (_deathVfx != null)
    {
        Instantiate(
            _deathVfx,
            transform.position,
            Quaternion.identity
        );
    }

    // 지정한 조명만 남기기
    if (_deathLight != null)
    {
        Light2D[] lights = FindObjectsByType<Light2D>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Light2D light in lights)
        {
            light.enabled = light == _deathLight;
        }

        _deathLight.gameObject.SetActive(true);
        _deathLight.enabled = true;
    }
    else
    {
        Debug.LogWarning(
            "사망 시 남길 Light2D를 지정해주세요.",
            this
        );
    }

    Time.timeScale = 1f;

    // 게임 시간이 멈춰도 연출은 계속 실행
    _damagedSlowSequence = DOTween.Sequence()
        .SetUpdate(UpdateType.Normal, true);


    // 1. 슬로우 적용
    _damagedSlowSequence.Append(
        DOTween.To(
                () => Time.timeScale,
                value => Time.timeScale = value,
                0.05f,
                0.7f
            )
            .SetEase(Ease.OutQuad)
    );

// 슬로우 시작 후 0.35초에 포커싱 실행
    _damagedSlowSequence.InsertCallback(0.6f, () =>
    {
        if (CameraFocusingController.Instance != null)
        {
            //CameraFocusingController.Instance.Focus(transform, 2f);
        }
    });


    // 2. 실제 시간 기준으로 2초 유지
    _damagedSlowSequence.AppendInterval(2f);

  
    
    // 3. 게임 완전 정지
    _damagedSlowSequence.AppendCallback(() =>
    {
        Time.timeScale = 0f;
    });

    // 4. 게임이 정지된 상태에서 조명 급축소
    if (_deathLight != null)
    {
        float targetRadius = Mathf.Max(0f, _deathLightRadius);

        float startOuterRadius = _deathLight.pointLightOuterRadius;
        float startInnerRadius = _deathLight.pointLightInnerRadius;

        float innerRatio = startOuterRadius > 0f
            ? Mathf.Clamp01(startInnerRadius / startOuterRadius)
            : 0f;

        _damagedSlowSequence.Append(
            DOTween.To(
                    () => _deathLight != null
                        ? _deathLight.pointLightOuterRadius
                        : targetRadius,
                    radius =>
                    {
                        if (_deathLight == null)
                            return;

                        _deathLight.pointLightInnerRadius =
                            radius * innerRatio;

                        _deathLight.pointLightOuterRadius = radius;
                    },
                    targetRadius,
                    Mathf.Max(0.01f, _deathLightShrinkDuration)
                )
                .SetEase(Ease.OutExpo)
        );
    }

    // 5. 조명 축소 완료 후 카메라 포커싱
    _damagedSlowSequence.OnComplete(() =>
    {
        _damagedSlowSequence = null;

        if (_animator != null)
        {
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.SetTrigger("Death");
        }

        DOVirtual.DelayedCall(1f, () =>
        {
            if (_deathLight == null)
            {
                _deathEvent?.Invoke();
                return;
            }

            float duration = 0.5f;
            float startOuterRadius =
                _deathLight.pointLightOuterRadius;

            float startInnerRadius =
                _deathLight.pointLightInnerRadius;

            DOVirtual.Float(
                    1f,
                    0f,
                    duration,
                    value =>
                    {
                        if (_deathLight == null)
                            return;

                        _deathLight.pointLightInnerRadius =
                            startInnerRadius * value;

                        _deathLight.pointLightOuterRadius =
                            startOuterRadius * value;
                    })
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    Debug.Log("플레이어 사망");
                    _deathEvent?.Invoke();
                });

        }, true);
    });
}

  
    private void OnDestroy()
    {
        _damagedSlowSequence?.Kill();
        _damagedSlowSequence = null;

        Time.timeScale = 1f;
    }
}
