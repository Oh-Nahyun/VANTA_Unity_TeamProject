using System.Collections.Generic;
using UnityEngine;

public class PlayerAvoidKeyGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _alarmMoveKeyPad;
    [SerializeField] private GameObject _alarmMoveAttackKeyPad;
    [SerializeField] private GameObject _alarmFinisherKeyPad;

    [Header("Spawn")]
    [SerializeField] private float _spawnDistance = 100f;

    [Header("Screen Padding")]
    [SerializeField] private float _screenPadding = 20f;

    private GameObject _currentAlarmKeyPad;

    private readonly Vector2[] _directions =
    {
        Vector2.left,
        Vector2.right,
        Vector2.up
    };

    public void CreateAlarmKeyPad(bool isMoveAttack)
    {
        GameObject prefab = isMoveAttack
            ? _alarmMoveAttackKeyPad
            : _alarmMoveKeyPad;

        CreateKeyPad(prefab);
    }

    public void CreateFinisherKey()
    {
        CreateKeyPad(_alarmFinisherKeyPad);
    }

    private void CreateKeyPad(GameObject prefab)
    {
        ClearAlarmKeyPad();

        if (prefab == null || _canvas == null)
            return;

        _currentAlarmKeyPad = Instantiate(
            prefab,
            _canvas.transform
        );

        RectTransform alarmRect =
            _currentAlarmKeyPad.GetComponent<RectTransform>();

        RectTransform canvasRect =
            _canvas.transform as RectTransform;

        Vector2 playerCanvasPosition =
            GetPlayerCanvasLoc();

        Vector2 spawnPosition =
            GetValidSpawnPosition(
                playerCanvasPosition,
                alarmRect,
                canvasRect
            );

        alarmRect.anchoredPosition = spawnPosition;
    }

    private Vector2 GetValidSpawnPosition(
        Vector2 playerPosition,
        RectTransform alarmRect,
        RectTransform canvasRect)
    {
        List<Vector2> validPositions =
            new List<Vector2>();

        // 세 방향 모두 검사
        foreach (Vector2 direction in _directions)
        {
            Vector2 candidatePosition =
                playerPosition +
                direction * _spawnDistance;

            if (IsInsideScreen(
                    candidatePosition,
                    alarmRect,
                    canvasRect))
            {
                validPositions.Add(candidatePosition);
            }
        }

        // 화면 내부에 생성 가능한 방향이 있다면
        if (validPositions.Count > 0)
        {
            int randomIndex =
                Random.Range(0, validPositions.Count);

            return validPositions[randomIndex];
        }

        // 모든 방향이 화면 밖이라면
        // 랜덤 방향 선택 후 화면 내부로 보정
        Vector2 randomDirection =
            _directions[
                Random.Range(0, _directions.Length)
            ];

        Vector2 fallbackPosition =
            playerPosition +
            randomDirection * _spawnDistance;

        return ClampToScreen(
            fallbackPosition,
            alarmRect,
            canvasRect
        );
    }

    private bool IsInsideScreen(
        Vector2 position,
        RectTransform alarmRect,
        RectTransform canvasRect)
    {
        GetPositionLimits(
            alarmRect,
            canvasRect,
            out Vector2 min,
            out Vector2 max
        );

        return
            position.x >= min.x &&
            position.x <= max.x &&
            position.y >= min.y &&
            position.y <= max.y;
    }

    private Vector2 ClampToScreen(
        Vector2 position,
        RectTransform alarmRect,
        RectTransform canvasRect)
    {
        GetPositionLimits(
            alarmRect,
            canvasRect,
            out Vector2 min,
            out Vector2 max
        );

        return new Vector2(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y)
        );
    }

    private void GetPositionLimits(
        RectTransform alarmRect,
        RectTransform canvasRect,
        out Vector2 min,
        out Vector2 max)
    {
        Rect canvasBounds = canvasRect.rect;

        Vector2 size = alarmRect.rect.size;

        Vector2 pivot = alarmRect.pivot;

        // UI Pivot까지 고려한 실제 영역
        Vector2 leftBottomOffset =
            new Vector2(
                size.x * pivot.x,
                size.y * pivot.y
            );

        Vector2 rightTopOffset =
            new Vector2(
                size.x * (1f - pivot.x),
                size.y * (1f - pivot.y)
            );

        min = new Vector2(
            canvasBounds.xMin +
            leftBottomOffset.x +
            _screenPadding,

            canvasBounds.yMin +
            leftBottomOffset.y +
            _screenPadding
        );

        max = new Vector2(
            canvasBounds.xMax -
            rightTopOffset.x -
            _screenPadding,

            canvasBounds.yMax -
            rightTopOffset.y -
            _screenPadding
        );

        // UI가 화면보다 클 경우 예외 처리
        if (min.x > max.x)
        {
            float center = (min.x + max.x) * 0.5f;
            min.x = center;
            max.x = center;
        }

        if (min.y > max.y)
        {
            float center = (min.y + max.y) * 0.5f;
            min.y = center;
            max.y = center;
        }
    }

    private Vector2 GetPlayerCanvasLoc()
    {
        RectTransform canvasRect =
            _canvas.transform as RectTransform;

        Camera targetCamera = Camera.main;

        Vector2 screenPosition =
            targetCamera.WorldToScreenPoint(
                transform.position
            );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera,
            out Vector2 canvasPosition
        );

        return canvasPosition;
    }

    public void ClearAlarmKeyPad()
    {
        if (_currentAlarmKeyPad != null)
        {
            Destroy(_currentAlarmKeyPad);
            _currentAlarmKeyPad = null;
        }
    }
}
