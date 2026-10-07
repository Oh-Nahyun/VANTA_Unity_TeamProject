using UnityEditor;
using UnityEngine;

/// <summary>
/// M_Boss2BladeGlow 에 추가한 연출 속성(히트 플래시·오라 외곽선·낫 에너지 흐름·디졸브 강화)을
/// 보스2 애니메이션 클립에 키로 넣는다. 메뉴: Tools / Boss2 VFX / Apply FX Animation Keys
/// 기존 _GlowAmount 처럼 SpriteRenderer 의 material._X 를 움직인다. 여러 번 실행해도 같은 결과(덮어씀).
/// 상태들이 Write Defaults 를 쓰므로, 키가 없는 다른 동작에서는 머티리얼 기본값(꺼짐)으로 돌아간다.
/// </summary>
public static class Boss2FxAnimKeys
{
    private const string ClipFolder = "Assets/05.Animations/Boss2/";

    [MenuItem("Tools/Boss2 VFX/Apply FX Animation Keys")]
    public static void Apply()
    {
        // 피격: 맞는 순간 번쩍였다가 빠르게 사라진다
        Set("Boss2Hit", "_FlashAmount", K(0f, 1f), K(0.12f, 0f));

        // 즉사기 시작: 외곽선이 켜지고, 위에서 아래로 빛나는 테두리와 함께 사라진다
        Set("StartInstantKillSkill", "_OutlineAmount", K(0f, 0f), K(0.3f, 1f), K(End, 1f));
        Set("StartInstantKillSkill", "_DissolveDirection", K(0f, 1f), K(End, 1f));
        Set("StartInstantKillSkill", "_DissolveEdgeIntensity", K(0f, 3f), K(End, 3f));

        // 즉사기 공격: 외곽선을 유지하다 끝에서 끈다
        Set("EndInstantKillSkill", "_OutlineAmount", K(0f, 1f), K(End - 0.25f, 1f), K(End, 0f));

        // 광역기 · 기습 회전베기: 시전 동안 외곽선
        Set("Boss2AOESkill", "_OutlineAmount", K(0f, 0f), K(0.25f, 1f), K(End - 0.25f, 1f), K(End, 0f));
        Set("Boss2SpecialAttack", "_OutlineAmount", K(0f, 0f), K(0.2f, 1f), K(End - 0.2f, 1f), K(End, 0f));

        // 강타 · 회전베기: 휘두를 때 낫 에너지 흐름이 강해진다
        Set("Boss2SmashAttack", "_BladeFlowAmount", K(0f, 0.35f), K(0.4f, 1f), K(0.75f, 1f), K(End, 0.35f));
        Set("Boss2SpecialAttack", "_BladeFlowAmount", K(0f, 0.35f), K(0.4f, 1f), K(0.75f, 1f), K(End, 0.35f));

        // 사망: 노이즈 디졸브 그대로, 테두리만 발광
        Set("Boss2Death", "_DissolveEdgeIntensity", K(0f, 3f), K(End, 3f));
        Set("Boss2Death", "_DissolveDirection", K(0f, 0f), K(End, 0f));

        AssetDatabase.SaveAssets();
        Debug.Log("[Boss2FxAnimKeys] 연출 키 적용 완료");
    }

    // 클립 길이 비율 대신 "끝" 을 표시하는 값. Set 에서 실제 길이로 바꾼다.
    private const float End = -1000f;

    private struct Key { public float Time; public float Value; }

    private static Key K(float time, float value) { return new Key { Time = time, Value = value }; }

    private static void Set(string clipName, string property, params Key[] keys)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + clipName + ".anim");
        if (clip == null)
        {
            Debug.LogError($"[Boss2FxAnimKeys] 클립을 찾지 못했습니다: {clipName}");
            return;
        }

        float length = clip.length;
        AnimationCurve curve = new AnimationCurve();
        foreach (Key k in keys)
        {
            // End 기준 값은 클립 끝에서 얼마나 앞인지로 계산하고, 클립 범위 안으로 자른다.
            float time = k.Time <= End / 2f ? length + (k.Time - End) : k.Time;
            curve.AddKey(Mathf.Clamp(time, 0f, length), k.Value);
        }
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }

        EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "material." + property);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
        EditorUtility.SetDirty(clip);
    }
}
