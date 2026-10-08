using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnvironmentChanger : MonoBehaviour
{
    [Header("메테리얼 적용 대상")]
    [SerializeField] private Renderer _targetRenderer;

    [Header("맵의 불")]
    [SerializeField] private List<Fire> _fires;

    [Header("불 변경 간격")]
    [SerializeField] private float _changeInterval = 0.4f;

    [Header("Radius")]
    [SerializeField] private float _targetRadius = 5f;
    [SerializeField] private float _radiusDuration = 0.4f;

    [Header("Camera Shake")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private float _impulseForce = 0.15f;

    private MaterialPropertyBlock _propertyBlock;

    private Sequence _changeSequence;

    private readonly List<Tween> _radiusTweens =
        new List<Tween>();

    private int _targetChangeCount;
    private int _completedChangeCount;

    private Action _onComplete;
    private Action<int> _onChangeStep;
    private IReadOnlyList<Fire> _activeFires;
    private bool _activateFiresOnChange;

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
        _propertyBlock =
            new MaterialPropertyBlock();

        ResetPropertyBlock();
    }

    public void StartChangeBackground(
        Action onComplete = null)
    {
        StartChangeBackground(
            _fires,
            true,
            null,
            onComplete
        );
    }

    public void StartChangeBackground(
        IReadOnlyList<Fire> fires,
        Action onComplete = null)
    {
        StartChangeBackground(
            fires,
            true,
            null,
            onComplete
        );
    }

    public void StartChangeBackground(
        IReadOnlyList<Fire> fires,
        Action<int> onChangeStep,
        Action onComplete = null)
    {
        StartChangeBackground(
            fires,
            true,
            onChangeStep,
            onComplete
        );
    }

    public void StartChangeBackgroundWithoutFireActivation(
        IReadOnlyList<Fire> fires,
        Action onComplete = null)
    {
        StartChangeBackground(
            fires,
            false,
            null,
            onComplete
        );
    }

    private void StartChangeBackground(
        IReadOnlyList<Fire> fires,
        bool activateFiresOnChange,
        Action<int> onChangeStep,
        Action onComplete)
    {
        if (_targetRenderer == null)
            return;

        KillTweens();

        ResetPropertyBlock();

        _activeFires = fires ?? _fires;
        _activateFiresOnChange = activateFiresOnChange;
        _onChangeStep = onChangeStep;

        _completedChangeCount = 0;
        _onComplete = onComplete;

        _targetChangeCount = Mathf.Min(
            ActiveFires.Count,
            SelectCenterIDs.Length,
            SelectedRadiusIDs.Length
        );

        SetFireCenters();

        _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        _propertyBlock.SetFloat(
            HiddenActiveID,
            1f
        );

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );

        if (_targetChangeCount <= 0)
        {
            CompleteEnvironmentChange();
            return;
        }

        _changeSequence =
            DOTween.Sequence();

        for (int i = 0;
             i < _targetChangeCount;
             i++)
        {
            int index = i;

            _changeSequence.AppendCallback(
                () =>
                {
                    ChangeEnvironment(index);
                }
            );

            if (i < _targetChangeCount - 1)
            {
                _changeSequence.AppendInterval(
                    _changeInterval
                );
            }
        }

        _changeSequence.OnComplete(
            () =>
            {
                _changeSequence = null;
            }
        );
    }

    private void ChangeEnvironment(
        int index)
    {
        if (index < 0 ||
            index >= ActiveFires.Count ||
            index >= SelectedRadiusIDs.Length)
        {
            ChangeComplete();
            return;
        }

        Fire fire =
            ActiveFires[index];

        if (fire == null)
        {
            ChangeComplete();
            return;
        }

        if (_activateFiresOnChange)
        {
            fire.ActivateFire();
        }

        _onChangeStep?.Invoke(index);

        SetFireCenter(index);

        _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        _propertyBlock.SetFloat(
            SelectedRadiusIDs[index],
            0f
        );

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );


        StartRadiusTween(index);
    }

    private void SetFireCenters()
    {
        if (_targetRenderer == null)
            return;

        _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        int count = Mathf.Min(
            ActiveFires.Count,
            SelectCenterIDs.Length
        );

        for (int i = 0;
             i < count;
             i++)
        {
            Fire fire =
                ActiveFires[i];

            if (fire == null)
                continue;

            Vector3 worldPosition =
                fire.transform.position;

            _propertyBlock.SetVector(
                SelectCenterIDs[i],
                new Vector4(
                    worldPosition.x,
                    worldPosition.y,
                    0f,
                    0f
                )
            );
        }

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );
    }

    private void SetFireCenter(
        int index)
    {
        if (_targetRenderer == null)
            return;

        if (index < 0 ||
            index >= ActiveFires.Count ||
            index >= SelectCenterIDs.Length)
        {
            return;
        }

        Fire fire =
            ActiveFires[index];

        if (fire == null)
            return;

        Vector3 worldPosition =
            fire.transform.position;

        _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        _propertyBlock.SetVector(
            SelectCenterIDs[index],
            new Vector4(
                worldPosition.x,
                worldPosition.y,
                0f,
                0f
            )
        );

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );
    }

    private void StartRadiusTween(
        int index)
    {
        int radiusID =
            SelectedRadiusIDs[index];

        float currentRadius = 0f;

        PlayCameraShake();

        Tween tween = null;

        tween = DOTween.To(
                () => currentRadius,
                value =>
                {
                    currentRadius = value;

                    if (_targetRenderer == null)
                        return;

                    _targetRenderer.GetPropertyBlock(
                        _propertyBlock
                    );

                    _propertyBlock.SetFloat(
                        radiusID,
                        value
                    );

                    _targetRenderer.SetPropertyBlock(
                        _propertyBlock
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

                    ChangeComplete();
                }
            );

        _radiusTweens.Add(tween);
    }

    private void ChangeComplete()
    {
        _completedChangeCount++;

        if (_completedChangeCount <
            _targetChangeCount)
        {
            return;
        }

        CompleteEnvironmentChange();
    }

    private void CompleteEnvironmentChange()
    {
        Action callback =
            _onComplete;

        _onComplete = null;
        _onChangeStep = null;
        _activeFires = null;

        callback?.Invoke();
    }

    private void PlayCameraShake()
    {
        if (_impulseSource == null)
            return;

        Vector2 randomDirection =
            Random.insideUnitCircle.normalized;

        Vector3 velocity =
            new Vector3(
                randomDirection.x,
                randomDirection.y,
                0f
            ) * _impulseForce;

        _impulseSource.GenerateImpulse(
            velocity
        );
    }

    private void ResetPropertyBlock()
    {
        if (_targetRenderer == null)
            return;

        _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        for (int i = 0;
             i < SelectedRadiusIDs.Length;
             i++)
        {
            _propertyBlock.SetFloat(
                SelectedRadiusIDs[i],
                0f
            );
        }

        _propertyBlock.SetFloat(
            HiddenActiveID,
            0f
        );

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );
    }

    private void KillTweens()
    {
        _changeSequence?.Kill();
        _changeSequence = null;

        for (int i = 0;
             i < _radiusTweens.Count;
             i++)
        {
            _radiusTweens[i]?.Kill();
        }

        _radiusTweens.Clear();

        _onComplete = null;
        _onChangeStep = null;
        _activeFires = null;
    }

    public void EndPhase2()
    {
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}   
