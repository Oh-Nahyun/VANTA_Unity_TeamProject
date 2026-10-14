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

    
    [Header("보스 최초등장할떄 플레이어 이모션 ")][SerializeField]private UnityEvent _bossMoveStart;

    [Header("보스 낙하해 등장했을때 ")][SerializeField]private UnityEvent _boss1DropFinish;

    [Header("보스 낙하해 등장했을때 이름 뜨기 ")][SerializeField]private UnityEvent _boss1DropName;

    [Header("플레이어 전투가 가능해질때 호출될 이벤트")]
    [SerializeField] private UnityEvent _playerBattleStartEvent;
    
    private bool _isSkipped;
    private bool _isComplete;
    private GameObject _dropInstance;
    private GameObject _zoomTarget;

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
        _playerMove = GetComponent<PlayerMove>();
    }

    public void MoveToPoint()
    {
        if (_isSkipped || _isComplete)
            return;
        GetComponent<PlayerStatusManager>().Acting.BeginGameStart(this);
        _boss1DropStart.Invoke();
        CameraFocusingController.Instance.SlowFocus(transform,1f,1f);

        Delay(
            1f,
            () =>
            {
                _boss1DropEmotion.Invoke();

            }
        );
        Delay(
            3f,
            () =>
            {
                Time.timeScale = 0.2f;
                _dropInstance = Instantiate(_bossDropVfx,new Vector3(0.25f,-0.41f,0),Quaternion.identity);
                CameraFocusingController.Instance.ShowOrigin();
            
                _playerMove.DashToPoint(_movePoint.position,PointMoveComplete);

            }
        );
        
        
    }

    private void PointMoveComplete()
    {
        Delay(
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
        _zoomTarget = zoomTarget;

        zoomTarget.transform.position =
            hitableObject.transform.position +
            hitableObject.transform.up * 1f;
        //CameraFocusingController.Instance.SlowFocus(hitableObject.transform,1f,1f);
        CameraFocusingController.Instance.Focus(zoomTarget.transform,1);
        Delay(
            1,
            () =>
            {
                _boss1DropName.Invoke();
            }
        );
        Delay(
            5,
            () =>
            {
                FocusToPlayer();
            }
        );
    }

    private void FocusToPlayer()
    {
        CameraFocusingController.Instance.SlowFocus(transform,1f,2f);
        _boss1DropFinish.Invoke();

        Delay(
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
    public void CancelForSkip()
    {
        _isSkipped = true;
        DOTween.Kill(this);
        GetComponent<PlayerStatusManager>().CancelStartForSkip();
    }

    public void ApplySkippedState()
    {
        if (_dropInstance != null)
            Destroy(_dropInstance);
        if (_zoomTarget != null)
            Destroy(_zoomTarget);
        _playerMove.SetActingPosition(_movePoint.position);
        MakeHpBar();
        CameraFocusingController.Instance.ReturnImmediately();
        GetComponent<PlayerStatusManager>().ApplySkippedVisuals(true);
    }

    public void CompleteAfterSkip()
    {
        GameStart();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (_zoomTarget != null)
            Destroy(_zoomTarget);
    }

    private void GameStart()
    {
        if (_isComplete)
            return;
        _isComplete = true;
        Cursor.visible = false;
        MakeHpBar();
        if (!_isSkipped)
            CameraFocusingController.Instance.SlowFocus(transform,0f,2f);
        
        GetComponent<PlayerStatusManager>().ApplyFinalSetting();
        
        _bossMoveStart.Invoke();
        _playerBattleStartEvent.Invoke();
        GetComponent<PlayerStatusManager>().Acting.StopActing();

        
    }
}
