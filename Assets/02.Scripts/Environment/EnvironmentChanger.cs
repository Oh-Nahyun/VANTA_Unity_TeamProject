using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class EnvironmentChanger : MonoBehaviour
{
    [Header("메테리얼 적용 대상")]
    [SerializeField] private Renderer _targetRenderer;

    [Header("맵의 불")]
    [SerializeField] private List<Fire> _fires;

    [Header("카메라")]
    [SerializeField] private Camera _targetCamera;

    [Header("불 변경 간격")]
    [SerializeField] private float _changeInterval = 0.4f;

    [Header("Radius")]
    [SerializeField] private float _targetRadius = 0.4f;
    [SerializeField] private float _radiusDuration = 0.4f;

    [Header("Camera Shake")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private float _impulseForce = 0.15f;

    private MaterialPropertyBlock _propertyBlock;

    private Sequence _changeSequence;

    private readonly List<Tween> _radiusTweens =
        new List<Tween>();

    private bool _isPhase2;

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

        _propertyBlock =
            new MaterialPropertyBlock();

        ResetPropertyBlock();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartChangeBackground();
        }
    }

    private void LateUpdate()
    {
        if (!_isPhase2)
            return;

        UpdateFireCenters();
    }

    public void StartChangeBackground()
    {
        if (_targetRenderer == null)
            return;

        KillTweens();

        ResetPropertyBlock();

        _isPhase2 = true;

        UpdateFireCenters();

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

        _changeSequence =
            DOTween.Sequence();

        int count = Mathf.Min(
            _fires.Count,
            SelectCenterIDs.Length,
            SelectedRadiusIDs.Length
        );

        for (int i = 0; i < count; i++)
        {
            int index = i;

            _changeSequence.AppendCallback(
                () =>
                {
                    ChangeEnvironment(index);
                }
            );

            if (index < count - 1)
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

    private void ChangeEnvironment(int index)
    {
        if (index < 0 ||
            index >= _fires.Count)
        {
            return;
        }

        Fire fire = _fires[index];

        if (fire == null)
            return;

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

        fire.ChangeFire(
            () =>
            {
                StartRadiusTween(index);
            }
        );
    }

    private void UpdateFireCenters()
    {
        if (_targetRenderer == null ||
            _targetCamera == null)
        {
        

             return;
        }   

    _targetRenderer.GetPropertyBlock(
            _propertyBlock
        );

        int count = Mathf.Min(
            _fires.Count,
            SelectCenterIDs.Length
        );

        for (int i = 0; i < count; i++)
        {
            Fire fire = _fires[i];

            if (fire == null)
                continue;

            Vector3 viewportPosition =
                _targetCamera.WorldToViewportPoint(
                    fire.transform.position
                );

            Vector2 selectCenter =
                new Vector2(
                    viewportPosition.x,
                    viewportPosition.y
                );

            _propertyBlock.SetVector(
                SelectCenterIDs[i],
                selectCenter
            );
        }

        _targetRenderer.SetPropertyBlock(
            _propertyBlock
        );
    }

    private void StartRadiusTween(int index)
    {
        int radiusID =
            SelectedRadiusIDs[index];

        float currentRadius = 0f;

        PlayCameraShake();

        Tween tween = DOTween.To(
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
            .SetEase(Ease.InOutSine);

        _radiusTweens.Add(tween);

        tween.OnComplete(
            () =>
            {
                _radiusTweens.Remove(tween);
            }
        );
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
    }

    public void EndPhase2()
    {
        _isPhase2 = false;
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
