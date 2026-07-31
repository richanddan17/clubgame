---
slug: skill-system-rework
status: approved
intent: clear
pending-action: write .omo/plans/skill-system-rework.md
approach: Make the attack skill system actually work: SkillType-driven SkillData, dedicated VFX prefabs, type-based dispatch in UseSkill, canonical root/CSV skill set, cooldown gate, data-integrity tests.
---

# Draft: skill-system-rework

## Components (topology ledger)
<!-- Lock the SHAPE before depth. One row per top-level component that can succeed or fail independently. -->
<!-- id | outcome (one line) | status: active|deferred | evidence path -->
- SkillData model | SkillType enum + bubble/melee/aoe fields | active | Assets/Script/SkillData.cs
- Skill execution dispatch | UseSkill routes by SkillType, cooldown gate, mana deferred | active | Assets/Script/player/PlayerController.cs
- Projectile/VFX prefabs | 3 dedicated projectile prefabs w/ VFX + 1 melee hitbox prefab | active | Assets/Prefabs/
- Melee hitbox | short-lived trigger collider dealing damage once | active | new MeleeHitbox.cs
- Canonical skill data | root/CSV set canonical, subfolder duplicates removed, Player re-equipped | active | Assets/Resources/SkillData/
- CSV importer | DataImportMenu extended for new fields | active | Assets/Editor/DataImportMenu.cs
- Tests | EditMode data-integrity tests for skill set | active | Assets/Tests/EditMode/

## Open assumptions (announced defaults)
<!-- Record any default you adopt instead of asking, so the user can veto it at the gate. -->
- Cooldown gate only; NO mana system in this plan (user chose "쿨다운 게이트만 먼저").
- Melee = short-lived hitbox (trigger collider, ~0.15s lifetime, single-hit per target).
- SkillType has exactly 4 members: Projectile / Melee / MeleeAoE / InstantArea.
- Skill visuals = dedicated VFX prefabs; no procedural sprite hacks.
- Skill bubble effects: StickyBlob/IceBlast→Red slow(3s), BigBubble→Yellow stun(1s); GumShot/FireBall/ThunderBolt/melee→none.
- TimeStop = user's personal fun experiment — COMPLETELY out of scope. NO rebalance, NO integration work, NO field updates, NO deletion. 301_TimeStop.asset stays byte-identical.
- Canonical skill set = root/CSV set (GumShot/StickyBlob/BigBubble/PopTrap, Slash/HeavyStrike/Whirlwind, FireBall/IceBlast/ThunderBolt); subfolder duplicates deleted.
- ObjectPooler: skills spawn prefabs directly (no pool for "Projectile"); pool system left as-is for bullets.
- Gun-sprite layering (body + aim arm w/ gun) is an asset-side decision — new assets coming; no code change planned for it.

## Findings (cited - path:lines)
- UseSkill hardcodes `ObjectPooler.SpawnFromPool("Projectile", ...)`; scene pools are only Blue/Red/Yellow → no "Projectile" pool → skills never fire. (Assets/Script/player/PlayerController.cs:~200,~221)
- All 10 root skill assets have `ProjectilePrefab: null`; 9 subfolder skill assets reference dangling guid c2088f95d17e2624c83176e859ee47c8 (prefab does not exist). (Assets/Resources/SkillData/*.asset)
- Duplicate IDs across root/subfolder sets: 211 GumShot vs ArrowShot, 212 StickyBlob vs SniperShot, 213 BigBubble vs TripleShot, 201 Slash dup, 202 HeavyStrike vs GreatSwing, 203 Whirlwind vs Stab. (tiger/datafiles/skill/*.csv)
- Player.prefab (guid 0d25e10f315caa24fb6de1bc8afb0b2e) equips the broken subfolder set (ArrowShot/SniperShot/TripleShot). (Assets/Prefabs/Player.prefab)
- SkillData.cs fields today: ID, SkillName, Damage, ManaCost, Cooldown, Icon, ProjectilePrefab, projectileCount, spreadAngle — no type/effect concept. (Assets/Script/SkillData.cs)
- CSV numbers: rangedskill.csv GumShot 12/2/0.4, StickyBlob 20/8/1.5, BigBubble 45/15/3.0, PopTrap 30/12/2.5; meleeskill.csv Slash 10/0/0.5, HeavyStrike 25/10/2.0, Whirlwind 15/15/3.0; magicskill.csv FireBall 30/15/1.5, IceBlast 25/20/2.0, ThunderBolt 45/35/4.0. (tiger/datafiles/skill/{ranged,melee,magic}skill.csv)
- 301_TimeStop.asset: ManaCost 50, Cooldown 15, linked TimeStop_Effect.prefab; TimeStopEffect.cs radius 5f, stunDuration 5f, lifeTime 1f, follows player, applies ApplyStun via IBubbleAffectable. (Assets/Resources/SkillData/301_TimeStop.asset, Assets/Script/TimeStopEffect.cs)
- Enemy damage/bubble plumbing exists: EnemyController/MeltingHaribo/RangedEnemy implement IBubbleAffectable (ApplyStun, ApplyBubbleEffect); Projectile.cs has BubbleType enum (Red/Yellow/Blue) + Initialize(). (Assets/Script/Projectile.cs, Assets/Script/IBubbleAffectable.cs)
- VFX sprites available: Assets/Sprite/vfx/ Magic Pack 9 files (Fire-bomb, Lightning, Dark-Bolt, spark), Ice Effect 01, Hit Effect 01 (3 frames), TimeMagic. Player attack sprites: Assets/Sprite/Soldier_1/Attack.png, Shot_1.png, Shot_2.png.
- SkillSlotUI.cs / SkillHUDManager.cs render cooldown overlays but impose no gameplay gate. (Assets/Script/SkillSlotUI.cs)
- test-framework com.unity.test-framework 1.6.0 present → EditMode tests runnable via Unity Test Runner / `-runTests -testPlatform EditMode`. (Packages/manifest.json)
- DataImportMenu.cs reads CSV → creates/updates SkillData SOs; must be extended for new fields. (Assets/Editor/DataImportMenu.cs)

## Decisions (with rationale)
1. **SkillType enum (4 values)** — Projectile/Melee/MeleeAoE/InstantArea. One data model covers gun, sword, magic, and the existing TimeStop; no per-skill code forks. (user-confirmed)
2. **Canonical set = root/CSV set** — gum/melee/magic trio from CSV; delete subfolder duplicates; re-equip Player.prefab. Prevents ID collisions and dangling refs. (user-confirmed)
3. **Dedicated VFX prefabs per skill** — FireBomb→FireBall, IceEffect→IceBlast, Lightning/DarkBolt→ThunderBolt, HitEffect→melee. Visual identity per skill; assets exist now. (user-confirmed)
4. **Per-skill bubble effect linkage** — StickyBlob/IceBlast slow (Red), BigBubble stun (Yellow); reuses ApplyBubbleEffect/ApplyStun on IBubbleAffectable enemies. (user-confirmed)
5. **Melee = short-lived hitbox** — trigger collider + 0.15s lifetime, one hit per target; MeleeAoE = larger radius/arc variant. No animation-timing coupling. (user-confirmed)
6. **Cooldown gate only; mana deferred** — gate in UseSkill + SkillSlotUI cooldown already present; mana system is a later project. (user-confirmed)
7. **Spawn prefabs directly, keep pool for bullets** — ObjectPooler's "Projectile" pool never existed; don't build pool infrastructure now. (default)
8. **TimeStop = hands-off** — user's personal fun experiment, NOT a deliverable. InstantArea enum member stays (user-confirmed in design Q&A) as a data-model capability for future skills; NO TimeStop-specific code or data changes. 301_TimeStop.asset untouched.
9. **Tests: EditMode data-integrity** — no skill asset has null Prefab/Type, no duplicate IDs, Player equips canonical set. (default; test-framework available)

## Scope IN
- SkillData.cs: SkillType, BubbleType(per-skill), MeleeRange/Arc, AoERadius, Prefab slot per type.
- UseSkill dispatch by SkillType + cooldown gate (no mana).
- Projectile skill: spawn skill prefab with VFX child + bubble effect wiring.
- Melee/MeleeAoE: MeleeHitbox.cs (trigger, lifetime, single-hit-per-target, damage from SO).
- InstantArea: enum member exists as data-model capability only; NO implementation work (canonical set has no InstantArea skill).
- Prefabs: 3 projectile VFX prefabs (GumShot bubble, FireBall, IceBlast, ThunderBolt — 4 total), 1 melee hitbox prefab, bubble effect variants.
- Data: root/CSV set canonical, delete subfolder duplicates, fix 101_Shotgun.asset stale data, Player.prefab re-equip.
- DataImportMenu.cs: new field support.
- EditMode tests: data integrity.
- .omo/evidence for QA artifacts.

## Scope OUT (Must NOT have)
- NO mana system / mana UI.
- NO animation-timed attack states (no new Animator controller work for melee).
- NO new enemy types or damage rework.
- NO multi-projectile spread rework beyond existing projectileCount/spreadAngle fields.
- NO gun-arm sprite layering code changes (asset-side; new assets pending).
- NO TimeStop work of any kind: no rebalance, no data-field updates, no deletion, no integration. 301_TimeStop.asset must remain byte-identical (user's personal fun experiment — out of scope entirely).
- NO editing code outside Assets/ (no tiger/ CSV writes unless importer demands; data changes go through editor/importer).

## Open questions
- None blocking. (Post-approval, executor may ask about exact TimeStop numbers if user vetoes defaults.)

## Approval gate
status: awaiting-approval
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
