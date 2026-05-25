using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class WizardSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/tiger/Setup Wizard2 Prefab", false, 20)]
    public static void SetupWizard2()
    {
        string prefabPath = "Assets/Prefabs/Wizard2.prefab";
        string spritePath = "Assets/Sprite/Evil Wizard 2/Sprites/Idle.png";
        string controllerPath = "Assets/Sprite/Evil Wizard 2/Animations/Wizard2Controller.controller";

        // 1. 기본 오브젝트 생성
        GameObject wizard = new GameObject("Wizard2");
        wizard.tag = "Enemy";

        // 2. 컴포넌트 추가
        var sr = wizard.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        
        var anim = wizard.AddComponent<Animator>();
        var rb = wizard.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        wizard.AddComponent<CapsuleCollider2D>();
        wizard.AddComponent<Health>();
        
        // 3. 애니메이터 컨트롤러 생성 및 설정
        if (!File.Exists(controllerPath))
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // 스테이트 추가
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Idle.anim");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Run.anim");
            var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Attack1.anim");
            var dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Death.anim");

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = dieClip;

            // 파라미터 추가
            controller.AddParameter("Walk", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            // 트랜지션 설정
            idleState.AddTransition(walkState).AddCondition(AnimatorConditionMode.If, 0, "Walk");
            walkState.AddTransition(idleState).AddCondition(AnimatorConditionMode.IfNot, 0, "Walk");
            
            // AnyState 트랜지션
            rootStateMachine.AddAnyStateTransition(attackState).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackState.AddTransition(idleState).hasExitTime = true;
            rootStateMachine.AddAnyStateTransition(dieState).AddCondition(AnimatorConditionMode.If, 0, "Die");

            anim.runtimeAnimatorController = controller;
        }
        else
        {
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        }

        // 4. RangedEnemy 스크립트 추가 및 설정
        var rangedEnemy = wizard.GetComponent<RangedEnemy>() ?? wizard.AddComponent<RangedEnemy>();
        rangedEnemy.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_red.prefab");
        
        // 데이터 에셋 로드 (Orc나 다른 원거리 데이터를 임시로 사용하거나 새로 생성 필요)
        rangedEnemy.data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Resources/EnemyData/104_Orc.asset"); 

        // 5. 프리팹으로 저장
        PrefabUtility.SaveAsPrefabAsset(wizard, prefabPath);
        DestroyImmediate(wizard);

        AssetDatabase.Refresh();
        Debug.Log("Wizard2 프리팹 생성 및 기본 설정 완료!");
        EditorUtility.DisplayDialog("Success", "Wizard2 프리팹이 Assets/Prefabs/Wizard2.prefab에 생성되었습니다.", "OK");
    }
}
