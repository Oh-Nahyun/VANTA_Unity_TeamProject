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

    /// <summary>
    /// 타이틀 디졸브 효과용 클래스
    /// </summary>
    private TitleDissolveEffect _titleDissolveEffect;

    private void Awake()
    {
        _titleDissolveEffect = GetComponent<TitleDissolveEffect>();
    }

    private void Start()
    {
        _gameStartButton.onClick.AddListener(OnClickStartButton);
        _gameSettingsButton.onClick.AddListener(OnClickSettingsButton);
        _gameExitButton.onClick.AddListener(OnClickExitButton);
    }

    private void OnClickStartButton()
    {
        _titleDissolveEffect.PlayDissolve();
        StartCoroutine(DestroyTitleUICoroutine());
    }

    private IEnumerator DestroyTitleUICoroutine()
    {
        yield return new WaitForSeconds(_titleDissolveEffect.DissolveDuration);
        gameObject.SetActive(false);
    }

    private void OnClickSettingsButton()
    {
        _gameSettingPanel.active = true;
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
