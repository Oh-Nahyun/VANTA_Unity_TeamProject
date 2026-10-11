using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("게임 재도전 버튼")]
    [SerializeField] private Button _gameRetryButton;

    [Header("게임 타이틀로 이동 버튼")]
    [SerializeField] private Button _gameTitleButton;

    [Header("버튼에 마우스를 올렸을 때 효과음")]
    [SerializeField] private AudioClip _buttonHoverSound;

    [Header("버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonClickSound;

    private void Start()
    {
        _gameRetryButton.onClick.AddListener(OnClickRetryButton);
        _gameTitleButton.onClick.AddListener(OnClickTitleButton);

        SoundGenerator.AddHoverSound(_gameRetryButton, _buttonHoverSound);
        SoundGenerator.AddHoverSound(_gameTitleButton, _buttonHoverSound);
    }

    private void OnClickRetryButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        InGameSceneManager.IsRetry = true;
        SceneManager.LoadScene(0); // InGameScene
    }

    private void OnClickTitleButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        InGameSceneManager.IsRetry = false;
        SceneManager.LoadScene(0); // InGameScene
    }
}
