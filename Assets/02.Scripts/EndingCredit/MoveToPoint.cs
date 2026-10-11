using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class MoveToPoint : MonoBehaviour
{
    [Header("도착 위치")]
    [SerializeField]
    private Transform _targetPoint;

    [Header("이동 시간")]
    [SerializeField, Min(0.01f)]
    private float _moveDuration = 2f;

    [Header("이동 곡선")]
    [SerializeField]
    private Ease _moveEase = Ease.Linear;

    [Header("도착시 호출")]
    [SerializeField]private UnityEvent _tweenFinished;
    
    
    private Tween _moveTween;

    private void OnEnable()
    {
        _moveTween?.Kill();

        if (_targetPoint == null)
            return;

        _moveTween = transform.DOMove(
                _targetPoint.position,
                _moveDuration
            )
            .SetEase(_moveEase)
            .OnComplete(() =>
            {
                _moveTween = null;
                _tweenFinished.Invoke();
                GetComponent<PetJump>().enabled = true;
            });
    }

    private void OnDisable()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }
}
