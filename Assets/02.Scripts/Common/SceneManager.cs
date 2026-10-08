using System;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [Header("타이틀 페이드 인 효과용 클래스")]
    [SerializeField] TitleFadeInOutEffect _titleFadeInOutEffect;

    private void Start()
    {
        _titleFadeInOutEffect.FadeIn();
    }
}
