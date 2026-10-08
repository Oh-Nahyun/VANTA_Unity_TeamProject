using System;
using DG.Tweening;
using UnityEngine;

public class Phase1ToPhase2Acting : MonoBehaviour
{
    [Header("배경체인져")][SerializeField]private EnvironmentChanger _environmentChanger;
    [Header("불끄기")][SerializeField]private LightOff _lightOff;
    private PlayerStatusManager _playerStatusManager;
    [Header("보스 최초등장할떄 소리 ")]
    [SerializeField] private AudioClip _boss2StageBgm;
    private void Awake()
    {
        _playerStatusManager = GetComponent<PlayerStatusManager>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StartPhaseChange();
        }
    }

    public void StartPhaseChange()
    {
        _playerStatusManager.PlayerControlOff();
        
        HitableObject hitableObject=FindAnyObjectByType<HitableObject>();
        CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        DOVirtual.DelayedCall(
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
        DOVirtual.DelayedCall(
            1,
            () =>
            {
                _lightOff.StartLightOff(
                    _environmentChanger.Fires,
                    StartBackgroundPhase2
                );
            }
        );

    }

    private void StartBackgroundPhase2()
    {
        DOVirtual.DelayedCall(
            3,
            () =>
            {
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
        HitableObject hitableObject=FindAnyObjectByType<HitableObject>();
        CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        //휘두르는 연출시작
        DOVirtual.DelayedCall(
            3f,
            () =>
            {
                StartPhase2Game();
            }
        );
    }

    private void StartPhase2Game()
    {
        _playerStatusManager.ApplyFinalSetting();
            CameraFocusingController.Instance.SlowFocus(transform,0f,1f);
            SoundManager.Instance.PlayBGM(_boss2StageBgm);
    }
}
