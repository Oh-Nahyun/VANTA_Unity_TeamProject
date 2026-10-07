using System;
using DG.Tweening;
using UnityEngine;

public class GameStartActing : MonoBehaviour
{
    [Header("회피시 이동할 장소")][SerializeField]private Transform _movePoint;
    private PlayerMove _playerMove;
    [Header("보스 HP바 ")][SerializeField]private GameObject _bossHPVar;
   
    public GameObject TestBoss;
    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
    }

    public void MoveToPoint()
    {
        
        Time.timeScale = 0.3f;
        _playerMove.DashToPoint(_movePoint.position,PointMoveComplete);
    }

    private void PointMoveComplete()
    {
        DOVirtual.DelayedCall(
            2,
            () =>
            {
                Time.timeScale = 1f;
                FocusToBoss();
            }
        );
    }

    private void FocusToBoss()
    {
        TestBoss.SetActive(true);

        HitableObject hitableObject=FindAnyObjectByType<HitableObject>();
        CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        DOVirtual.DelayedCall(
            5,
            () =>
            {
                FocusToPlayer();
            }
        );
    }

    private void FocusToPlayer()
    {
        CameraFocusingController.Instance.SlowFocus(transform,1f,1f);
        
        DOVirtual.DelayedCall(
            2,
            () =>
            {

                GameStart();
            }
        );
        
    }

    private void MakeHpBar()
    {
        _bossHPVar.SetActive(true);   
    }
    private void GameStart()
    {
        MakeHpBar();
        CameraFocusingController.Instance.SlowFocus(transform,0f,2f);
        
        GetComponent<PlayerStatusManager>().ApplyFinalSetting();
        
        DOVirtual.DelayedCall(
            2,
            () =>
            {
                CameraFocusingController.Instance.ReturnCamera();
            }
        );
    }
}
