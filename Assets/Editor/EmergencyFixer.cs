using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class EmergencyFixer : EditorWindow
{
    [MenuItem("Custom Tools/tiger/EMERGENCY FIX ALL", false, -100)]
    public static void FixEverything()
    {
        Debug.Log("--- [1] 긴급 복구 시작 ---");

        // 1. 태그 및 레이어 생성
        CreateTag("Enemy");
        CreateTag("Player");
        CreateLayer("Ground");
        AssetDatabase.SaveAssets();

        // 2. 플레이어 태그 강제 할당
        GameObject playerObj = GameObject.Find("Player") ?? GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.tag = "Player";
            Debug.Log("플레이어 태그를 'Player'로 설정했습니다.");
        }

        // 3. 플레이어 설정 복구
        FixPlayerSettings();

        // 4. 슬라임 프리팹 및 스포너 복구
        FixEnemySettings();

        // 5. UI 레이아웃 복구
        FixUILayout();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("FIX COMPLETE", "모든 설정이 복구되었습니다!\n1. 점프/발사/태그 복구\n2. HP바 빨간색 및 위치 고정", "확인");
        Debug.Log("--- [End] 긴급 복구 완료 ---");
    }

    private static void FixUILayout()
    {
        GameObject hpBar = GameObject.Find("Player_HP_Bar");
        if (hpBar == null) return;

        RectTransform rect = hpBar.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(250, 35);
        }

        Image[] images = hpBar.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name == "Fill")
            {
                img.color = Color.red;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
            }
            else if (img.gameObject.name == "Background")
            {
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }

        RectTransform fillArea = hpBar.transform.Find("Fill Area")?.GetComponent<RectTransform>();
        if (fillArea != null)
        {
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;
        }
    }

    private static void FixPlayerSettings()
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        if (player.GetComponent<Health>() == null)
        {
            player.gameObject.AddComponent<Health>();
        }

        SerializedObject so = new SerializedObject(player);
        int groundLayerIndex = LayerMask.NameToLayer("Ground");

        SerializedProperty moveSettings = so.FindProperty("moveSettings");
        if (moveSettings != null)
        {
            moveSettings.FindPropertyRelative("GroundLayer").intValue = 1 << groundLayerIndex;
            moveSettings.FindPropertyRelative("WalkSpeed").floatValue = 6f;
            moveSettings.FindPropertyRelative("RunSpeed").floatValue = 10f;
            moveSettings.FindPropertyRelative("JumpForce").floatValue = 14f;
            moveSettings.FindPropertyRelative("CrouchScaleMultiplier").floatValue = 0.5f;
        }

        SerializedProperty combatSettings = so.FindProperty("combatSettings");
        if (combatSettings != null)
        {
            if (combatSettings.FindPropertyRelative("FirePoint").objectReferenceValue == null)
            {
                Transform fp = player.transform.Find("FirePoint") ?? player.transform.Find("firePoint");
                combatSettings.FindPropertyRelative("FirePoint").objectReferenceValue = fp;
            }

            SerializedProperty projectileArray = combatSettings.FindPropertyRelative("ColorProjectilePrefabs");
            projectileArray.arraySize = 3;
            projectileArray.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_blue.prefab");
            projectileArray.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_red.prefab");
            projectileArray.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_yellow.prefab");

            SerializedProperty skills = combatSettings.FindPropertyRelative("EquippedSkills");
            if (skills.arraySize == 0)
            {
                SkillData[] allSkills = Resources.LoadAll<SkillData>("SkillData");
                if (allSkills.Length > 0)
                {
                    skills.arraySize = 1;
                    skills.GetArrayElementAtIndex(0).objectReferenceValue = allSkills[0];
                }
            }
        }
        so.ApplyModifiedProperties();
    }

    private static void FixEnemySettings()
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Slime.prefab");
        if (spawner != null && slimePrefab != null)
        {
            spawner.enemyPrefab = slimePrefab;
        }

        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.GetComponent<Health>() == null) enemy.gameObject.AddComponent<Health>();
        }
    }

    private static void CreateTag(string tagName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool exists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) { exists = true; break; }
        }
        if (!exists)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
        }
    }

    private static void CreateLayer(string layerName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName) return;
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }
}
