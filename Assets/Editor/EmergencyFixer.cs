using UnityEngine;
using UnityEditor;
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
        CreateLayer("Ground");
        AssetDatabase.SaveAssets();

        // 2. 슬라임 프리팹 생성
        CreateSlimePrefab();

        // 3. 씬 오브젝트 정리
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player != null)
        {
            var legacy = player.GetComponent("PlayerMoving");
            if (legacy != null) { DestroyImmediate(legacy); Debug.Log("낡은 스크립트 제거 완료."); }
        }

        // 4. 스포너 연결
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Slime.prefab");
        if (spawner != null && slimePrefab != null)
        {
            spawner.enemyPrefab = slimePrefab;
            Debug.Log("스포너에 슬라임 연결 완료.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("FIX COMPLETE", "슬라임 프리팹 생성 및 설정을 완료했습니다! \nAssets/Prefabs 폴더를 확인해 보세요.", "확인");
        Debug.Log("--- [End] 긴급 복구 완료 ---");
    }

    private static void CreateSlimePrefab()
    {
        string path = "Assets/Prefabs/Slime.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            if (!AssetDatabase.IsValidFolder("Assets")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            else AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject tempSlime = new GameObject("Slime_Temp");
        try
        {
            var sr = tempSlime.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/FreePixelMob/SlimeA.png");
            
            var anim = tempSlime.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprite/FreePixelMob/Slime.controller");

            var rb = tempSlime.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            tempSlime.AddComponent<CapsuleCollider2D>();
            tempSlime.AddComponent<CanvasGroup>();
            
            // Mobs 스크립트 추가
            // 파일 시스템에서 Mobs 스크립트가 있는지 확인 후 추가
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Sprite/FreePixelMob/Mobs.cs") != null)
            {
                // Mobs는 클래스 이름이 Mobs이므로 문자열로 컴포넌트 추가 시도
                tempSlime.AddComponent<Mobs>();
            }
            else
            {
                Debug.LogWarning("Mobs.cs 스크립트 파일을 Assets/Sprite/FreePixelMob/ 에서 찾을 수 없습니다.");
            }

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(tempSlime, path);
            Debug.Log($"슬라임 프리팹 생성 성공: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"프리팹 생성 중 오류: {e.Message}");
        }
        finally
        {
            DestroyImmediate(tempSlime);
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
            Debug.Log($"태그 생성 완료: {tagName}");
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
                Debug.Log($"레이어 생성 완료: {layerName}");
                return;
            }
        }
    }
}
