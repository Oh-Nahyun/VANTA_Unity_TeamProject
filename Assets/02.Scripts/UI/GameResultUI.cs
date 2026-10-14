using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    [Header("게임 오버 UI")]
    [SerializeField] private GameObject _gameOverUI;

    [Header("게임 클리어 UI")]
    [SerializeField] private GameObject _gameClearUI;

    [Header("게임 오버 창이 뜰 때 효과음")]
    [SerializeField] private AudioClip _gameOverSound;

    [Header("게임 엔딩 씬 전환 대기 시간")]
    [SerializeField] private float _endingSceneDelay = 3f;

    /// <summary>
    /// 결과 창 표시 여부
    /// </summary>
    public static bool IsResultShown
    {
        get;
        private set;
    }

    /// <summary>
    /// 게임 엔딩 씬 전환 코루틴
    /// </summary>
    private Coroutine _loadEndingSceneCoroutine;

    private void Start()
    {
        SetGameResult();
    }

    private void SetGameResult()
    {
        IsResultShown = false;
        _gameOverUI.SetActive(false);
        _gameClearUI.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        IsResultShown = true;
        _gameOverUI.SetActive(true);
        SoundManager.Instance.PlayEffectSound(_gameOverSound);
    }

    // public void ShowGameClearUI()
    // {
    //     IsResultShown = true;
    //     _gameClearUI.SetActive(true);
    // }

    public void LoadGameEndingScene()
    {
        if (_loadEndingSceneCoroutine != null) return;

        _loadEndingSceneCoroutine = StartCoroutine(LoadEndingScene());
    }

    private IEnumerator LoadEndingScene()
    {
        yield return new WaitForSecondsRealtime(_endingSceneDelay);

        SceneManager.LoadScene(1); // GameEndingScene
    }
}
