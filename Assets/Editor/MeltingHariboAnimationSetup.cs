using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class MeltingHariboAnimationSetup : EditorWindow
{
    [MenuItem("Custom Tools/Setup MeltingHaribo Animator")]
    public static void SetupAnimator()
    {
        string path = "Assets/Sprite/enemy/melting_haribo/melting_haribo.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + path);
            return;
        }

        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(i);
        }
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        ChildAnimatorState[] existingStates = sm.states;
        foreach (var child in existingStates)
        {
            sm.RemoveState(child.state);
        }

        var stateIdle = sm.AddState("Idle");
        var stateMelting = sm.AddState("MeltingDown");
        var stateUnder = sm.AddState("Underground");
        var stateSolid = sm.AddState("Solidifying");
        var stateAttack = sm.AddState("Attack");
        var stateStun = sm.AddState("Stunned");

        sm.defaultState = stateIdle;

        AddTransition(stateIdle, stateMelting, 1);
        AddTransition(stateMelting, stateUnder, 2);
        AddTransition(stateUnder, stateSolid, 3);
        AddTransition(stateSolid, stateAttack, 4);
        AddTransition(stateAttack, stateIdle, 0);

        // AnyState 접근 방식 수정 (sm.anyState가 아닌 stateMachine.anyState로 접근하거나, Unity 버전에 따라 다를 수 있음)
        // Unity API에서 AnimatorStateMachine의 AnyState는 sm.anyState가 맞는데 오류가 난다면 
        // 스테이트 머신 자체를 통해 접근해야 합니다.
        var anyTrans = sm.AddAnyStateTransition(stateStun);
        anyTrans.AddCondition(AnimatorConditionMode.Equals, 6, "State");
        anyTrans.hasExitTime = false;
        anyTrans.duration = 0f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("MeltingHaribo 애니메이터 설정이 완료되었습니다!");
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, int stateVal)
    {
        var trans = from.AddTransition(to);
        trans.AddCondition(AnimatorConditionMode.Equals, stateVal, "State");
        trans.hasExitTime = false;
        trans.duration = 0f;
    }
}
