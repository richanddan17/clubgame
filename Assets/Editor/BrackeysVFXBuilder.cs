using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Brackeys 무료 VFX 시트(단일 텍스처 다중 슬라이스) 8종(231-238) 투사체 프리팹을
/// 2단계(Loop → Hit) 프레임으로 구성하는 에디터 빌더. MagicVFXBuilder.cs 패턴 미러
/// (BuildSkillPrefab + SaveAsPrefabAsset + SerializedObject, 기존 파일 무접촉).
///
/// SheetStageSpec 로더: 서브스프라이트 이름 `^{Prefix}_(\d+)$` 정규식 필터 +
/// int.Parse 자연 정렬(문자열 정렬 금지 — _10 이 _2 앞에 오는 오류) +
/// [First..Last] 슬라이스(0-based, 경계 초과 시 클램프 + Debug.LogWarning,
/// null = 전체 프레임).
///
/// 크기 규칙: brackeys 프레임은 FireBall(7x7px) 대비 최대 20배 커서 blind scale 3
/// 상속 금지. 스킬별 scale = clamp(21 / maxFramePx, 0.05, 1.0) — maxFramePx 는
/// 해당 스킬 loop/hit 프레임의 최대 변(px, sprite.textureRect), 21 = 7px * scale 3
/// (FireBall 이 0.21 유닛으로 보이는 크기 정합 목표).
///
/// 재생: Loop 는 처음 min(시트 프레임 수, 60) 프레임(60 = 5초 @12fps 상한, 리소스 보호),
/// Hit 는 [0..29] 클램프, fps 12, autoPlay true, startFrames 는 빈 배열.
/// 멱등: 기존 프리팹은 제자리 갱신(GUID 유지), SpriteVFXAnimator 는 중복 생성하지 않는다.
/// 배치모드 진입점: BrackeysVFXBuilder.BuildAndVerifyAllBrackeysVFX
/// </summary>
public static class BrackeysVFXBuilder
{
    private const string SheetFolder = "Assets/Sprite/vfx/brackeys_vfx_bundle/brackeys_vfx_bundle/predrawn";
    private const string PrefabFolder = "Assets/Prefabs/Projectiles";

    private const int LoopFrameCap = 60;     // 60 frames = 5 s @ 12 fps (resource guard)
    private const float Fps = 12f;
    private const float Speed = 15f;         // template default only — runtime speed comes from CSV ProjectileSpeed
    private const float LifeTime = 3f;
    private const int SortingOrder = 10;
    private const float ColliderRadius = 0.2f;
    private const float ScaleTargetPx = 21f; // FireBall: 7px sprite * scale 3 = 21px = 0.21 units
    private const float MinScale = 0.05f;
    private const float MaxScale = 1f;

    /// <summary>
    /// 하나의 Brackeys 시트 단계(스테이지) 명세. 로드 시 서브스프라이트를
    /// `^{Prefix}_(\d+)$` 이름으로 필터링하고 int.Parse 자연 정렬 후
    /// [First..Last] (0-based, 포함) 로 자른다. First/Last 가 null 이면 전체.
    /// 경계 초과는 클램프 + Debug.LogWarning (미검증 카운트 시트에 안전).
    /// </summary>
    public struct SheetStageSpec
    {
        public string SheetPath;   // Assets-relative path of the sliced PNG sheet
        public string Prefix;      // sub-sprite name base (e.g. "explosion_6x5")
        public int? First;         // 0-based first frame index (inclusive); null = first frame
        public int? Last;          // 0-based last frame index (inclusive); null = last frame

        public SheetStageSpec(string SheetPath, string Prefix, int? First = null, int? Last = null)
        {
            this.SheetPath = SheetPath;
            this.Prefix = Prefix;
            this.First = First;
            this.Last = Last;
        }
    }

    private sealed class SkillSpec
    {
        public int Id;
        public string PrefabName;   // exact prefab / GameObject base name (Todo 4 prefabMap keys)
        public SheetStageSpec Loop;
        public SheetStageSpec Hit;
    }

    /// <summary>
    /// 8종 SkillSpec (plan 테이블 그대로). Loop = 처음 min(시트 프레임 수, 60) 프레임
    /// [0-based] (First/Last null + LoopFrameCap), Hit = [0..29] 클램프.
    /// </summary>
    private static readonly SkillSpec[] Skills =
    {
        new SkillSpec
        {
            Id = 231, PrefabName = "FireOrbProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/fire_point_6x5.png", "fire_point_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/explosion_6x5.png", "explosion_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 232, PrefabName = "FireRingProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/fire_ring_6x5.png", "fire_ring_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/explosion_6x5.png", "explosion_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 233, PrefabName = "ElectricRingProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/electric_ring_6x5.png", "electric_ring_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/star_explosion_6x5.png", "star_explosion_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 234, PrefabName = "VortexProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/vortex_6x5.png", "vortex_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/explosion_6x5.png", "explosion_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 235, PrefabName = "LightStreakProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/lightstreaks_6x5.png", "lightstreaks_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/big_hit_6x5.png", "big_hit_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 236, PrefabName = "WavyBoltProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/wavy_blue_6x5.png", "wavy_blue_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/explosion_6x5.png", "explosion_6x5", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 237, PrefabName = "ChargeProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/charge_7x6.png", "charge_7x6"),
            Hit = new SheetStageSpec(SheetFolder + "/impact_white_6x4.png", "impact_white_6x4", First: 0, Last: 29),
        },
        new SkillSpec
        {
            Id = 238, PrefabName = "BloodBoltProjectile",
            Loop = new SheetStageSpec(SheetFolder + "/dithered_fire_6x5.png", "dithered_fire_6x5"),
            Hit = new SheetStageSpec(SheetFolder + "/blood_impact_6x5.png", "blood_impact_6x5", First: 0, Last: 29),
        },
    };

    // ------------------------------------------------------------------
    // Entry points
    // ------------------------------------------------------------------

    /// <summary>Build + verify in one call — the batchmode entry point.</summary>
    public static void BuildAndVerifyAllBrackeysVFX()
    {
        try
        {
            BuildAllBrackeysVFX();
            VerifyAllBrackeysVFX();
            Debug.Log("[BrackeysVFXBuilder] BuildAndVerifyAllBrackeysVFX PASSED — all 8 brackeys prefabs green.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BrackeysVFXBuilder] BuildAndVerifyAllBrackeysVFX FAILED: {e.Message}");
            throw;
        }
    }

    [MenuItem("Custom Tools/tiger/Magic VFX/Build Brackeys VFX Prefabs")]
    public static void BuildBrackeysVFXPrefabsMenu()
    {
        BuildAndVerifyAllBrackeysVFX();
    }

    /// <summary>Build all 8 brackeys projectile prefabs (idempotent; throws on any hard failure).</summary>
    public static void BuildAllBrackeysVFX()
    {
        foreach (SkillSpec skill in Skills)
        {
            BuildSkillPrefab(skill);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BrackeysVFXBuilder] BuildAllBrackeysVFX complete ({Skills.Length} skills).");
    }

    /// <summary>
    /// Verify all 8 brackeys prefabs on disk. Throws on any failure so that a
    /// batchmode -executeMethod run exits non-zero.
    /// </summary>
    public static void VerifyAllBrackeysVFX()
    {
        int checkedCount = 0;
        foreach (SkillSpec skill in Skills)
        {
            VerifySkillPrefab(skill);
            checkedCount++;
        }
        Debug.Log($"[BrackeysVFXBuilder] VerifyAllBrackeysVFX PASSED: {checkedCount}/{Skills.Length} prefabs OK.");
    }

    // ------------------------------------------------------------------
    // Implementation
    // ------------------------------------------------------------------

    private static void BuildSkillPrefab(SkillSpec skill)
    {
        // Loop = first min(sheet frames, 60) frames; Hit = [0..29] clamped.
        Sprite[] loop = LoadStage(skill.Loop, LoopFrameCap);
        Sprite[] hit = LoadStage(skill.Hit, 0);

        // F8 visual-continuity gate: loop must be naturally sorted, contiguous, >1 frame.
        ValidateLoopContinuity(skill, loop);

        // Per-skill scale: normalize the largest frame side so every projectile
        // renders near FireBall's 0.21-unit world size (7px * scale 3).
        float maxFramePx = ComputeMaxFramePx(loop, hit);
        float s = Mathf.Clamp(ScaleTargetPx / maxFramePx, MinScale, MaxScale);

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
            Debug.LogWarning($"[BrackeysVFXBuilder] {prefabPath} is a placeholder (no Projectile); recreating it.");
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

        // Transform localScale = (s, s, 1) — per-skill computed, never blind scale 3.
        go.transform.localScale = new Vector3(s, s, 1f);

        // SpriteRenderer (template: sortingOrder 10) + initial sprite = first loop frame.
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = SortingOrder;
        sr.sprite = loop[0];

        // CircleCollider2D (template: isTrigger, radius 0.2).
        CircleCollider2D col = go.GetComponent<CircleCollider2D>();
        if (col == null) col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = ColliderRadius;

        // Projectile — private serialized fields via SerializedObject.
        // speed/lifeTime are template defaults only; runtime speed is overridden
        // by PlayerController.Initialize from CSV ProjectileSpeed (12-18).
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
        WriteSpriteArray(vfxSo, "startFrames", new Sprite[0]);
        WriteSpriteArray(vfxSo, "loopFrames", loop);
        WriteSpriteArray(vfxSo, "hitFrames", hit);
        vfxSo.FindProperty("fps").floatValue = Fps;
        vfxSo.FindProperty("autoPlay").boolValue = true;
        vfxSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(go);
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        Debug.Log($"[BrackeysVFXBuilder] {(recreated ? "CREATED" : "UPDATED in place")}: {skill.Id} {skill.PrefabName} loop={loop.Length} hit={hit.Length} scale=({s:0.###}, {s:0.###}, 1) maxFramePx={maxFramePx:0.#} -> {prefabPath}");
    }

    private static void VerifySkillPrefab(SkillSpec skill)
    {
        // Re-derive the exact frames the builder must have written (single source of truth).
        Sprite[] loop = LoadStage(skill.Loop, LoopFrameCap);
        Sprite[] hit = LoadStage(skill.Hit, 0);
        ValidateLoopContinuity(skill, loop);

        float maxFramePx = ComputeMaxFramePx(loop, hit);
        float s = Mathf.Clamp(ScaleTargetPx / maxFramePx, MinScale, MaxScale);

        string prefabPath = PrefabFolder + "/" + skill.PrefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: prefab missing: {prefabPath}");
        if (prefab.name != skill.PrefabName)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: prefab name '{prefab.name}' != '{skill.PrefabName}'");

        // Transform localScale == (s, s, 1)
        Vector3 scale = prefab.transform.localScale;
        if (Mathf.Abs(scale.x - s) > 0.001f || Mathf.Abs(scale.y - s) > 0.001f || Mathf.Abs(scale.z - 1f) > 0.001f)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {skill.PrefabName} scale {scale} != ({s}, {s}, 1)");

        // SpriteRenderer sortingOrder + first loop frame sprite
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {prefabPath} missing SpriteRenderer");
        if (sr.sortingOrder != SortingOrder)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {skill.PrefabName} sortingOrder {sr.sortingOrder} != {SortingOrder}");
        if (sr.sprite == null)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {skill.PrefabName} SpriteRenderer.sprite is null");

        // CircleCollider2D isTrigger radius 0.2
        CircleCollider2D col = prefab.GetComponent<CircleCollider2D>();
        if (col == null || !col.isTrigger)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {prefabPath} missing/disabled trigger CircleCollider2D");
        if (Mathf.Abs(col.radius - ColliderRadius) > 0.001f)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {skill.PrefabName} collider radius {col.radius} != {ColliderRadius}");

        // Projectile speed / lifeTime
        Projectile projectile = prefab.GetComponent<Projectile>();
        if (projectile == null)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {prefabPath} missing Projectile");
        SerializedObject projSo = new SerializedObject(projectile);
        CheckFloat(projSo, "speed", Speed);
        CheckFloat(projSo, "lifeTime", LifeTime);

        // SpriteVFXAnimator arrays / fps / autoPlay
        SpriteVFXAnimator vfx = prefab.GetComponent<SpriteVFXAnimator>();
        if (vfx == null)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {prefabPath} has no SpriteVFXAnimator");
        SerializedObject vfxSo = new SerializedObject(vfx);
        CheckSpriteArray(vfxSo, "startFrames", 0);
        CheckSpriteArray(vfxSo, "loopFrames", loop.Length);
        CheckSpriteArray(vfxSo, "hitFrames", hit.Length);
        CheckFloat(vfxSo, "fps", Fps);
        if (!vfxSo.FindProperty("autoPlay").boolValue)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {skill.PrefabName} autoPlay is false");

        Debug.Log($"[BrackeysVFXBuilder] Verify OK: {skill.Id} {skill.PrefabName} scale=({s:0.###}, {s:0.###}, 1) loop={loop.Length} hit={hit.Length} fps={Fps} sortingOrder={SortingOrder}");
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

    private static void CheckSpriteArray(SerializedObject so, string propertyName, int expectedSize)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop.arraySize != expectedSize)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {propertyName}.arraySize={prop.arraySize} != {expectedSize}");
        for (int i = 0; i < prop.arraySize; i++)
        {
            if (prop.GetArrayElementAtIndex(i).objectReferenceValue == null)
                throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {propertyName}[{i}] is null");
        }
    }

    private static void CheckFloat(SerializedObject so, string propertyName, float expected)
    {
        float actual = so.FindProperty(propertyName).floatValue;
        if (Mathf.Abs(actual - expected) > 0.001f)
            throw new System.Exception($"[BrackeysVFXBuilder] Verify FAILED: {propertyName}={actual} != {expected}");
    }

    /// <summary>
    /// Load one stage's frames from a single-texture slice sheet (NOT per-file PNGs —
    /// MagicVFXBuilder.LoadStage :266 is a separated-PNG loader and does not apply here).
    /// Sub-sprites are regex-filtered by `^{Prefix}_(\d+)$`, natural-sorted by int.Parse,
    /// sliced by [First..Last] (clamped + warned), then capped to <paramref name="cap"/>
    /// frames (cap &lt;= 0 = no cap). An empty required stage -> Debug.LogError + throw.
    /// </summary>
    private static Sprite[] LoadStage(SheetStageSpec spec, int cap)
    {
        Sprite[] all = LoadSheetFrames(spec);
        Sprite[] frames = Slice(spec, all, cap);
        if (frames.Length == 0)
        {
            Debug.LogError($"[BrackeysVFXBuilder] required stage '{spec.Prefix}' is EMPTY (sheet={spec.SheetPath}, matches={all.Length}) — aborting this prefab.");
            throw new System.Exception($"Brackeys stage '{spec.Prefix}' has no frames (sheet matches={all.Length}).");
        }
        Debug.Log($"[BrackeysVFXBuilder] stage '{spec.Prefix}': {frames.Length}/{all.Length} frames; first='{frames[0].name}', last='{frames[frames.Length - 1].name}'");
        return frames;
    }

    /// <summary>
    /// Load + regex-filter + NATURAL numeric sort of one sheet's sub-sprites.
    /// String sort would put _10 before _2 — this is the bug the plan flags.
    /// </summary>
    private static Sprite[] LoadSheetFrames(SheetStageSpec spec)
    {
        Regex pattern = BuildFramePattern(spec.Prefix);
        List<Sprite> matches = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(spec.SheetPath))
        {
            Sprite sprite = asset as Sprite;
            if (sprite == null) continue;
            if (pattern.IsMatch(sprite.name)) matches.Add(sprite);
        }

        matches.Sort((a, b) => ParseFrameNumber(a.name, pattern).CompareTo(ParseFrameNumber(b.name, pattern)));
        return matches.ToArray();
    }

    /// <summary>
    /// Slice [First..Last] (0-based, inclusive). Out-of-bounds values clamp to the
    /// valid range with a Debug.LogWarning; null First/Last = full range. Then cap the
    /// result length to <paramref name="cap"/> frames when cap &gt; 0.
    /// </summary>
    private static Sprite[] Slice(SheetStageSpec spec, Sprite[] frames, int cap)
    {
        int count = frames.Length;
        if (count == 0) return frames;

        int first = 0;
        int last = count - 1;

        if (spec.First.HasValue)
        {
            int requested = spec.First.Value;
            if (requested < 0)
            {
                Debug.LogWarning($"[BrackeysVFXBuilder] {spec.Prefix}: First={requested} < 0; clamping to 0.");
                requested = 0;
            }
            else if (requested > last)
            {
                Debug.LogWarning($"[BrackeysVFXBuilder] {spec.Prefix}: First={requested} out of bounds (frames={count}); clamping to {last}.");
                requested = last;
            }
            first = requested;
        }

        if (spec.Last.HasValue)
        {
            int requested = spec.Last.Value;
            if (requested < 0)
            {
                Debug.LogWarning($"[BrackeysVFXBuilder] {spec.Prefix}: Last={requested} < 0; clamping to 0.");
                requested = 0;
            }
            else if (requested > last)
            {
                Debug.LogWarning($"[BrackeysVFXBuilder] {spec.Prefix}: Last={requested} out of bounds (frames={count}); clamping to {last}.");
                requested = last;
            }
            last = requested;
        }

        if (last < first) return new Sprite[0]; // inverted slice -> empty (caught by LoadStage)

        int sliceCount = last - first + 1;
        if (cap > 0 && sliceCount > cap) sliceCount = cap;

        Sprite[] result = new Sprite[sliceCount];
        for (int i = 0; i < sliceCount; i++)
        {
            result[i] = frames[first + i];
        }
        return result;
    }

    /// <summary>
    /// F8 visual-continuity gate: the loop's frame numbers must be naturally sorted,
    /// contiguous (no gaps), and the first frame must differ from the last (a 1-frame
    /// loop cannot animate). Throws when violated so a bad slice never reaches a prefab.
    /// </summary>
    private static void ValidateLoopContinuity(SkillSpec skill, Sprite[] loopFrames)
    {
        Regex pattern = BuildFramePattern(skill.Loop.Prefix);

        if (loopFrames.Length < 2)
            throw new System.Exception($"[BrackeysVFXBuilder] F8 gate FAILED: {skill.PrefabName} loop has {loopFrames.Length} frame(s); need >= 2 for a continuous animation.");

        int first = ParseFrameNumber(loopFrames[0].name, pattern);
        int prev = first;
        for (int i = 1; i < loopFrames.Length; i++)
        {
            int cur = ParseFrameNumber(loopFrames[i].name, pattern);
            if (cur <= prev)
                throw new System.Exception($"[BrackeysVFXBuilder] F8 gate FAILED: {skill.PrefabName} loop frame {i} '{loopFrames[i].name}' is not strictly after frame {i - 1} '{loopFrames[i - 1].name}' (not naturally sorted).");
            if (cur != prev + 1)
                throw new System.Exception($"[BrackeysVFXBuilder] F8 gate FAILED: {skill.PrefabName} loop gap between '{loopFrames[i - 1].name}' ({prev}) and '{loopFrames[i].name}' ({cur}) — must be contiguous.");
            prev = cur;
        }
        if (first == prev)
            throw new System.Exception($"[BrackeysVFXBuilder] F8 gate FAILED: {skill.PrefabName} loop first frame == last frame (single-frame animation).");

        Debug.Log($"[BrackeysVFXBuilder] F8 gate OK: {skill.PrefabName} loop frames {first}..{prev} contiguous ({loopFrames.Length} frames).");
    }

    /// <summary>
    /// Max frame side (px) across the skill's loop + hit frames, from sprite.textureRect.
    /// Feeds the per-skill scale: s = clamp(21 / maxFramePx, 0.05, 1.0).
    /// </summary>
    private static float ComputeMaxFramePx(Sprite[] loopFrames, Sprite[] hitFrames)
    {
        float max = 0f;
        foreach (Sprite sprite in loopFrames)
            max = Mathf.Max(max, Mathf.Max(sprite.textureRect.width, sprite.textureRect.height));
        foreach (Sprite sprite in hitFrames)
            max = Mathf.Max(max, Mathf.Max(sprite.textureRect.width, sprite.textureRect.height));
        return max;
    }

    private static Regex BuildFramePattern(string prefix)
    {
        // ^Prefix_(\d+)$ — e.g. "explosion_6x5_0". Regex.Escape keeps Prefix literal.
        return new Regex("^" + Regex.Escape(prefix) + "_(\\d+)$");
    }

    private static int ParseFrameNumber(string spriteName, Regex pattern)
    {
        return int.Parse(pattern.Match(spriteName).Groups[1].Value);
    }
}
