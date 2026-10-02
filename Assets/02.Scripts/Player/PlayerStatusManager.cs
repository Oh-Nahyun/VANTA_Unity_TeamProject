using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerStatusManager : MonoBehaviour
{
    [SerializeField] private Light2D _spotLight;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    [Header("Spot Light")]
    [SerializeField] private float _fadeOutDuration = 1f;

    [Header("Camera")]
    [SerializeField] private float _targetOrthographicSize = 5f;
    [SerializeField] private float _zoomDuration = 1f;

    private Sequence _originSequence;
    private Animator _animator;

    private void OnEnable()
    {
        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        
    }

    private void Awake()
    {
        _animator=GetComponent<Animator>();
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartPlayerOriginSetting();
        }
    }

    public void StartPlayerOriginSetting()
    {
        _originSequence?.Kill();

        _originSequence = DOTween.Sequence();
        
        _animator.SetTrigger("Start");

        _originSequence.Join(
            DOTween.To(
                    () => _cinemachineCamera.Lens.OrthographicSize,
                    value =>
                    {
                        var lens = _cinemachineCamera.Lens;
                        lens.OrthographicSize = value;
                        _cinemachineCamera.Lens = lens;
                    },
                    _targetOrthographicSize,
                    _zoomDuration
                )
                .SetEase(Ease.InOutQuad)
        );

        _originSequence.Join(
            DOTween.To(
                    () => _spotLight.intensity,
                    value => _spotLight.intensity = value,
                    0f,
                    _fadeOutDuration
                )
                .SetEase(Ease.OutQuad)
        );

        _originSequence.OnComplete(() =>
        {
            _spotLight.gameObject.SetActive(false);
            ActivatePlayer();
        });
    }

    private void ActivatePlayer()
    {
        _playerMove.enabled = true;
        _playerAttack.enabled = true;
    }

    private void OnDestroy()
    {
        _originSequence?.Kill();
    }
}
