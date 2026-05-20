using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class WizardSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Create Wizard2 Prefab")]
    public static void CreateWizard2()
    {
        string prefabPath = "Assets/Prefabs/Wizard2.prefab";
        string spriteFolder = "Assets/Sprite/Evil Wizard 2/Sprites";
        string animFolder = "Assets/Sprite/Evil Wizard 2/Animations";
        string idleSpritePath = spriteFolder + "/Idle.png";
        string controllerPath = "Assets/Sprite/Evil Wizard 2/Wizard2_Controller.controller";

        // 1. 기본 게임오브젝트 생성
        GameObject wizardObj = new GameObject("Wizard2");
        wizardObj.tag = "Enemy";
        wizardObj.transform.localScale = new Vector3(3f, 3f, 3f); // 크기 3,3으로 설정

        // 2. SpriteRenderer 설정
        SpriteRenderer sr = wizardObj.AddComponent<SpriteRenderer>();
        Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(idleSpritePath);
        if (idleSprite != null) sr.sprite = idleSprite;
        sr.sortingOrder = 1;

        // 3. 애니메이터 설정 (추가됨)
        Animator animator = wizardObj.AddComponent<Animator>();
        AnimatorController controller = CreateOrGetController(controllerPath, animFolder);
        animator.runtimeAnimatorController = controller;

        // 4. 물리 및 필수 컴포넌트 추가
        Rigidbody2D rb = wizardObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = wizardObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 2.5f); // 위자드 체형에 맞게 약간 조정

        // 5. 시스템 컴포넌트 추가
        wizardObj.AddComponent<Health>();
        EnemyController enemyCtrl = wizardObj.AddComponent<EnemyController>();

        // 6. EnemyData 연결
        string dataPath = "Assets/Resources/EnemyData/105_Wizard2.asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.EnemyName = "Wizard2";
            data.HP = 150;
            data.Speed = 2f;
            data.DetectionRange = 12f;
            if (!Directory.Exists("Assets/Resources/EnemyData")) Directory.CreateDirectory("Assets/Resources/EnemyData");
            AssetDatabase.CreateAsset(data, dataPath);
        }

        SerializedObject so = new SerializedObject(enemyCtrl);
        so.FindProperty("data").objectReferenceValue = data;
        so.ApplyModifiedProperties();

        // 7. 프리팹으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wizardObj, prefabPath);
        DestroyImmediate(wizardObj);

        if (prefab != null)
        {
            Debug.Log("Wizard2 프리팹 생성 완료 (애니메이션 포함)");
            EditorUtility.DisplayDialog("Success", "애니메이션이 포함된 Wizard2 프리팹이 생성되었습니다!", "확인");
        }
        AssetDatabase.Refresh();
    }

    private static AnimatorController CreateOrGetController(string path, string animFolder)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        // 파라미터 추가 (EnemyController.cs 대응)
        if (!HasParameter(controller, "Walk")) controller.AddParameter("Walk", AnimatorControllerParameterType.Bool);
        if (!HasParameter(controller, "Attack")) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        if (!HasParameter(controller, "Die")) controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // 클립 로드
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFolder + "/Idle.anim");
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFolder + "/Run.anim");
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFolder + "/Attack1.anim");
        AnimationClip dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFolder + "/Death.anim");

        var rootStateMachine = controller.layers[0].stateMachine;

        // 상태 생성 및 연결
        var idleState = AddStateIfMissing(rootStateMachine, "Idle", idleClip);
        var walkState = AddStateIfMissing(rootStateMachine, "Walk", runClip);
        var attackState = AddStateIfMissing(rootStateMachine, "Attack", attackClip);
        var dieState = AddStateIfMissing(rootStateMachine, "Die", dieClip);

        // 트랜지션 설정
        AddTransitionIfMissing(idleState, walkState, "Walk", true);
        AddTransitionIfMissing(walkState, idleState, "Walk", false);
        
        // Any State에서 Die로 가는 트랜지션
        if (dieState != null)
        {
            bool hasDieTransition = false;
            foreach (var t in rootStateMachine.anyStateTransitions)
            {
                if (t.destinationState == dieState) { hasDieTransition = true; break; }
            }
            if (!hasDieTransition)
            {
                var t = rootStateMachine.AddAnyStateTransition(dieState);
                t.AddCondition(AnimatorConditionMode.If, 0, "Die");
            }
        }

        return controller;
    }

    private static bool HasParameter(AnimatorController controller, string name)
    {
        foreach (var p in controller.parameters) if (p.name == name) return true;
        return false;
    }

    private static AnimatorState AddStateIfMissing(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        foreach (var s in sm.states) if (s.state.name == name) return s.state;
        var newState = sm.AddState(name);
        newState.motion = clip;
        return newState;
    }

    private static void AddTransitionIfMissing(AnimatorState from, AnimatorState to, string param, bool value)
    {
        foreach (var t in from.transitions) if (t.destinationState == to) return;
        var transition = from.AddTransition(to);
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        transition.duration = 0.1f;
    }
}
