using System.Collections;
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
    
    private Vector3 _visualPivotOriginPosition;
    
    [SerializeField] private SpriteRenderer[] _bossSpriteRenderers;
    [SerializeField] private GameObject _warningZonePrefab;
    [SerializeField] private Animator _animator;

    private Boss1AttackController _boss1AttackController;
    private Boss1RoarEffect _roarEffect;
    
    private GameObject _warningZone;
    private Boss1WarningZoneVFX _warningZoneVFX;
    
    private Boss1Sound _boss1Sound;

    private void Awake()
    {
        SetBossVisible(false);
        _boss1AttackController = GetComponent <Boss1AttackController>();
        _roarEffect = GetComponent<Boss1RoarEffect>();
        _boss1Sound = GetComponent<Boss1Sound>();
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
        _warningZoneVFX = _warningZone.GetComponent<Boss1WarningZoneVFX>();
        
        if (_warningZoneVFX != null)
        {
            _warningZoneVFX.Play(_warningDuration);
        }
        
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
        _boss1Sound.Play(Boss1SoundType.EntranceLand);
        
        if (_warningZone == null)
        {
            return;
        }
        
        if (_warningZoneVFX != null)
        {
            _warningZoneVFX.Remove();
        }
        
        else
        {
            Destroy(_warningZone);
        }
        _warningZone = null;
        _warningZoneVFX = null;
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

    private IEnumerator RoarRoutine()
    {
        _roarEffect.Play();

        // 전체 포효 연출 유지
        yield return new WaitForSeconds(_roarDuration);

        EndRoar();
    }
    
    public void EndRoar()
    {
        _roarEffect.Stop();
        
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

    public void IntroClawSound()
    {
        _boss1Sound.Play(Boss1SoundType.Claw);
    }
}
