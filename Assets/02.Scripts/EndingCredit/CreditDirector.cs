using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CreditDirector : MonoBehaviour
{
    [Header("생성할 크레딧 프리팹")]
    [SerializeField]
    private List<Credit> _credits = new List<Credit>();

    [Header("생성 위치")]
    [SerializeField]
    private Transform _spawnPoint;

    [Header("파괴 후 다음 크레딧까지 대기 시간")]
    [SerializeField, Min(0f)]
    private float _spawnInterval = 2f;

    private Tween _spawnDelayTween;
    private Credit _currentCredit;

    private int _nextIndex;
    private int _runId;
    private bool _isPlaying;

    

    private void OnEnable()
    {
        StartCredit();
    }

    public void StartCredit()
    {
        StopCredit();

        if (_spawnPoint == null)
        {
            Debug.LogWarning(
                "[CreditDirector] 생성 위치가 지정되지 않았습니다.",
                this
            );
            return;
        }

        _nextIndex = 0;
        _isPlaying = true;

        SpawnNextCredit();
    }

    private void SpawnNextCredit()
    {
        if (!_isPlaying)
            return;

        // 비어 있는 항목은 건너뛰기
        while (_nextIndex < _credits.Count &&
               _credits[_nextIndex] == null)
        {
            _nextIndex++;
        }

        if (_nextIndex >= _credits.Count)
        {
            _isPlaying = false;
            return;
        }

        Credit prefab = _credits[_nextIndex];
        _nextIndex++;

        _currentCredit = Instantiate(
            prefab,
            _spawnPoint.position,
            _spawnPoint.rotation
        );

        int currentRunId = _runId;

        _currentCredit.Initialize(() =>
        {
            OnCreditDestroyed(currentRunId);
        });
    }

    private void OnCreditDestroyed(int currentRunId)
    {
        // 중지하거나 다시 시작한 이전 크레딧의 신호는 무시
        if (!_isPlaying || currentRunId != _runId)
            return;

        _currentCredit = null;

        if (_nextIndex >= _credits.Count)
        {
            _isPlaying = false;
            return;
        }

        _spawnDelayTween = DOVirtual.DelayedCall(
            _spawnInterval,
            () =>
            {
                _spawnDelayTween = null;
                SpawnNextCredit();
            },
            ignoreTimeScale: false
        );
    }

    public void StopCredit()
    {
        _isPlaying = false;
        _runId++;

        _spawnDelayTween?.Kill();
        _spawnDelayTween = null;

        if (_currentCredit != null)
        {
            Destroy(_currentCredit.gameObject);
            _currentCredit = null;
        }
    }

    private void OnDisable()
    {
        StopCredit();
    }
}
