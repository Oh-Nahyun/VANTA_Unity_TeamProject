using System;
using UnityEngine;

public class PlayerAvoidKeyGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _alarmMoveKeyPad;
    [SerializeField] private GameObject _alarmMoveAttackKeyPad;

    [Header("Spawn")]
    [SerializeField] private float _spawnDistance = 100f;

    private GameObject _currentAlarmKeyPad;

    public void CreateAlarmKeyPad(bool isMoveAttack)
    {
        ClearAlarmKeyPad();

        RectTransform canvasRect =
            _canvas.transform as RectTransform;

        // 플레이어의 Canvas 기준 위치
        Vector2 playerCanvasPosition = GetPlayerCanvasLoc();

        // Left / Right / Up 중 랜덤
        Vector2 direction = GetRandomDirection();

        Vector2 spawnPosition =
            playerCanvasPosition + direction * _spawnDistance;

        if (isMoveAttack)
        {
            _currentAlarmKeyPad = Instantiate(
                _alarmMoveAttackKeyPad,
                _canvas.transform
            );
        }
        else
        {
            _currentAlarmKeyPad = Instantiate(
                _alarmMoveKeyPad,
                _canvas.transform
            );
        }
    
        

        RectTransform alarmRect =
            _currentAlarmKeyPad.GetComponent<RectTransform>();

        alarmRect.anchoredPosition = spawnPosition;
        
        
    }

    private Vector2 GetPlayerCanvasLoc()
    {
        RectTransform canvasRect =
            _canvas.transform as RectTransform;

        Vector2 screenPosition =
            Camera.main.WorldToScreenPoint(transform.position);

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

    private Vector2 GetRandomDirection()
    {
        int random = UnityEngine.Random.Range(0, 3);

        switch (random)
        {
            case 0:
                return Vector2.left;

            case 1:
                return Vector2.right;

            case 2:
                return Vector2.up;

            default:
                return Vector2.up;
        }
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
