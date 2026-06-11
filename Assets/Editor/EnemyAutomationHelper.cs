using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class EnemyAutomationHelper
{
    [MenuItem("Custom Tools/1. Rename Selected Sprites")]
    public static void RenameSprites()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0) { EditorUtility.DisplayDialog("Error", "PNG 파일을 먼저 선택해주세요.", "OK"); return; }
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.ToLower().EndsWith(".png")) continue;
            string fileName = Path.GetFileNameWithoutExtension(path);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int spriteCount = 0;
            foreach (Object asset in assets) { if (asset is Sprite sprite) { sprite.name = $"{fileName}_{spriteCount}"; spriteCount++; } }
            EditorUtility.SetDirty(obj); AssetDatabase.ImportAsset(path);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "스프라이트 이름 변경 완료!", "OK");
    }

    [MenuItem("Custom Tools/3. Automate Bullet Creation")]
    public static void AutomateBulletCreation()
    {
        string[] allPngs = Directory.GetFiles("Assets/Sprite", "*.png", SearchOption.AllDirectories);
        string prefabFolder = "Assets/Prefabs/Projectiles";
        if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);
        int count = 0;
        foreach (string path in allPngs) {
            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
            if (fileName.Contains("bullet") || fileName.Contains("shot")) { CreateBulletPrefab(path, prefabFolder); count++; }
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"{count}개의 투사체 프리팹 생성 완료!", "OK");
    }

    private static void CreateBulletPrefab(string spritePath, string prefabFolder)
    {
        string fileName = Path.GetFileNameWithoutExtension(spritePath);
        string prefabPath = $"{prefabFolder}/{fileName}.prefab";
        GameObject go = new GameObject(fileName);
        var sr = go.AddComponent<SpriteRenderer>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (var asset in assets) { if (asset is Sprite s) { sr.sprite = s; break; } }
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true;
        var projType = GetTypeByName("Projectile"); if (projType != null) go.AddComponent(projType);
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath); GameObject.DestroyImmediate(go);
    }

    [MenuItem("Custom Tools/2. Automate Enemy Creation")]
    public static void AutomateEnemyCreation()
    {
        // [필독] 만약 계속 에러가 난다면 유니티의 Animator 창을 닫고 실행해 주세요.
        string rootPath = "Assets/Sprite/enemy";
        if (!Directory.Exists(rootPath)) return;
        string[] enemyFolders = Directory.GetDirectories(rootPath);
        foreach (string folderPath in enemyFolders) {
            string enemyName = Path.GetFileName(folderPath);
            CreateEnemyAssets(folderPath, enemyName);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "적 생성 완료! (에러 방지를 위해 기존 애니메이터 설정은 유지되었습니다.)", "OK");
    }

    private static System.Type GetTypeByName(string name)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
            var type = assembly.GetType(name); if (type != null) return type;
        }
        return null;
    }

    private static void CreateEnemyAssets(string folderPath, string enemyName)
    {
        string animFolder = $"Assets/Animation/enemy/{enemyName}";
        string prefabPath = $"Assets/Prefabs/{enemyName}.prefab";
        string controllerPath = $"{animFolder}/{enemyName}_Controller.controller";

        if (!Directory.Exists(animFolder)) Directory.CreateDirectory(animFolder);

        // --- 에러 방지 핵심: 파일 존재 여부만 체크하고 직접적인 조작 최소화 ---
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        
        if (controller == null)
        {
            // 파일이 없을 때만 딱 한 번 생성
            var newController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = newController.layers[0].stateMachine;
            string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
            List<string> filteredPngs = pngFiles.Where(f => !f.ToLower().Contains("bullet")).OrderBy(f => f).ToList();
            
            foreach (string pngPath in filteredPngs) {
                AnimationClip clip = CreateClipFromSpriteSheet(pngPath, animFolder, Path.GetFileNameWithoutExtension(pngPath));
                if (clip != null) {
                    var state = rootStateMachine.AddState(clip.name); state.motion = clip;
                    if (clip.name.ToLower().Contains("idle") || rootStateMachine.defaultState == null) rootStateMachine.defaultState = state;
                }
            }
            controller = newController;
        }

        // --- 프리팹 처리 ---
        GameObject go;
        bool isNew = !File.Exists(prefabPath);
        if (!isNew) go = PrefabUtility.LoadPrefabContents(prefabPath);
        else { go = new GameObject(enemyName); go.tag = "Enemy"; }
        
        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        
        var rb = go.GetComponent<Rigidbody2D>() ?? go.AddComponent<Rigidbody2D>();
        if (isNew) { rb.gravityScale = 3.0f; rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        
        if (go.GetComponent<Collider2D>() == null) {
            var col = go.AddComponent<CapsuleCollider2D>(); col.isTrigger = false; col.size = new Vector2(0.5f, 0.5f);
        }

        // --- 스크립트 및 데이터 할당 ---
        if (enemyName.ToLower().Contains("haribo")) {
            var haribo = go.GetComponent<MeltingHaribo>() ?? go.AddComponent<MeltingHaribo>();
            string[] p = Directory.GetFiles(folderPath, "*.png");
            List<Sprite> allSprites = new List<Sprite>();
            foreach (var png in p.Where(f=>!f.ToLower().Contains("bullet")).OrderBy(f=>f)) {
                var assets = AssetDatabase.LoadAllAssetsAtPath(png);
                allSprites.AddRange(assets.OfType<Sprite>().OrderBy(s => s.name));
            }
            if (allSprites.Count > 0) haribo.hariboSprites = allSprites.ToArray();
        } else {
            var controllerType = GetTypeByName("EnemyController");
            if (controllerType != null) {
                var enemyCtrl = (MonoBehaviour)go.GetComponent(controllerType) ?? (MonoBehaviour)go.AddComponent(controllerType);
                string dataPath = $"Assets/Resources/EnemyData/{enemyName}.asset";
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
                if (data == null) {
                    data = ScriptableObject.CreateInstance<EnemyData>();
                    data.EnemyName = enemyName; data.HP = 60; data.Speed = 2f; data.DetectionRange = 10f;
                    if (!Directory.Exists("Assets/Resources/EnemyData")) Directory.CreateDirectory("Assets/Resources/EnemyData");
                    AssetDatabase.CreateAsset(data, dataPath);
                }
                var so = new SerializedObject(enemyCtrl);
                so.FindProperty("data").objectReferenceValue = data;
                so.FindProperty("autoInitialize").boolValue = true;
                so.ApplyModifiedProperties();
            }
        }
        if (go.GetComponent<Health>() == null) go.AddComponent<Health>();

        if (!isNew) { PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction); PrefabUtility.UnloadPrefabContents(go); }
        else { PrefabUtility.SaveAsPrefabAsset(go, prefabPath); GameObject.DestroyImmediate(go); }
    }

    private static AnimationClip CreateClipFromSpriteSheet(string pngPath, string animFolder, string fileName)
    {
        string clipPath = $"{animFolder}/{fileName}.anim";
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existingClip != null) return existingClip;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(pngPath);
        List<Sprite> sprites = assets.OfType<Sprite>().OrderBy(s => s.name).ToList();
        if (sprites.Count == 0) return null;
        AnimationClip clip = new AnimationClip(); clip.frameRate = 12;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = fileName.ToLower().Contains("idle") || fileName.ToLower().Contains("move");
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++) keyframes[i] = new ObjectReferenceKeyframe { time = i / 12f, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" }, keyframes);
        AssetDatabase.CreateAsset(clip, clipPath); return clip;
    }
}
