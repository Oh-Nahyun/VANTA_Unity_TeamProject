using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Boss1의 게임 시작 등장 연출을 관리하는 컨트롤러.
/// 
/// 게임 시작 전에는 Boss1의 렌더링을 숨기고 전투 로직을 정지시킨 상태로 대기
/// 외부에서 StartIntro()가 호출되면 ->
/// 경고존 생성 → Boss1 등장 → 착지 애니메이션 → 이후 등장 연출 순서
/// 등장 연출이 완전히 종료된 후 Boss1AttackController.StartBattle()을 호출하여
/// 실제 보스 로직 작동
/// </summary>

public class Boss1IntroController : MonoBehaviour
{
    /// <summary>
    /// StartIntro() 호출 후 경고존이 나타나기까지 기다리는 시간
    /// 게임 시작 직후 바로 경고존이 나타나는 것을 방지하기 위한 연출용 딜레이
    /// </summary>
    [Header("경고존 등장 전 시간")]
    [SerializeField] private float _warningZoneAppearDuration = 1f;
    
    /// <summary>
    /// 경고존이 생성된 후 Boss1이 등장하기까지 기다리는 시간.
    /// </summary>
    [Header("등장 경고 시간")]
    [SerializeField] private float _warningDuration = 2f;
    
    [Header("포효 연출 시간")]
    [SerializeField] private float _roarDuration = 2f;

    [SerializeField] private Transform _visualPivot;
    [SerializeField] private float _roarShakeDuration = 1f;
    [SerializeField] private float _roarShakeStrength = 0.1f;
    [SerializeField] private int _roarShakeVibrato = 30;
    
    [Header("포효 파동")]
    [SerializeField] private Material _roarWaveMaterial;
    [SerializeField] private float _waveDuration = 0.8f;
    [SerializeField] private float _waveStrength = 0.03f;
    [SerializeField] private float _waveStrength2 = 0.02f;
    
    [Header("포효 파동 반복")]
    [SerializeField] private int _waveCount = 3;
    [SerializeField] private float _waveInterval = 0.15f;
    
    [Header("포효 파동 겹침")]
    [SerializeField] private float _waveOverlapDelay = 0.2f;
    
    [Header("포효 카메라 쉐이크")]
    [SerializeField] private CinemachineImpulseSource _roarImpulseSource;

    private Tween _waveTween;
    
    private Tween _roarShakeTween;
    private Vector3 _visualPivotOriginPosition;
    
    [SerializeField] private SpriteRenderer[] _bossSpriteRenderers;
    [SerializeField] private GameObject _warningZonePrefab;
    [SerializeField] private Animator _animator;

    private Boss1AttackController _boss1AttackController;
    
    private GameObject _warningZone;

    private void Awake()
    {
        SetBossVisible(false);
        _boss1AttackController = GetComponent <Boss1AttackController>();
        
        _visualPivotOriginPosition = _visualPivot.localPosition;
        
        _roarWaveMaterial.SetFloat("_WaveStrength", 0f);
        _roarWaveMaterial.SetFloat("_WaveStrength2", 0f);
        _roarWaveMaterial.SetFloat("_WaveProgress", 0f);
        _roarWaveMaterial.SetFloat("_WaveProgress2", 0f);
    }

    /// <summary>
    /// Boss1 등장 연출을 시작하는 메소드
    /// 이것만 실행하면 보스1 알아서 작동합니다~
    /// </summary>

    /*
    private void Start() // 내꺼 시작용 (PR, 프리팹 오버라이드 할 때 꺼야함)
    {
        StartCoroutine(IntroRoutine());
    }
    */

    
    public void StartIntro() // 인게임 테스트용
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(_warningZoneAppearDuration);
        
        _warningZone = Instantiate(_warningZonePrefab, transform.position, Quaternion.identity);
        
        yield return new WaitForSeconds(_warningDuration);

        SetBossVisible(true);
        
        _animator.Play("IntroJumpLand", 0, 0f);
    }

    private void SetBossVisible(bool visible)
    {
        foreach (SpriteRenderer spriteRenderer in _bossSpriteRenderers)
        {
            spriteRenderer.forceRenderingOff = !visible;
        }
    }

    public void RemoveWarningZone()
    {
        if (_warningZone == null)
        {
            return;
        }
        
        Destroy(_warningZone);
        _warningZone = null;
    }
    
    public void EndIntroJumpLand()
    {
        _animator.Play("IntroClaw", 0, 0f);
    }

    public void EndIntroClawStart()
    {
        StartRoar();
    }

    private void StartRoar()
    {
        StartCoroutine(RoarRoutine());
    }
    
    private void StartRoarWave()
    {
        _waveTween?.Kill();

        _roarWaveMaterial.SetFloat("_WaveProgress", 0f);
        _roarWaveMaterial.SetFloat("_WaveProgress2", 0f);
        _roarWaveMaterial.SetFloat("_WaveStrength", _waveStrength);
        _roarWaveMaterial.SetFloat("_WaveStrength2", _waveStrength2);

        Sequence sequence = DOTween.Sequence();
        
        float cycleDuration = _waveDuration + _waveOverlapDelay + _waveInterval;

        for (int i = 0; i < _waveCount; i++)
        {
            float startTime = i * cycleDuration;
            
            sequence.Insert(
                startTime,
                DOVirtual.Float(
                    0f,
                    1f,
                    _waveDuration,
                    value =>
                    {
                        _roarWaveMaterial.SetFloat(
                            "_WaveProgress",
                            value
                        );
                    })
            );

            // 첫 번째 파동보다 조금 늦게 두 번째 파동 시작
            sequence.Insert(
                startTime + _waveOverlapDelay,
                DOVirtual.Float(
                    0f,
                    1f,
                    _waveDuration,
                    value =>
                    {
                        _roarWaveMaterial.SetFloat(
                            "_WaveProgress2",
                            value
                        );
                    })
            );
        }

        sequence.OnComplete(() =>
        {
            _roarWaveMaterial.SetFloat("_WaveProgress", 0f);
            _roarWaveMaterial.SetFloat("_WaveProgress2", 0f);
            _roarWaveMaterial.SetFloat("_WaveStrength", 0f);
            _roarWaveMaterial.SetFloat("_WaveStrength2", 0f);

            _waveTween = null;
        });

        _waveTween = sequence;
    }

    private IEnumerator RoarRoutine()
    {
        _roarImpulseSource.GenerateImpulse();
        
        _roarShakeTween?.Kill();

        _visualPivot.localPosition = _visualPivotOriginPosition;

        _roarShakeTween = _visualPivot.DOShakePosition(_roarShakeDuration, _roarShakeStrength, _roarShakeVibrato, 
                90f, false, false)
            .OnComplete(() =>
            {
                _visualPivot.localPosition = _visualPivotOriginPosition;
                _roarShakeTween = null;
            });
        
        StartRoarWave();

        // 전체 포효 연출 유지
        yield return new WaitForSeconds(_roarDuration);

        EndRoar();
    }
    
    public void EndRoar()
    {
        _waveTween?.Kill();
        _waveTween = null;

        _roarWaveMaterial.SetFloat("_WaveProgress", 0f);
        _roarWaveMaterial.SetFloat("_WaveProgress2", 0f);
        _roarWaveMaterial.SetFloat("_WaveStrength", 0f);
        _roarWaveMaterial.SetFloat("_WaveStrength2", 0f);
        
        _animator.Play("IntroClawEnd", 0, 0f);
    }
    
    public void EndIntroClawEnd()
    {
        _animator.Play("Idle", 0, 0f);
    }
    
    public void Boss1StartBattle()
    {
        _boss1AttackController.StartBattle();
    }
}
