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
        string dataPath = "Assets/Resources/EnemyData/105_Wizard2.asset";

        // 0. 전용 데이터 에셋 생성 및 업데이트
        EnemyData wizardData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        if (wizardData == null)
        {
            wizardData = ScriptableObject.CreateInstance<EnemyData>();
            if (!Directory.Exists("Assets/Resources/EnemyData")) Directory.CreateDirectory("Assets/Resources/EnemyData");
            AssetDatabase.CreateAsset(wizardData, dataPath);
        }
        
        // 항상 최신 값으로 업데이트
        wizardData.ID = 105;
        wizardData.EnemyName = "Wizard2";
        wizardData.HP = 80f;
        wizardData.Damage = 15f;
        wizardData.Speed = 2.5f;
        wizardData.DetectionRange = 12f;
        wizardData.AttackInterval = 2.5f;
        EditorUtility.SetDirty(wizardData);

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

        var col = wizard.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 1.2f); // 대략적인 마법사 크기

        wizard.AddComponent<Health>();
        
        // 3. 애니메이터 컨트롤러 설정
        if (!File.Exists(controllerPath))
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Idle.anim");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Run.anim");
            var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Attack1.anim");
            var dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprite/Evil Wizard 2/Animations/Death.anim");

            var idleState = rootStateMachine.AddState("Idle"); idleState.motion = idleClip;
            var walkState = rootStateMachine.AddState("Walk"); walkState.motion = walkClip;
            var attackState = rootStateMachine.AddState("Attack"); attackState.motion = attackClip;
            var dieState = rootStateMachine.AddState("Die"); dieState.motion = dieClip;

            controller.AddParameter("Walk", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            idleState.AddTransition(walkState).AddCondition(AnimatorConditionMode.If, 0, "Walk");
            walkState.AddTransition(idleState).AddCondition(AnimatorConditionMode.IfNot, 0, "Walk");
            rootStateMachine.AddAnyStateTransition(attackState).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackState.AddTransition(idleState).hasExitTime = true;
            rootStateMachine.AddAnyStateTransition(dieState).AddCondition(AnimatorConditionMode.If, 0, "Die");

            anim.runtimeAnimatorController = controller;
        }
        else
        {
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        }

        // 4. RangedEnemy 스크립트 추가 및 데이터 연결
        var rangedEnemy = wizard.GetComponent<RangedEnemy>() ?? wizard.AddComponent<RangedEnemy>();
        rangedEnemy.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_red.prefab");
        rangedEnemy.data = wizardData;
        
        // 근접 공격 능력치 설정
        rangedEnemy.meleeDamage = 25f;
        rangedEnemy.meleeRange = 2.0f;
        rangedEnemy.meleeOffset = new Vector2(1.2f, 0.5f);

        // 5. 프리팹으로 저장
        PrefabUtility.SaveAsPrefabAsset(wizard, prefabPath);
        DestroyImmediate(wizard);

        AssetDatabase.Refresh();
        Debug.Log("Wizard2 프리팹 및 전용 데이터(HP 80, DMG 15) 설정 완료!");
        EditorUtility.DisplayDialog("Success", "Wizard2 설정 완료!\nHP: 80, Damage: 15", "OK");
    }
}
