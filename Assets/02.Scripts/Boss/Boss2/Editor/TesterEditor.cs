using UnityEditor;
using UnityEngine;

/// <summary>
/// Tester 인스펙터 아래에 FadeIn / FadeOut 버튼을 붙인다. 누르면 해당 UnityEvent 를 바로 호출한다.
/// UnityEvent 리스너가 기본값(Runtime Only)이라 플레이 중에만 동작한다.
/// </summary>
[CustomEditor(typeof(Tester))]
public class TesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("페이드 테스트", EditorStyles.boldLabel);

        if (Application.isPlaying == false)
        {
            EditorGUILayout.HelpBox("플레이 중에만 실행할 수 있습니다.", MessageType.Info);
            return;
        }

        Tester tester = (Tester)target;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("FadeIn", GUILayout.Height(28f)))
            {
                tester.PlayFadeIn();
            }

            if (GUILayout.Button("FadeOut", GUILayout.Height(28f)))
            {
                tester.PlayFadeOut();
            }
        }
    }
}
