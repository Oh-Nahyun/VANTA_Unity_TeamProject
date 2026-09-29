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
    [Header("회피 거리")][SerializeField]private float _dashDistance = 1.5f;
    private Animator _animator;
    private ObjectZoom _objectZoom;
    private PlayerAvoidKeyGenerator _playerAvoidKeyGenerator;
    
    private KeyCode _avoidKey;
    private bool _isAvoidInteraction;

    private Tween _avoidTimer;

    private void Awake()
    {
        _objectZoom = GetComponentInChildren<ObjectZoom>();
        _playerAvoidKeyGenerator = GetComponent<PlayerAvoidKeyGenerator>();
        _animator=GetComponent<Animator>();
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

    public void StartAvoidInteraction(KeyCode keyCode)
    {
        // 기존 타이머가 있다면 제거
        _avoidTimer?.Kill();

        _avoidKey = keyCode;
        _isAvoidInteraction = true;

        Time.timeScale = _slowAmount;

        _objectZoom.ZoomIn(_avoidZoomAmount);
        _playerAvoidKeyGenerator.CreateAlarmKeyPad(keyCode);

        _animator.SetBool("AvoidReady",true);

        
        // 제한시간 시작
        _avoidTimer = DOVirtual
            .DelayedCall(_avoidDuration, AvoidEnd)
            .SetUpdate(true);
    }


    private void AvoidSuccess()
    {
        _animator.SetTrigger("Avoid");
        DashToReverseSide();
        AvoidEnd();
    }
    
    
    private void AvoidEnd()
    {
        if (!_isAvoidInteraction)
            return;

        _animator.SetBool("AvoidReady",false);

        // 입력 성공으로 AvoidEnd가 먼저 호출됐다면
        // 진행 중인 자동 종료 타이머 제거
        _avoidTimer?.Kill();
        _avoidTimer = null;

        _playerAvoidKeyGenerator.ClearAlarmKeyPad();

        _isAvoidInteraction = false;

        Time.timeScale = 1f;

        _objectZoom.ZoomOut();

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
            ((Vector2)transform.position - (Vector2)boss.transform.position).normalized;

        Vector2 targetPosition =
            rb.position + direction * _dashDistance;

        Debug.Log(
            $"회피 이동 | 현재: {rb.position} / 방향: {direction} / 목표: {targetPosition}"
        );

        rb.DOMove(targetPosition, 0.5f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                Debug.Log("회피 이동 완료");
            });
    }
    private void OnDestroy()
    {
        _avoidTimer?.Kill();
    }
}
