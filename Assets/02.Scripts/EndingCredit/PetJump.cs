using DG.Tweening;
using UnityEngine;

public class PetJump : MonoBehaviour
{
    [Header("점프 높이")]
    [SerializeField, Min(0f)]
    private float _jumpHeight = 1f;

    [Header("점프 전체 시간")]
    [SerializeField, Min(0.01f)]
    private float _jumpDuration = 0.5f;

    private Sequence _jumpSequence;
    private Vector3 _startLocalPosition;
    private bool _isJumping;

    public void Jump()
    {
        if (_isJumping)
            return;

        _isJumping = true;
        _startLocalPosition = transform.localPosition;

        float halfDuration = _jumpDuration * 0.5f;

        _jumpSequence = DOTween.Sequence();

        _jumpSequence.Append(
            transform.DOLocalMoveY(
                _startLocalPosition.y + _jumpHeight,
                halfDuration
            ).SetEase(Ease.OutQuad)
        );

        _jumpSequence.Append(
            transform.DOLocalMoveY(
                _startLocalPosition.y,
                halfDuration
            ).SetEase(Ease.InQuad)
        );

        _jumpSequence.OnComplete(() =>
        {
            _isJumping = false;
            _jumpSequence = null;
        });
    }

    private void OnDisable()
    {
        _jumpSequence?.Kill();
        _jumpSequence = null;

        if (_isJumping)
        {
            Vector3 position = transform.localPosition;
            position.y = _startLocalPosition.y;
            transform.localPosition = position;
        }

        _isJumping = false;
    }
}
