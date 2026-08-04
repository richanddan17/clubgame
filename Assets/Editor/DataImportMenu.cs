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
    private string biomePath = "";
    private string skillPresetPath = "";

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
        if (player == null) { EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Error", "플레이어 찾기 실패", "확인"); return; }

        var mf = player.GetComponent<MeshFilter>();
        var mr = player.GetComponent<MeshRenderer>();
        if (mf != null) DestroyImmediate(mf);
        if (mr != null) DestroyImmediate(mr);

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>() ?? player.AddComponent<SpriteRenderer>();
        Animator animator = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();

        if (sr.sprite == null) { EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Notice", "이미지를 먼저 넣어주세요", "확인"); return; }

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
        EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Success", "애니메이션 설정 완료! \n'Idle', 'Walk', 'Run' 클립 생성됨.", "확인");
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
        EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Magic Setup", "완료!", "확인");
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
        biomePath = Path.Combine(baseDataPath, "biome/biome.csv");
        skillPresetPath = Path.Combine(baseDataPath, "skill/preset.csv");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CSV Status", EditorStyles.boldLabel);
        DrawStatusRow("Ranged Skill", ref skillRangedPath);
        DrawStatusRow("Melee Skill", ref skillMeleePath);
        DrawStatusRow("Unit", ref unitPath);
        DrawStatusRow("Biome", ref biomePath);
        DrawStatusRow("Skill Preset", ref skillPresetPath);
        if (GUILayout.Button("IMPORT ALL", GUILayout.Height(40))) 
        {
            EditorApplication.delayCall += ImportAll;
        }
    }

    private void DrawStatusRow(string label, ref string path) { EditorGUILayout.BeginHorizontal(); EditorGUILayout.LabelField(label, GUILayout.Width(100)); EditorGUILayout.LabelField(File.Exists(path) ? "Ready" : "Missing"); EditorGUILayout.EndHorizontal(); }

    public static void ImportAll() 
    { 
        DataImportMenu window = GetWindow<DataImportMenu>(); 
        window.ImportEnemyData(); 
        window.ImportBiomeData();
        window.ImportSkillData();
        window.ImportSkillPresets();
    }

    public void ImportSkillData()
    {
        ImportSkillFile(skillRangedPath);
        ImportSkillFile(skillMeleePath);
        ImportSkillFile(skillMagicPath);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Skill Data Import Complete!");
    }

    private void ImportSkillFile(string path)
    {
        if (!File.Exists(path)) return;
        string[] lines = File.ReadAllLines(path);
        EnsureFolder("Assets/Resources/SkillData");

        if (lines.Length < 2) return;

        // Parse header dynamically by column name so BOTH old (ID,Name,Damage,ManaCost,Cooldown)
        // and new (…,Type,Bubble,Speed,MeleeRange,MeleeArc) schemas work.
        string[] header = lines[0].Split(',');
        Dictionary<string, int> col = new Dictionary<string, int>();
        for (int h = 0; h < header.Length; h++)
        {
            string key = header[h].Trim();
            if (!string.IsNullOrEmpty(key) && !col.ContainsKey(key)) col[key] = h;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');

            if (data.Length < 5) continue;

            int id = int.Parse(data[col["ID"]]);
            string skillName = data[col["Name"]];
            string assetPath = $"Assets/Resources/SkillData/{id}_{skillName}.asset";

            SkillData asset = GetOrCreateAsset<SkillData>(assetPath);
            asset.ID = id;
            asset.SkillName = skillName;
            asset.Damage = float.Parse(data[col["Damage"]]);
            asset.ManaCost = float.Parse(data[col["ManaCost"]]);
            asset.Cooldown = float.Parse(data[col["Cooldown"]]);

            // --- NEW schema columns (absent in old CSV files) ---
            if (col.ContainsKey("Type"))
            {
                string raw = data[col["Type"]];
                SkillType parsed;
                asset.SkillType = string.IsNullOrEmpty(raw) || !System.Enum.TryParse(raw, out parsed)
                    ? SkillType.Projectile
                    : parsed;
            }

            if (col.ContainsKey("Bubble"))
            {
                string raw = data[col["Bubble"]];
                // None / empty -> no bubble effect (BubbleEffect defaults to Blue)
                asset.UseBubbleEffect = raw == "Red" || raw == "Blue" || raw == "Yellow";
                asset.BubbleEffect = raw == "Red" ? Projectile.BubbleType.Red
                                    : raw == "Yellow" ? Projectile.BubbleType.Yellow
                                    : raw == "Blue" ? Projectile.BubbleType.Blue
                                    : Projectile.BubbleType.Blue;
            }

            if (col.ContainsKey("Speed"))
            {
                string raw = data[col["Speed"]];
                asset.ProjectileSpeed = string.IsNullOrEmpty(raw) ? 15f : float.Parse(raw);
            }

            if (col.ContainsKey("MeleeRange"))
            {
                string raw = data[col["MeleeRange"]];
                asset.MeleeRange = string.IsNullOrEmpty(raw) ? 0f : float.Parse(raw);
            }

            if (col.ContainsKey("MeleeArc"))
            {
                string raw = data[col["MeleeArc"]];
                asset.MeleeArc = string.IsNullOrEmpty(raw) ? 0f : float.Parse(raw);
            }

            EditorUtility.SetDirty(asset);
        }
    }

    /// <summary>Batchmode entry: imports only the 3 skill CSVs (no window interaction needed).</summary>
    public static void ImportSkillDataOnly()
    {
        DataImportMenu window = GetWindow<DataImportMenu>();
        window.InitializePaths();
        window.ImportSkillData();
        Debug.Log("[DataImportMenu] ImportSkillDataOnly complete!");
    }

    /// <summary>Batchmode entry: assigns ProjectilePrefab to the root SkillData assets (201..223).</summary>
    public static void LinkSkillPrefabs()
    {
        const string rootFolder = "Assets/Resources/SkillData";
        const string meleePrefabPath = "Assets/Prefabs/Projectiles/MeleeHitbox.prefab";

        bool meleePrefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(meleePrefabPath) != null;

        Dictionary<int, string> prefabMap = new Dictionary<int, string>();
        prefabMap[201] = meleePrefabPath;   // Slash
        prefabMap[202] = meleePrefabPath;   // HeavyStrike
        prefabMap[203] = meleePrefabPath;   // Whirlwind
        prefabMap[211] = "Assets/Prefabs/BubbleProjectile_blue.prefab";   // GumShot
        prefabMap[212] = "Assets/Prefabs/BubbleProjectile_red.prefab";    // StickyBlob (Bubble=Red)
        prefabMap[213] = "Assets/Prefabs/BubbleProjectile_yellow.prefab"; // BigBubble (Bubble=Yellow, stun)
        prefabMap[214] = "Assets/Prefabs/BubbleProjectile_blue.prefab";   // PopTrap (no bubble effect, reuse blue)
        prefabMap[221] = "Assets/Prefabs/Projectiles/FireBallProjectile.prefab";
        prefabMap[222] = "Assets/Prefabs/Projectiles/IceBlastProjectile.prefab";
        prefabMap[223] = "Assets/Prefabs/Projectiles/ThunderBoltProjectile.prefab";

        // Iterate ONLY root SkillData assets directly under Assets/Resources/SkillData
        // (skip subfolder assets like Ranged/Melee/Magic/*).
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { rootFolder });
        int linked = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf("/", rootFolder.Length + 1) >= 0) continue; // subfolder asset -> skip

            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null || !prefabMap.ContainsKey(skill.ID)) continue;

            string prefabPath = prefabMap[skill.ID];
            if (skill.ID >= 201 && skill.ID <= 203)
            {
                if (!meleePrefabExists)
                {
                    Debug.LogWarning("[DataImportMenu] LinkSkillPrefabs: MeleeHitbox.prefab missing, leaving melee skills " + skill.ID + " unlinked (retry expected).");
                    continue;
                }
                prefabPath = meleePrefabPath;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[DataImportMenu] LinkSkillPrefabs: prefab not found: " + prefabPath);
                continue;
            }

            skill.ProjectilePrefab = prefab;
            EditorUtility.SetDirty(skill);
            linked++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataImportMenu] LinkSkillPrefabs complete. Linked=" + linked + " MeleeHitboxExists=" + meleePrefabExists);
    }

    /// <summary>Batchmode entry: equips the 4 GumMaster skills (211..214) on Player.prefab.</summary>
    public static void EquipGumMasterOnPlayer()
    {
        const string prefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError("[DataImportMenu] EquipGumMasterOnPlayer: Player.prefab not found at " + prefabPath);
            return;
        }

        PlayerController controller = playerPrefab.GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.LogError("[DataImportMenu] EquipGumMasterOnPlayer: PlayerController not found on Player.prefab");
            return;
        }

        string[] skillPaths =
        {
            "Assets/Resources/SkillData/211_GumShot.asset",
            "Assets/Resources/SkillData/212_StickyBlob.asset",
            "Assets/Resources/SkillData/213_BigBubble.asset",
            "Assets/Resources/SkillData/214_PopTrap.asset"
        };

        List<SkillData> skills = new List<SkillData>();
        foreach (string p in skillPaths)
        {
            SkillData s = AssetDatabase.LoadAssetAtPath<SkillData>(p);
            if (s == null)
            {
                Debug.LogError("[DataImportMenu] EquipGumMasterOnPlayer: missing skill asset " + p);
                continue;
            }
            skills.Add(s);
        }
        if (skills.Count != 4)
        {
            Debug.LogError("[DataImportMenu] EquipGumMasterOnPlayer: expected 4 GumMaster skills, found " + skills.Count + ". Aborting.");
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty equipped = so.FindProperty("combatSettings.EquippedSkills");
        if (equipped == null)
        {
            Debug.LogError("[DataImportMenu] EquipGumMasterOnPlayer: property 'combatSettings.EquippedSkills' not found");
            return;
        }

        equipped.ClearArray();
        for (int i = 0; i < skills.Count; i++)
        {
            equipped.InsertArrayElementAtIndex(i);
            equipped.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
        }
        so.ApplyModifiedProperties();

        PrefabUtility.SavePrefabAsset(playerPrefab);

        GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (reloaded != null)
        {
            SerializedObject verify = new SerializedObject(reloaded.GetComponent<PlayerController>());
            SerializedProperty list = verify.FindProperty("combatSettings.EquippedSkills");
            if (list != null)
            {
                string[] names = new string[list.arraySize];
                for (int i = 0; i < list.arraySize; i++)
                    names[i] = list.GetArrayElementAtIndex(i).objectReferenceValue != null
                        ? list.GetArrayElementAtIndex(i).objectReferenceValue.name
                        : "<null>";
                Debug.Log("[DataImportMenu] EquipGumMasterOnPlayer: EquippedSkills=[" + string.Join(", ", names) + "]");
            }
        }

        Debug.Log("[DataImportMenu] EquipGumMasterOnPlayer: Player.prefab equipped with 211/212/213/214 GumMaster skills.");
    }

    /// <summary>Batchmode entry: static wrapper around the instance ImportSkillPresets() so -executeMethod can target presets only.</summary>
    public static void ImportSkillPresetsOnly()
    {
        DataImportMenu window = GetWindow<DataImportMenu>();
        window.InitializePaths();
        window.ImportSkillPresets();
        Debug.Log("[DataImportMenu] ImportSkillPresetsOnly complete!");
    }

    /// <summary>
    /// Batchmode pre-deletion dependency scan: scans every text asset under Assets/
    /// for any reference to the delete-target GUIDs (Magic/Ranged/Melee subfolder dupes,
    /// 101_Shotgun, NewSkillData). Expected: 0 references. Any reference -> log error (caller must STOP).
    /// </summary>
    public static void LogSkillDataReferences()
    {
        string[] targets =
        {
            "Assets/Resources/SkillData/Magic",
            "Assets/Resources/SkillData/Ranged",
            "Assets/Resources/SkillData/Melee",
            "Assets/Resources/SkillData/101_Shotgun.asset",
            "Assets/Resources/SkillData/NewSkillData.asset"
        };

        List<string> deleteGuids = new List<string>();
        List<string> deletePaths = new List<string>();
        foreach (string t in targets)
        {
            if (AssetDatabase.IsValidFolder(t))
            {
                string[] guids = AssetDatabase.FindAssets("", new[] { t });
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (!AssetDatabase.IsValidFolder(p)) { deleteGuids.Add(g); deletePaths.Add(p); }
                }
            }
            else if (File.Exists(t))
            {
                string g = AssetDatabase.AssetPathToGUID(t);
                if (!string.IsNullOrEmpty(g)) { deleteGuids.Add(g); deletePaths.Add(t); }
            }
            else
            {
                Debug.LogWarning("[DataImportMenu] LogSkillDataReferences: delete target not found: " + t);
            }
        }

        Debug.Log("[DataImportMenu] LogSkillDataReferences: scanning references TO " + deleteGuids.Count + " delete-target assets.");
        for (int i = 0; i < deletePaths.Count; i++)
            Debug.Log("[DataImportMenu]   target[" + i + "] " + deletePaths[i] + " guid=" + deleteGuids[i]);

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string[] all = AssetDatabase.GetAllAssetPaths();
        int totalRefs = 0;
        foreach (string path in all)
        {
            if (!path.StartsWith("Assets")) continue;
            if (path.EndsWith(".meta")) continue;
            if (deletePaths.Contains(path)) continue; // skip the delete target itself

            string full = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) continue;
            string text;
            try { text = File.ReadAllText(full); }
            catch { continue; }
            foreach (string g in deleteGuids)
            {
                if (text.IndexOf(g, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogError("[DataImportMenu] REFERENCE FOUND: " + path + " contains delete-target guid " + g);
                    totalRefs++;
                }
            }
        }

        Debug.Log("[DataImportMenu] LogSkillDataReferences done. total references to delete targets = " + totalRefs + " (expected 0).");
    }

    /// <summary>Batchmode entry: deletes the duplicate subfolder assets and the two junk assets. Never touches 301_TimeStop.</summary>
    public static void DeleteDuplicateSkillData()
    {
        const string timestop = "Assets/Resources/SkillData/301_TimeStop.asset";
        bool tsBefore = File.Exists(timestop);
        Debug.Log("[DataImportMenu] DeleteDuplicateSkillData: 301_TimeStop exists before deletion = " + tsBefore);
        if (!tsBefore)
        {
            Debug.LogError("[DataImportMenu] DeleteDuplicateSkillData: ABORT - 301_TimeStop missing before deletion!");
            return;
        }

        string[] targets =
        {
            "Assets/Resources/SkillData/Magic",
            "Assets/Resources/SkillData/Ranged",
            "Assets/Resources/SkillData/Melee",
            "Assets/Resources/SkillData/101_Shotgun.asset",
            "Assets/Resources/SkillData/NewSkillData.asset"
        };

        foreach (string t in targets)
        {
            if (AssetDatabase.IsValidFolder(t))
            {
                string[] inside = AssetDatabase.FindAssets("", new[] { t });
                foreach (string g in inside)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (!AssetDatabase.IsValidFolder(p))
                        Debug.Log("[DataImportMenu]   deleting child: " + p + " guid=" + g);
                }
                if (AssetDatabase.DeleteAsset(t))
                    Debug.Log("[DataImportMenu] DELETED folder " + t);
                else
                    Debug.LogError("[DataImportMenu] FAILED to delete folder " + t);
            }
            else if (File.Exists(t))
            {
                if (AssetDatabase.DeleteAsset(t))
                    Debug.Log("[DataImportMenu] DELETED asset " + t);
                else
                    Debug.LogError("[DataImportMenu] FAILED to delete asset " + t);
            }
            else
            {
                Debug.LogWarning("[DataImportMenu] delete target already gone (ok): " + t);
            }
        }

        AssetDatabase.Refresh();

        bool tsAfter = File.Exists(timestop);
        Debug.Log("[DataImportMenu] DeleteDuplicateSkillData: 301_TimeStop exists after deletion = " + tsAfter);
        if (!tsAfter) Debug.LogError("[DataImportMenu] DeleteDuplicateSkillData: CRITICAL - 301_TimeStop was deleted!");

        string[] remaining = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Resources/SkillData" });
        Debug.Log("[DataImportMenu] remaining SkillData assets under Assets/Resources/SkillData: " + remaining.Length);
        foreach (string g in remaining)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string root = "Assets/Resources/SkillData";
            bool isSubfolder = p.IndexOf("/", root.Length + 1) >= 0;
            if (isSubfolder)
                Debug.LogWarning("[DataImportMenu]   SUBFOLDER REMAINS: " + p);
            else
                Debug.Log("[DataImportMenu]   root: " + p);
        }
    }

    /// <summary>Batchmode QA diagnostic: logs the exact FindAssets order ImportSkillPresets relies on (guids[0]).</summary>
    public static void QaLogFindAssets201()
    {
        string[] guids = AssetDatabase.FindAssets("201_ t:SkillData", new[] { "Assets/Resources/SkillData" });
        Debug.Log("[DataImportMenu] QaLogFindAssets201: " + guids.Length + " matches for filter '201_ t:SkillData'");
        for (int i = 0; i < guids.Length; i++)
            Debug.Log("[DataImportMenu]   match[" + i + "] guid=" + guids[i] + " path=" + AssetDatabase.GUIDToAssetPath(guids[i]));
    }

    /// <summary>Batchmode QA helper: recreates a 201_Slash duplicate in a subfolder that FindAssets ordering picks FIRST.</summary>
    public static void CreateQADuplicate201Early()
    {
        EnsureFolder("Assets/Resources/SkillData/0QA_Temp");
        const string path = "Assets/Resources/SkillData/0QA_Temp/201_Slash.asset";
        SkillData dup = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (dup == null)
        {
            dup = CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(dup, path);
        }
        dup.ID = 201;
        dup.SkillName = "Slash";
        dup.Damage = 10;
        dup.ManaCost = 0;
        dup.Cooldown = 0.5f;
        dup.SkillType = SkillType.Melee;
        dup.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/MeleeHitbox.prefab");
        EditorUtility.SetDirty(dup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataImportMenu] QA early duplicate created: " + path + " guid=" + AssetDatabase.AssetPathToGUID(path));
    }

    /// <summary>Batchmode QA cleanup: removes the temporary duplicate folders created by the QA helpers.</summary>
    public static void CleanupQADuplicates()
    {
        foreach (string t in new[] { "Assets/Resources/SkillData/0QA_Temp", "Assets/Resources/SkillData/Melee" })
        {
            if (AssetDatabase.IsValidFolder(t))
            {
                if (AssetDatabase.DeleteAsset(t))
                    Debug.Log("[DataImportMenu] QA cleanup DELETED " + t);
                else
                    Debug.LogError("[DataImportMenu] QA cleanup FAILED " + t);
            }
            else
                Debug.Log("[DataImportMenu] QA cleanup target already gone: " + t);
        }
        AssetDatabase.Refresh();
        Debug.Log("[DataImportMenu] CleanupQADuplicates done");
    }

    /// <summary>Batchmode QA helper: recreates the Melee/201_Slash duplicate to prove preset resolution needs the deletion.</summary>
    public static void CreateQADuplicate201()
    {
        EnsureFolder("Assets/Resources/SkillData/Melee");
        const string path = "Assets/Resources/SkillData/Melee/201_Slash.asset";
        SkillData dup = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (dup == null)
        {
            dup = CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(dup, path);
        }
        dup.ID = 201;
        dup.SkillName = "Slash";
        dup.Damage = 10;
        dup.ManaCost = 0;
        dup.Cooldown = 0.5f;
        dup.SkillType = SkillType.Melee;
        dup.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/MeleeHitbox.prefab");
        EditorUtility.SetDirty(dup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataImportMenu] QA duplicate created: " + path + " guid=" + AssetDatabase.AssetPathToGUID(path));
    }

    public void ImportSkillPresets()
    {
        if (!File.Exists(skillPresetPath)) return;
        string[] lines = File.ReadAllLines(skillPresetPath);
        EnsureFolder("Assets/Resources/SkillPresets");
        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            
            string presetName = data[0];
            string[] skillIds = data[1].Split(';');
            string assetPath = $"Assets/Resources/SkillPresets/{presetName}.asset";
            
            SkillPreset asset = GetOrCreateAsset<SkillPreset>(assetPath);
            asset.presetName = presetName;
            asset.skills.Clear();
            
            foreach (var id in skillIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                string[] guids = AssetDatabase.FindAssets($"{id}_ t:SkillData", new[] { "Assets/Resources/SkillData" });
                if (guids.Length > 0)
                {
                    asset.skills.Add(AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guids[0])));
                }
            }
            
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Skill Presets Import Complete!");
    }

    public void ImportEnemyData()
    {
        if (!File.Exists(unitPath)) return;
        string[] lines = File.ReadAllLines(unitPath);
        EnsureFolder("Assets/Resources/EnemyData");
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            if (data.Length < 8) continue; // ID, Name, HP, Speed, Damage, Detect, AttackR, Interval

            int id = int.Parse(data[0]); 
            string assetPath = $"Assets/Resources/EnemyData/{id}_{data[1]}.asset";
            EnemyData asset = GetOrCreateAsset<EnemyData>(assetPath);

            asset.ID = id; 
            asset.EnemyName = data[1]; 
            asset.HP = float.Parse(data[2]); 
            asset.Speed = float.Parse(data[3]);
            asset.Damage = float.Parse(data[4]);
            asset.DetectionRange = float.Parse(data[5]);
            asset.AttackRange = float.Parse(data[6]);
            asset.AttackInterval = float.Parse(data[7]);

            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("<color=green>[DataImport]</color> Enemy Data All Import Complete!");
    }

    public void ImportBiomeData()
    {
        if (!File.Exists(biomePath)) return;
        string[] lines = File.ReadAllLines(biomePath);
        EnsureFolder("Assets/Resources/BiomeData");
        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] data = lines[i].Split(',');
            
            // ID,Name,TintR,TintG,TintB,EnemyIDs,BG_Layer1
            string id = data[0];
            string biomeName = data[1];
            float r = float.Parse(data[2]);
            float g = float.Parse(data[3]);
            float b = float.Parse(data[4]);
            string[] enemyIds = data[5].Split(';');
            string bgGuid = data.Length > 6 ? data[6] : "";

            string assetPath = $"Assets/Resources/BiomeData/{biomeName}.asset";
            BiomeData asset = GetOrCreateAsset<BiomeData>(assetPath);
            
            asset.biomeName = biomeName;
            asset.tilemapTint = new Color(r, g, b, 1f);
            
            // 몬스터 연결
            List<EnemyData> enemyList = new List<EnemyData>();
            foreach (var eid in enemyIds)
            {
                if (string.IsNullOrEmpty(eid)) continue;
                // Resources/EnemyData 폴더에서 ID로 시작하는 에셋 검색
                string[] guids = AssetDatabase.FindAssets($"{eid}_ t:EnemyData", new[] { "Assets/Resources/EnemyData" });
                if (guids.Length > 0)
                {
                    enemyList.Add(AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guids[0])));
                }
            }
            asset.allowedEnemies = enemyList.ToArray();

            // 배경 이미지 연결 (GUID 기반)
            if (!string.IsNullOrEmpty(bgGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(bgGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    asset.backgroundLayers = new Sprite[] { bgSprite };
                }
            }

            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Biome Data Import Complete!");
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
