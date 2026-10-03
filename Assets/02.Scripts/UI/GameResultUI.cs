using System;
using UnityEngine;

public class GameResultUI : MonoBehaviour
{
    [Header("게임 오버 UI")]
    [SerializeField] private GameObject _gameOverUI;

    [Header("게임 클리어 UI")]
    [SerializeField] private GameObject _gameClearUI;

    /// <summary>
    /// 플레이어 클래스
    /// </summary>
    private Player _player;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    public void ShowGameOverUI()
    {
        _gameOverUI.SetActive(true);
    }

    public void ShowGameClearUI()
    {
        _gameOverUI.SetActive(true);
    }
}
