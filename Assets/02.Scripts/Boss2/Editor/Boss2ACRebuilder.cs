using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 브랜치 전환 중 유실된 Boss2AC 컨트롤러의 공격/피격/사망 부분을 다시 만든다.
/// 살아남은 Idle, Move, Sprint, Dodge 와 그 전이는 건드리지 않으며,
/// 이미 존재하는 파라미터·상태·전이는 건너뛰므로 여러 번 실행해도 안전하다.
/// </summary>
public static class Boss2ACRebuilder
{
    private const string ControllerPath = "Assets/05.Animations/Boss2/Boss2AC.controller";
    private const string ClipFolder = "Assets/05.Animations/Boss2/";

    // 전이 블렌드. 콤보 전이는 짧아야 한다.
    // 80% 지점에서 콤보가 발동한 뒤 블렌드가 끝나기 전에 원본 클립이 EndAttack 이벤트에
    // 도달하면 콤보 도중에 카운터가 리셋된다. Boss2Attack 기준 여유가 0.167초뿐이다.
    private const float ComboBlend = 0.1f;
    private const float ReturnBlend = 0.1f;

    [MenuItem("Tools/Boss2/Rebuild Animator Controller")]
    public static void Rebuild()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"컨트롤러를 찾지 못했습니다: {ControllerPath}");
            return;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "AttackCount", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Death", AnimatorControllerParameterType.Trigger);

        AnimatorState idle = FindState(stateMachine, "Boss2Idle");
        AnimatorState move = FindState(stateMachine, "Boss2Move");
        AnimatorState sprint = FindState(stateMachine, "Boss2Sprint");
        AnimatorState dodge = FindState(stateMachine, "Boss2Dodge");

        if (idle == null)
        {
            Debug.LogError("Boss2Idle 상태가 없습니다. 컨트롤러가 예상과 다릅니다.");
            return;
        }

        AnimatorState attack = EnsureState(stateMachine, "Boss2Attack", new Vector3(560f, -60f, 0f));
        AnimatorState doubleAttack = EnsureState(stateMachine, "Boss2DoubleAttack", new Vector3(820f, -60f, 0f));
        AnimatorState hit = EnsureState(stateMachine, "Boss2Hit", new Vector3(560f, 200f, 0f));
        AnimatorState death = EnsureState(stateMachine, "Boss2Death", new Vector3(820f, 200f, 0f));

        // ---- 진입 ----
        // 1타는 AttackCount 가 0 일 때만. 유실 전에는 0 이 더블슬래시로 가 있어 콤보 순서가 뒤집혀 있었다.
        AnimatorStateTransition idleToAttack = EnsureTransition(idle, attack);
        if (idleToAttack != null)
        {
            SetTiming(idleToAttack, false, 0f, ComboBlend);
            idleToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            idleToAttack.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackCount");
        }

        // ---- 콤보 ----
        // Has Exit Time 은 끄고, Attack 트리거를 세우는 시점을 80% 애니메이션 이벤트가 정한다.
        // 유실 전에는 조건이 Attack 하나뿐이라 0% 시점에 서로 캔슬되는 무한 핑퐁이었다.
        AnimatorStateTransition attackToDouble = EnsureTransition(attack, doubleAttack);
        if (attackToDouble != null)
        {
            SetTiming(attackToDouble, false, 0f, ComboBlend);
            attackToDouble.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            attackToDouble.AddCondition(AnimatorConditionMode.Equals, 1f, "AttackCount");
        }

        // ---- 복귀 ----
        // 클립을 끝까지 재생시켜야 93% 지점의 EndAttack 이벤트가 확실히 불린다.
        // 유실 전에는 78.6% / 83.3% 에 빠져나가 100% 에 있던 이벤트를 놓치고 있었다.
        AnimatorStateTransition attackToIdle = EnsureTransition(attack, idle);
        if (attackToIdle != null)
        {
            SetTiming(attackToIdle, true, 1f, ReturnBlend);
        }

        AnimatorStateTransition doubleToIdle = EnsureTransition(doubleAttack, idle);
        if (doubleToIdle != null)
        {
            SetTiming(doubleToIdle, true, 1f, ReturnBlend);
        }

        // ---- 피격 ----
        foreach (AnimatorState source in new[] { idle, move, sprint })
        {
            if (source == null)
            {
                continue;
            }

            AnimatorStateTransition toHit = EnsureTransition(source, hit);
            if (toHit != null)
            {
                SetTiming(toHit, false, 0f, 0f);
                toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            }
        }

        // 유실 전에는 Exit Time 이 0 이라 피격 모션이 한 프레임도 재생되지 않았다.
        AnimatorStateTransition hitToIdle = EnsureTransition(hit, idle);
        if (hitToIdle != null)
        {
            SetTiming(hitToIdle, true, 1f, ReturnBlend);
        }

        // ---- 사망 ----
        AnimatorStateTransition hitToDeath = EnsureTransition(hit, death);
        if (hitToDeath != null)
        {
            SetTiming(hitToDeath, false, 0f, ReturnBlend);
            hitToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        }

        // ---- 공격 중 구르기 ----
        // 이 전이가 없으면 위치는 구르는데 화면은 공격 모션으로 어긋난다.
        if (dodge != null)
        {
            foreach (AnimatorState source in new[] { attack, doubleAttack })
            {
                AnimatorStateTransition toDodge = EnsureTransition(source, dodge);
                if (toDodge != null)
                {
                    SetTiming(toDodge, false, 0f, 0f);
                    toDodge.AddCondition(AnimatorConditionMode.If, 0f, "IsDodge");
                }
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Boss2AC 재구성 완료. 그래프와 Parameters 탭을 확인하세요.");
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name)
            {
                Debug.Log($"파라미터 '{name}' 는 이미 있어 건너뜁니다.");
                return;
            }
        }

        controller.AddParameter(name, type);
        Debug.Log($"파라미터 '{name}' ({type}) 추가");
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == name)
            {
                return child.state;
            }
        }

        return null;
    }

    private static AnimatorState EnsureState(AnimatorStateMachine stateMachine, string name, Vector3 position)
    {
        AnimatorState existing = FindState(stateMachine, name);
        if (existing != null)
        {
            Debug.Log($"상태 '{name}' 는 이미 있어 건너뜁니다.");
            return existing;
        }

        AnimatorState state = stateMachine.AddState(name, position);

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipFolder}{name}.anim");
        if (clip == null)
        {
            Debug.LogWarning($"'{name}.anim' 클립을 찾지 못했습니다. 상태는 만들었지만 Motion 이 비어 있습니다.");
        }
        else
        {
            state.motion = clip;
        }

        Debug.Log($"상태 '{name}' 추가");
        return state;
    }

    /// <summary>
    /// 같은 대상으로 가는 전이가 이미 있으면 null 을 돌려주어 중복 생성을 막는다.
    /// </summary>
    private static AnimatorStateTransition EnsureTransition(AnimatorState source, AnimatorState destination)
    {
        if (source == null || destination == null)
        {
            return null;
        }

        foreach (AnimatorStateTransition transition in source.transitions)
        {
            if (transition.destinationState == destination)
            {
                Debug.Log($"전이 '{source.name}' -> '{destination.name}' 는 이미 있어 건너뜁니다.");
                return null;
            }
        }

        Debug.Log($"전이 '{source.name}' -> '{destination.name}' 추가");
        return source.AddTransition(destination);
    }

    private static void SetTiming(AnimatorStateTransition transition, bool hasExitTime, float exitTime, float duration)
    {
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.offset = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.canTransitionToSelf = false;
    }
}
