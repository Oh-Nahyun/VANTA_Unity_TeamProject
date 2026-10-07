using System;
using DG.Tweening;
using UnityEngine;

public class Phase1ToPhase2Acting : MonoBehaviour
{
    [Header("배경체인져")][SerializeField]private EnvironmentChanger _environmentChanger;
    private PlayerStatusManager _playerStatusManager;

    private void Awake()
    {
        _playerStatusManager = GetComponent<PlayerStatusManager>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
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
                StartChangeEnvironment();
            }
        );
        //3초 대기후 배경 변화 연출 시작
    }

    private void StartChangeEnvironment()
    {
        CameraFocusingController.Instance.ShowOrigin();
        //CameraFocusingController.Instance.SlowFocus(transform,-1.5f,1f);
        DOVirtual.DelayedCall(
            1,
            () =>
            {
                //_playerStatusManager.DefaultCameraSet();

                _environmentChanger.StartChangeBackground(StartBossChange);
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
    }
}
