using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 페이드 인/아웃 테스트용. 인스펙터의 FadeIn / FadeOut 버튼(TesterEditor)으로 연결된 이벤트를 실행한다.
/// </summary>
public class Tester : MonoBehaviour
{
    public UnityEvent FadeIn;
    public UnityEvent FadeOut;

    [ContextMenu("Fade In")]
    public void PlayFadeIn()
    {
        FadeIn?.Invoke();
    }

    [ContextMenu("Fade Out")]
    public void PlayFadeOut()
    {
        FadeOut?.Invoke();
    }
}
