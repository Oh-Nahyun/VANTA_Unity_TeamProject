using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameTitleButtonUI : MonoBehaviour
{
    [Header("게임 시작 버튼")]
    [SerializeField] private Button _gameStartButton;

    [Header("게임 설정 버튼")]
    [SerializeField] private Button _gameSettingsButton;

    [Header("게임 종료 버튼")]
    [SerializeField] private Button _gameExitButton;

    private void Start()
    {
        _gameStartButton.onClick.AddListener(OnClickStartButton);
        _gameSettingsButton.onClick.AddListener(OnClickSettingsButton);
        _gameExitButton.onClick.AddListener(OnClickExitButton);
    }

    private void OnClickStartButton()
    {
        SceneManager.LoadScene(1); // InGameScene
    }

    private void OnClickSettingsButton()
    {
        // TODO : 게임 설정 시스템 구현하기
    }

    private void OnClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
