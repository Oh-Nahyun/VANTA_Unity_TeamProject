using UnityEngine;

public class Boss1TestStarter : MonoBehaviour
{
    [SerializeField] private Boss1IntroController _boss1IntroController;

    [Header("테스트 자동 실행")]
    [SerializeField] private bool _autoStartIntro = true;
    [SerializeField] private bool _autoStartBattle = false;

    private void Start()
    {
        if (_autoStartIntro)
        {
            StartIntro();
        }

        if (_autoStartBattle)
        {
            StartBattle();
        }
    }

    [ContextMenu("Start Intro")]
    public void StartIntro()
    {
        _boss1IntroController.StartIntro();
    }

    [ContextMenu("Start Battle")]
    public void StartBattle()
    {
        _boss1IntroController.Boss1StartBattle();
    }
}
