using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameTitleUI : MonoBehaviour
{
    [Header("게임 시작 버튼")]
    [SerializeField] private Button _gameStartButton;

    [Header("게임 설정 버튼")]
    [SerializeField] private Button _gameSettingsButton;

    [Header("게임 종료 버튼")]
    [SerializeField] private Button _gameExitButton;

    [Header("게임 설정 패널")]
    [SerializeField] private GameObject _gameSettingPanel;

    [Header("타이틀 디졸브 효과용 클래스")]
    [SerializeField] TitleDissolveEffect _titleDissolveEffect;

    [Header("타이틀 페이드 아웃 효과용 클래스")]
    [SerializeField] TitleFadeInOutEffect _titleFadeInOutEffect;

    private void Start()
    {
        _gameStartButton.onClick.AddListener(OnClickStartButton);
        _gameSettingsButton.onClick.AddListener(OnClickSettingsButton);
        _gameExitButton.onClick.AddListener(OnClickExitButton);
    }

    private void OnClickStartButton()
    {
        StartCoroutine(DestroyTitleUICoroutine());
    }

    private void OnClickSettingsButton()
    {
        _gameSettingPanel.active = true;
    }

    private void OnClickExitButton()
    {
        StartCoroutine(ExitAfterFadeOutCoroutine());
    }

    private IEnumerator DestroyTitleUICoroutine()
    {
        _titleDissolveEffect.PlayButtonDissolve();
        yield return new WaitForSeconds(_titleDissolveEffect.ButtonDissolveDuration);
        transform.GetChild(1).gameObject.SetActive(false);

        _titleDissolveEffect.PlayTitleDissolve();
        yield return new WaitForSeconds(_titleDissolveEffect.TitleDissolveDuration);
        transform.GetChild(0).gameObject.SetActive(false);
    }

    private IEnumerator ExitAfterFadeOutCoroutine()
    {
        _titleFadeInOutEffect.FadeOut();
        yield return new WaitForSeconds(_titleFadeInOutEffect.FadeDuration);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
