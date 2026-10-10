using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    [Header("게임 오버 UI")]
    [SerializeField] private GameObject _gameOverUI;

    [Header("게임 클리어 UI")]
    [SerializeField] private GameObject _gameClearUI;

    private void Start()
    {
        SetGameResult();
    }

    private void SetGameResult()
    {
        _gameOverUI.SetActive(false);
        _gameClearUI.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        _gameOverUI.SetActive(true);
    }

    // public void ShowGameClearUI()
    // {
    //     _gameClearUI.SetActive(true);
    // }

    public void LoadGameEndingScene()
    {
        SceneManager.LoadScene(1); // GameEndingScene
    }
}
