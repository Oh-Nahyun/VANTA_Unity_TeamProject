using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerStatusManager : MonoBehaviour
{
    [Header("시작 연출 사용")]
    [SerializeField] private bool _useStartSequence = false;

    [Header("참조")]
    [SerializeField] private Light2D _spotLight;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    [Header("Spot Light")]
    [SerializeField] private float _startOuterRadius = 10f;
    [SerializeField] private float _targetOuterRadius = 6f;
    [SerializeField] private float _spotLightDuration = 1f;

    [Header("Camera")]
    [SerializeField] private float _startOrthographicSize = 5f;
    [SerializeField] private float _targetOrthographicSize = 3f;
    [SerializeField] private float _zoomDuration = 1f;
    [Header("게임 start ui ")][SerializeField]private GameObject _gameStartUI;

    private Sequence _originSequence;
    private Animator _animator;

    private bool _isStart;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_useStartSequence)
        {
            _playerMove.enabled = false;
            _playerAttack.enabled = false;

            Time.timeScale = 0.6f;

            _spotLight.pointLightOuterRadius =
                _startOuterRadius;
        }
        else
        {
            ApplyFinalSetting();
        }
    }

    public void PlayerControlOff()
    {
        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        _skillManager.enabled = false;
    }

    private void OnEnable()
    {
        if (_useStartSequence)
        {
            DefaultCameraSet();

        }

    }

    public void DefaultCameraSet()
    {
      
            var lens = _cinemachineCamera.Lens;

            if (_useStartSequence)
            {
                lens.OrthographicSize =
                    _startOrthographicSize;
            }
            else
            {
                lens.OrthographicSize =
                    _targetOrthographicSize;
            }

            _cinemachineCamera.Lens = lens;
        
    }

    private void Update()
    {
        if (!_useStartSequence)
            return;

       
    }

    public void StartPlayerOriginSetting()
    {
        if (!_useStartSequence)
            return;

        if (_isStart)
            return;
        _gameStartUI.gameObject.SetActive(false);
        _isStart = true;

        Time.timeScale = 1f;

        _originSequence?.Kill();

        _animator.SetTrigger("Start");

        float currentOrthoSize =
            _cinemachineCamera.Lens.OrthographicSize;

        _originSequence = DOTween.Sequence()
            .SetUpdate(true);

        _originSequence.Append(
            DOTween.To(
                    () => currentOrthoSize,
                    value =>
                    {
                        currentOrthoSize = value;

                        var lens =
                            _cinemachineCamera.Lens;

                        lens.OrthographicSize =
                            value;

                        _cinemachineCamera.Lens =
                            lens;
                    },
                    _targetOrthographicSize,
                    _zoomDuration
                )
                .SetEase(Ease.InOutSine)
        );

        _originSequence.Append(
            DOTween.To(
                    () => _spotLight.pointLightOuterRadius,
                    value =>
                        _spotLight.pointLightOuterRadius =
                            value,
                    _targetOuterRadius,
                    _spotLightDuration
                )
                .SetEase(Ease.InOutSine)
        );

        _originSequence.OnComplete(
            StartActing
        );
    }

    private void StartActing()
    {
        GetComponent<GameStartActing>().MoveToPoint();
    }
    
    public void ApplyFinalSetting()
    {
        _isStart = true;

        Time.timeScale = 1f;

        _spotLight.pointLightOuterRadius =
            _targetOuterRadius;

        _playerMove.enabled = true;
        _playerAttack.enabled = true;
        _skillManager.enabled = true;
        _animator.SetTrigger("Start");
    }

    public void ActivatePlayer()
    {
        _playerMove.enabled = true;
        _playerAttack.enabled = true;
        _skillManager.enabled = true;
    }

    private void OnDestroy()
    {
        _originSequence?.Kill();

        Time.timeScale = 1f;
    }
}
