using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Animations;

public class DataImportMenu : EditorWindow
{
    private string skillRangedPath = "";
    private string skillMeleePath = "";
    private string skillMagicPath = "";
    private string unitPath = "";
    private string shopItemPath = "";

    private string baseDataPath => Path.Combine(Application.dataPath, "../tiger/datafiles");

    [MenuItem("Custom Tools/tiger/Create Enemy Marker", false, 10)]
    public static void CreateEnemyMarker()
    {
        GameObject marker = new GameObject("EnemyMarker");
        marker.AddComponent<EnemyMarker>();
        marker.transform.position = Vector3.zero;
        Selection.activeGameObject = marker;
        Undo.RegisterCreatedObjectUndo(marker, "Create Enemy Marker");
    }

    [MenuItem("Custom Tools/tiger/Setup Player Animations", false, 5)]
    public static void SetupPlayerAnimations()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { EditorUtility.DisplayDialog("Error", "플레이어 찾기 실패", "확인"); return; }

        var mf = player.GetComponent<MeshFilter>();
        var mr = player.GetComponent<MeshRenderer>();
        if (mf != null) DestroyImmediate(mf);
        if (mr != null) DestroyImmediate(mr);

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>() ?? player.AddComponent<SpriteRenderer>();
        Animator animator = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();

        if (sr.sprite == null) { EditorUtility.DisplayDialog("Notice", "이미지를 먼저 넣어주세요", "확인"); return; }

        string spritePath = AssetDatabase.GetAssetPath(sr.sprite);
        string spriteFolder = Path.GetDirectoryName(spritePath);
        string animFolder = "Assets/Animation/Player";
        EnsureFolder(animFolder);

        AnimationClip idleClip = CreateClipFromFolder(spriteFolder, "Idle", "PlayerIdle", true);
        AnimationClip walkClip = CreateClipFromFolder(spriteFolder, "Walk", "PlayerWalk", true);
        AnimationClip runClip = CreateClipFromFolder(spriteFolder, "Run", "PlayerRun", true);

        if (idleClip == null) idleClip = CreateClipFromSprites(spritePath, "PlayerIdle", true);

        var controller = AnimatorController.CreateAnimatorControllerAtPath($"{animFolder}/PlayerController.controller");
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isRunning", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        var idleState = sm.AddState("Idle"); idleState.motion = idleClip;

        if (walkClip != null)
        {
            var walkState = sm.AddState("Walk"); walkState.motion = walkClip;
            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            if (runClip != null)
            {
                var runState = sm.AddState("Run"); runState.motion = runClip;
                var walkToRun = walkState.AddTransition(runState); walkToRun.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
                var runToWalk = runState.AddTransition(walkState); runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
                var runToIdle = runState.AddTransition(idleState); runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
        }

        animator.runtimeAnimatorController = controller;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "애니메이션 설정 완료! \n'Idle', 'Walk', 'Run' 클립 생성됨.", "확인");
    }

    [MenuItem("Custom Tools/tiger/Initialize Game Scene", false, 0)]
    public static void InitializeGameScene()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) { mainCam = new GameObject("Main Camera").AddComponent<Camera>(); mainCam.tag = "MainCamera"; }
        var follow = mainCam.GetComponent<CameraFollow>() ?? mainCam.gameObject.AddComponent<CameraFollow>();

        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Quad); ground.name = "Ground";
            ground.transform.position = new Vector3(0, -5, 0); ground.transform.localScale = new Vector3(20, 1, 1);
            DestroyImmediate(ground.GetComponent<MeshCollider>()); ground.AddComponent<BoxCollider2D>();
            int layer = LayerMask.NameToLayer("Ground"); if (layer != -1) ground.layer = layer;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            player = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Quad);
            player.name = "Player"; player.tag = "Player";
        }

        var legacy = player.GetComponent("PlayerMoving"); if (legacy != null) DestroyImmediate(legacy);
        if (player.GetComponent<MeshCollider>()) DestroyImmediate(player.GetComponent<MeshCollider>());
        if (!player.GetComponent<BoxCollider2D>()) player.AddComponent<BoxCollider2D>();
        var rb = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.freezeRotation = true;
        var controller = player.GetComponent<PlayerController>() ?? player.AddComponent<PlayerController>();

        Transform groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck == null) { groundCheck = new GameObject("GroundCheck").transform; groundCheck.SetParent(player.transform); groundCheck.localPosition = new Vector3(0, -0.6f, 0); }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        so.FindProperty("groundLayer").intValue = 1 << LayerMask.NameToLayer("Ground");
        so.ApplyModifiedProperties();

        var playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>() ?? player.AddComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput.actions == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length > 0) { playerInput.actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0])); playerInput.defaultActionMap = "Player"; }
        }

        // --- 순수한 슬라임 프리팹 생성 (FreePixelMob 폴더 원본 사용) ---
        string slimePrefabPath = "Assets/Prefabs/Slime.prefab";
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(slimePrefabPath);
        if (slimePrefab == null)
        {
            GameObject tempSlime = new GameObject("SlimePrefab"); tempSlime.tag = "Enemy";
            
            // 비주얼 설정
            var sr = tempSlime.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/FreePixelMob/SlimeA.png");
            
            // 애니메이터 설정 (원본 컨트롤러 연결)
            var anim = tempSlime.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprite/FreePixelMob/Slime.controller");

            // 물리 설정
            var srb = tempSlime.AddComponent<Rigidbody2D>();
            srb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; srb.freezeRotation = true;
            tempSlime.AddComponent<CapsuleCollider2D>();

            // 스크립트 설정 (순수하게 원본 Mobs.cs만 사용)
            tempSlime.AddComponent<CanvasGroup>(); // Mobs.cs에서 RequireComponent로 요구함
            tempSlime.AddComponent<Mobs>();        // 원본 Mobs 스크립트!

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(tempSlime, slimePrefabPath);
            DestroyImmediate(tempSlime);
            Debug.Log("순수 슬라임 원본 프리팹(Slime.prefab) 생성 완료!");
        }

        follow.target = player.transform;
        EditorUtility.DisplayDialog("Magic Setup", "완료!", "확인");
    }

    [MenuItem("Custom Tools/tiger/Data Import/Open Import Window", false, 1)]
    public static void OpenImportWindow() { DataImportMenu window = GetWindow<DataImportMenu>("Data Import"); window.minSize = new Vector2(700, 600); window.Show(); window.InitializePaths(); }

    private void InitializePaths()
    {
        skillMeleePath = Path.Combine(baseDataPath, "skill/meleeskill.csv");
        skillRangedPath = Path.Combine(baseDataPath, "skill/rangedskill.csv");
        skillMagicPath = Path.Combine(baseDataPath, "skill/magicskill.csv");
        unitPath = Path.Combine(baseDataPath, "unit/unit.csv");
        shopItemPath = Path.Combine(baseDataPath, "shop/shop.csv");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CSV Status", EditorStyles.boldLabel);
        DrawStatusRow("Ranged Skill", ref skillRangedPath);
        DrawStatusRow("Melee Skill", ref skillMeleePath);
        DrawStatusRow("Unit", ref unitPath);
        if (GUILayout.Button("IMPORT ALL", GUILayout.Height(40))) ImportAll();
    }

    private void DrawStatusRow(string label, ref string path) { EditorGUILayout.BeginHorizontal(); EditorGUILayout.LabelField(label, GUILayout.Width(100)); EditorGUILayout.LabelField(File.Exists(path) ? "Ready" : "Missing"); EditorGUILayout.EndHorizontal(); }

    public static void ImportAll() { DataImportMenu window = GetWindow<DataImportMenu>(); window.ImportEnemyData(); }

    public void ImportEnemyData()
    {
        if (!File.Exists(unitPath)) return;
        string[] lines = File.ReadAllLines(unitPath);
        EnsureFolder("Assets/Resources/EnemyData");
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            int id = int.Parse(data[0]); string assetPath = $"Assets/Resources/EnemyData/{id}_{data[1]}.asset";
            EnemyData asset = GetOrCreateAsset<EnemyData>(assetPath);
            asset.ID = id; asset.EnemyName = data[1]; asset.HP = float.Parse(data[2]); asset.Speed = float.Parse(data[3]);
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] folders = path.Split('/'); string current = folders[0];
        for (int i = 1; i < folders.Length; i++) { if (!AssetDatabase.IsValidFolder(current + "/" + folders[i])) AssetDatabase.CreateFolder(current, folders[i]); current += "/" + folders[i]; }
    }

    private T GetOrCreateAsset<T>(string path) where T : ScriptableObject { T asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) { asset = CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); } return asset; }

    private static AnimationClip CreateClipFromFolder(string folderPath, string kw, string name, bool loop)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath }); List<Sprite> sprites = new List<Sprite>();
        foreach (var g in guids) { string p = AssetDatabase.GUIDToAssetPath(g); if (p.ToLower().Contains(kw.ToLower())) { foreach (var a in AssetDatabase.LoadAllAssetsAtPath(p)) if (a is Sprite s && !sprites.Contains(s)) sprites.Add(s); } }
        if (sprites.Count == 0) return null; sprites.Sort((a, b) => string.Compare(a.name, b.name));
        return BuildAnimationClip(sprites, name, loop);
    }

    private static AnimationClip BuildAnimationClip(List<Sprite> sprites, string name, bool loop)
    {
        AnimationClip clip = new AnimationClip { name = name };
        if (loop) { AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip); s.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, s); }
        EditorCurveBinding b = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        ObjectReferenceKeyframe[] kf = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++) { kf[i] = new ObjectReferenceKeyframe { time = i / 10f, value = sprites[i] }; }
        AnimationUtility.SetObjectReferenceCurve(clip, b, kf);
        AssetDatabase.CreateAsset(clip, $"Assets/Animation/Player/{name}.anim"); return clip;
    }

    private static AnimationClip CreateClipFromSprites(string path, string name, bool loop)
    {
        List<Sprite> sprites = new List<Sprite>(); foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path)) if (a is Sprite s) sprites.Add(s);
        if (sprites.Count == 0) return null; return BuildAnimationClip(sprites, name, loop);
    }
}
