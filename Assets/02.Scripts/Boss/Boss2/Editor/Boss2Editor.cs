using UnityEditor;
using UnityEngine;

/// <summary>
/// Boss2 인스펙터 아래에 일시정지 / 재개 버튼을 붙인다. 플레이 중에만 동작한다.
/// </summary>
[CustomEditor(typeof(Boss2))]
public class Boss2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("보스 제어", EditorStyles.boldLabel);

        if (Application.isPlaying == false)
        {
            EditorGUILayout.HelpBox("플레이 중에만 실행할 수 있습니다.", MessageType.Info);
            return;
        }

        Boss2 boss = (Boss2)target;

        // 일시정지 여부는 FSM 상태로 표시한다. (Boss2.PauseBoss 가 FSM 을 일시정지한다)
        Boss2Controller controller = boss.GetComponent<Boss2Controller>();
        bool isPaused = controller != null && controller.IsPaused;
        EditorGUILayout.LabelField("상태", isPaused ? "일시정지" : "진행 중");

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(isPaused))
            {
                if (GUILayout.Button("일시정지 (PauseBoss)", GUILayout.Height(28f)))
                {
                    boss.PauseBoss();
                }
            }

            using (new EditorGUI.DisabledScope(isPaused == false))
            {
                if (GUILayout.Button("재개 (ResumeBoss)", GUILayout.Height(28f)))
                {
                    boss.ResumeBoss();
                }
            }
        }
    }

    // 플레이 중에는 상태 표시가 계속 갱신되도록 다시 그린다.
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
