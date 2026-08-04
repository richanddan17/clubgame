using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 마법 스킬 6종(221 FireBall / 222 IceBlast / 223 ThunderBolt / 224 DarkBolt /
/// 225 Holy / 226 Acid) 투사체 프리팹에 3단계(Start → Loop → Hit) 프레임을 부여하는
/// 에디터 빌더.
///
/// 분리 프레임 PNG를 자연수 정렬(숫자 오름차순)로 Sprite[] 로 로드해
/// SpriteVFXAnimator 의 startFrames/loopFrames/hitFrames 에 할당하고,
/// SpriteRenderer 의 초기 스프라이트를 첫 loop 프레임으로 설정한다.
/// 멱등: 기존 프리팹은 제자리 갱신(GUID 유지), SpriteVFXAnimator 는 중복 생성하지 않는다.
/// 배치모드 진입점: MagicVFXBuilder.BuildAndVerifyAllMagicVFX
/// </summary>
public static class MagicVFXBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Projectiles";
    private const string VfxRoot = "Assets/Sprite/vfx";

    // Verified on disk 2026-08-04 (glob). All counts match the plan ranges.
    private const string Mp9 = VfxRoot + "/Magic Pack 9 files/Magic Pack 9 files/sprites";
    private const string Ice1 = VfxRoot + "/Ice Effect 01/Ice Effect 01/Ice VFX 1/Separated Frames";
    private const string HitEffect = VfxRoot + "/Hit Effect 01/Hit Effect 01";
    private const string Holy = VfxRoot + "/Holy VFX 01-02/Holy VFX 01/Separated Frames";
    private const string Acid = VfxRoot + "/Acid VFX 01 - 02/Acid VFX 01 - 02/Acid VFX 2/Separated Frames";

    private const float Fps = 12f;
    private const float Speed = 15f;
    private const float LifeTime = 3f;

    private sealed class StageSpec
    {
        public string Folder;   // Assets-relative folder of the PNGs
        public string Prefix;   // filename prefix (digits follow immediately)
        public int First;       // first frame number (inclusive)
        public int Last;        // last frame number (inclusive)
    }

    private sealed class SkillSpec
    {
        public int Id;
        public string PrefabName;   // exact prefab file base name (Todo 4 prefabMap keys)
        public StageSpec Start;     // null => stage intentionally empty (223/224/226)
        public StageSpec Loop;
        public StageSpec Hit;
    }

    private static readonly SkillSpec[] Skills =
    {
        new SkillSpec
        {
            Id = 221, PrefabName = "FireBallProjectile",
            Start = new StageSpec { Folder = Mp9 + "/FireBomb", Prefix = "Fire-bomb", First = 1, Last = 3 },
            Loop = new StageSpec { Folder = Mp9 + "/FireBomb", Prefix = "Fire-bomb", First = 4, Last = 7 },
            Hit = new StageSpec { Folder = Mp9 + "/FireBomb", Prefix = "Fire-bomb", First = 8, Last = 15 },
        },
        new SkillSpec
        {
            Id = 222, PrefabName = "IceBlastProjectile",
            Start = new StageSpec { Folder = Ice1, Prefix = "VFX 1 Start", First = 1, Last = 3 },
            Loop = new StageSpec { Folder = Ice1, Prefix = "VFX 1 Repeatable", First = 1, Last = 10 },
            Hit = new StageSpec { Folder = Ice1, Prefix = "VFX 1 Hit", First = 1, Last = 8 },
        },
        new SkillSpec
        {
            Id = 223, PrefabName = "ThunderBoltProjectile",
            Start = null,
            Loop = new StageSpec { Folder = Mp9 + "/Lightning", Prefix = "Lightning", First = 1, Last = 11 },
            Hit = new StageSpec { Folder = HitEffect, Prefix = "Hit Effect 01 ", First = 1, Last = 3 },
        },
        new SkillSpec
        {
            Id = 224, PrefabName = "DarkBoltProjectile",
            Start = null,
            Loop = new StageSpec { Folder = Mp9 + "/DarkBolt", Prefix = "Dark-Bolt", First = 1, Last = 4 },
            Hit = new StageSpec { Folder = Mp9 + "/DarkBolt", Prefix = "Dark-Bolt", First = 5, Last = 12 },
        },
        new SkillSpec
        {
            Id = 225, PrefabName = "HolyProjectile",
            Start = new StageSpec { Folder = Holy, Prefix = "Holy VFX 01 Initial", First = 1, Last = 2 },
            Loop = new StageSpec { Folder = Holy, Prefix = "Holy VFX 01 Repeatable", First = 1, Last = 8 },
            Hit = new StageSpec { Folder = Holy, Prefix = "Holy VFX 01 Impact", First = 1, Last = 7 },
        },
        new SkillSpec
        {
            Id = 226, PrefabName = "AcidProjectile",
            Start = null,
            Loop = new StageSpec { Folder = Acid, Prefix = "Acid VFX 02Repeatable", First = 1, Last = 12 },
            Hit = new StageSpec { Folder = Acid, Prefix = "Acid VFX 02 Ending", First = 1, Last = 6 },
        },
    };

    // ------------------------------------------------------------------
    // Entry points
    // ------------------------------------------------------------------

    /// <summary>Build + verify in one call — the batchmode entry point.</summary>
    public static void BuildAndVerifyAllMagicVFX()
    {
        BuildAllMagicVFX();
        VerifyAllMagicVFX();
        Debug.Log("[MagicVFXBuilder] BuildAndVerifyAllMagicVFX COMPLETE — all 6 prefabs green.");
    }

    [MenuItem("Custom Tools/tiger/Magic VFX/Build Magic VFX Prefabs")]
    public static void BuildMagicVFXPrefabsMenu()
    {
        BuildAndVerifyAllMagicVFX();
    }

    /// <summary>Build all 6 projectile prefabs (idempotent; throws on any hard failure).</summary>
    public static void BuildAllMagicVFX()
    {
        foreach (SkillSpec skill in Skills)
        {
            BuildSkillPrefab(skill);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MagicVFXBuilder] BuildAllMagicVFX complete ({Skills.Length} skills).");
    }

    /// <summary>
    /// Verify all 6 prefabs on disk. Throws on any failure so that a batchmode
    /// -executeMethod run exits non-zero.
    /// </summary>
    public static void VerifyAllMagicVFX()
    {
        int checkedCount = 0;
        foreach (SkillSpec skill in Skills)
        {
            string prefabPath = PrefabFolder + "/" + skill.PrefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: prefab missing: {prefabPath}");

            SpriteVFXAnimator vfx = prefab.GetComponent<SpriteVFXAnimator>();
            if (vfx == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} has no SpriteVFXAnimator");

            if (prefab.GetComponent<Projectile>() == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} missing Projectile");
            if (prefab.GetComponent<SpriteRenderer>() == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} missing SpriteRenderer");
            CircleCollider2D col = prefab.GetComponent<CircleCollider2D>();
            if (col == null || !col.isTrigger)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} missing/disabled trigger CircleCollider2D");

            SerializedObject so = new SerializedObject(vfx);
            int start = so.FindProperty("startFrames").arraySize;
            int loop = so.FindProperty("loopFrames").arraySize;
            int hit = so.FindProperty("hitFrames").arraySize;
            float fps = so.FindProperty("fps").floatValue;

            if (loop <= 0 || so.FindProperty("loopFrames").GetArrayElementAtIndex(0).objectReferenceValue == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} loopFrames empty/invalid (size={loop})");
            if (hit <= 0 || so.FindProperty("hitFrames").GetArrayElementAtIndex(0).objectReferenceValue == null)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} hitFrames empty/invalid (size={hit})");
            if (fps <= 0f)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} fps={fps}");

            if (skill.Start != null && start <= 0)
                throw new System.Exception($"[MagicVFXBuilder] Verify FAILED: {prefabPath} startFrames empty but required (size={start})");

            checkedCount++;
            Debug.Log($"[MagicVFXBuilder] Verify OK: {skill.Id} {skill.PrefabName} start={start} loop={loop} hit={hit} fps={fps}");
        }

        Debug.Log($"[MagicVFXBuilder] VerifyAllMagicVFX PASSED: {checkedCount}/{Skills.Length} prefabs OK.");
    }

    // ------------------------------------------------------------------
    // Implementation
    // ------------------------------------------------------------------

    private static void BuildSkillPrefab(SkillSpec skill)
    {
        Sprite[] start = LoadStage(skill, skill.Start, allowEmpty: skill.Start == null);
        Sprite[] loop = LoadStage(skill, skill.Loop, allowEmpty: false);
        Sprite[] hit = LoadStage(skill, skill.Hit, allowEmpty: false);

        string prefabPath = PrefabFolder + "/" + skill.PrefabName + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        GameObject go;
        bool recreated;
        if (existing == null)
        {
            go = new GameObject(skill.PrefabName);
            recreated = true;
        }
        else if (existing.GetComponent<Projectile>() == null)
        {
            // Broken placeholder without the projectile logic — replace it.
            Debug.LogWarning($"[MagicVFXBuilder] {prefabPath} is a placeholder (no Projectile); recreating it.");
            AssetDatabase.DeleteAsset(prefabPath);
            go = new GameObject(skill.PrefabName);
            recreated = true;
        }
        else
        {
            // Update in place so the prefab keeps its GUID / meta.
            go = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            recreated = false;
        }

        // Transform (template: localScale 3)
        go.transform.localScale = new Vector3(3f, 3f, 1f);

        // SpriteRenderer (template: sortingOrder 10) + initial sprite = first loop frame.
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        sr.sprite = loop.Length > 0 ? loop[0] : (start.Length > 0 ? start[0] : sr.sprite);

        // CircleCollider2D (template: isTrigger, radius 0.2)
        CircleCollider2D col = go.GetComponent<CircleCollider2D>();
        if (col == null) col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        // Projectile (private serialized fields via SerializedObject)
        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile == null) projectile = go.AddComponent<Projectile>();
        SerializedObject projSo = new SerializedObject(projectile);
        projSo.FindProperty("speed").floatValue = Speed;
        projSo.FindProperty("lifeTime").floatValue = LifeTime;
        projSo.ApplyModifiedPropertiesWithoutUndo();

        // SpriteVFXAnimator — overwrite arrays if present (idempotent), never duplicate.
        SpriteVFXAnimator vfx = go.GetComponent<SpriteVFXAnimator>();
        if (vfx == null) vfx = go.AddComponent<SpriteVFXAnimator>();
        SerializedObject vfxSo = new SerializedObject(vfx);
        WriteSpriteArray(vfxSo, "startFrames", start);
        WriteSpriteArray(vfxSo, "loopFrames", loop);
        WriteSpriteArray(vfxSo, "hitFrames", hit);
        vfxSo.FindProperty("fps").floatValue = Fps;
        vfxSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(go);
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        Debug.Log($"[MagicVFXBuilder] {(recreated ? "CREATED" : "UPDATED in place")}: {skill.Id} {skill.PrefabName} start={start.Length} loop={loop.Length} hit={hit.Length} -> {prefabPath}");
    }

    private static void WriteSpriteArray(SerializedObject so, string propertyName, Sprite[] frames)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        prop.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
    }

    /// <summary>
    /// Load one stage's frames. Frames are enumerated in ascending numeric order
    /// (natural numeric sort: frame10 sorts after frame9).
    /// Missing file -> Debug.LogWarning and skip; a required stage that ends up
    /// empty -> Debug.LogError and throw (aborts that prefab, fails batchmode).
    /// </summary>
    private static Sprite[] LoadStage(SkillSpec skill, StageSpec stage, bool allowEmpty)
    {
        if (stage == null)
        {
            Debug.Log($"[MagicVFXBuilder] {skill.Id} {skill.PrefabName} start stage: intentionally EMPTY.");
            return new Sprite[0];
        }

        List<Sprite> frames = new List<Sprite>();
        int missingCount = 0;
        for (int i = stage.First; i <= stage.Last; i++)
        {
            string path = stage.Folder + "/" + stage.Prefix + i + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                // Fallback: any Sprite sub-asset in the file (defensive; the
                // project's separated PNGs are imported as Multiple-mode sprites
                // so LoadAssetAtPath<Sprite> already returns the first piece).
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j] is Sprite)
                    {
                        sprite = (Sprite)all[j];
                        break;
                    }
                }
            }

            if (sprite != null)
            {
                frames.Add(sprite);
            }
            else
            {
                missingCount++;
                Debug.LogWarning($"[MagicVFXBuilder] {skill.Id} {skill.PrefabName} missing frame (skipped): {path}");
            }
        }

        if (frames.Count == 0 && !allowEmpty)
        {
            Debug.LogError($"[MagicVFXBuilder] {skill.Id} {skill.PrefabName} required stage '{stage.Prefix}' is EMPTY ({missingCount} files missing) — aborting this prefab.");
            throw new System.Exception($"Skill {skill.Id} ({skill.PrefabName}) required stage '{stage.Prefix}' has no frames.");
        }

        if (frames.Count > 0)
        {
            Debug.Log($"[MagicVFXBuilder] {skill.Id} {skill.PrefabName} stage '{stage.Prefix}': {frames.Count} frames (missing {missingCount}); first='{frames[0].name}', last='{frames[frames.Count - 1].name}'");
        }
        return frames.ToArray();
    }
}
