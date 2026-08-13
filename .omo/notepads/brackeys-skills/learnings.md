# brackeys-skills — momus review (gate #1, 2026-08-06)

VERDICT: APPROVE — no blockers, no unresolved majors. 5 minor findings.

## Verified against actual repo state (all PASS)
- All 14 sheet frame counts match grep on .meta exactly (plan table = ground truth: fire_point 45, fire_ring 30, electric_ring 55, star_explosion 34, vortex 64, lightstreaks 500, big_hit 311, wavy_blue 121, charge 256, impact_white 78, dithered_fire 500, blood_impact 59, explosion 284, wavy_purple 119 unused). Min sheet 30 -> Hit [0..29] clamp safe (all Hit sheets >= 34).
- Test counts: EditMode 17 (7+9+1 across 3 files) -> plan 17->18 OK; PlayMode 5 ([UnityTest] x5) -> plan 5->6 OK. Draft baseline (16/16, 4/4) is STALE; plan numbers match reality.
- Root SkillData = 15 (14 canonical + 301) -> 15->23 OK. FILE_MAP precedent confirms Linked=14 -> plan 14->22 OK.
- Integrity math: CanonicalAssetNames 14->22, CanonicalPrefabLinks 13->21, SkillIdsUnique >=15->>=23, SkillInventoryClean 15->23 — all internally consistent.
- Intermediate-state claim verified: ONLY SkillInventoryClean asserts exact root count; CanonicalSkillsWired (:104) and SkillPrefabStructure (:185) iterate CanonicalPrefabLinks.Keys -> exactly 1 expected failure until Todo 5. Claim on line 61 is accurate.
- CanonicalSkillsWired no-collision for 231-238: id>=211 -> Projectile expectation (:114-119) holds; bubble 212/213/222 only (:121-124) holds (all 8 CSV Bubble=None).
- Builder "mirror" is read-only reuse: MagicVFXBuilder.cs untouched (Must NOT), LoadStage :266 (separate-PNG loader) correctly identified as non-reusable for single-texture slices.
- All references exist and point at correct content (minor line drift only, see findings).
- CSV rows: 10-col schema, balance band (25-45/15-35/1.5-4.0/12-20) all within, Name<->prefab path mapping exact.
- Waves/dependency matrix consistent; commit strategy = user commits, git add -A forbidden; all acceptance criteria agent-executable; F1-F4 cover compliance/quality/manual-QA/scope.

## Minor findings
1. Matrix cell "Todo 1 Can parallelize with: 2" vs Wave 1 mandatory serial 1->2 (narrative explains; annotate cell).
2. Reference line drift (+-3): FireBall sortingOrder :85/:84, radius :132, Projectile block :133-147; DataImportMenu Linked log :363/:362, LogError :352-353; SkillExecutionTests CreateFireBallSkill :124-136, equip helpers :182-204; MagicVFXBuilder batch entry :100-130 vs ":1-60".
3. FILE_MAP timing: Todo 5(f) lists full set while Commit strategy mandates per-todo immediate updates — annotate Todo 5(f) as final increment only.
4. Draft baseline stale (16/16, 4/4) — plan numbers verified against actual files; do not "fix" plan to match draft.
5. F3 "남은 사용자 단계" vs "Zero human intervention" — disclosed, but clarify the user eyeball step is informational, not an F-gate criterion.
# brackeys-skills — Oracle review (gate #2, 2026-08-06)

VERDICT: APPROVE — no blockers. 13/13 MUST DO claims verified; 9 minor findings (all line-number
imprecision or process nuance, none behavior-affecting).

## Verified-exact (file:line evidence)
- Frame counts 14/14 exact: fire_point 45, explosion 284, fire_ring 30, electric_ring 55,
  star_explosion 34, vortex 64, lightstreaks 500, big_hit 311, wavy_blue 121, charge_7x6 256,
  impact_white 78, dithered_fire 500, blood_impact 59, wavy_purple 119. Min loop = 30 -> hit [0..29] clamp safe.
- All 14 predrawn metas spriteMode:2 + textureType:8; names `^{base}_(\d+)$` starting at _0
  (explosion_6x5.png.meta:4-97, charge_7x6.png.meta:4-59 spot-checked).
- MagicVFXBuilder.cs: BuildSkillPrefab :179-248 (SaveAsPrefabAsset :244, SerializedObject :228-241),
  LoadStage :266-318 (per-file PNG loader -> NOT reusable for sliced sheets, plan correct), structure :1-60.
- FireBallProjectile.prefab: scale(3,3,1) :33, sortingOrder 10 :85, CircleCollider2D isTrigger :127 radius 0.2 :132,
  Projectile guid 748bc7fe4f5592044adef09a9696c5a8 :142 (= Projectile.cs.meta guid), speed 15 :145, lifeTime 3 :146.
- magicskill.csv: 10-col header :1, 227 TimeWarp :8 (append anchor), balance band confirmed for all 8 new rows.
- DataImportMenu.cs: header parse :224-230, Enum.TryParse :255, Bubble :260-269, Speed :271-275,
  asset naming {id}_{Name}.asset :241, ImportSkillDataOnly :294-300, prefabMap :310-324 (14 entries, 227 at :324),
  LinkSkillPrefabs :303-364 (subfolder skip :333, LogError+skip :350-354), ImportAll :196-203 distinct from ImportSkillDataOnly.
- SkillData.cs: 17 fields, SkillType enum has Projectile :3, UseBubbleEffect :25, ProjectileSpeed :24.
- SkillDataIntegrityTests.cs: ALL cited ranges exact (18-25/28-43/49-75/53/80-96/85/102-134/115-117/122-124/
  139-177/151/154-166/290-328/270-285/220-265/333-363/370-394). EditMode total = 9+1+7 = 17 -> 18 after +1. 
  Root SkillData = 15 (project-wide FindAssets also 15; no assets outside Assets/Resources/SkillData).
  CanonicalAssetNames 14, CanonicalPrefabLinks 13, prefabMap 14 — 14->22 / 13->21 / 15->23 arithmetic all consistent.
  Intermediate-state claim correct: after Todo 2 ONLY SkillInventoryClean fails (15 vs 23); others iterate
  canonical lists unchanged until Todo 5.
- SkillExecutionTests.cs: 5 [UnityTest] -> 6 after +1. Precedent ProjectileWithVFX_PlaysHitAndDelaysDeactivation :248-275
  (magic-skill-vfx plan Todo 2 DONE 2026-08-04). No asmdef barrier: PlayMode asmdef refs ClubGame.Combat
  (SpriteVFXAnimator/Projectile/Health direct); PlayerController stays reflection-based (existing helpers reused).
- SpriteVFXAnimator.cs: fields :11-16, PlayHit :87-94, HitDuration :96; empty startFrames -> Loop stage (:35-37),
  destroyOnHitEnd false -> Projectile owns deactivation. Projectile.cs: OnEnable :40-46, Deactivate :48-57,
  HandleImpact :59-74 (delayed Invoke(Deactivate, HitDuration) :68) — test asserts exactly this.
- EditMode asmdef references ClubGame.Combat (ClubGame.EditModeTests.asmdef:4-6); Combat.asmdef name = ClubGame.Combat.
- Unity 6000.3.12f1 (ProjectVersion.txt), binary exists D:\coding\6000.3.12f1\Editor\Unity.exe.
  Batchmode gotchas match timestop-style-magic-skills.md verbatim (lines 45-50/72/93/136). flipbooks=14 TGA, particles=185 PNG.
- GUID idempotency: in-place SaveAsPrefabAsset preserves meta/GUID (repo precedent MagicVFXBuilder docstring :13 + DONE notes).

## Minor findings (fix in plan, no behavior risk)
1. MagicVFXBuilder.cs is 320 lines ("319줄 참조" off-by-one). Batch entry points are :99-124, not :1-60.
2. Prefab refs ±1: sortingOrder :85 (not 84), m_Radius :132 (not within 96-131), lifeTime :146 (not 132-145).
3. DataImportMenu: ImportSkillFile spans :214-291 (205-212 is ImportSkillData); Linked= log at :363 (not 362).
4. SkillExecutionTests: CreateFireBallSkill :124-136 (not 122-134); EquipSkill/InvokeUseSkill/ResetSkillCooldowns :182-204 (not 167-189).
5. "timestop 선례" file is .omo/plans/timestop-style-magic-skills.md (no timestop.md) — precedent content is real.
6. Todo 1 QA (B) "prefab 삭제 후 재실행 -> GUID 유지" holds ONLY if the .prefab file is deleted while .meta survives;
   AssetDatabase.DeleteAsset removes the meta -> NEW GUID. Specify "delete .prefab only, keep .meta".
7. ImportSkillDataOnly re-imports all 3 skill CSVs (ImportSkillData :205-212) — idempotent overwrite, no diffs, but F1 diff check should expect no other file changes.
8. Prefab speed 15/lifeTime 3 are template defaults only — PlayerController.cs:236 overrides speed with
   skill.ProjectileSpeed (CSV 12-18); ObjectPooler.ReturnToPool (:75-78) is SetActive(false) only — default poolTag safe.
