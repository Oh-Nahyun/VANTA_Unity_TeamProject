using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("게임 설정 패널 나가리 버튼")]
    [SerializeField] private Button _goBackButton;

    private void Start()
    {
        _goBackButton.onClick.AddListener(OnClickGoBackButton);
    }

    private void OnClickGoBackButton()
    {
        gameObject.SetActive(false);
    }
}
