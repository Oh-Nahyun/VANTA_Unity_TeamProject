using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class Phase1ToPhase2Acting : MonoBehaviour
{
    [SerializeField]private Boss2IntroCinematic _boss2IntroCinematic;
    [Header("배경체인져")][SerializeField]private EnvironmentChanger _environmentChanger;
    [Header("불끄기")][SerializeField]private LightOff _lightOff;

    [Header("불끄기소리")]
    [SerializeField] private AudioClip _lightOffSound;
    private PlayerStatusManager _playerStatusManager;
    [Header("보스 최초등장할떄 소리 ")]
    [SerializeField] private AudioClip _boss2StageBgm;
    [Header("보스 변환중 소리 ")]
    [SerializeField] private AudioClip _phaseChangeBgm;
    [Header("페이즈 변환 시작할때 이벤트 ")]
    [SerializeField] private UnityEvent _phaseStartEvent;
    [Header("불이 꺼지기 시작할때 시작할때 이벤트 ")]
    [SerializeField] private UnityEvent _lightOffStartEvent;
    
    [Header("불이 켜지기 시작할때 시작할때 이벤트 ")]
    [SerializeField] private UnityEvent _lightOnStartEvent;
    
    [Header("불이 다 켜지고 난후 보스에게 포커싱이 된후  이벤트 ")]
    [SerializeField] private UnityEvent _bossFocusEvent;
    [Header("보스 포커싱후 다시 플레이어에게 복귀할대  이벤트 ")]
    [SerializeField] private UnityEvent _returnPlayerEvent;
    private bool _isSkipped;
    private bool _isComplete;
    public Boss2IntroCinematic BossIntro => _boss2IntroCinematic;

    private Tween Delay(float seconds, TweenCallback callback)
    {
        if (_isSkipped || _isComplete)
            return null;
        return DOVirtual.DelayedCall(seconds, () =>
        {
            if (!_isSkipped && !_isComplete)
                callback();
        }).SetId(this).SetLink(gameObject);
    }

    private void Awake()
    {
        _playerStatusManager = GetComponent<PlayerStatusManager>();
    }

    private void Update()
    {
        
        
    }

    public void StartPhaseChange()
    {
        if (_isSkipped || _isComplete)
            return;
        _playerStatusManager.Acting.BeginPhaseChange(this);
        SoundManager.Instance.ChangeBGM(_phaseChangeBgm);
        _phaseStartEvent.Invoke();
        
        _playerStatusManager.PlayerControlOff();
        
        HitableObject hitableObject=FindAnyObjectByType<HitableObject>();
        CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        Delay(
            3f,
            () =>
            {
                StartOffLight();
            }
        );
        //3초 대기후 배경 변화 연출 시작
    }

    private void StartOffLight()
    {
        CameraFocusingController.Instance.ShowOrigin(); 
        _lightOffStartEvent.Invoke();
        Delay(
            1,
            () =>
            {
                SoundManager.Instance.PlayEffectSound(_lightOffSound);

                
                _lightOff.StartLightOff(
                    _environmentChanger.Fires,
                    StartBackgroundPhase2
                );
            }
        );

    }

    private void StartBackgroundPhase2()
    {
        Delay(
            3,
            () =>
            {
                _lightOnStartEvent.Invoke();
                
                int targetLightCount =
                    Mathf.Min(
                        _lightOff.Fires.Count,
                        8
                    );

                int completedLightCount = 0;
                bool environmentComplete = false;

                void CompleteBackgroundPhase2()
                {
                    if (!environmentComplete ||
                        completedLightCount < targetLightCount)
                    {
                        return;
                    }

                    _lightOff.CompleteSyncedLightOn();

                    StartBossChange();
                }

                _lightOff.BeginSyncedLightOn(_lightOff.Fires);

                _environmentChanger.StartChangeBackground(
                    _lightOff.Fires,
                    index =>
                    {
                        _lightOff.LightOnWithoutFireActivation(
                            index,
                            () =>
                            {
                                completedLightCount++;
                                CompleteBackgroundPhase2();
                            }
                        );
                    },
                    () =>
                    {
                        environmentComplete = true;
                        CompleteBackgroundPhase2();
                    }
                );
            }
        );
    }

    private void StartBossChange()
    {
        if (_isSkipped)
            return;
        HitableObject hitableObject=FindAnyObjectByType<HitableObject>();
        CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        _bossFocusEvent.Invoke();
        _boss2IntroCinematic.Play(StartPhase2Game);
    }

    public void CancelForSkip()
    {
        _isSkipped = true;
        DOTween.Kill(this);
        _playerStatusManager.CancelStartForSkip();
    }

    public void ApplySkippedState()
    {
        _playerStatusManager.PlayerControlOff();
        GetComponent<PlayerMove>().FinishActingMoveImmediately();
        _environmentChanger.ApplyPhase2Immediately();
        _lightOff.RestoreImmediately();
        CameraFocusingController.Instance.ReturnImmediately();
        _playerStatusManager.ApplySkippedVisuals(false);
        SoundManager.Instance.PlayBGM(_boss2StageBgm);
    }

    public void CompleteAfterSkip()
    {
        CompletePhase2();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public void StartPhase2Game()
    {
        if (_isSkipped)
            return;
        CompletePhase2();
    }

    private void CompletePhase2()
    {
        if (_isComplete)
            return;
        _isComplete = true;
        //_boss2IntroCinematic.Play(PlayerMoveEnable);
        _playerStatusManager.ApplyFinalSetting();
        if (!_isSkipped)
        {
            CameraFocusingController.Instance.SlowFocus(transform,0f,3f);
            SoundManager.Instance.PlayBGM(_boss2StageBgm);
        }
        

        _returnPlayerEvent.Invoke();
        _playerStatusManager.Acting.StopActing();

    }

    private void PlayerMoveEnable()
    {
        
    }
}
