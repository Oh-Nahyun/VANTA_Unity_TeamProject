using DG.Tweening;
using UnityEngine;

public class PlayerAvoid : MonoBehaviour
{
    [Header("타임슬로우 량")]
    [SerializeField] private float _slowAmount = 0.2f;

    [Header("회피기 사용할때 줌 량")]
    [SerializeField] private float _avoidZoomAmount = 1.5f;

    [Header("회피 입력 제한시간")]
    [SerializeField] private float _avoidDuration = 2f;

    [Header("회피 거리")]
    [SerializeField] private float _dashDistance = 1.5f;

    [Header("Focus")]
    [SerializeField] private SkillFocusScreenController _focusScreenController;

    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private KeyCode _avoidKey;
    private bool _isAvoidInteraction;

    private GameObject _avoidCaller;
    private Tween _avoidTimer;


    private void Awake()
    {
        _objectZoom = GetComponentInChildren<ObjectZoom>();
        _playerAvoidKeyGenerator = GetComponent<PlayerAvoidKeyGenerator>();
        _playerMove=GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _animator = GetComponent<Animator>();
    }


    private void Update()
    {
        CheckInputSuccess();

        if (Input.GetKeyDown(KeyCode.K))
        {
            StartAvoidInteraction(KeyCode.B);
        }
    }


    private void CheckInputSuccess()
    {
        if (!_isAvoidInteraction)
            return;

        if (Input.GetKeyDown(_avoidKey))
        {
            AvoidSuccess();
        }
    }


    public void StartAvoidInteraction(KeyCode keyCode,GameObject caller =null)
    {
        
        // 기존 타이머 제거
        _avoidTimer?.Kill();

        _avoidKey = keyCode;
        _isAvoidInteraction = true;

        _avoidCaller = caller;
        
        _playerMove.enabled = false;
        _playerAttack.enabled = false;
        // 타임 슬로우
        Time.timeScale = _slowAmount;

        // 줌
        _objectZoom.ZoomIn(_avoidZoomAmount);

        // 키 UI 생성
        _playerAvoidKeyGenerator.CreateAlarmKeyPad(keyCode);

        // 회피 준비 애니메이션
        _animator.SetBool("AvoidReady", true);

        // =========================
        // Focus 시작
        // =========================
        if (_focusScreenController != null)
        {
            _focusScreenController.StartFocusScreenSetting(null);
        }

        // 제한시간 시작
        _avoidTimer = DOVirtual
            .DelayedCall(_avoidDuration, AvoidEnd)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                AvoidFail();
           
        });
    }


    private void AvoidSuccess()
    {
        _animator.SetTrigger("Avoid");

        _avoidCaller.GetComponent<IAvoidInteraction>().AvoidSuccess();
        DashToReverseSide();

        AvoidEnd();
    }

    private void AvoidFail()
    {
        Debug.Log("회피 이동 완료");
        _avoidCaller.GetComponent<IAvoidInteraction>().AvoidFailure();
            
        _playerMove.enabled = true;
        _playerAttack.enabled = true;
    }


    private void AvoidEnd()
    {
        if (!_isAvoidInteraction)
            return;

        _animator.SetBool("AvoidReady", false);

        // 진행 중인 자동 종료 타이머 제거
        _avoidTimer?.Kill();
        _avoidTimer = null;

        // 키 UI 제거
        _playerAvoidKeyGenerator.ClearAlarmKeyPad();

        _isAvoidInteraction = false;


        // 타임스케일 복구
        Time.timeScale = 1f;

        // 줌 복구
        _objectZoom.ZoomOut();

        // =========================
        // Focus 종료
        // =========================
        if (_focusScreenController != null)
        {
            _focusScreenController.EndFocusScreenSetting();
        }

        Debug.Log($"회피 종료 : {_avoidKey}");
    }


    public void DashToReverseSide()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        HitableObject boss = FindAnyObjectByType<HitableObject>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D 없음");
            return;
        }

        if (boss == null)
        {
            Debug.LogError("HitableObject 없음");
            return;
        }

        // 보스 -> 플레이어 방향
        Vector2 direction =
            ((Vector2)transform.position -
             (Vector2)boss.transform.position).normalized;

        Vector2 targetPosition =
            rb.position +
            direction * _dashDistance;

        Debug.Log(
            $"회피 이동 | 현재: {rb.position} / 방향: {direction} / 목표: {targetPosition}"
        );

        rb.DOMove(targetPosition, 0.5f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                _playerMove.enabled = true;
                _playerAttack.enabled = true;

            });
    }


    private void OnDestroy()
    {
        _avoidTimer?.Kill();
    }
}
