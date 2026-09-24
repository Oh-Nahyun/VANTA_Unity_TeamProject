using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkillAttack : MonoBehaviour
{
    [Header("Skill Dash")]
    [SerializeField] private float _dashDuration = 0.15f;
    [SerializeField] private Ease _dashEase = Ease.OutQuad;
    [Header("참조")]
    [SerializeField]private Rigidbody2D _rigidbody;
    private Collider2D[] _playerColliders;
    private Sequence _dashSequence;

    private void Awake()
    {
        if (_rigidbody != null)
            _playerColliders = _rigidbody.GetComponentsInChildren<Collider2D>(true);
    }
    
    public void DashStart(List<SkillIndicator> selectedIndicators,Action onComplete)
    {
        if (_rigidbody == null || selectedIndicators == null || selectedIndicators.Count == 0)
            return;

        _dashSequence?.Kill();

        SetPlayerColliders(false);
        _dashSequence = DOTween.Sequence();

        foreach (SkillIndicator indicator in selectedIndicators)
        {
            if (indicator == null)
                continue;

            _dashSequence.Append(
                _rigidbody.DOMove(indicator.transform.position, _dashDuration)
                    .SetEase(_dashEase));
            indicator.SkillAttackComplete();
        }

        _dashSequence
            .OnComplete(() =>
            {
                SetPlayerColliders(true);
                _dashSequence = null;
                onComplete.Invoke();
            })
            .OnKill(() => SetPlayerColliders(true));
    }

    private void SetPlayerColliders(bool enabled)
    {
        if (_playerColliders == null)
            return;

        foreach (Collider2D playerCollider in _playerColliders)
        {
            if (playerCollider != null)
                playerCollider.enabled = enabled;
        }
    }
}
