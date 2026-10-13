using UnityEngine;
using UnityEngine.EventSystems;
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

    /// <summary>
    /// 키보드 조작 대상 버튼 목록
    /// </summary>
    private Button[] _buttons;

    private void OnEnable()
    {
        GameTitleUI.IsKeyboardMode = false;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void Start()
    {
        _buttons = new[] { _gameRetryButton, _gameTitleButton };

        foreach (Button button in _buttons)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        _gameRetryButton.onClick.AddListener(OnClickRetryButton);
        _gameTitleButton.onClick.AddListener(OnClickTitleButton);

        SoundGenerator.AddHoverSound(_gameRetryButton, _buttonHoverSound);
        SoundGenerator.AddHoverSound(_gameTitleButton, _buttonHoverSound);
    }

    private void Update()
    {
        bool isJustSwitched = GameTitleUI.UpdateInputMode();
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (!GameTitleUI.IsKeyboardMode)
        {
            if (selected != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return;
        }

        if (isJustSwitched || selected == null)
        {
            EventSystem.current.SetSelectedGameObject(_gameRetryButton.gameObject);
            return;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        bool isUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                    || (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame));
        bool isDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)
                      || (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame));

        if (!isUp && !isDown) return;

        int index = System.Array.FindIndex(_buttons, button => button.gameObject == selected);
        index = Mathf.Clamp(index + (isDown ? 1 : -1), 0, _buttons.Length - 1);

        EventSystem.current.SetSelectedGameObject(_buttons[index].gameObject);
    }

    private void LateUpdate()
    {
        Cursor.visible = !GameTitleUI.IsKeyboardMode;
    }

    public void OnClickRetryButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        InGameSceneManager.IsRetry = true;
        SceneManager.LoadScene(0); // InGameScene
    }

    public void OnClickTitleButton()
    {
        SoundManager.Instance.PlayEffectSound(_buttonClickSound);
        InGameSceneManager.IsRetry = false;
        SceneManager.LoadScene(0); // InGameScene
    }
}
