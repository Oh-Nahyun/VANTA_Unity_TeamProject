using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("남은 크레딧 개수별 이벤트")]
    [SerializeField]
    private UnityEvent _onRemainingThree = new UnityEvent();

    [SerializeField]
    private UnityEvent _onRemainingTwo = new UnityEvent();

    [SerializeField]
    private UnityEvent _onRemainingOne = new UnityEvent();

    [Header("모든 크레딧 종료 이벤트")]
    [SerializeField]
    private UnityEvent _onCreditsFinished = new UnityEvent();

    private Tween _spawnDelayTween;
    private Credit _currentCredit;

    private int _nextIndex;
    private int _remainingCount;
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
        _remainingCount = CountValidCredits();
        _isPlaying = true;

        int currentRunId = _runId;

        InvokeRemainingEvent();

        // 이벤트에서 중지하거나 다시 시작했으면 이전 실행은 종료
        if (!_isPlaying || currentRunId != _runId)
            return;

        SpawnNextCredit();
    }

    private int CountValidCredits()
    {
        int count = 0;

        foreach (Credit credit in _credits)
        {
            if (credit != null)
                count++;
        }

        return count;
    }

    private void InvokeRemainingEvent()
    {
        switch (_remainingCount)
        {
            case 3:
                _onRemainingThree?.Invoke();
                break;

            case 2:
                _onRemainingTwo?.Invoke();
                break;

            case 1:
                _onRemainingOne?.Invoke();
                break;

            case 0:
                _isPlaying = false;
                _onCreditsFinished?.Invoke();
                break;
        }
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
            _remainingCount = 0;
            InvokeRemainingEvent();
            return;
        }

        Credit prefab = _credits[_nextIndex];
        _nextIndex++;

        int currentRunId = _runId;

        _currentCredit = Instantiate(
            prefab,
            _spawnPoint.position,
            _spawnPoint.rotation
        );

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
        _remainingCount = Mathf.Max(0, _remainingCount - 1);

        InvokeRemainingEvent();

        // 이벤트에서 중지하거나 다시 시작했으면 이전 실행은 종료
        if (!_isPlaying || currentRunId != _runId)
            return;

        _spawnDelayTween = DOVirtual.DelayedCall(
            _spawnInterval,
            () =>
            {
                _spawnDelayTween = null;

                if (!_isPlaying || currentRunId != _runId)
                    return;

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
