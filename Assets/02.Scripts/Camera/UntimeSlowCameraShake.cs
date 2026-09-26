using System;
using DG.Tweening;
using UnityEngine;

public class UntimeSlowCameraShake : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform _shakeTarget;

    [Header("Shake Settings")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private float _strength = 0.15f;
    [SerializeField] private int _vibrato = 15;
    [SerializeField] private float _randomness = 90f;

    private Tween _shakeTween;


    private void Awake()
    {
        _shakeTarget=Camera.main.transform;
    }

    private void OnEnable()
    {
        Shake();
    }

    public void Shake()
    {
        _shakeTween?.Kill();

        _shakeTween = _shakeTarget
            .DOShakePosition(
                _duration,
                _strength,
                _vibrato,
                _randomness
            )
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _shakeTween?.Kill();
    }
}
