using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameClearUI : MonoBehaviour
{
    [Header("게임 다음으로 이동 버튼")]
    [SerializeField] private Button _gameNextButton;

    [Header("게임 타이틀로 이동 버튼")]
    [SerializeField] private Button _gameTitleButton;

    [Header("버튼에 마우스를 올렸을 때 효과음")]
    [SerializeField] private AudioClip _buttonHoverSound;

    [Header("버튼 클릭 효과음")]
    [SerializeField] private AudioClip _buttonClickSound;

    private void Start()
    {
        _gameNextButton.onClick.AddListener(OnClickNextButton);
        _gameTitleButton.onClick.AddListener(OnClickTitleButton);

        SoundGenerator.AddHoverSound(_gameNextButton, _buttonHoverSound);
        SoundGenerator.AddHoverSound(_gameTitleButton, _buttonHoverSound);
    }

    private void OnClickNextButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        // TODO : [UGUI] 보스 클리어 후 다음 연출 시작하기
        SceneManager.LoadScene(0); // InGameScene
    }

    private void OnClickTitleButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        SceneManager.LoadScene(0); // InGameScene
    }
}
