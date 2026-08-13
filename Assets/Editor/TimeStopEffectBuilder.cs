using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

/// <summary>
/// TimeWarp_Effect 프리팹 빌더 (Timestop 스타일 마법 스킬 — Todo 3).
///
/// Pipoya VFX TimeMagic 209 시트(192x192)의 15개 서브스프라이트(_0~_14)를
/// 자연수 정렬(int.Parse — 문자열 정렬 금지: _10 이 _2 보다 앞에 오는 오류)로
/// 로드해 TimeWarp_Effect.prefab 을 구성한다:
///   Transform scale (5,5,1) / SpriteRenderer sortingOrder 20 + 첫 프레임
///   TimeStopEffect(radius 15, stunDuration 3, lifeTime 1.5)
///   SimpleSpriteAnimator(15 프레임, fps 12, loop false)
/// 그리고 227_TimeWarp 어셋의 Icon 을 209 시트 스프라이트로 설정한다.
/// 멱등: 기존 프리팹은 SaveAsPrefabAsset 으로 제자리 덮어쓰기(GUID 유지) —
/// 파일 시스템 삭제/재생성은 하지 않는다.
/// 배치모드 진입점: TimeStopEffectBuilder.BuildTimeWarpEffect
/// </summary>
public static class TimeStopEffectBuilder
{
    private const string SheetPath = "Assets/Sprite/vfx/Pipoya VFX TimeMagic/Pipoya VFX TimeMagic/192x192/pipo-btleffect209_192.png";
    private const string PrefabPath = "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab";
    private const string SkillDataPath = "Assets/Resources/SkillData/227_TimeWarp.asset";

    private const int ExpectedFrameCount = 15;
    private const float Fps = 12f;
    private const float Radius = 15f;
    private const float StunDuration = 3f;
    private const float LifeTime = 1.5f;

    private static readonly Regex FrameNamePattern = new Regex(@"^pipo-btleffect209_192_(\d+)$");

    // ------------------------------------------------------------------
    // Entry points
    // ------------------------------------------------------------------

    /// <summary>Build + set icon + verify in one call — the batchmode entry point.</summary>
    public static void BuildTimeWarpEffect()
    {
        BuildTimeWarpEffectInternal();
        VerifyTimeWarpEffect();
        Debug.Log("PASSED: TimeWarp_Effect.prefab built (15 frames, fps=12, radius=15, stun=3, life=1.5, icon set)");
    }

    [MenuItem("Custom Tools/tiger/Time Stop/Build TimeWarp Effect Prefab")]
    public static void BuildTimeWarpEffectMenu()
    {
        BuildTimeWarpEffect();
    }

    // ------------------------------------------------------------------
    // Implementation
    // ------------------------------------------------------------------

    private static void BuildTimeWarpEffectInternal()
    {
        Sprite[] frames = LoadSortedFrames();
        if (frames.Length != ExpectedFrameCount)
        {
            Debug.LogError($"[TimeStopEffectBuilder] expected {ExpectedFrameCount} frames, got {frames.Length} — aborting (no prefab written).");
            throw new System.Exception($"[TimeStopEffectBuilder] 209 sheet has {frames.Length} frames, expected {ExpectedFrameCount}.");
        }
        Debug.Log($"[TimeStopEffectBuilder] frames in order: {string.Join(", ", Array.ConvertAll(frames, f => f.name))}");

        // (b) GameObject composition — template: TimeStop_Effect.prefab.
        GameObject go = new GameObject("TimeWarp_Effect");

        // Transform localScale (5,5,1)
        go.transform.localScale = new Vector3(5f, 5f, 1f);

        // SpriteRenderer sortingOrder 20 + sprite = first frame
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 20;
        sr.sprite = frames[0];

        // TimeStopEffect (private serialized fields via SerializedObject)
        TimeStopEffect effect = go.AddComponent<TimeStopEffect>();
        SerializedObject effectSo = new SerializedObject(effect);
        effectSo.FindProperty("radius").floatValue = Radius;
        effectSo.FindProperty("stunDuration").floatValue = StunDuration;
        effectSo.FindProperty("lifeTime").floatValue = LifeTime;
        effectSo.ApplyModifiedPropertiesWithoutUndo();

        // SimpleSpriteAnimator — frames (natural-sorted), fps, loop=false
        SimpleSpriteAnimator animator = go.AddComponent<SimpleSpriteAnimator>();
        SerializedObject animSo = new SerializedObject(animator);
        SerializedProperty framesProp = animSo.FindProperty("frames");
        framesProp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
        {
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        animSo.FindProperty("fps").floatValue = Fps;
        animSo.FindProperty("loop").boolValue = false;
        animSo.ApplyModifiedPropertiesWithoutUndo();

        // (c) Prefab save — idempotent: overwrite in place so the GUID is preserved.
        EditorUtility.SetDirty(go);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        // (d) 227 icon.
        SetTimeWarpIcon();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TimeStopEffectBuilder] built {PrefabPath} ({frames.Length} frames) + 227 icon set.");
    }

    /// <summary>
    /// Load the 209 sheet's sub-sprites, natural-numeric sorted (_0.._14).
    /// String sort would put _10 before _2 — this is the bug the plan flags.
    /// </summary>
    private static Sprite[] LoadSortedFrames()
    {
        List<Sprite> matches = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
        {
            Sprite sprite = asset as Sprite;
            if (sprite == null) continue;
            if (FrameNamePattern.IsMatch(sprite.name)) matches.Add(sprite);
        }

        // NATURAL numeric sort via int.Parse.
        matches.Sort((a, b) => ParseFrameNumber(a.name).CompareTo(ParseFrameNumber(b.name)));

        return matches.ToArray();
    }

    private static int ParseFrameNumber(string spriteName)
    {
        return int.Parse(FrameNamePattern.Match(spriteName).Groups[1].Value);
    }

    private static void SetTimeWarpIcon()
    {
        SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(SkillDataPath);
        if (skill == null)
            throw new System.Exception($"[TimeStopEffectBuilder] SkillData asset missing: {SkillDataPath}");

        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SheetPath);
        if (iconSprite == null)
            throw new System.Exception($"[TimeStopEffectBuilder] 209 sheet sprite not found: {SheetPath}");

        SerializedObject so = new SerializedObject(skill);
        so.FindProperty("Icon").objectReferenceValue = iconSprite;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
    }

    /// <summary>
    /// Verify the written prefab + icon on disk. Throws on any failure so that a
    /// batchmode -executeMethod run exits non-zero.
    /// </summary>
    private static void VerifyTimeWarpEffect()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: prefab missing: {PrefabPath}");

        Vector3 scale = prefab.transform.localScale;
        if (Mathf.Abs(scale.x - 5f) > 0.001f || Mathf.Abs(scale.y - 5f) > 0.001f || Mathf.Abs(scale.z - 1f) > 0.001f)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: scale {scale} != (5,5,1)");

        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: no SpriteRenderer");
        if (sr.sortingOrder != 20)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: sortingOrder {sr.sortingOrder} != 20");

        TimeStopEffect effect = prefab.GetComponent<TimeStopEffect>();
        if (effect == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: no TimeStopEffect");
        SerializedObject effectSo = new SerializedObject(effect);
        CheckFloat(effectSo, "radius", Radius);
        CheckFloat(effectSo, "stunDuration", StunDuration);
        CheckFloat(effectSo, "lifeTime", LifeTime);

        SimpleSpriteAnimator animator = prefab.GetComponent<SimpleSpriteAnimator>();
        if (animator == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: no SimpleSpriteAnimator");
        SerializedObject animSo = new SerializedObject(animator);
        SerializedProperty framesProp = animSo.FindProperty("frames");
        if (framesProp.arraySize != ExpectedFrameCount)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: frames={framesProp.arraySize} != {ExpectedFrameCount}");
        for (int i = 0; i < framesProp.arraySize; i++)
        {
            Sprite f = framesProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            if (f == null)
                throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: frame {i} is null");
            if (ParseFrameNumber(f.name) != i)
                throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: frame {i} is '{f.name}' — not in numeric order");
        }
        CheckFloat(animSo, "fps", Fps);
        if (animSo.FindProperty("loop").boolValue)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: loop should be false");

        SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(SkillDataPath);
        if (skill == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: 227 asset missing: {SkillDataPath}");
        Sprite icon = new SerializedObject(skill).FindProperty("Icon").objectReferenceValue as Sprite;
        if (icon == null)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: 227 Icon not set");
        if (AssetDatabase.GetAssetPath(icon) != SheetPath)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: 227 Icon references {AssetDatabase.GetAssetPath(icon)}, expected {SheetPath}");

        float fps = animSo.FindProperty("fps").floatValue;
        bool loop = animSo.FindProperty("loop").boolValue;
        float radius = effectSo.FindProperty("radius").floatValue;
        float stun = effectSo.FindProperty("stunDuration").floatValue;
        float life = effectSo.FindProperty("lifeTime").floatValue;
        Debug.Log($"[TimeStopEffectBuilder] Verify OK: frames={framesProp.arraySize} fps={fps} loop={loop} radius={radius} stun={stun} life={life} icon='{icon.name}'");
    }

    private static void CheckFloat(SerializedObject so, string propertyName, float expected)
    {
        float actual = so.FindProperty(propertyName).floatValue;
        if (Mathf.Abs(actual - expected) > 0.001f)
            throw new System.Exception($"[TimeStopEffectBuilder] Verify FAILED: {propertyName}={actual} != {expected}");
    }
}
