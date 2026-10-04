using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameClearUI : MonoBehaviour
{
    [Header("게임 다음으로 이동 버튼")]
    [SerializeField] private Button _gameNextButton;

    [Header("게임 타이틀로 이동 버튼")]
    [SerializeField] private Button _gameTitleButton;

    private void Start()
    {
        _gameNextButton.onClick.AddListener(OnClickNextButton);
        _gameTitleButton.onClick.AddListener(OnClickTitleButton);
    }

    private void OnClickNextButton()
    {
        // TODO : [UGUI] 보스 클리어 후 다음 연출 시작하기
    }

    private void OnClickTitleButton()
    {
        SceneManager.LoadScene(0); // GameTitleScene
    }
}
