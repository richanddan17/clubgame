using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShootingSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Setup Shooting & Background", false, -90)]
    public static void Setup()
    {
        Debug.Log("--- 슈팅 및 배경 설정 시작 ---");

        // 1. 색상별 발사체 프리팹 생성
        string[] colors = { "blue", "red", "yellow" };
        GameObject[] projectilePrefabs = new GameObject[3];

        for (int i = 0; i < colors.Length; i++)
        {
            string colorName = colors[i];
            string path = $"Assets/Prefabs/BubbleProjectile_{colorName}.prefab";
            
            // 기존 프리팹이 있으면 제거하고 새로 생성 (애니메이션 갱신을 위해)
            GameObject obj = new GameObject($"BubbleProjectile_{colorName}");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprite/gumball {colorName}.png");
            sr.sortingOrder = 10;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            obj.AddComponent<Projectile>();
            
            // 애니메이터 추가 및 컨트롤러 연결
            var animator = obj.AddComponent<Animator>();
            // 이미 프로젝트에 있는 애니메이터 컨트롤러를 찾아 연결
            string controllerPath = $"Assets/Sprite/gumball {colorName}_0.controller";
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            projectilePrefabs[i] = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        // 2. Player 프리팹 설정
        string playerPrefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject playerInstance = PrefabUtility.LoadPrefabContents(playerPrefabPath);
            PlayerController pc = playerInstance.GetComponent<PlayerController>();
            
            if (pc != null)
            {
                // FirePoint 확인 및 추가
                Transform fp = playerInstance.transform.Find("FirePoint");
                if (fp == null)
                {
                    GameObject fpObj = new GameObject("FirePoint");
                    fpObj.transform.SetParent(playerInstance.transform);
                    fpObj.transform.localPosition = new Vector3(0.5f, 0, 0);
                    fp = fpObj.transform;
                }

                SerializedObject so = new SerializedObject(pc);
                
                // firePoint 연결
                so.FindProperty("firePoint").objectReferenceValue = fp;
                
                // colorProjectilePrefabs 리스트 채우기
                SerializedProperty prefabsProp = so.FindProperty("colorProjectilePrefabs");
                prefabsProp.ClearArray();
                for (int i = 0; i < projectilePrefabs.Length; i++)
                {
                    prefabsProp.InsertArrayElementAtIndex(i);
                    prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = projectilePrefabs[i];
                }

                // 스킬 데이터 확인 (비어있으면 기본 스킬 추가)
                SerializedProperty skillsProp = so.FindProperty("equippedSkills");
                if (skillsProp.arraySize == 0)
                {
                    string[] skillGuids = AssetDatabase.FindAssets("t:SkillData");
                    if (skillGuids.Length > 0)
                    {
                        skillsProp.InsertArrayElementAtIndex(0);
                        skillsProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(skillGuids[0]));
                    }
                }

                so.ApplyModifiedProperties();
            }
            
            PrefabUtility.SaveAsPrefabAsset(playerInstance, playerPrefabPath);
            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        // 3. 배경 설정 (기존 로직 유지)
        SetupBackground();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup Complete", "슈팅 시스템 완성!\n1. 'R' 키로 색상 변경 (파랑->빨강->노랑)\n2. 마우스 왼쪽 클릭으로 발사\n3. 플레이어 프리팹 자동 갱신 완료", "확인");
    }

    private static void SetupBackground()
    {
        GameObject bgParent = GameObject.Find("Background_Parent");
        if (bgParent != null) Object.DestroyImmediate(bgParent);

        bgParent = new GameObject("Background_Parent");
        string[] bgNames = { "background.png", "woods - fourth.png", "woods - third.png", "woods - second.png", "woods - first.png" };
        float[] multipliers = { 0.98f, 0.85f, 0.65f, 0.45f, 0.25f };

        float cameraY = 0;
        if (Camera.main != null) cameraY = Camera.main.transform.position.y;

        for (int i = 0; i < bgNames.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprite/karsiori/tilemap/backgrounds/{bgNames[i]}");
            if (sprite == null) continue;

            GameObject bg = new GameObject($"{bgNames[i]}_Layer");
            bg.transform.SetParent(bgParent.transform);
            bg.transform.localScale = new Vector3(2.5f, 2.5f, 1);
            bg.transform.localPosition = new Vector3(0, cameraY, 0);

            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = -10 + i;
            
            var pb = bg.AddComponent<ParallaxBackground>();
            pb.parallaxEffect = multipliers[i];
        }
    }
}
