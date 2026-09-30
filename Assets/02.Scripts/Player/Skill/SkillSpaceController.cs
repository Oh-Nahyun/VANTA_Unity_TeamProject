using System;
using DG.Tweening;
using UnityEngine;

public class 
    SkillSpaceController : MonoBehaviour
{
    [Header("스킬 공간 디졸브")]
    [Tooltip("X = 스킬 진행도 / Y = 디졸브 값")]
    [SerializeField]
    private AnimationCurve _dissolveCurve =
        new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.75f),
            new Keyframe(0.75f, 0.5f),
            new Keyframe(0.9f, 0f),
            new Keyframe(1f, 0f)
        );

    private static readonly int SkillSpaceDissolveAmountID =
        Shader.PropertyToID("_SkillSpaceDissolveAmount");

    private float _skillDissolveProgress;

    private Tween _skillDissolveTween;

    private void Awake()
    {
        // 처음에는 완전히 표시
        Shader.SetGlobalFloat(
            SkillSpaceDissolveAmountID,
            1f
        );
    }
    
    public void StartSkillSpaceDissolve(float skillTime)
    {
        _skillDissolveTween?.Kill();

        _skillDissolveProgress = 0f;

        // 스킬 시작 시 완전히 표시
        Shader.SetGlobalFloat(
            SkillSpaceDissolveAmountID,
            _dissolveCurve.Evaluate(0f)
        );


        // Progress 자체는 항상
        //
        // 0 -----------------> 1
        //
        // 로 Linear하게 진행
        //
        // 실제 Shader 값은
        // AnimationCurve가 결정
        _skillDissolveTween =
            DOTween.To(
                    () => _skillDissolveProgress,

                    value =>
                    {
                        _skillDissolveProgress = value;

                        float dissolveAmount =
                            _dissolveCurve.Evaluate(value);

                        Shader.SetGlobalFloat(
                            SkillSpaceDissolveAmountID,
                            dissolveAmount
                        );
                    },

                    1f,
                    skillTime
                )
                .SetEase(Ease.Linear)
                .SetUpdate(true);
    }


    public void EndSkillSpaceDissolve()
    {
        _skillDissolveTween?.Kill();
        _skillDissolveTween = null;
        
        _skillDissolveProgress = 0f;

        Shader.SetGlobalFloat(
            SkillSpaceDissolveAmountID,
            1f
        );
        
    }

    private void OnDestroy()
    {
        _skillDissolveTween?.Kill();

        Shader.SetGlobalFloat(
            SkillSpaceDissolveAmountID,
            1f
        );    }
}
