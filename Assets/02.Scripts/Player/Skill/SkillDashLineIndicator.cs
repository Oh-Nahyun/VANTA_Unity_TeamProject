using System.Collections.Generic;
using UnityEngine;

public class SkillDashLineIndicator : MonoBehaviour
{
    [SerializeField] private Transform _player;

    [Header("라인 프리팹")]
    [SerializeField] private LineRenderer _selectedLinePrefab;
    [SerializeField] private LineRenderer _previewLineRenderer;

    [Header("라인 여백")]
    [SerializeField] private float _lineOffset = 0.5f;

    [Header("화살표 길이 단위")]
    [Tooltip("화살표 하나가 차지하는 라인 길이")]
    [SerializeField] private float _arrowSectionLength = 0.5f;

    private readonly List<LineRenderer> _createdLines = new();

    // 마지막으로 선택된 Indicator
    private Transform _lastSelectedPoint;


    private void OnEnable()
    {
        ResetLine();
    }

    private void OnDisable()
    {
        ResetLine();
    }


    public void AddNewPoint(SkillIndicator selectedIndicator)
    {
        if (selectedIndicator == null)
            return;

        Transform startTransform;

        // 처음이면 Player → Indicator
        if (_lastSelectedPoint == null)
        {
            startTransform = _player;
        }
        // 그 다음부터는 이전 Indicator → Indicator
        else
        {
            startTransform = _lastSelectedPoint;
        }

        CreateLine(
            startTransform.position,
            selectedIndicator.transform.position
        );

        _lastSelectedPoint = selectedIndicator.transform;

        HidePreview();
    }


    private void CreateLine(
        Vector3 startCenter,
        Vector3 endCenter)
    {
        // 화살표 단위로 스냅된 라인 위치 계산
        if (!CalculateLinePositions(
                startCenter,
                endCenter,
                out Vector3 startPosition,
                out Vector3 endPosition))
        {
            return;
        }

        // LineRenderer 생성
        LineRenderer line = Instantiate(
            _selectedLinePrefab,
            transform
        );

        line.positionCount = 2;

        line.SetPosition(
            0,
            startPosition
        );

        line.SetPosition(
            1,
            endPosition
        );

        line.enabled = true;

        _createdLines.Add(line);
    }


    public void ShowPreview(SkillIndicator indicator)
    {
        if (indicator == null)
        {
            HidePreview();
            return;
        }

        Vector3 startCenter;

        if (_lastSelectedPoint == null)
        {
            startCenter = _player.position;
        }
        else
        {
            startCenter = _lastSelectedPoint.position;
        }

        Vector3 endCenter =
            indicator.transform.position;

        // 화살표 단위로 스냅된 라인 위치 계산
        if (!CalculateLinePositions(
                startCenter,
                endCenter,
                out Vector3 startPosition,
                out Vector3 endPosition))
        {
            HidePreview();
            return;
        }

        _previewLineRenderer.positionCount = 2;

        _previewLineRenderer.SetPosition(
            0,
            startPosition
        );

        _previewLineRenderer.SetPosition(
            1,
            endPosition
        );

        _previewLineRenderer.enabled = true;
    }


    /// <summary>
    /// 실제 거리를 화살표 하나의 길이 단위로 끊어서
    /// LineRenderer의 시작/끝 위치를 계산한다.
    /// </summary>
    private bool CalculateLinePositions(
        Vector3 startCenter,
        Vector3 endCenter,
        out Vector3 startPosition,
        out Vector3 endPosition)
    {
        Vector3 direction =
            endCenter - startCenter;

        // 거리가 너무 가까우면 라인을 만들지 않음
        if (direction.sqrMagnitude < 0.001f)
        {
            startPosition = Vector3.zero;
            endPosition = Vector3.zero;

            return false;
        }

        direction.Normalize();

        // 시작점 여백
        startPosition =
            startCenter +
            direction * _lineOffset;

        // 원래 도착해야 할 끝 위치
        Vector3 targetEndPosition =
            endCenter -
            direction * _lineOffset;

        // 실제 사용할 수 있는 라인 길이
        float lineLength =
            Vector3.Distance(
                startPosition,
                targetEndPosition
            );

        // 화살표 하나의 길이 단위로 내림
        //
        // 예:
        // Section = 0.5
        //
        // 4.93 → 4.5
        // 4.71 → 4.5
        // 4.51 → 4.5
        // 4.49 → 4.0
        float snappedLength =
            Mathf.Floor(
                lineLength / _arrowSectionLength
            ) * _arrowSectionLength;

        // 화살표 하나조차 들어갈 공간이 없으면 표시하지 않음
        if (snappedLength < _arrowSectionLength)
        {
            endPosition = startPosition;

            return false;
        }

        // 스냅된 길이를 기준으로 실제 끝 위치 계산
        endPosition =
            startPosition +
            direction * snappedLength;

        return true;
    }


    public void HidePreview()
    {
        if (_previewLineRenderer == null)
            return;

        _previewLineRenderer.positionCount = 0;
        _previewLineRenderer.enabled = false;
    }


    public void ResetLine()
    {
        HidePreview();

        foreach (LineRenderer line in _createdLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        _createdLines.Clear();

        _lastSelectedPoint = null;
    }
}
