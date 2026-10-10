using System;
using UnityEngine;

public class InGameSceneManager : MonoBehaviour
{
    [Header("타이틀 페이드 인 효과용 클래스")]
    [SerializeField] TitleFadeInOutEffect _titleFadeInOutEffect;

    /// <summary>
    /// 게임 재도전 상태 확인용 변수
    /// </summary>
    public static bool IsRetry;

    private void Start()
    {
        _titleFadeInOutEffect.FadeIn();
    }
}
