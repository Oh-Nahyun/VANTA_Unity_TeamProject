using System;
using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Boss1PhaseController : MonoBehaviour
    {
        public enum Boss1Phase
        {
            phase1,
            phase2
        }
        
        [SerializeField] private Boss1Life _boss1Life;
        [SerializeField] private Animator _animator;
        [SerializeField] private Boss1Shadow _boss1Shadow;
        private Boss1AttackController _boss1AttackController;
        private Boss1Movement _boss1Movement;
        private Boss1ProjectileManager _boss1ProjectileManager;
        private BoxCollider2D _boxCollider2D;
        private Boss1HitFlash _boss1HitFlash;

        [Header("깨지는 순간")]
        [SerializeField] private float _breakFlashDuration = 0.15f;
        [SerializeField] private CinemachineImpulseSource _breakImpulseSource;
        [SerializeField] private float _breakImpulseForce = 0.8f;
        [SerializeField] private Light2D _breakLight;
        [SerializeField] private float _breakLightIntensity = 4f;
        [SerializeField] private float _breakLightDuration = 0.4f;
        [SerializeField] private float _breakHoldTime = 0.3f;
        
        [Header("2페이즈 전환 연출")]
        [SerializeField] private float _buffDuration = 1.6f;
        [SerializeField] private float _colorChangeDuration = 3f;
        [SerializeField] private float _endDelay = 0.2f;
        
        [Header("포효 연출")]
        [SerializeField] private float _roarDuration = 1.5f;
        [SerializeField] private float _roarStrength = 1.3f; // 등장보다 강하게 1f 이상으로

        private Boss1RoarEffect _boss1RoarEffect;

        private Coroutine _transitionCoroutine;
        private Tween _breakLightTween;
        
        private bool _isPhase2Transitioning; // 지금 페이즈 전환을 진행 중인지
        
        private Boss1Sound _boss1Sound;

        public Boss1Phase CurrentPhase {get; private set;}

        public event Action<Boss1Phase> OnPhaseChanged; // 페이즈 바꾸라고 알림

        private void Awake()
        {
            CurrentPhase = Boss1Phase.phase1;
            
            _boss1AttackController = GetComponent<Boss1AttackController>();
            _boss1Movement = GetComponent<Boss1Movement>();
            _boss1ProjectileManager = GetComponent<Boss1ProjectileManager>();
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _boss1HitFlash = GetComponent<Boss1HitFlash>();
            _boss1RoarEffect = GetComponent<Boss1RoarEffect>();
            _boss1Sound = GetComponent<Boss1Sound>();
            
            if (_breakLight != null)
            {
                _breakLight.intensity = 0f;
            }
        }

        private void OnEnable()
        {
            _boss1Life.OnPhase2Triggered += StartPhase2Transition;
        }

        private void OnDisable()
        {
            _boss1Life.OnPhase2Triggered -= StartPhase2Transition;
        }

        private void StartPhase2Transition()
        {
            _isPhase2Transitioning = true;
            
            _boss1AttackController.LockAttack();
            _boss1Movement.Stop();
            _boss1Movement.LockFacing();
            _boss1ProjectileManager.DisappearAllProjectiles();
            
            SetHittable(false);
            
            Debug.Log("보스1 페이즈2 진입");
            
            _transitionCoroutine = StartCoroutine(Phase2TransitionRoutine());
        }
        
        private IEnumerator Phase2TransitionRoutine()
        {
            // 1.깨지는 순간
            PlayBreak();
            yield return new WaitForSeconds(_breakHoldTime);

            // 2.버프
            _animator.Play("Buff", 0, 0f);
            DOVirtual.Float(0f, 1f, _colorChangeDuration, value =>
                {
                    _boss1HitFlash.SetPhase2Amount(value);
                    if (_boss1Shadow != null)
                    {
                        _boss1Shadow.SetPhase2Amount(value);
                    }
                })
                .SetEase(Ease.InQuad)
                .SetLink(gameObject);
            yield return new WaitForSeconds(_buffDuration);

            // 3.포효
            _boss1RoarEffect.Play(_roarStrength);
            yield return new WaitForSeconds(_roarDuration);
            _boss1RoarEffect.Stop();

            // 4.마무리
            _animator.Play("Idle", 0, 0f);
            yield return new WaitForSeconds(_endDelay);

            _transitionCoroutine = null;
            EnterPhase2();
        }

        private void PlayBreak()
        {
            _boss1HitFlash.PlayHitFlash(_breakFlashDuration);

            if (_breakImpulseSource != null)
            {
                _breakImpulseSource.GenerateImpulseWithVelocity(Vector3.down * _breakImpulseForce);
            }

            if (_breakLight != null)
            {
                _breakLightTween?.Kill();
                _breakLight.intensity = _breakLightIntensity;
                _breakLightTween = DOTween.To(
                        () => _breakLight.intensity, 
                        x => _breakLight.intensity = x, 
                        0f, _breakLightDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
        }
        
        private void EnterPhase2()
        {
            CurrentPhase = Boss1Phase.phase2;
            OnPhaseChanged?.Invoke(CurrentPhase);

            _isPhase2Transitioning = false;
            SetHittable(true);
            _boss1AttackController.UnlockAttack();
            _boss1Movement.UnlockFacing();
            _boss1Movement.StartNormalChase();
        }
        
        private void SetHittable(bool hittable)
        {
            _boxCollider2D.enabled = hittable;
        }

        public void PlayBuffSound()
        {
            _boss1Sound.Play(Boss1SoundType.Buff);
        }
    }
