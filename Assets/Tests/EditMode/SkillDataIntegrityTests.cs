using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Todo-6 데이터 무결성 테스트.
/// SkillData / Projectile / MeleeHitbox 는 ClubGame.Combat asmdef 안에 있으므로 직접 참조한다.
/// SkillPreset / PlayerController 는 predefined Assembly-CSharp 에 있으므로
/// (asmdef -> predefined 역참조는 Unity 제한) SerializedObject 로 우회 접근한다.
/// </summary>
public class SkillDataIntegrityTests
{
    private const string SkillDataFolder = "Assets/Resources/SkillData";
    private const string PresetsFolder = "Assets/Resources/SkillPresets";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    private static readonly string[] CanonicalAssetNames =
    {
        "201_Slash.asset", "202_HeavyStrike.asset", "203_Whirlwind.asset",
        "211_GumShot.asset", "212_StickyBlob.asset", "213_BigBubble.asset", "214_PopTrap.asset",
        "221_FireBall.asset", "222_IceBlast.asset", "223_ThunderBolt.asset",
        "224_DarkBolt.asset", "225_Holy.asset", "226_Acid.asset",
        "227_TimeWarp.asset",
    };

    /// <summary>스킬 ID -> 기대 ProjectilePrefab 경로 (캐노니컬 링크 13개).</summary>
    private static readonly Dictionary<int, string> CanonicalPrefabLinks = new Dictionary<int, string>
    {
        { 201, "Assets/Prefabs/Projectiles/MeleeHitbox.prefab" },
        { 202, "Assets/Prefabs/Projectiles/MeleeHitbox.prefab" },
        { 203, "Assets/Prefabs/Projectiles/MeleeHitbox.prefab" },
        { 211, "Assets/Prefabs/BubbleProjectile_blue.prefab" },
        { 212, "Assets/Prefabs/BubbleProjectile_red.prefab" },
        { 213, "Assets/Prefabs/BubbleProjectile_yellow.prefab" },
        { 214, "Assets/Prefabs/BubbleProjectile_blue.prefab" },
        { 221, "Assets/Prefabs/Projectiles/FireBallProjectile.prefab" },
        { 222, "Assets/Prefabs/Projectiles/IceBlastProjectile.prefab" },
        { 223, "Assets/Prefabs/Projectiles/ThunderBoltProjectile.prefab" },
        { 224, "Assets/Prefabs/Projectiles/DarkBoltProjectile.prefab" },
        { 225, "Assets/Prefabs/Projectiles/HolyProjectile.prefab" },
        { 226, "Assets/Prefabs/Projectiles/AcidProjectile.prefab" },
    };

    // ------------------------------------------------------------------
    // 1. SkillInventoryClean
    // ------------------------------------------------------------------
    [Test]
    public void SkillInventoryClean()
    {
        // 루트에 정확히 15개 에셋 (하위 폴더 제외) — 14개 캐노니컬 + 227_TimeWarp(신규) + 301_TimeStop
        List<string> rootPaths = GetRootSkillDataPaths();
        Assert.AreEqual(15, rootPaths.Count,
            $"Assets/Resources/SkillData 루트에는 정확히 15개의 SkillData 에셋이 있어야 합니다. 실제: {rootPaths.Count}");

        // 기대하는 14개 캐노니컬 파일이 전부 존재하는지 확인 (301 은 아래에서 별도 확인)
        for (int i = 0; i < CanonicalAssetNames.Length; i++)
        {
            string path = $"{SkillDataFolder}/{CanonicalAssetNames[i]}";
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SkillData>(path),
                $"캐노니컬 스킬 에셋이 없습니다: {path}");
        }
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillDataFolder}/301_TimeStop.asset"),
            "301_TimeStop.asset 이 루트에 없습니다.");

        // SkillData 아래에 하위 폴더가 하나도 없어야 한다 (Magic/Ranged/Melee 삭제 확인)
        string[] subFolders = AssetDatabase.GetSubFolders(SkillDataFolder);
        Assert.AreEqual(0, subFolders.Length,
            $"Assets/Resources/SkillData 아래에 하위 폴더가 있으면 안 됩니다: {string.Join(", ", subFolders)}");

        // 명시적으로 Magic / Ranged / Melee 폴더가 존재하지 않아야 한다
        Assert.IsFalse(AssetDatabase.IsValidFolder($"{SkillDataFolder}/Magic"), "Magic 폴더가 남아있습니다.");
        Assert.IsFalse(AssetDatabase.IsValidFolder($"{SkillDataFolder}/Ranged"), "Ranged 폴더가 남아있습니다.");
        Assert.IsFalse(AssetDatabase.IsValidFolder($"{SkillDataFolder}/Melee"), "Melee 폴더가 남아있습니다.");
    }

    // ------------------------------------------------------------------
    // 2. SkillIdsUnique
    // ------------------------------------------------------------------
    [Test]
    public void SkillIdsUnique()
    {
        Dictionary<int, string> seen = new Dictionary<int, string>();
        string[] guids = AssetDatabase.FindAssets("t:SkillData");
        Assert.GreaterOrEqual(guids.Length, 15, "프로젝트에 최소 15개의 SkillData 에셋이 있어야 합니다.");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.LoadAssetAtPath<SkillData>(path) is not SkillData skill) continue;

            Assert.IsFalse(seen.ContainsKey(skill.ID),
                $"SkillData ID 중복: ID {skill.ID} 가 '{seen.GetValueOrDefault(skill.ID)}' 와 '{path}' 에 모두 존재합니다.");
            seen[skill.ID] = path;
        }
    }

    // ------------------------------------------------------------------
    // 3. CanonicalSkillsWired
    // ------------------------------------------------------------------
    [Test]
    public void CanonicalSkillsWired()
    {
        foreach (int id in CanonicalPrefabLinks.Keys)
        {
            SkillData skill = LoadRootSkillById(id);
            Assert.IsNotNull(skill, $"ID {id} 에 해당하는 루트 스킬 에셋이 없습니다.");

            // ProjectilePrefab 이 연결되어 있고 캐노니컬 경로를 가리켜야 한다
            Assert.IsNotNull(skill.ProjectilePrefab, $"ID {id} 스킬의 ProjectilePrefab 이 null 입니다.");
            Assert.AreEqual(CanonicalPrefabLinks[id], AssetDatabase.GetAssetPath(skill.ProjectilePrefab),
                $"ID {id} 스킬의 ProjectilePrefab 경로가 캐노니컬 링크와 다릅니다.");

            // SkillType (Todo-4 테이블): 211-214 & 221-226 = Projectile, 201-202 = Melee, 203 = MeleeAoE
            SkillType expectedType = id >= 211 ? SkillType.Projectile
                : id == 201 || id == 202 ? SkillType.Melee
                : SkillType.MeleeAoE;
            Assert.AreEqual(expectedType, skill.SkillType,
                $"ID {id} 스킬의 SkillType 이 기대값과 다릅니다. (기대: {expectedType}, 실제: {skill.SkillType})");

            // 거품 필드: 212=Red, 213=Yellow, 222=Red, 그 외 전부 UseBubbleEffect=false
            bool expectedBubble = id == 212 || id == 213 || id == 222;
            Assert.AreEqual(expectedBubble, skill.UseBubbleEffect,
                $"ID {id} 스킬의 UseBubbleEffect 가 기대값({expectedBubble})과 다릅니다.");

            if (expectedBubble)
            {
                Projectile.BubbleType expectedType2 =
                    id == 213 ? Projectile.BubbleType.Yellow : Projectile.BubbleType.Red;
                Assert.AreEqual(expectedType2, skill.BubbleEffect,
                    $"ID {id} 스킬의 BubbleEffect 가 기대값({expectedType2})과 다릅니다.");
            }
        }
    }

    // ------------------------------------------------------------------
    // 3b. MagicVFXAnimatorWired
    // ------------------------------------------------------------------
    [Test]
    public void MagicVFXAnimatorWired()
    {
        // 마법 투사체 221-226 6종 전부 SpriteVFXAnimator 가 배선돼 있어야 한다.
        // frames/fps 는 [SerializeField] private 이므로 SerializedObject 로 읽는다
        // (Todo-3 MagicVFXBuilder 도 같은 경로로 쓰므로 읽기가 보장된다).
        for (int id = 221; id <= 226; id++)
        {
            SkillData skill = LoadRootSkillById(id);
            Assert.IsNotNull(skill, $"ID {id} 에 해당하는 루트 스킬 에셋이 없습니다.");
            Assert.IsNotNull(skill.ProjectilePrefab, $"ID {id} 스킬의 ProjectilePrefab 이 null 입니다.");

            SpriteVFXAnimator vfx = skill.ProjectilePrefab.GetComponentInChildren<SpriteVFXAnimator>(true);
            Assert.IsNotNull(vfx, $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 SpriteVFXAnimator 가 없습니다.");

            SerializedObject so = new SerializedObject(vfx);

            SerializedProperty loop = so.FindProperty("loopFrames");
            Assert.IsNotNull(loop, $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 loopFrames 필드가 없습니다.");
            Assert.Greater(loop.arraySize, 0, $"ID {id} 프리팹의 loopFrames 가 비어 있습니다.");

            SerializedProperty hit = so.FindProperty("hitFrames");
            Assert.IsNotNull(hit, $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 hitFrames 필드가 없습니다.");
            Assert.Greater(hit.arraySize, 0, $"ID {id} 프리팹의 hitFrames 가 비어 있습니다.");

            SerializedProperty fps = so.FindProperty("fps");
            Assert.IsNotNull(fps, $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 fps 필드가 없습니다.");
            Assert.Greater(fps.floatValue, 0f, $"ID {id} 프리팹의 fps 가 0 이하입니다.");

            // start 는 221/222/225 에만 필수 (223/224/226 은 의도적으로 비어 있을 수 있음)
            if (id == 221 || id == 222 || id == 225)
            {
                SerializedProperty start = so.FindProperty("startFrames");
                Assert.IsNotNull(start, $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 startFrames 필드가 없습니다.");
                Assert.Greater(start.arraySize, 0,
                    $"ID {id} 프리팹의 startFrames 가 비어 있습니다 (221/222/225 는 start 필수).");
            }
        }
    }

    // ------------------------------------------------------------------
    // 4. SkillPrefabStructure
    // ------------------------------------------------------------------
    [Test]
    public void SkillPrefabStructure()
    {
        foreach (int id in CanonicalPrefabLinks.Keys)
        {
            SkillData skill = LoadRootSkillById(id);
            Assert.IsNotNull(skill, $"ID {id} 에 해당하는 루트 스킬 에셋이 없습니다.");
            Assert.IsNotNull(skill.ProjectilePrefab, $"ID {id} 스킬의 ProjectilePrefab 이 null 입니다.");

            GameObject prefab = skill.ProjectilePrefab;

            if (id >= 211)
            {
                // 투사체 스킬: Projectile 컴포넌트 + 논-null 스프라이트 + 트리거 콜라이더
                Projectile projectile = prefab.GetComponentInChildren<Projectile>(true);
                Assert.IsNotNull(projectile, $"ID {id} 프리팹({prefab.name})에 Projectile 컴포넌트가 없습니다.");

                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(sr, $"ID {id} 프리팹({prefab.name})에 SpriteRenderer 가 없습니다.");
                Assert.IsNotNull(sr.sprite, $"ID {id} 프리팹({prefab.name})의 스프라이트가 null 입니다.");

                Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>(true);
                bool hasTrigger = false;
                foreach (Collider2D c in colliders) { if (c != null && c.isTrigger) { hasTrigger = true; break; } }
                Assert.IsTrue(hasTrigger, $"ID {id} 프리팹({prefab.name})에 트리거 콜라이더가 없습니다.");
            }
            else
            {
                // 근접 스킬 (201/202/203): MeleeHitbox 컴포넌트
                MeleeHitbox hitbox = prefab.GetComponentInChildren<MeleeHitbox>(true);
                Assert.IsNotNull(hitbox, $"ID {id} 프리팹({prefab.name})에 MeleeHitbox 컴포넌트가 없습니다.");
            }
        }
    }

    // ------------------------------------------------------------------
    // 5. PlayerEquipsGumMaster
    // ------------------------------------------------------------------
    [Test]
    public void PlayerEquipsGumMaster()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Assert.IsNotNull(playerPrefab, $"Player 프리팹을 찾을 수 없습니다: {PlayerPrefabPath}");

        // PlayerController 는 Assembly-CSharp 타입 -> SerializedObject 로 EquippedSkills 접근
        List<string> equippedPaths = new List<string>();
        Component[] components = playerPrefab.GetComponentsInChildren<Component>(true);
        foreach (Component c in components)
        {
            if (c == null) continue;
            if (c.GetType().Name != "PlayerController") continue;

            SerializedObject so = new SerializedObject(c);
            // EquippedSkills 는 PlayerController.combatSettings (중첩 CombatSettings 클래스) 안에 있다
            SerializedProperty combat = so.FindProperty("combatSettings");
            Assert.IsNotNull(combat, "PlayerController에 combatSettings 필드가 없습니다.");
            SerializedProperty skills = combat.FindPropertyRelative("EquippedSkills");
            Assert.IsNotNull(skills, "PlayerController.combatSettings 에 EquippedSkills 필드가 없습니다.");

            for (int i = 0; i < skills.arraySize; i++)
            {
                Object refObj = skills.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(refObj, $"EquippedSkills[{i}] 참조가 null 입니다.");
                equippedPaths.Add(AssetDatabase.GetAssetPath(refObj));
            }
            break;
        }

        // GumMaster 4종: 211/212/213/214 (경로 비교)
        Assert.AreEqual(4, equippedPaths.Count,
            $"EquippedSkills 는 정확히 4개여야 합니다. 실제: {equippedPaths.Count}");

        HashSet<string> expected = new HashSet<string>
        {
            $"{SkillDataFolder}/211_GumShot.asset",
            $"{SkillDataFolder}/212_StickyBlob.asset",
            $"{SkillDataFolder}/213_BigBubble.asset",
            $"{SkillDataFolder}/214_PopTrap.asset",
        };
        foreach (string p in equippedPaths)
        {
            Assert.IsTrue(expected.Contains(p), $"EquippedSkills 가 루트 GumMaster 스킬이 아닙니다: {p}");
        }
    }

    // ------------------------------------------------------------------
    // 6. TimeStopUntouched
    // ------------------------------------------------------------------
    [Test]
    public void TimeStopUntouched()
    {
        SkillData timeStop = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillDataFolder}/301_TimeStop.asset");
        Assert.IsNotNull(timeStop, "301_TimeStop.asset 을 로드할 수 없습니다.");

        Assert.AreEqual(301, timeStop.ID, "301 스킬의 ID 가 301이 아닙니다.");
        Assert.AreEqual(SkillType.InstantArea, timeStop.SkillType, "301 스킬의 SkillType 이 InstantArea(3)가 아닙니다.");
        Assert.AreEqual("Time Stop", timeStop.SkillName, "301 스킬의 SkillName 이 'Time Stop'이 아닙니다.");
        Assert.AreEqual(50f, timeStop.ManaCost, "301 스킬의 ManaCost 가 50이 아닙니다.");
        Assert.AreEqual(15f, timeStop.Cooldown, "301 스킬의 Cooldown 이 15가 아닙니다.");
        Assert.IsNotNull(timeStop.ProjectilePrefab, "301 스킬의 ProjectilePrefab 이 null 입니다.");
        Assert.AreEqual("Assets/Prefabs/Projectiles/TimeStop_Effect.prefab",
            AssetDatabase.GetAssetPath(timeStop.ProjectilePrefab),
            "301 스킬의 ProjectilePrefab 이 TimeStop_Effect.prefab 을 가리키지 않습니다.");
    }

    // ------------------------------------------------------------------
    // 6b. InstantAreaSkillsWired
    // ------------------------------------------------------------------
    [Test]
    public void InstantAreaSkillsWired()
    {
        // InstantArea 스킬 301/227: 프리팹 링크 + TimeStopEffect 스크립트 존재 계약.
        // TimeStopEffect 는 Assembly-CSharp 타입이라 asmdef 테스트에서 직접 참조 불가
        // -> MonoScript + 각 컴포넌트 SerializedObject 의 m_Script 참조 비교로 우회.
        (int id, string expectedPath)[] cases =
        {
            (301, "Assets/Prefabs/Projectiles/TimeStop_Effect.prefab"),
            (227, "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab"),
        };

        MonoScript timeStopScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Script/TimeStopEffect.cs");
        Assert.IsNotNull(timeStopScript, "TimeStopEffect.cs MonoScript 를 로드할 수 없습니다.");

        foreach ((int id, string expectedPath) in cases)
        {
            SkillData skill = LoadRootSkillById(id);
            Assert.IsNotNull(skill, $"ID {id} 에 해당하는 루트 스킬 에셋이 없습니다.");
            Assert.AreEqual(SkillType.InstantArea, skill.SkillType,
                $"ID {id} 스킬의 SkillType 이 InstantArea(3)가 아닙니다.");
            Assert.IsNotNull(skill.ProjectilePrefab, $"ID {id} 스킬의 ProjectilePrefab 이 null 입니다.");
            Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(skill.ProjectilePrefab),
                $"ID {id} 스킬의 ProjectilePrefab 경로가 기대값과 다릅니다.");

            Component[] components = skill.ProjectilePrefab.GetComponentsInChildren<Component>(true);
            bool hasTimeStopEffect = false;
            foreach (Component c in components)
            {
                if (c == null) continue;
                SerializedObject so = new SerializedObject(c);
                SerializedProperty script = so.FindProperty("m_Script");
                if (script == null || script.objectReferenceValue == null) continue;
                if (script.objectReferenceValue == timeStopScript) { hasTimeStopEffect = true; break; }
            }
            Assert.IsTrue(hasTimeStopEffect,
                $"ID {id} 프리팹({skill.ProjectilePrefab.name})에 TimeStopEffect 컴포넌트가 없습니다.");
        }
    }

    // ------------------------------------------------------------------
    // 7. PresetsResolveToRoot
    // ------------------------------------------------------------------
    [Test]
    public void PresetsResolveToRoot()
    {
        string[] presetGuids = AssetDatabase.FindAssets("t:SkillPreset", new[] { PresetsFolder });
        Assert.GreaterOrEqual(presetGuids.Length, 4, "SkillPresets 폴더에 최소 4개의 프리셋이 있어야 합니다.");

        HashSet<string> rootPaths = new HashSet<string>(GetRootSkillDataPaths());

        foreach (string guid in presetGuids)
        {
            string presetPath = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject preset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(presetPath);
            Assert.IsNotNull(preset, $"프리셋을 로드할 수 없습니다: {presetPath}");

            SerializedObject so = new SerializedObject(preset);
            SerializedProperty skills = so.FindProperty("skills");
            Assert.IsNotNull(skills, $"프리셋 {presetPath} 에 skills 필드가 없습니다.");

            for (int i = 0; i < skills.arraySize; i++)
            {
                Object refObj = skills.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(refObj, $"프리셋 {presetPath} 의 skills[{i}] 참조가 null 입니다.");
                Assert.IsTrue(refObj is SkillData,
                    $"프리셋 {presetPath} 의 skills[{i}] 가 SkillData 가 아닙니다.");

                string refPath = AssetDatabase.GetAssetPath(refObj);
                Assert.IsTrue(rootPaths.Contains(refPath),
                    $"프리셋 {presetPath} 이 루트가 아닌 에셋을 참조합니다: {refPath}");
            }
        }
    }

    // ------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------

    /// <summary>Assets/Resources/SkillData 루트에 있는 SkillData 에셋 경로 목록.</summary>
    private static List<string> GetRootSkillDataPaths()
    {
        List<string> rootPaths = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { SkillDataFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(SkillDataFolder + "/")) continue;
            string rest = path.Substring(SkillDataFolder.Length + 1);
            if (rest.Contains("/")) continue; // 하위 폴더 에셋 제외
            rootPaths.Add(path);
        }
        return rootPaths;
    }

    /// <summary>루트 SkillData 폴더에서 ID 로 SkillData 를 찾는다. 없으면 null.</summary>
    private static SkillData LoadRootSkillById(int id)
    {
        foreach (string path in GetRootSkillDataPaths())
        {
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null && skill.ID == id) return skill;
        }
        return null;
    }
}
