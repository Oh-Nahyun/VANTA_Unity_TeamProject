using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("게임 재도전 버튼")]
    [SerializeField] private Button _gameRetryButton;

    [Header("게임 타이틀로 이동 버튼")]
    [SerializeField] private Button _gameTitleButton;

    private void Start()
    {
        _gameRetryButton.onClick.AddListener(OnClickRetryButton);
        _gameTitleButton.onClick.AddListener(OnClickTitleButton);
    }

    private void OnClickRetryButton()
    {
        SceneManager.LoadScene(0); // InGameScene
    }

    private void OnClickTitleButton()
    {
        SceneManager.LoadScene(0); // InGameScene
    }
}
