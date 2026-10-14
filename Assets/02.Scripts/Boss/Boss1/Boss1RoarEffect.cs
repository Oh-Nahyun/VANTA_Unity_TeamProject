using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class Boss1RoarEffect : MonoBehaviour
{
    [Header("몸 떨림")]
    [SerializeField] private Transform _visualPivot;
    [SerializeField] private float _shakeDuration = 0.1f;
    [SerializeField] private float _shakeStrength = 0.1f;
    [SerializeField] private int _shakeVibrato = 30;

    [Header("포효 파동")]
    [SerializeField] private Material _waveMaterial;
    [SerializeField] private float _waveDuration = 0.3f;
    [SerializeField] private float _waveStrength = 0.03f;
    [SerializeField] private float _waveStrength2 = 0.015f;
    [SerializeField] private int _waveCount = 5;
    [SerializeField] private float _waveInterval = 0.1f;
    [SerializeField] private float _waveOverlapDelay = 0.1f;

    [Header("카메라 셰이크")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    
    [Header("집중선")]
    [SerializeField] private Boss1RoarSpeedLines _speedLines;

    private Tween _shakeTween;
    private Tween _waveTween;
    private Vector3 _visualPivotOriginPosition;
    
    private Boss1Sound _boss1Sound;

    private void Awake()
    {
        _boss1Sound = GetComponent<Boss1Sound>();
        _visualPivotOriginPosition = _visualPivot.localPosition;
        ResetWave();
    }
    
    public void Play(float strength = 1f)
    {
        _boss1Sound.Play(Boss1SoundType.Roar);
        
        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulse(strength);
        }

        PlayShake(strength);
        PlayWave(strength);
        
        if (_speedLines != null)
        {
            _speedLines.Play(strength);
        }
    }

    private void PlayShake(float strength)
    {
        _shakeTween?.Kill();
        _visualPivot.localPosition = _visualPivotOriginPosition;

        _shakeTween = _visualPivot.DOShakePosition(_shakeDuration, _shakeStrength * strength, _shakeVibrato,
                90f, false, false)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _visualPivot.localPosition = _visualPivotOriginPosition;
                _shakeTween = null;
            });
    }

    private void PlayWave(float strength)
    {
        _waveTween?.Kill();

        _waveMaterial.SetFloat("_WaveProgress", 0f);
        _waveMaterial.SetFloat("_WaveProgress2", 0f);
        _waveMaterial.SetFloat("_WaveStrength", _waveStrength * strength);
        _waveMaterial.SetFloat("_WaveStrength2", _waveStrength2 * strength);

        Sequence sequence = DOTween.Sequence();
        float cycleDuration = _waveDuration + _waveOverlapDelay + _waveInterval;

        for (int i = 0; i < _waveCount; i++)
        {
            float startTime = i * cycleDuration;

            sequence.Insert(startTime,
                DOVirtual.Float(0f, 1f, _waveDuration, value => _waveMaterial.SetFloat("_WaveProgress", value)));

            sequence.Insert(startTime + _waveOverlapDelay,
                DOVirtual.Float(0f, 1f, _waveDuration, value => _waveMaterial.SetFloat("_WaveProgress2", value)));
        }

        sequence.OnComplete(() =>
        {
            ResetWave();
            _waveTween = null;
        });

        _waveTween = sequence;
    }

    private void ResetWave()
    {
        _waveMaterial.SetFloat("_WaveProgress", 0f);
        _waveMaterial.SetFloat("_WaveProgress2", 0f);
        _waveMaterial.SetFloat("_WaveStrength", 0f);
        _waveMaterial.SetFloat("_WaveStrength2", 0f);
    }

    private void OnDisable()
    {
        Stop();
    }
    
    public void Stop()
    {
        if (_boss1Sound != null)
        {
            _boss1Sound.Stop(Boss1SoundType.Roar);
        }
        
        _waveTween?.Kill();
        _waveTween = null;
        ResetWave();
        
        if (_speedLines != null)
        {
            _speedLines.Stop();
        }
    }
}
