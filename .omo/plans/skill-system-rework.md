# skill-system-rework - Work Plan

## TL;DR (For humans)

**What you'll get:** The four attack skills (ZXCV) will actually work again. Each key fires a different attack — a basic shot, a slowing sticky blob, a stunning big bubble, and a trap — each on its own cooldown with proper visuals. The skill list is cleaned up (broken and duplicate entries removed) and the player is equipped with those 4 working skills.

**Why this approach:** The skills were broken because the game code asked for a projectile pool that doesn't exist and the skill files had empty or mismatched references. Instead of hand-patching each skill, a spreadsheet becomes the single source of truth, all skill data is generated from it, and the game picks the right behavior from each skill's type — one code change fixes every skill at once and keeps them consistent.

**What it will NOT do:** Your TimeStop experiment stays exactly as it is (byte for byte). No mana system, no new enemies, no changes to the normal gun, no attack animations.

**Effort:** Medium — 6 focused work items (data model, combat code, visuals, spreadsheet + importer, cleanup, automated tests).
**Risk:** Medium - touches the player's core combat code and every skill asset; every step is verified by automated tests that run headlessly.
**Decisions to sanity-check:** (1) The exact numbers (damage, cooldown, which bubble effect) are taken from the existing data — tweak after playing. (2) 3 melee skills (Slash/HeavyStrike/Whirlwind) are wired and tested but NOT assigned to any key — the equipped set is all-ranged; assign them later if you want melee. (3) Skills are categorized into 4 types (projectile / melee / melee-around-player / instant-area); the 4th type exists as a placeholder only.

Your next move: approve, or run a high-accuracy review. Full execution detail follows below.

---

> TL;DR (machine): Medium effort/risk; 6 commits — SkillType model + defaults test, MeleeHitbox + UseSkill type-dispatch + cooldown gate + PlayMode tests, 3 VFX projectile prefabs, CSV schema 5→10 cols + importer + LinkSkillPrefabs/EquipGumMasterOnPlayer, delete 11 duplicate/junk assets + re-import + Player re-equip (GumMaster 211-214), EditMode integrity suite; verified headless via batchmode EditMode+PlayMode; TimeStop byte-identical.

## Scope
### Must have
- SkillData gets a `SkillType` enum (Projectile / Melee / MeleeAoE / InstantArea) plus per-type fields: `ProjectileSpeed`, `UseBubbleEffect` + `BubbleEffect`, `MeleeRange`, `MeleeArc`, `HitboxLifetime`, `AoERadius`. All existing fields stay untouched.
- `UseSkill` (PlayerController.cs:198) dispatches by `SkillType` and enforces a per-slot cooldown gate (no mana). No code path calls `ObjectPooler.SpawnFromPool("Projectile", ...)` for skills anymore.
- New `MeleeHitbox.cs`: short-lived (0.15s) trigger hitbox, one hit per target, damage + optional bubble effect via `IBubbleAffectable`.
- Prefabs: 3 new VFX projectile prefabs (FireBallProjectile, IceBlastProjectile, ThunderBoltProjectile) + 1 MeleeHitbox prefab under `Assets/Prefabs/Projectiles/`. GumShot/StickyBlob/BigBubble/PopTrap REUSE the existing `Assets/Prefabs/BubbleProjectile_{blue,red,yellow}.prefab`.
- Skill data becomes canonical: root set (10 assets from CSV) fully wired (prefab refs, types, bubble effects); subfolder duplicates deleted; Player.prefab re-equipped with the GumMaster set (211-214) on ZXCV.
- CSV schema extended (`ID,Name,Damage,ManaCost,Cooldown,Type,Bubble,Speed,MeleeRange,MeleeArc`) + `DataImportMenu.cs` importer + new `LinkSkillPrefabs()` and `EquipGumMasterOnPlayer()` editor methods.
- EditMode data-integrity test suite + PlayMode skill-execution tests, run headless via Unity batchmode.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- NO changes to `301_TimeStop.asset`, `Assets/Prefabs/Projectiles/TimeStop_Effect.prefab`, or `Assets/Script/TimeStopEffect.cs` — must remain byte-identical (user's personal experiment; `git diff` on these must be empty at the end).
- NO mana system, NO mana UI.
- NO animation-timed melee (no new Animator states/transitions for attacks).
- NO changes to `TryFire` (PlayerController.cs:236), `ObjectPooler`, the Blue/Red/Yellow pools, or the normal gun behavior.
- NO new enemy types, NO damage-system rework, NO multi-projectile spread rework.
- NO changes outside `Assets/` except the 3 skill CSVs under `tiger/datafiles/skill/` (data source of truth, extended by this plan).
- InstantArea: enum member exists but NO implementation work (no canonical skill uses it).
- New VFX prefabs: NO Animator component (static sprite; bubble wobble stays bubble-only).

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after for code (EditMode model tests + PlayMode execution tests), spec-first integrity suite (Todo 6 encodes the target end-state and must pass green). Framework: com.unity.test-framework 1.6.0 (installed, Packages/manifest.json).
- Unity binary (version 6000.3.12f1, ProjectSettings/ProjectVersion.txt): `"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe"` — verify it exists first; if not, locate via `where Unity.exe` / Hub install dir before running any command.
- Compile check (run after every code todo): `Unity.exe -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile -` → assert log contains NO `error CS`. If `Library/` is missing, run this once as a pure import pass, then again for the check.
- Test run template: `Unity.exe -batchmode -runTests -projectPath "D:\coding\github c\clubgame" -testPlatform EditMode -testResults "D:\coding\github c\clubgame\.omo\evidence\<file>.xml" -logFile -` (PlayMode: `-testPlatform PlayMode`). Pass = XML root `<test-run result="Passed" failed="0" ...>`.
- Evidence: every todo writes its receipt to `.omo/evidence/task-<N>-skill-system-rework.<ext>` (test XML, batchmode log excerpt, git diff output). F-tasks use `.omo/evidence/final-<N>-skill-system-rework.<ext>`.
- First step of the whole plan: `git status --porcelain` → snapshot pre-existing dirty/untracked files into `.omo/evidence/task-0-dirty-worktree.txt`; never commit or overwrite unrelated user changes; keep this list out of scope.

## Execution strategy
### Parallel execution waves
- **Wave 1** (parallel): Todo 1 (SkillData model) · Todo 3 (VFX projectile prefabs) — no shared files.
- **Wave 2** (parallel): Todo 2 (MeleeHitbox.cs + UseSkill dispatch + PlayMode tests) · Todo 4 (CSV schema + importer + editor methods) — both depend on Todo 1, touch disjoint files.
- **Wave 3**: Todo 5 (data cleanup + pipeline run + Player re-equip) — depends on Todo 2 (MeleeHitbox prefab), Todo 3 (VFX prefabs), Todo 4 (importer).
- **Wave 4**: Todo 6 (EditMode integrity suite) — depends on Todo 5 (asserts the end-state).

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 SkillData model | — | 2, 4 | 3 |
| 2 UseSkill+MeleeHitbox | 1 | 5 | 4 |
| 3 VFX prefabs | — | 5 | 1 |
| 4 CSV+importer | 1 | 5 | 2 |
| 5 Cleanup+re-equip | 2, 3, 4 | 6 | — |
| 6 Integrity tests | 5 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [ ] 1. SkillData.cs: add SkillType enum + per-type fields, with EditMode model test
  What to do / Must NOT do: In `Assets/Script/SkillData.cs`, add a TOP-LEVEL enum before the class: `public enum SkillType { Projectile, Melee, MeleeAoE, InstantArea }`. Add these serialized public fields to the class: `public SkillType SkillType;` (default Projectile), `public float ProjectileSpeed = 15f;`, `public bool UseBubbleEffect;`, `public Projectile.BubbleType BubbleEffect;`, `public float MeleeRange = 1.5f;`, `public float MeleeArc = 120f;`, `public float HitboxLifetime = 0.15f;`, `public float AoERadius = 3f;`. Must NOT rename/remove/retype ANY existing field (ID, SkillName, Damage, ManaCost, Cooldown, Icon, ProjectilePrefab, projectileCount, spreadAngle) — serialized .asset data depends on them. Create test infrastructure here: `Assets/Tests/EditMode/ClubGame.EditModeTests.asmdef` (name "ClubGame.EditModeTests", references: ["Assembly-CSharp", "UnityEditor"], optionalUnityReferences: ["TestAssemblies"]) and `Assets/Tests/EditMode/SkillDataModelTests.cs` asserting the defaults of a fresh `ScriptableObject.CreateInstance<SkillData>()`.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2, 4
  References: `Assets/Script/SkillData.cs` (full file, 18 lines — current fields); `Assets/Script/Projectile.cs:5` (BubbleType enum {Blue,Red,Yellow}); `Packages/manifest.json` (test-framework 1.6.0); asmdef template: Unity default "Test Assemblies" (EditMode) — reference Assembly-CSharp (no asmdefs exist in project; scripts compile to default assemblies).
  Acceptance criteria (agent-executable): compile check passes (batchmode log has no `error CS`); `SkillDataModelTests` green under `-testPlatform EditMode` (receipt XML `result="Passed" failed="0"`).
  QA scenarios (exact tool + invocation): happy — batchmode EditMode run (template in Verification strategy) with test asserting defaults; failure — temporarily assert `ProjectileSpeed == 0f` to prove the test actually fails (revert after), evidence `.omo/evidence/task-1-skill-system-rework.xml`.
  Commit: Y | feat(skill): add SkillType enum and per-type fields to SkillData

- [ ] 2. MeleeHitbox.cs + UseSkill type-dispatch + cooldown gate + MeleeHitbox prefab + PlayMode tests
  What to do / Must NOT do:
  (a) New `Assets/Script/MeleeHitbox.cs` (MonoBehaviour): public `void Initialize(float damage, GameObject owner, float lifeTime, float range, bool useBubble, Projectile.BubbleType bubbleType)` — stores fields, sets `transform.localScale = new Vector3(range, range, 1)`; `OnEnable`: `_hitTargets = new HashSet<Health>()`, `Destroy(gameObject, lifeTime)`; `OnTriggerEnter2D`: skip if `collision.gameObject == owner`; get `Health h = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>()`; return if null or already in `_hitTargets`; add to set; `h.TakeDamage(_damage, transform.position)`; if `useBubble`, get `IBubbleAffectable` (component or parent) and call `ApplyBubbleEffect(bubbleType)`. Must NOT use ObjectPooler (direct Instantiate/Destroy).
  (b) `Assets/Script/player/PlayerController.cs`: add field `private float[] _skillLastUsed = new float[4];`, initialize all to `-10f` in Awake. Replace `UseSkill` (lines 198-222): keep slot-range and null-skill guards; ADD cooldown gate FIRST: `if (Time.time - _skillLastUsed[slotIndex] < skill.Cooldown) return;` (does not consume cooldown on blocked attempts). Then `switch (skill.SkillType)`: **Projectile** — if `skill.ProjectilePrefab == null` → `Debug.LogWarning` + return (no cooldown); compute mouse aim as today (lines 205-210); `var obj = Instantiate(skill.ProjectilePrefab, combatSettings.FirePoint.position, Quaternion.Euler(0,0,angle));` and `proj.Initialize(skill.Damage, _isFacingRight, skill.ProjectileSpeed, null, gameObject, skill.BubbleEffect, skill.UseBubbleEffect);` — NOTE the last arg is `skill.UseBubbleEffect` because Projectile applies bubbles only when `isSpecial == true` (Projectile.cs:69,78). **Melee** — spawn position `combatSettings.FirePoint.position + direction * (skill.MeleeRange * 0.6f)`, rotation `Quaternion.Euler(0,0,angle)`; **MeleeAoE** — spawn at `transform.position`, rotation identity (360° around player); both: `Instantiate(skill.ProjectilePrefab, ...)` (melee skills store the hitbox prefab in the SAME `ProjectilePrefab` slot), `GetComponent<MeleeHitbox>().Initialize(skill.Damage, gameObject, skill.HitboxLifetime, skill.MeleeRange, skill.UseBubbleEffect, skill.BubbleEffect)`. **InstantArea** — `Debug.LogWarning("InstantArea skill type not implemented")` + return (no cooldown). On any successful spawn: `_skillLastUsed[slotIndex] = Time.time;` then `if (SkillHUDManager.Instance != null) SkillHUDManager.Instance.TriggerCooldown(slotIndex);`. Must NOT touch `TryFire` (236-295), `HandleInput` (120-173), `ObjectPooler`, `UpdateSkillHUD`.
  (c) New prefab `Assets/Prefabs/Projectiles/MeleeHitbox.prefab`: copy the structure of `Assets/Prefabs/BubbleProjectile_blue.prefab` (Transform + SpriteRenderer sortingOrder 10 + CircleCollider2D isTrigger radius 1) but with sprite `Assets/Sprite/vfx/Hit Effect 01/Hit Effect 01/Hit Effect 01 1.png`, add `MeleeHitbox` script component, NO Animator, NO Projectile.
  (d) Tests: `Assets/Tests/PlayMode/ClubGame.PlayModeTests.asmdef` (name "ClubGame.PlayModeTests", references ["Assembly-CSharp"], optionalUnityReferences ["TestAssemblies"]) + `Assets/Tests/PlayMode/SkillExecutionTests.cs`: build a scene programmatically (ground, enemy GameObject with Health + CircleCollider2D + a test stub `TestBubbleAffectable : MonoBehaviour, IBubbleAffectable` recording last bubble type, player GameObject with PlayerController + Rigidbody2D + Collider2D + Animator); configure the private `combatSettings` (via SerializedObject `FindProperty("combatSettings")`: set `FirePoint` and `EquippedSkills` to real root assets `211_GumShot`, `201_Slash`); invoke private `UseSkill` via `typeof(PlayerController).GetMethod("UseSkill", BindingFlags.NonPublic|BindingFlags.Instance)`; reset `_skillLastUsed` via reflection between tests. Assert: (happy) projectile skill spawns an object with `Projectile` at FirePoint aimed at mouse, enemy `Health.CurrentHealth` decreased, `TriggerCooldown` called; (happy) immediate second `UseSkill(0)` within Cooldown spawns NOTHING (cooldown gate); (happy) melee skill spawns hitbox and damages an enemy exactly once over its lifetime; (failure) a skill with `ProjectilePrefab == null` spawns nothing and does NOT consume cooldown (next call after resetting time still blocked? no — assert `_skillLastUsed` unchanged and no spawn).
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 5
  References: `Assets/Script/player/PlayerController.cs:198-222` (UseSkill to replace), `:236-295` (TryFire — do not touch), `:24-30` (CombatSettings), `:72-91` (Awake — add cooldown init); `Assets/Script/Projectile.cs:16-36` (Initialize signature — 7th param `special` gates bubble), `:57-82` (trigger damage/bubble logic); `Assets/Script/IBubbleAffectable.cs` + `Assets/Script/EnemyController.cs:211-224` (ApplyBubbleEffect: Red=slow 3s, Yellow=stun 1s); `Assets/Script/Health.cs:50-78` (TakeDamage); `Assets/Script/SkillHUDManager.cs:28-34` (TriggerCooldown); `Assets/Prefabs/BubbleProjectile_blue.prefab` (prefab template, full YAML — Projectile script guid `748bc7fe4f5592044adef09a9696c5a8`, collider radius 0.2, trigger); `Assets/Sprite/vfx/Hit Effect 01/Hit Effect 01/Hit Effect 01 1.png`.
  Acceptance criteria (agent-executable): compile check green; `-testPlatform PlayMode` green (`result="Passed" failed="0"`); code review: `SpawnFromPool("Projectile"` appears nowhere in Assets/Script/player/PlayerController.cs.
  QA scenarios: happy — PlayMode suite green, evidence `.omo/evidence/task-2-skill-system-rework.xml`; failure — comment out the cooldown gate, prove the "second call spawns nothing" test fails, revert; evidence `.omo/evidence/task-2-failure-notes.txt`.
  Commit: Y | feat(skill): add MeleeHitbox and SkillType dispatch with cooldown gate

- [ ] 3. New VFX projectile prefabs (FireBall / IceBlast / ThunderBolt)
  What to do / Must NOT do: Create 3 prefabs under `Assets/Prefabs/Projectiles/`, each copied from `Assets/Prefabs/BubbleProjectile_blue.prefab` (GameObject, Transform localScale 3, SpriteRenderer sortingOrder 10, CircleCollider2D isTrigger radius 0.2, Projectile component speed 15 lifeTime 3) with these swaps: sprite → the VFX sprite, `m_Name` → prefab name, REMOVE the Animator component (static sprite):
  - `FireBallProjectile.prefab` — sprite `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/spritesheets/Fire-bomb.png`
  - `IceBlastProjectile.prefab` — sprite `Assets/Sprite/vfx/Ice Effect 01/Ice Effect 01/Ice VFX 2/Ice VFX 2 Active.png`
  - `ThunderBoltProjectile.prefab` — sprite `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/spritesheets/Lightning.png`
  If the source texture is sliced as a spritesheet, assign the FIRST Sprite sub-asset (`AssetDatabase.LoadAllAssetsAtPath` → first `Sprite`); do NOT change texture import settings unless no Sprite resolves. Must NOT modify `Assets/Prefabs/BubbleProjectile_{blue,red,yellow}.prefab`; must NOT create prefabs for GumShot/StickyBlob/BigBubble/PopTrap (they reuse the bubble prefabs).
  Parallelization: Wave 1 | Blocked by: — | Blocks: 5
  References: `Assets/Prefabs/BubbleProjectile_blue.prefab` (template YAML — Projectile script guid `748bc7fe4f5592044adef09a9696c5a8`); sprites: `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/spritesheets/Fire-bomb.png`, `.../spritesheets/Lightning.png`, `Assets/Sprite/vfx/Ice Effect 01/Ice Effect 01/Ice VFX 2/Ice VFX 2 Active.png`.
  Acceptance criteria (agent-executable): all 3 prefabs load via `AssetDatabase.LoadAssetAtPath<GameObject>`; each has a `Projectile` component and a non-null `SpriteRenderer.sprite` and a trigger collider — asserted by the Todo-6 test `SkillPrefabStructure` (runs in Wave 4; for this todo run the same assertions as a one-off EditMode check in batchmode, evidence `.omo/evidence/task-3-skill-system-rework.xml`); `.meta` files exist and are committed.
  QA scenarios: happy — EditMode prefab-load assertions green; failure — delete one prefab, assert the check fails, restore.
  Commit: Y | feat(skill): add FireBall/IceBlast/ThunderBolt projectile prefabs

- [ ] 4. CSV schema + DataImportMenu extension (importer fields, LinkSkillPrefabs, EquipGumMasterOnPlayer)
  What to do / Must NOT do: Extend the 3 skill CSVs (`tiger/datafiles/skill/rangedskill.csv`, `meleeskill.csv`, `magicskill.csv`) with header `ID,Name,Damage,ManaCost,Cooldown,Type,Bubble,Speed,MeleeRange,MeleeArc` and these exact rows (empty = not set):
  - 211,GumShot,12,2,0.4,Projectile,None,15,, | 212,StickyBlob,20,8,1.5,Projectile,Red,8,, | 213,BigBubble,45,15,3.0,Projectile,Yellow,15,, | 214,PopTrap,30,12,2.5,Projectile,None,15,,
  - 201,Slash,10,0,0.5,Melee,None,15,1.5,120 | 202,HeavyStrike,25,10,2.0,Melee,None,15,2.0,90 | 203,Whirlwind,15,15,3.0,MeleeAoE,None,15,2.0,360
  - 221,FireBall,30,15,1.5,Projectile,None,15,, | 222,IceBlast,25,20,2.0,Projectile,Red,12,, | 223,ThunderBolt,45,35,4.0,Projectile,None,15,,
  In `Assets/Editor/DataImportMenu.cs` `ImportSkillFile` (line 214): keep the existing 5-column parse and `data.Length < 5` guard; AFTER it, parse optional columns when present and non-empty: `data[5]` Type via `Enum.TryParse<SkillType>`, `data[6]` Bubble → if not "None" set `UseBubbleEffect = true` + `Enum.TryParse<Projectile.BubbleType>`, `data[7]` Speed, `data[8]` MeleeRange, `data[9]` MeleeArc. Add two new methods + MenuItems ("Custom Tools/tiger/Data Import/Link Skill Prefabs", ".../Equip GumMaster on Player"):
  - `LinkSkillPrefabs()`: iterate root SkillData assets ONLY (`AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/SkillData")`, skip subfolders — filter by path depth or by expected file names); assign `asset.ProjectilePrefab` by ID table — 211→`Assets/Prefabs/BubbleProjectile_blue.prefab`, 212→`BubbleProjectile_red.prefab`, 213→`BubbleProjectile_yellow.prefab`, 214→`BubbleProjectile_blue.prefab`, 221→`Assets/Prefabs/Projectiles/FireBallProjectile.prefab`, 222→`IceBlastProjectile.prefab`, 223→`ThunderBoltProjectile.prefab`, 201/202/203→`Assets/Prefabs/Projectiles/MeleeHitbox.prefab`; `Debug.LogError` + skip if the prefab does not load; `EditorUtility.SetDirty`.
  - `EquipGumMasterOnPlayer()`: load `Assets/Prefabs/Player.prefab`, get `PlayerController`, via `SerializedObject.FindProperty("combatSettings").FindPropertyRelative("EquippedSkills")` clear and append the 4 root assets 211/212/213/214, `PrefabUtility.SavePrefabAsset`.
  Must NOT change `ImportSkillPresets`/`ImportEnemyData`/`ImportBiomeData` behavior; must NOT write any other tiger/ CSV.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 5
  References: `tiger/datafiles/skill/{rangedskill,meleeskill,magicskill}.csv` (current 5-col format); `Assets/Editor/DataImportMenu.cs:196-240` (ImportAll/ImportSkillData/ImportSkillFile), `:242-275` (ImportSkillPresets — note `AssetDatabase.FindAssets($"{id}_ t:SkillData", ...)` prefix search is RECURSIVE, which is why duplicates must be gone before re-import in Todo 5), `:371` (GetOrCreateAsset), `:364-369` (EnsureFolder); `Assets/Script/SkillPreset.cs`; `tiger/datafiles/skill/preset.csv` (GumMaster=211;212;213;214, Warrior=201;202;203, Mage=221;222;223, Hybrid=201;211;221); `Assets/Script/SkillData.cs` (new fields from Todo 1).
  Acceptance criteria (agent-executable): `Unity.exe -batchmode -quit -projectPath ... -executeMethod DataImportMenu.ImportAll -logFile -` exits with no errors in log; after running, root assets carry the table values (verify via a throwaway EditMode check or the T6 assertions); `LinkSkillPrefabs` run assigns non-null `ProjectilePrefab` to all 10 canonical assets.
  QA scenarios: happy — run ImportAll + LinkSkillPrefabs in batchmode, log shows no errors, evidence `.omo/evidence/task-4-skill-system-rework.txt`; failure — temporarily set `Type` to a bogus value in one CSV row, assert importer logs an error and skips the line without crashing, revert CSV.
  Commit: Y | feat(data): extend skill CSV schema and importer with type/bubble/melee fields

- [ ] 5. Data cleanup: delete duplicate/junk assets, run the data pipeline, re-equip Player.prefab
  What to do / Must NOT do: (1) Delete via `AssetDatabase.DeleteAsset` (or git rm including `.meta`): folders `Assets/Resources/SkillData/Magic/`, `.../Ranged/`, `.../Melee/` (9 duplicate assets) and files `Assets/Resources/SkillData/101_Shotgun.asset` + `Assets/Resources/SkillData/NewSkillData.asset` (junk: not in any CSV, zero references — verify with `AssetDatabase.FindAssets`/dependency scan before deleting). (2) MUST NOT delete or modify `Assets/Resources/SkillData/301_TimeStop.asset`. (3) Run the pipeline IN THIS ORDER via batchmode `-executeMethod` (or the editor window): `ImportSkillData` → `LinkSkillPrefabs` → `EquipGumMasterOnPlayer` → `ImportSkillPresets` (preset re-import MUST come after deletion so the recursive `FindAssets($"{id}_ ...")` prefix search cannot match subfolder duplicates). (4) Verify final state: `Assets/Resources/SkillData/` contains exactly 11 assets at root (10 canonical + 301_TimeStop), no subfolders; Player.prefab `EquippedSkills` = the 4 GumMaster root assets; no scene/prefab references any deleted asset (dependency scan clean).
  Parallelization: Wave 3 | Blocked by: 2, 3, 4 | Blocks: 6
  References: asset inventory (glob of `Assets/Resources/SkillData/**/*.asset`): root = NewSkillData, 101_Shotgun, 201_Slash, 202_HeavyStrike, 203_Whirlwind, 211_GumShot, 212_StickyBlob, 213_BigBubble, 214_PopTrap, 221_FireBall, 222_IceBlast, 223_ThunderBolt, 301_TimeStop; subfolders = Magic/{221,222,223}, Ranged/{211_ArrowShot,212_SniperShot,213_TripleShot}, Melee/{201_Slash,202_GreatSwing,203_Stab}; `Assets/Editor/DataImportMenu.cs:205-212` (ImportSkillData), `:242-275` (ImportSkillPresets), Todo-4's new methods; `Assets/Prefabs/Player.prefab` (PlayerController component with `combatSettings` — currently equips ArrowShot/SniperShot/TripleShot); `tiger/datafiles/skill/preset.csv`.
  Acceptance criteria (agent-executable): `git status --porcelain` shows deletions of the 9 subfolder assets + 2 junk assets; `git diff --stat` on `Assets/Resources/SkillData/301_TimeStop.asset` is EMPTY; Player.prefab equips 211/212/213/214 root assets (verify via prefab YAML or SerializedObject); no dangling references to deleted GUIDs anywhere in Assets.
  QA scenarios: happy — run pipeline, run a dependency scan (`AssetDatabase.GetDependencies` over all scenes/prefabs, assert no deleted-GUID references), evidence `.omo/evidence/task-5-skill-system-rework.txt`; failure — temporarily keep `Melee/201_Slash.asset`, re-run `ImportSkillPresets`, show the Hybrid preset resolved to the wrong (subfolder) asset, delete it again (proves why order matters).
  Commit: Y | chore(skill): remove duplicate/junk skill assets and re-equip player with GumMaster set

- [ ] 6. EditMode data-integrity test suite (end-state spec)
  What to do / Must NOT do: Add `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` (uses the Todo-1 asmdef). Tests, all green simultaneously:
  1. `SkillInventoryClean`: `Assets/Resources/SkillData` contains EXACTLY 11 assets at root; no folders named Magic/Ranged/Melee under it.
  2. `SkillIdsUnique`: no two SkillData assets in the project share an ID.
  3. `CanonicalSkillsWired`: each of the 10 canonical assets (201,202,203,211,212,213,214,221,222,223) has non-null `ProjectilePrefab`; SkillType matches the Todo-4 table (211-214 & 221-223=Projectile, 201-202=Melee, 203=MeleeAoE); bubble fields match (212=Red, 213=Yellow, 222=Red, all others UseBubbleEffect=false).
  4. `SkillPrefabStructure`: every projectile skill's prefab has a `Projectile` component, non-null sprite, trigger collider; every melee skill's prefab (201/202/203) has a `MeleeHitbox` component.
  5. `PlayerEquipsGumMaster`: `Assets/Prefabs/Player.prefab` → PlayerController → EquippedSkills == the 4 root assets 211/212/213/214 (compare by asset path).
  6. `TimeStopUntouched`: `301_TimeStop.asset` has ID=301, SkillName=TimeStop, ManaCost=50, Cooldown=15, non-null ProjectilePrefab pointing at `Assets/Prefabs/Projectiles/TimeStop_Effect.prefab`.
  7. `PresetsResolveToRoot`: every asset under `Assets/Resources/SkillPresets/` references only root SkillData assets (no subfolder paths).
  Must NOT add tests that require a running scene or Input System; must NOT weaken assertions to make them pass.
  Parallelization: Wave 4 | Blocked by: 5 | Blocks: —
  References: `Assets/Tests/EditMode/` (Todo-1 asmdef + model test); asset paths from Todo 5; `Assets/Script/SkillData.cs`, `Assets/Script/Projectile.cs`, `Assets/Script/MeleeHitbox.cs` (Todo 2); `Assets/Script/SkillPreset.cs`; Player.prefab path `Assets/Prefabs/Player.prefab`.
  Acceptance criteria (agent-executable): `-testPlatform EditMode` run: XML `result="Passed" failed="0"`, all 7+1 tests listed.
  QA scenarios: happy — full EditMode suite green, evidence `.omo/evidence/task-6-skill-system-rework.xml`; failure — corrupt one expected value (e.g. assert GumShot bubble is Red), prove the suite catches it, revert to the real assertion.
  Commit: Y | test(skill): add EditMode data-integrity suite

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit — for every todo 1-6: checkbox checked, evidence file present at the documented path, commit exists with the documented message. Git log shows exactly the 6 planned commits; no file outside the documented scope (Assets/ + 3 skill CSVs) changed vs. the task-0 worktree snapshot.
- [ ] F2. Code quality review — review the new code (MeleeHitbox, UseSkill dispatch, importer additions, test code) for: no `SpawnFromPool("Projectile"` in PlayerController, no ObjectPooler usage in MeleeHitbox, no duplicated parsing logic, no dead fields/methods, names consistent with existing code, no `Debug.Log` noise left in hot paths.
- [ ] F3. Real manual QA — open the project in the Unity editor, play the scene, press Z/X/C/V and observe: each key fires the expected GumMaster skill (GumShot, StickyBlob, BigBubble, PopTrap) with correct prefab/visuals; StickyBlob slows (Red) and BigBubble stuns (Yellow) an enemy; cooldown HUD ticks for each slot; no console errors or warnings from the skill code.
- [ ] F4. Scope fidelity — walk the Must NOT have list line by line against the final diff: TimeStop files byte-identical, no mana code, no Animator on the 3 new VFX prefabs, `TryFire`/`ObjectPooler`/Blue-Red-Yellow pools untouched, no enemy/damage-system/spread changes, no edits outside Assets/ + the 3 skill CSVs, InstantArea has no implementation.

## Commit strategy
- 6 atomic commits, one per todo, each created ONLY after that todo's verification passes (compile + test run green). Commit messages are fixed in each todo's "Commit:" line.
- Stage explicit paths only (never `git add -A`): the files the todo touched plus their `.meta` files. Never stage the pre-existing dirty/untracked files captured in `.omo/evidence/task-0-dirty-worktree.txt`.
- Evidence files under `.omo/evidence/` are NOT committed (verification receipts live outside the repo).
- Commits land on the current branch in todo order (1→6). No amend, rebase, or squash afterwards; if a todo's verification fails, fix within the todo and recommit before moving on.

## Success criteria
- All 6 todo checkboxes AND F1-F4 are checked, each with its evidence file present under `.omo/evidence/`.
- `git log` shows exactly the 6 planned commits; `git status --porcelain` shows nothing beyond the task-0 pre-existing dirty files.
- Batchmode runs green: compile check (no `error CS`), EditMode suite (`result="Passed" failed="0"`), PlayMode suite (`result="Passed" failed="0"`).
- In-editor manual QA (F3) passes: ZXCV fire the 4 GumMaster skills with correct visuals, bubble effects apply (Red slow on StickyBlob, Yellow stun on BigBubble), cooldown HUD ticks, no console errors.
- `git diff` on `301_TimeStop.asset`, `TimeStop_Effect.prefab`, and `TimeStopEffect.cs` is EMPTY (byte-identical).
