using UnityEditor;
using UnityEngine;

/// <summary>
/// 회피 서포터(PlayAssist) 리모컨. Tools/Cheat/Play Assist 리모컨 으로 연다.
/// 서포터는 플레이를 시작하면 스스로 붙는다(씬에 저장되지 않음). 이 창에서 기능을 켜고 끄며, 설정은 EditorPrefs 에 기억한다.
/// </summary>
public class PlayAssistRemote : EditorWindow
{
    [MenuItem("Tools/Cheat/Play Assist 리모컨")]
    private static void Open()
    {
        PlayAssistRemote window = GetWindow<PlayAssistRemote>("Play Assist 리모컨");
        window.minSize = new Vector2(260f, 300f);
    }

    private void OnEnable()
    {
        if (Application.isPlaying == false)
        {
            PlayAssistSettings.Load();
        }
        PlayAssistSettings.Changed += Repaint;
    }

    private void OnDisable()
    {
        PlayAssistSettings.Changed -= Repaint;
    }

    private void OnInspectorUpdate()
    {
        // 플레이 중 단축키로 바뀐 상태도 창에 보이게 한다.
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        Color old = GUI.backgroundColor;
        GUI.backgroundColor = PlayAssistSettings.Enabled ? new Color(0.45f, 0.9f, 0.45f) : new Color(0.9f, 0.45f, 0.45f);
        if (GUILayout.Button(PlayAssistSettings.Enabled ? "서포터 ON  (누르면 끄기)" : "서포터 OFF  (누르면 켜기)", GUILayout.Height(44)))
        {
            PlayAssistSettings.Enabled = !PlayAssistSettings.Enabled;
            PlayAssistSettings.Save();
        }
        GUI.backgroundColor = old;

        EditorGUILayout.Space(6);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(PlayAssistSettings.Enabled == false))
        {
            PlayAssistSettings.AutoDodge = EditorGUILayout.ToggleLeft("자동 회피 (공격 신호에 대시)", PlayAssistSettings.AutoDodge);
            PlayAssistSettings.AutoQte = EditorGUILayout.ToggleLeft("회피 QTE 자동 성공", PlayAssistSettings.AutoQte);
            PlayAssistSettings.Invincible = EditorGUILayout.ToggleLeft("안전망 무적 (놓친 공격 대비)", PlayAssistSettings.Invincible);
            PlayAssistSettings.AutoBattle = EditorGUILayout.ToggleLeft("QA 자동 전투 (접근·공격·피니셔 연타)", PlayAssistSettings.AutoBattle);

            EditorGUILayout.Space(4);
            PlayAssistSettings.DodgeDistance = EditorGUILayout.Slider("회피 거리", PlayAssistSettings.DodgeDistance, 1f, 5f);
            PlayAssistSettings.ProjectileLead = EditorGUILayout.Slider("탄 반응 시간(초)", PlayAssistSettings.ProjectileLead, 0.05f, 0.5f);
            PlayAssistSettings.QteTiming = EditorGUILayout.Slider(new GUIContent("QTE 성공 시점", "입력 제한 시간의 몇 % 가 지났을 때 성공시킬지. 0.8 = 2초 중 1.6초"), PlayAssistSettings.QteTiming, 0.3f, 0.95f);
        }
        PlayAssistSettings.ShowOverlay = EditorGUILayout.ToggleLeft("게임 화면에 상태 표시", PlayAssistSettings.ShowOverlay);
        if (EditorGUI.EndChangeCheck())
        {
            PlayAssistSettings.Save();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("플레이 중", Application.isPlaying ? "예" : "아니오");
        EditorGUILayout.LabelField("반응 횟수", PlayAssistSettings.DodgeCount.ToString());
        EditorGUILayout.LabelField("최근 반응", PlayAssistSettings.LastReport ?? "-");
        if (GUILayout.Button("횟수 초기화"))
        {
            PlayAssistSettings.ResetCount();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox($"게임 중 단축키\n{PlayAssistSettings.ToggleKey}: 서포터 켜기/끄기\n{PlayAssistSettings.OverlayKey}: 화면 상태 표시 숨기기 (촬영용)\n\n게임 코드는 바꾸지 않으며 정식 빌드에는 포함되지 않습니다.", MessageType.Info);
    }
}
