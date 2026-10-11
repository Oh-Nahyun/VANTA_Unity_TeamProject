using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class EndingDirector : MonoBehaviour
{
    [Header("배우들 — 활성화 순서")]
    [SerializeField]
    private List<GameObject> _actors = new List<GameObject>();

    [Header("적 배우들 — 활성화 순서")]
    [SerializeField]
    private List<GameObject> _enemyActors = new List<GameObject>();

    [Header("배우 활성화 간격")]
    [SerializeField, Min(0.01f)]
    private float _activateInterval = 3f;

    [Header("적 배우 활성화 간격")]
    [SerializeField, Min(0.01f)]
    private float _enemyActivateInterval = 2f;
    
    [Header("엔딩 크레딧 등장 딜레이")]
    [SerializeField, Min(0f)]
    private float _endingCreditDelay = 3f;
    
    [Header("엔딩 크레딧 등장 시점")] [SerializeField]
    private UnityEvent _endingCreditStart;
    
    private Sequence _sequence;


    void Awake()
    {
        Cursor.visible = false;
    }
    private void Start()
    {
        SetInactive(_actors);
        SetInactive(_enemyActors);

        _sequence = DOTween.Sequence();

        AppendActors(_actors, _activateInterval);
        AppendActors(_enemyActors, _enemyActivateInterval);
        
        
             DOVirtual.DelayedCall(
            _endingCreditDelay,
            () => _endingCreditStart?.Invoke()
        );
        
    }

    private void SetInactive(List<GameObject> actors)
    {
        foreach (GameObject actor in actors)
        {
            if (actor != null)
                actor.SetActive(false);
        }
    }

    private void AppendActors(
        List<GameObject> actors,
        float interval)
    {
        foreach (GameObject actor in actors)
        {
            if (actor == null)
                continue;

            GameObject targetActor = actor;

            _sequence.AppendInterval(interval);

            _sequence.AppendCallback(() =>
            {
                if (targetActor != null)
                    targetActor.SetActive(true);
            });
        }
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        _sequence = null;
    }
}
