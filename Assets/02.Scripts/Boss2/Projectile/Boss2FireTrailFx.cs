using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 투사체 불꽃 연출. 트레일 폭과 불빛 세기를 노이즈로 출렁여 이글거리게 하고,
/// 투사체가 사라질 때 트레일·파티클·불빛을 떼어내 남은 불꽃이 자연스럽게 꺼진 뒤 지운다.
/// </summary>
public class Boss2FireTrailFx : MonoBehaviour
{
    [Header("연출 요소 (자식)")]
    [Tooltip("몸통 트레일, 빛나는 심지 트레일 등 여러 개를 겹칠 수 있다")]
    [SerializeField] private TrailRenderer[] _trails;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Light2D _light;

    [Header("이글거림")]
    [Tooltip("출렁이는 속도")]
    [SerializeField] private float _flickerSpeed = 12f;

    [Tooltip("트레일 폭이 기본값에서 위아래로 흔들리는 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float _trailFlicker = 0.25f;

    [Tooltip("불빛 세기가 기본값에서 위아래로 흔들리는 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float _lightFlicker = 0.25f;

    [Header("사라질 때")]
    [Tooltip("투사체가 사라진 뒤 불빛이 꺼지는 시간")]
    [SerializeField] private float _lightFadeTime = 0.2f;

    private float[] _baseTrailWidths;
    private float _baseLightIntensity = 1f;
    private float _noiseSeed;
    private bool _isDetached;

    private void Awake()
    {
        _noiseSeed = Random.value * 100f;

        int trailCount = _trails != null ? _trails.Length : 0;
        _baseTrailWidths = new float[trailCount];
        for (int i = 0; i < trailCount; i++)
        {
            _baseTrailWidths[i] = _trails[i] != null ? _trails[i].widthMultiplier : 1f;
        }

        if (_light != null)
        {
            _baseLightIntensity = _light.intensity;
        }
    }

    private void Update()
    {
        if (_isDetached)
            return;

        for (int i = 0; i < _baseTrailWidths.Length; i++)
        {
            if (_trails[i] == null)
                continue;

            // 트레일마다 다른 위상으로 흔들어 겹친 트레일이 함께 일렁이지 않게 한다.
            _trails[i].widthMultiplier = _baseTrailWidths[i] * (1f + Flicker(i) * _trailFlicker);
        }

        if (_light != null)
        {
            _light.intensity = _baseLightIntensity * (1f + Flicker(-1) * _lightFlicker);
        }
    }

    // -1 ~ 1 사이로 부드럽게 흔들리는 값
    private float Flicker(int channel)
    {
        return Mathf.PerlinNoise(Time.time * _flickerSpeed, _noiseSeed + channel * 7.3f) * 2f - 1f;
    }

    /// <summary>
    /// 투사체를 지우기 직전에 부른다. 연출 요소를 떼어내 방출만 멈추고, 남은 불꽃이 꺼지면 지운다.
    /// </summary>
    public void Detach()
    {
        if (_isDetached)
            return;

        _isDetached = true;

        if (_trails != null)
        {
            foreach (TrailRenderer trail in _trails)
            {
                if (trail == null)
                    continue;

                trail.emitting = false;
                DetachAndDestroy(trail.transform, trail.time);
            }
        }

        if (_particles != null)
        {
            foreach (ParticleSystem particle in _particles)
            {
                if (particle == null)
                    continue;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                DetachAndDestroy(particle.transform, particle.main.startLifetime.constantMax);
            }
        }

        if (_light != null)
        {
            // 남은 불꽃과 함께 약해졌다가 짧게 꺼진다.
            _light.intensity = _baseLightIntensity * 0.5f;
            DetachAndDestroy(_light.transform, _lightFadeTime);
        }
    }

    // 투사체 자식이 아닌 독립 오브젝트로 만든 뒤 delay 초 후 지운다. 투사체가 먼저 지워져도 남아서 꺼진다.
    private void DetachAndDestroy(Transform target, float delay)
    {
        if (target == transform)
            return;

        target.SetParent(null, true);
        Destroy(target.gameObject, Mathf.Max(delay, 0.05f));
    }
}
