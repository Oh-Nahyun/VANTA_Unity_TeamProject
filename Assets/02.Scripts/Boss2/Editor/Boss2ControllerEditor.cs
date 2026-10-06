using UnityEditor;
using UnityEngine;

/// <summary>
/// Boss2Controller 인스펙터 아래에 액션·스킬 테스트 버튼을 붙인다.
/// 버튼은 실제 FSM 경로(이동 → 공격 → Recover, 무적 포함)로 한 번 실행하고 끝나면 Idle 에서 멈춘다.
/// 연속 실행 버튼은 모든 액션을 순서대로 Recover 없이 바로바로 이어서 실행한다.
/// </summary>
[CustomEditor(typeof(Boss2Controller))]
public class Boss2ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("액션 테스트", EditorStyles.boldLabel);

        if (Application.isPlaying == false)
        {
            EditorGUILayout.HelpBox("플레이 중에만 실행할 수 있습니다. 조건(거리·체력·쿨다운)은 무시하고 바로 실행합니다.", MessageType.Info);
            return;
        }

        Boss2Controller controller = (Boss2Controller)target;
        string state = controller.CurrentStateName + (controller.IsTestRunning ? "  (테스트 중)" : "");
        if (controller.TestQueueRemaining > 0)
        {
            state += $"  남은 연속 테스트 {controller.TestQueueRemaining}";
        }
        EditorGUILayout.LabelField("현재 상태", state);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("모든 액션 연속 실행", GUILayout.Height(28f)))
            {
                controller.TestAllActions(false);
            }

            if (GUILayout.Button("모든 액션 + 스킬 연속 실행", GUILayout.Height(28f)))
            {
                controller.TestAllActions(true);
            }
        }

        EditorGUILayout.Space(4f);
        for (int i = 0; i < controller.Actions.Count; i++)
        {
            Boss2Action action = controller.Actions[i];
            string invincible = action.IsInvincible ? "  [무적]" : "";
            if (GUILayout.Button($"{action.Name}   ({action.Move} → {action.Attack}){invincible}"))
            {
                controller.TestAction(i);
            }
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("즉사기"))
            {
                controller.TestSkill(Boss2SkillPatternType.InstantKill);
            }

            if (GUILayout.Button("광역기"))
            {
                controller.TestSkill(Boss2SkillPatternType.AreaOfEffect);
            }
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("FSM 시작"))
            {
                controller.StartFsm();
            }

            if (GUILayout.Button("FSM 정지"))
            {
                controller.StopFsm();
            }
        }
    }

    // 플레이 중에는 현재 상태 표시가 계속 갱신되도록 다시 그린다.
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
