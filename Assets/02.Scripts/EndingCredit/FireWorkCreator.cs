using DG.Tweening;
using UnityEngine;

public class FireWorkCreator : MonoBehaviour
{
    [Header("폭죽 프리팹")]
    [SerializeField]
    private FireWorkMove _fireWorkPrefab;

    [Header("폭죽 부모")]
    [SerializeField]
    private RectTransform _fireWorkParent;

    [Header("생성 간격 범위")]
    [SerializeField]
    private Vector2 _spawnIntervalRange =
        new Vector2(0.5f, 1.5f);

    [Header("한 번에 생성되는 폭죽 개수")]
    [SerializeField, Range(1, 3)]
    private int _maxSpawnCount = 3;

    [Header("동시 발사 폭죽 사이의 미세한 시간차")]
    [SerializeField]
    private Vector2 _burstIntervalRange =
        new Vector2(0.05f, 0.2f);

    private Tween _spawnTween;

    private bool _isPlaying;

    public void StartFireWork()
    {
        if (_isPlaying)
            return;

        _isPlaying = true;

        SpawnFireWorks();
    }

    public void StopFireWork()
    {
        _isPlaying = false;

        _spawnTween?.Kill();
        _spawnTween = null;
    }

    private void SpawnFireWorks()
    {
        if (!_isPlaying)
            return;

        // 이번 발사에서 1~최대 3개
        int spawnCount =
            Random.Range(
                1,
                _maxSpawnCount + 1
            );

        SpawnBurst(
            spawnCount,
            0
        );
    }

    private void SpawnBurst(
        int spawnCount,
        int currentIndex)
    {
        if (!_isPlaying)
            return;

        CreateFireWork();

        currentIndex++;

        // 이번 묶음의 폭죽을 전부 발사했다면
        // 다음 발사 묶음까지 랜덤 대기
        if (currentIndex >= spawnCount)
        {
            ScheduleNextBurst();
            return;
        }

        // 같은 묶음의 다음 폭죽은 약간의 시간차를 두고 발사
        float burstInterval =
            Random.Range(
                _burstIntervalRange.x,
                _burstIntervalRange.y
            );

        _spawnTween =
            DOVirtual.DelayedCall(
                burstInterval,
                () =>
                {
                    _spawnTween = null;

                    SpawnBurst(
                        spawnCount,
                        currentIndex
                    );
                }
            );
    }

    private void ScheduleNextBurst()
    {
        if (!_isPlaying)
            return;

        float interval =
            Random.Range(
                _spawnIntervalRange.x,
                _spawnIntervalRange.y
            );

        _spawnTween =
            DOVirtual.DelayedCall(
                interval,
                () =>
                {
                    _spawnTween = null;

                    SpawnFireWorks();
                }
            );
    }

    private void CreateFireWork()
    {
        if (_fireWorkPrefab == null ||
            _fireWorkParent == null)
        {
            return;
        }

        Instantiate(
            _fireWorkPrefab,
            _fireWorkParent
        );
    }

    private void OnDisable()
    {
        StopFireWork();
    }
}
