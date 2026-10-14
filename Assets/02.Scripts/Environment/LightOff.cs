using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightOff : MonoBehaviour
{
    [Header("Full Screen Shader")]
    [SerializeField]
    private FullScreenPassRendererFeature _lightFeature;

    [SerializeField]
    private Camera _targetCamera;

    [Header("맵의 불")]
    [SerializeField]
    private List<Fire> _fires;

    [Header("불 변경 간격")]
    [SerializeField]
    private float _changeInterval = 0.4f;

    [Header("Radius")]
    [SerializeField]
    private float _targetRadius = 0.2f;

    [SerializeField]
    private float _radiusDuration = 0.4f;

    private Material _originalMaterial;
    private Material _runtimeMaterial;

    private Sequence _sequence;

    private readonly List<Tween> _radiusTweens =
        new List<Tween>();

    private readonly float[] _currentRadius =
        new float[8];

    private int _targetCount;
    private int _completedCount;

    private Action _onComplete;
    private IReadOnlyList<Fire> _activeFires;

    public IReadOnlyList<Fire> Fires => _fires;

    private IReadOnlyList<Fire> ActiveFires =>
        _activeFires ?? _fires;

    private static readonly int[] SelectCenterIDs =
    {
        Shader.PropertyToID("_SelectCenter1"),
        Shader.PropertyToID("_SelectCenter2"),
        Shader.PropertyToID("_SelectCenter3"),
        Shader.PropertyToID("_SelectCenter4"),
        Shader.PropertyToID("_SelectCenter5"),
        Shader.PropertyToID("_SelectCenter6"),
        Shader.PropertyToID("_SelectCenter7"),
        Shader.PropertyToID("_SelectCenter8")
    };

    private static readonly int[] SelectedRadiusIDs =
    {
        Shader.PropertyToID("_SelectedRadius1"),
        Shader.PropertyToID("_SelectedRadius2"),
        Shader.PropertyToID("_SelectedRadius3"),
        Shader.PropertyToID("_SelectedRadius4"),
        Shader.PropertyToID("_SelectedRadius5"),
        Shader.PropertyToID("_SelectedRadius6"),
        Shader.PropertyToID("_SelectedRadius7"),
        Shader.PropertyToID("_SelectedRadius8")
    };

    private static readonly int HiddenActiveID =
        Shader.PropertyToID("_HiddenActive");

    private void Awake()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        if (_lightFeature == null)
            return;

        _originalMaterial =
            _lightFeature.passMaterial;

        if (_originalMaterial == null)
            return;

        _runtimeMaterial =
            new Material(_originalMaterial);

        _lightFeature.passMaterial =
            _runtimeMaterial;

        ResetMaterial();
    }

    public void StartLightOff(Action onComplete = null)
    {
        StartLightOff(
            _fires,
            onComplete
        );
    }

    public void StartLightOff(
        IReadOnlyList<Fire> fires,
        Action onComplete = null)
    {
        if (_runtimeMaterial == null)
            return;

        KillTweens();
        ResetMaterial();

        _activeFires = fires ?? _fires;
        _onComplete = onComplete;
        _completedCount = 0;

        _targetCount = Mathf.Min(
            ActiveFires.Count,
            SelectCenterIDs.Length,
            SelectedRadiusIDs.Length
        );

        UpdateFirePositions();

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            1f
        );

        if (_targetCount <= 0)
        {
            CompleteLightOff();
            return;
        }

        _sequence = DOTween.Sequence();

        for (int i = 0; i < _targetCount; i++)
        {
            int index = i;

            _sequence.AppendCallback(
                () =>
                {
                    FireOff(index);
                }
            );

            if (i < _targetCount - 1)
            {
                _sequence.AppendInterval(
                    _changeInterval
                );
            }
        }

        _sequence.OnComplete(
            () =>
            {
                _sequence = null;
            }
        );
    }
    public void ActivateAllFire()
    {
        int count = Mathf.Min(
            _fires.Count,
            SelectCenterIDs.Length
        );

        for (int i = 0; i < count; i++)
        {
            Fire fire = _fires[i];

            if (fire == null)
                continue;

            fire.ActivateFire();
        }
    }
    private void FireOff(int index)
    {
        if (index < 0 ||
            index >= ActiveFires.Count ||
            index >= SelectedRadiusIDs.Length)
        {
            RadiusComplete();
            return;
        }

        Fire fire = ActiveFires[index];

        if (fire == null)
        {
            RadiusComplete();
            return;
        }

        UpdateFirePosition(index);

        fire.DeactivateFire();

        StartRadiusTween(index);
    }

    private void StartRadiusTween(
        int index)
    {
        if (index < 0 ||
            index >= ActiveFires.Count ||
            ActiveFires[index] == null)
        {
            RadiusComplete();
            return;
        }

        UpdateFirePosition(index);

        Tween tween = null;

        tween = DOTween.To(
                () => _currentRadius[index],
                value =>
                {
                    _currentRadius[index] =
                        value;

                    _runtimeMaterial.SetFloat(
                        SelectedRadiusIDs[index],
                        value
                    );
                },
                _targetRadius,
                _radiusDuration
            )
            .SetEase(Ease.InOutSine)
            .OnComplete(
                () =>
                {
                    _radiusTweens.Remove(tween);

                    RadiusComplete();
                }
            );

        _radiusTweens.Add(tween);
    }

    public void LightOn(
        Action onComplete = null)
    {
        if (_runtimeMaterial == null)
            return;

        KillTweens();

        _activeFires = _fires;
        _onComplete = onComplete;
        _completedCount = 0;

        UpdateFirePositions();

        _targetCount = Mathf.Min(
            ActiveFires.Count,
            SelectedRadiusIDs.Length
        );

        if (_targetCount <= 0)
        {
            _runtimeMaterial.SetFloat(
                HiddenActiveID,
                0f
            );

            onComplete?.Invoke();

            return;
        }

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            1f
        );

        _sequence = DOTween.Sequence();

        for (int i = 0; i < _targetCount; i++)
        {
            int index = i;

            _sequence.AppendCallback(
                () =>
                {
                    LightOn(
                        index,
                        LightOnComplete
                    );
                }
            );

            if (i < _targetCount - 1)
            {
                _sequence.AppendInterval(
                    _changeInterval
                );
            }
        }

        _sequence.OnComplete(
            () =>
            {
                _sequence = null;
            }
        );
    }

    public void BeginSyncedLightOn(
        IReadOnlyList<Fire> fires = null)
    {
        if (_runtimeMaterial == null)
            return;

        KillTweens();

        _activeFires = fires ?? _fires;

        UpdateFirePositions();

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            1f
        );
    }

    public void RestoreImmediately()
    {
        KillTweens();
        ResetMaterial();
        for (int i = 0; i < Mathf.Min(_fires.Count, SelectedRadiusIDs.Length); i++)
            _fires[i]?.ApplyPhase2Immediately();
    }

    public void CompleteSyncedLightOn()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            0f
        );

        _activeFires = null;
    }

    private void UpdateFirePositions()
    {
        if (_runtimeMaterial == null ||
            _targetCamera == null)
        {
            return;
        }

        int count = Mathf.Min(
            ActiveFires.Count,
            SelectCenterIDs.Length
        );

        for (int i = 0; i < count; i++)
        {
            UpdateFirePosition(i);
        }
    }

    private void UpdateFirePosition(
        int index)
    {
        if (_runtimeMaterial == null ||
            _targetCamera == null)
        {
            return;
        }

        if (index < 0 ||
            index >= ActiveFires.Count ||
            index >= SelectCenterIDs.Length)
        {
            return;
        }

        Fire fire = ActiveFires[index];

        if (fire == null)
            return;

        Vector3 viewportPosition =
            _targetCamera.WorldToViewportPoint(
                fire.transform.position
            );

        _runtimeMaterial.SetVector(
            SelectCenterIDs[index],
            new Vector4(
                viewportPosition.x,
                viewportPosition.y,
                0f,
                0f
            )
        );
    }

    private void RadiusComplete()
    {
        _completedCount++;

        if (_completedCount <
            _targetCount)
        {
            return;
        }

        CompleteLightOff();
    }

    private void LightOnComplete()
    {
        _completedCount++;

        if (_completedCount <
            _targetCount)
        {
            return;
        }

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            0f
        );

        CompleteLightOff();
    }

    private void CompleteLightOff()
    {
        Action callback =
            _onComplete;

        _onComplete = null;
        _activeFires = null;

        callback?.Invoke();
    }

    private void ResetMaterial()
    {
        if (_runtimeMaterial == null)
            return;

        for (int i = 0;
             i < SelectedRadiusIDs.Length;
             i++)
        {
            _currentRadius[i] = 0f;

            _runtimeMaterial.SetFloat(
                SelectedRadiusIDs[i],
                0f
            );
        }

        _runtimeMaterial.SetFloat(
            HiddenActiveID,
            0f
        );
    }

    private void KillTweens()
    {
        _sequence?.Kill();
        _sequence = null;

        for (int i = 0;
             i < _radiusTweens.Count;
             i++)
        {
            _radiusTweens[i]?.Kill();
        }

        _radiusTweens.Clear();

        _onComplete = null;
        _activeFires = null;
    }
    public void LightOn(int index, Action onComplete = null)
    {
        LightOn(
            index,
            true,
            onComplete
        );
    }

    public void LightOnWithoutFireActivation(
        int index,
        Action onComplete = null)
    {
        LightOn(
            index,
            false,
            onComplete
        );
    }

    private void LightOn(
        int index,
        bool activateFire,
        Action onComplete = null)
    {
        if (_runtimeMaterial == null ||
            index < 0 ||
            index >= ActiveFires.Count ||
            index >= SelectedRadiusIDs.Length)
        {
            onComplete?.Invoke();
            return;
        }

        Fire fire = ActiveFires[index];

        if (fire == null)
        {
            onComplete?.Invoke();
            return;
        }

        UpdateFirePosition(index);

        if (activateFire)
        {
            fire.ActivateFire();
        }

        Tween tween = null;

        tween = DOTween.To(
                () => _currentRadius[index],
                value =>
                {
                    _currentRadius[index] = value;

                    _runtimeMaterial.SetFloat(
                        SelectedRadiusIDs[index],
                        value
                    );
                },
                0f,
                _radiusDuration
            )
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                _radiusTweens.Remove(tween);

                onComplete?.Invoke();
            });

        _radiusTweens.Add(tween);
    }
    private void OnDestroy()
    {
        KillTweens();

        if (_lightFeature != null &&
            _originalMaterial != null)
        {
            _lightFeature.passMaterial =
                _originalMaterial;
        }

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}
