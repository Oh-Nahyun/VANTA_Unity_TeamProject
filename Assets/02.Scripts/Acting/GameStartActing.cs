using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GameStartActing : MonoBehaviour
{
    [Header("회피시 이동할 장소")][SerializeField]private Transform _movePoint;
    private PlayerMove _playerMove;
    [Header("보스 착지 Vfx 호출 ")][SerializeField]private GameObject _bossDropVfx;
    [Header("보스 HP바 ")][SerializeField]private GameObject _bossHPVar;
    [Header("보스 최초등장할떄 ")][SerializeField]private UnityEvent _boss1DropStart;
    [Header("보스 최초등장할떄 플레이어 이모션 ")][SerializeField]private UnityEvent _boss1DropEmotion;

    [Header("보스 낙하해 등장했을때 ")][SerializeField]private UnityEvent _boss1DropFinish;

    [Header("보스 낙하해 등장했을때 이름 뜨기 ")][SerializeField]private UnityEvent _boss1DropName;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
    }

    public void MoveToPoint()
    {
        _boss1DropStart.Invoke();
        CameraFocusingController.Instance.SlowFocus(transform,1f,1f);

        DOVirtual.DelayedCall(
            1f,
            () =>
            {
                _boss1DropEmotion.Invoke();

            }
        );
        DOVirtual.DelayedCall(
            3f,
            () =>
            {
                Time.timeScale = 0.2f;
                Instantiate(_bossDropVfx,new Vector3(0.25f,-0.41f,0),Quaternion.identity);
                CameraFocusingController.Instance.ShowOrigin();
            
                _playerMove.DashToPoint(_movePoint.position,PointMoveComplete);

            }
        );
        
        
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

        HitableObject hitableObject = FindAnyObjectByType<HitableObject>();

        GameObject zoomTarget = new GameObject("ZoomTarget");

        zoomTarget.transform.position =
            hitableObject.transform.position +
            hitableObject.transform.up * 1f;
        //CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        CameraFocusingController.Instance.Focus(zoomTarget.transform,1);
        DOVirtual.DelayedCall(
            1,
            () =>
            {
                _boss1DropName.Invoke();
            }
        );
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
        _boss1DropFinish.Invoke();

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
        
      
    }
}
