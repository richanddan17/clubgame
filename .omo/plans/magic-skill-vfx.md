# magic-skill-vfx - Work Plan

## TL;DR (For humans)

**What you'll get:** 마법 스킬 6종에 실제 이펙트가 붙습니다. 파이어볼(221)은 불덩이가 날아가다 터지고, 얼음(222)은 얼음 발사→이동→맞을 때 얼음 파편, 번개(223)는 번개가 내리꽂힙니다. 거기에 어둠의 볼트(224), 성스러운 빛(225), 산성 안개(226) 스킬 3종이 새로 추가됩니다. 모든 이펙트는 코드 기반 스프라이트 애니메이션(Animator/파티클 없음)이라 가볍고, 나중에 쉽게 바꿀 수 있습니다.

**Why this approach:** 넣어둔 VFX 팩들이 이미 "발사(Start) → 이동(Repeatable) → 히트(Hit)" 3단계로 분리돼 있어서, 이를 그대로 코드 컴포넌트의 3단계 프레임 배열에 매핑하면 최소 코드로 최대 연출이 됩니다. 에디터 툴이 프레임을 자동으로 프리팹에 넣어줘서 수작업 실수가 없고, 테스트가 "모든 마법 스킬에 이펙트가 달려있는지"를 검증합니다.

**What it will NOT do:** 타임스톱 실험(301)은 바이트 단위로 그대로 둡니다. 근접/총알/버블 스킬, 적, 대미지 시스템은 건드리지 않습니다. 새 스킬 3종은 데이터에만 추가되고(프리셋/장착 변경 없음), VividMotion 포탈 팩 등 나머지 팩은 이번 범위에서 제외됩니다.

**Effort:** Medium — 코드 컴포넌트 1개 + 에디터 툴 1개 + 프리팹 6종 + 데이터 3행 + 테스트 갱신.
**Risk:** Low-Medium — Projectile.cs 히트 경로를 조금 바꾸지만(이동 정지+지연 파괴) 기존 발사체 로직은 유지하고, 테스트가 전부 잡아줍니다.
**Decisions to sanity-check:** (1) 신규 스킬 밸런스 수치(35/30/25 대미지 등)는 제가 잡은 기본값 — 게임해보고 조정. (2) 223 번개는 히트 시 범용 "Hit Effect 01" 사용. (3) 새 스킬은 장착 안 됨 — 프리셋 시스템 만들면 거기서 추가.

Your next move: approve, or run a high-accuracy review. Full execution detail follows below.

---

> TL;DR (machine): Medium effort/risk; 5 commits — SpriteVFXAnimator(3단계 코드 애니메이션)+EditMode 단위테스트, Projectile 임팩트 훅(이동정지+지연Deactivate)+PlayMode 테스트, MagicVFXBuilder로 프리팹 6종(기존3 프레임 부여+신규3 생성), magicskill.csv +3행/prefabMap+3/배치 파이프라인, 무결성 스위트 11→14/10→13 갱신+MagicVFXAnimatorWired; 헤드리스 batchmode EditMode+PlayMode 검증; TimeStop byte-identical.

## Scope
### Must have
- `SpriteVFXAnimator.cs` (Assets/Script/Combat/): 3단계 상태머신 — Start(1회)→Loop(무한 루프)→PlayHit() 시 Hit(1회). 직렬화 필드 `Sprite[] startFrames / loopFrames / hitFrames`, `float fps = 12`, `bool autoPlay = true`(OnEnable에서 Start→Loop 자동 재생), `bool destroyOnHitEnd`(기본 false; Projectile이 Deactivate 책임). 공개 API: `void PlayHit()`, `float HitDuration => hitFrames.Length / fps`. SpriteRenderer는 `GetComponent<SpriteRenderer>()`로 캐시, 없는 경우 null-safe (렌더만 스킵). 프레임 도달 시 스프라이트 교체.
- `Projectile.cs` (Assets/Script/Combat/Projectile.cs) 임팩트 훅: `OnEnable`에서 `_vfx = GetComponent<SpriteVFXAnimator>()` 캐시. `OnTriggerEnter2D`의 모든 `Deactivate()` 호출(라인 72, 80)을 `HandleImpact()`로 대체 — VFX가 있으면: 콜라이더 비활성(`GetComponent<Collider2D>().enabled = false`), 이동 정지(`speed = 0`), `_vfx.PlayHit()` 후 `CancelInvoke(nameof(Deactivate))` + `Invoke(nameof(Deactivate), _vfx.HitDuration)`; VFX가 없으면 기존대로 즉시 `Deactivate()`. `Deactivate()` 멱등 가드(`private bool _deactivated`) 추가 — 중복 호출 시 무시, `OnEnable`에서 리셋.
- 프리팹 6종 (Assets/Prefabs/Projectiles/): 기존 3종(FireBall/IceBlast/ThunderBolt)에 SpriteVFXAnimator 부여 + 신규 3종(DarkBoltProjectile/HolyProjectile/AcidProjectile) 생성. 구조는 기존 FireBallProjectile.prefab 템플릿 유지: Transform localScale 3, SpriteRenderer sortingOrder 10, CircleCollider2D isTrigger radius 0.2, Projectile(speed 15, lifeTime 3).
- `MagicVFXBuilder.cs` (Assets/Editor/): 분리 프레임 PNG를 숫자 자연 정렬로 로드해 Sprite[] 배열 구성, 6개 프리팹에 생성/갱신(멱등). MenuItem("Custom Tools/tiger/Magic VFX/Build Magic VFX Prefabs") + public static `BuildAllMagicVFX()` (배치모드 -executeMethod 진입점).
- 데이터: magicskill.csv에 3행 추가(224 DarkBolt / 225 Holy / 226 Acid), DataImportMenu.prefabMap에 3건 추가, `ImportSkillDataOnly`→`LinkSkillPrefabs` 배치 실행으로 신규 SkillData 에셋 3개 생성·링크.
- 테스트: EditMode `SpriteVFXAnimatorTests.cs`(상태머신 단위) 신규, `SkillDataIntegrityTests.cs` 갱신(루트 11→14, 캐노니컬 10→13, `MagicVFXAnimatorWired` 신규), PlayMode `SkillExecutionTests.cs`에 임팩트 테스트 1종 추가.
- FILE_MAP.md 갱신 (사용자 요구: "고치면 바로바로 파일맵에 저장").

### Must NOT have (guardrails, anti-slop, scope boundaries)
- NO Animator 컴포넌트 / AnimationClip / ParticleSystem — 코드 기반 스프라이트 애니메이션만.
- NO VFX 텍스처 .meta·임포트 설정 변경 (리슬라이싱 금지; TimeStop이 쓰는 Ice VFX 2 포함 어떤 팩도 재설정 안 함).
- NO 301_TimeStop 관련: `.asset`, `TimeStop_Effect.prefab`, `TimeStopEffect.cs` byte-identical (`git diff` 빈 결과 필수).
- NO preset.csv / Player.prefab / EquippedSkills / SkillHUDManager / 버블 시스템 / 적·대미지 코드 변경.
- NO BubbleProjectile_{blue,red,yellow}.prefab, MeleeHitbox, ObjectPooler, TryFire, UseSkill 로직 변경 — Projectile.cs 훅만 (라인 57-82 영역).
- NO 제외 팩(VividMotion ZIP, Brackeys, Pipoya TimeMagic/HEXShield, Smoke, Smear, Wood) 사용.
- NO Assets 밖 편집 — magicskill.csv + FILE_MAP.md만 예외.
- NO 신규 스킬 프리셋 장착 (데이터에만 존재).

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after for code (EditMode unit + PlayMode impact), spec-first integrity update (Todo 5 encodes the final end-state 14/13 and must pass green). Framework: com.unity.test-framework 1.6.0 (Packages/manifest.json).
- Unity binary (6000.3.12f1, ProjectSettings/ProjectVersion.txt): `"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe"` — verify it exists first; if not, locate via `where Unity.exe` / Hub install dir.
- Compile check (after every code todo): `Unity.exe -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile -` → assert log contains NO `error CS`.
- Test run template: `Unity.exe -batchmode -runTests -projectPath "D:\coding\github c\clubgame" -testPlatform EditMode -testResults "D:\coding\github c\clubgame\.omo\evidence\<file>.xml" -logFile -` (PlayMode: `-testPlatform PlayMode`). Pass = XML root `<test-run result="Passed" failed="0" ...>`.
- Evidence: every todo writes receipts to `.omo/evidence/task-<N>-magic-skill-vfx.<ext>`; F-tasks use `.omo/evidence/final-<N>-magic-skill-vfx.<ext>`.
- First step of the whole plan: `git status --porcelain` → snapshot pre-existing dirty/untracked files into `.omo/evidence/task-0-dirty-worktree.txt`; never commit or overwrite unrelated user changes (rework's 97 files live there — the executor works on top of them, commits are the user's job).

## Execution strategy
### Parallel execution waves
- **Wave 1**: Todo 1 (SpriteVFXAnimator.cs + EditMode unit tests) — no deps, everything else depends on it.
- **Wave 2** (parallel): Todo 2 (Projectile.cs 훅 + PlayMode 임팩트 테스트) · Todo 3 (MagicVFXBuilder + 프리팹 6종) — 둘 다 Todo 1 의존, 서로 파일 불연속 (Combat/Projectile.cs vs Editor/MagicVFXBuilder.cs).
- **Wave 3**: Todo 4 (CSV 3행 + prefabMap + 배치 파이프라인) — Todo 3 의존 (프리팹이 있어야 LinkSkillPrefabs가 링크 가능).
- **Wave 4**: Todo 5 (무결성 스위트 갱신 11→14/10→13 + MagicVFXAnimatorWired) — Todo 2/3/4 의존, 최종 상태 명세.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 SpriteVFXAnimator+단위테스트 | — | 2, 3 | — |
| 2 Projectile 훅+PlayMode | 1 | 5 | 3 |
| 3 Builder+프리팹 6종 | 1 | 4 | 2 |
| 4 CSV+prefabMap+파이프라인 | 3 | 5 | — |
| 5 무결성 스위트 갱신 | 2, 3, 4 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [~] 1. SpriteVFXAnimator.cs: 3단계 코드 스프라이트 애니메이션 컴포넌트 + EditMode 단위 테스트
  BLOCKED (2026-08-03 14:27 KST): 사용자의 Unity Editor(PID 5100, Hub 실행)가 프로젝트를 열고 있어 Unity 독점 프로젝트 락 때문에 batchmode -runTests/컴파일 검증 불가 ("Aborting batchmode due to fatal error"). 사용자 Editor는 종료 금지(사용자 소유). 해제 조건: 사용자가 Unity Editor를 닫으면 재위임. 코드/테스트 작성 자체는 락과 무관하나, 실패-우선 TDD 증거 + EditMode XML 증거가 배치모드에서만 생성되므로 완료 불가.
  What to do / Must NOT do: New `Assets/Script/Combat/SpriteVFXAnimator.cs` (namespace-less, same as Projectile.cs — ClubGame.Combat asmdef). Fields: `[SerializeField] private Sprite[] startFrames; [SerializeField] private Sprite[] loopFrames; [SerializeField] private Sprite[] hitFrames; [SerializeField] private float fps = 12f; [SerializeField] private bool autoPlay = true; [SerializeField] private bool destroyOnHitEnd = false;`. Private state: `enum Stage { Start, Loop, Hit, Done }`, `Stage _stage`, `int _frameIndex`, `float _timer`, `SpriteRenderer _sr`, `bool _hitFired`. `Awake`: `_sr = GetComponent<SpriteRenderer>()`. `OnEnable`: reset `_frameIndex = 0; _timer = 0; _hitFired = false; _stage = autoPlay && startFrames.Length > 0 ? Stage.Start : (loopFrames.Length > 0 ? Stage.Loop : Stage.Done);` plus guard `if (fps <= 0) fps = 12f;`. `Update`: advance `_timer += Time.deltaTime`; `float frameTime = 1f / fps; while (_timer >= frameTime) { _timer -= frameTime; _frameIndex++; ApplyCurrentFrame(); }` — ApplyCurrentFrame resolves the frame array for `_stage` (Start: `startFrames[_frameIndex % startFrames.Length]` with transition to Loop when `_frameIndex >= startFrames.Length`; Loop: `loopFrames[_frameIndex % loopFrames.Length]`; Hit: `hitFrames[Mathf.Min(_frameIndex, hitFrames.Length - 1)]` and when `_frameIndex >= hitFrames.Length` → `_stage = Done`, fire `_hitFired` once, then `if (destroyOnHitEnd) Destroy(gameObject)`). Public: `public void PlayHit() { if (_stage == Stage.Hit || _stage == Stage.Done) return; _stage = hitFrames.Length > 0 ? Stage.Hit : Stage.Done; _frameIndex = 0; _timer = 0; }`; `public float HitDuration => hitFrames.Length > 0 ? hitFrames.Length / fps : 0f;`. Must NOT use Animator/AnimationClip/ParticleSystem; must NOT touch rendering beyond `_sr.sprite = ...` (null-check `_sr`). Then `Assets/Tests/EditMode/SpriteVFXAnimatorTests.cs` (uses existing ClubGame.EditModeTests asmdef): construct `new GameObject` + `SpriteRenderer` + `SpriteVFXAnimator`, assign 3 dummy `Sprite.Create(Texture2D)` arrays (2 start / 3 loop / 2 hit), `SetActive(true)`, step `Update` manually via reflection or run in a coroutine-free way (call private Update through `SendMessage` is not allowed — instead expose nothing new; drive via `Time` is not controllable in EditMode → test via repeated `Update` reflection call with `Time.deltaTime` fixed by temporarily setting `Time.timeScale`? NO — EditMode has no Time progression. Instead: assert state transitions by calling `PlayHit()` and checking `HitDuration > 0`, and test the frame-array guards (empty hitFrames → PlayHit makes HitDuration 0; autoPlay=false with no frames → no throw). Where Update-loop timing is needed, use `[UnityTest]` in PlayMode instead — keep EditMode to pure-logic assertions).
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2, 3
  References: `Assets/Script/Combat/Projectile.cs:1-5` (namespace-less convention, same asmdef); `Assets/Tests/EditMode/SkillDataIntegrityTests.cs:1-11` (asmdef usage pattern); `Assets/Script/Combat/ClubGame.Combat.asmdef` (target asmdef — verify name at build time); `Assets/Tests/EditMode/ClubGame.EditModeTests.asmdef`.
  Acceptance criteria (agent-executable): compile check green (no `error CS`); EditMode run green with `SpriteVFXAnimatorTests` listed (`result="Passed" failed="0"`).
  QA scenarios: happy — batchmode EditMode run, evidence `.omo/evidence/task-1-magic-skill-vfx.xml`; failure — temporarily make `PlayHit()` ignore calls when `hitFrames.Length == 0` incorrectly (or set `HitDuration` to 0 always), prove the guard test fails, revert.
  Commit: N (사용자 직접 커밋 — 프로젝트 관례) | feat(vfx): add 3-stage SpriteVFXAnimator component

- [ ] 2. Projectile.cs: 임팩트 훅 (VFX 재생 + 이동 정지 + 지연 Deactivate) + PlayMode 임팩트 테스트
  What to do / Must NOT do: In `Assets/Script/Combat/Projectile.cs`: add `private SpriteVFXAnimator _vfx;` and `private bool _deactivated;`. `OnEnable`: `_deactivated = false; _vfx = GetComponent<SpriteVFXAnimator>();` (keep existing CancelInvoke/Invoke lifeTime). `Deactivate()`: first line `if (_deactivated) return; _deactivated = true;` then existing pool return (lines 44-50). Replace BOTH `Deactivate();` calls in `OnTriggerEnter2D` (lines 72, 80) with `HandleImpact();`. New `private void HandleImpact()`: `if (_vfx != null && _vfx.HitDuration > 0f) { var col = GetComponent<Collider2D>(); if (col != null) col.enabled = false; speed = 0f; _vfx.PlayHit(); CancelInvoke(nameof(Deactivate)); Invoke(nameof(Deactivate), _vfx.HitDuration); } else { Deactivate(); }`. Must NOT change damage/bubble logic (lines 57-71, 76-79), must NOT touch TryFire/ObjectPooler/UseSkill/MeleeHitbox. Then add ONE PlayMode test to `Assets/Tests/PlayMode/SkillExecutionTests.cs` (existing scene-builder pattern from the rework): fire a skill whose prefab HAS SpriteVFXAnimator (e.g. FireBall 221 after Todo 3 — see wave order; if prefab not yet built, the test must skip-gracefully: `Assert.Ignore` when the prefab lacks the component, then pass after Todo 3) — spawn projectile, collide with enemy, assert enemy `Health.CurrentHealth` decreased AND projectile's `_stage` reached Hit (via public `HitDuration` + internal state reflection is overkill — assert the object is NOT destroyed immediately: it must still exist right after impact (Invoke delay), and destroyed within `HitDuration + 0.5f`).
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 5
  References: `Assets/Script/Combat/Projectile.cs:38-50` (OnEnable/Deactivate), `:52-55` (Update translate), `:57-82` (OnTriggerEnter2D — the two Deactivate sites); `Assets/Script/Combat/SpriteVFXAnimator.cs` (Todo 1, `PlayHit`/`HitDuration`); PlayMode test pattern: `Assets/Tests/PlayMode/SkillExecutionTests.cs` + `ClubGame.PlayModeTests.asmdef` (rework-built; verify Unity.InputSystem Mouse setup pattern inside).
  Acceptance criteria (agent-executable): compile check green; EditMode green (regression); PlayMode green (`result="Passed" failed="0"`) including the new impact test; code review: `Deactivate` guarded, both call sites routed through `HandleImpact`, no changes outside lines 38-82.
  QA scenarios: happy — PlayMode suite green, evidence `.omo/evidence/task-2-magic-skill-vfx.xml`; failure — temporarily revert one call site to direct `Deactivate()`, prove the impact test fails (immediate destroy), revert.
  Commit: N (사용자 직접 커밋) | feat(vfx): play hit VFX and delay deactivation on projectile impact

- [ ] 3. MagicVFXBuilder.cs: 프레임 자동 부여 + 프리팹 6종 (기존 3종 갱신 + 신규 3종 생성)
  What to do / Must NOT do: New `Assets/Editor/MagicVFXBuilder.cs`. Constants: frame root paths and per-skill stage arrays:
  - 221 FireBall: Start `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/sprites/FireBomb/Fire-bomb{1..3}.png`, Loop `{4..7}`, Hit `{8..15}`.
  - 222 IceBlast: Start `Assets/Sprite/vfx/Ice Effect 01/Ice Effect 01/Ice VFX 1/Separated Frames/VFX 1 Start{1..3}.png`, Loop `VFX 1 Repeatable{1..10}.png`, Hit `VFX 1 Hit{1..8}.png`.
  - 223 ThunderBolt: Start empty, Loop `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/sprites/Lightning/Lightning{1..11}.png`, Hit `Assets/Sprite/vfx/Hit Effect 01/Hit Effect 01/Hit Effect 01 {1..3}.png`.
  - 224 DarkBolt: Start empty, Loop `.../sprites/DarkBolt/Dark-Bolt{1..4}.png`, Hit `{5..12}.png`.
  - 225 Holy: Start `Assets/Sprite/vfx/Holy VFX 01-02/Holy VFX 01/Separated Frames/Holy VFX 01 Initial{1..2}.png`, Loop `Holy VFX 01 Repeatable{1..8}.png`, Hit `Holy VFX 01 Impact{1..7}.png`.
  - 226 Acid: Start empty, Loop `Assets/Sprite/vfx/Acid VFX 01 - 02/Acid VFX 01 - 02/Acid VFX 2/Separated Frames/Acid VFX 02Repeatable{1..12}.png`, Hit `Acid VFX 02Ending{1..6}.png`.
  Load each PNG via `AssetDatabase.LoadAssetAtPath<Sprite>(path)`; skip+`Debug.LogWarning` any missing file; a stage array may be empty only where the table says "empty" — otherwise `Debug.LogError` and abort that prefab. `public static void BuildAllMagicVFX()`: for each of 6 prefabs (paths `Assets/Prefabs/Projectiles/{FireBallProjectile,IceBlastProjectile,ThunderBoltProjectile,DarkBoltProjectile,HolyProjectile,AcidProjectile}.prefab`): load existing prefab via `AssetDatabase.LoadAssetAtPath<GameObject>` (or create new GO), ensure components: Transform (localScale 3), SpriteRenderer (sortingOrder 10), CircleCollider2D (isTrigger, radius 0.2), `Projectile` (speed 15, lifeTime 3 — via SerializedObject since fields are private), `SpriteVFXAnimator` (assign the 3 arrays + fps 12 via SerializedObject). Set the SpriteRenderer's initial sprite = first loop frame (or first start frame). Save via `PrefabUtility.SaveAsPrefabAsset` (delete+recreate if the old prefab was a broken placeholder). MenuItem "Custom Tools/tiger/Magic VFX/Build Magic VFX Prefabs" calling it; also `public static` so batchmode `-executeMethod MagicVFXBuilder.BuildAllMagicVFX` works. Must NOT modify `BubbleProjectile_*`, `MeleeHitbox.prefab`, `TimeStop_Effect.prefab`; must NOT change any .meta/import settings; must NOT create Animators. One-off verification: batchmode `-executeMethod MagicVFXBuilder.BuildAllMagicVFX` then a throwaway EditMode check (or rely on Todo-5 `MagicVFXAnimatorWired`) asserting all 6 prefabs load, have SpriteVFXAnimator with non-empty loopFrames and hitFrames.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 4
  References: prefab template `Assets/Prefabs/Projectiles/FireBallProjectile.prefab:22-36` (scale 3), `:84` (sortingOrder 10), `:96-131` (CircleCollider2D trigger radius 0.2), `:132-145` (Projectile guid 748bc7fe4f5592044adef09a9696c5a8, speed 15, lifeTime 3); sprite load pattern `Assets/Editor/PrefabAutoCreator.cs:97-106` (FindBestSprite), `:272-283` (CreateProjectilePrefab SaveAsPrefabAsset); VFX frame paths (glob-verified, all individual PNGs — no slicing needed); `Assets/Editor/DataImportMenu.cs:310-320` (existing prefabMap paths — new prefab names must match Todo 4 additions).
  Acceptance criteria (agent-executable): 6 prefab files exist under Assets/Prefabs/Projectiles/ with .meta files; each loads as GameObject with SpriteVFXAnimator (non-empty loop+hit) — verified via Todo-5 test `MagicVFXAnimatorWired` (Wave 4) or one-off EditMode check in this wave (evidence `.omo/evidence/task-3-magic-skill-vfx.xml`); build log has no `error CS`.
  QA scenarios: happy — run builder in batchmode, load all 6 prefabs, assert components+arrays, evidence `.omo/evidence/task-3-magic-skill-vfx.txt`; failure — delete `DarkBoltProjectile.prefab` after building, rerun builder, assert it is recreated (idempotency + regeneration), evidence `.omo/evidence/task-3-failure-notes.txt`.
  Commit: N (사용자 직접 커밋) | feat(vfx): build 6 magic projectile prefabs with 3-stage sprite VFX

- [ ] 4. magicskill.csv +3행, DataImportMenu.prefabMap +3, 배치 파이프라인 실행 (신규 SkillData 3종 생성·링크)
  What to do / Must NOT do: Append to `tiger/datafiles/skill/magicskill.csv` exactly these rows (same 10-column header as lines 1-4):
  `224,DarkBolt,35,25,3.0,Projectile,None,16,0,0`
  `225,Holy,30,22,2.5,Projectile,None,15,0,0`
  `226,Acid,25,20,2.0,Projectile,None,12,0,0`
  In `Assets/Editor/DataImportMenu.cs` prefabMap (lines 310-320) add: `prefabMap[224] = "Assets/Prefabs/Projectiles/DarkBoltProjectile.prefab"; prefabMap[225] = "Assets/Prefabs/Projectiles/HolyProjectile.prefab"; prefabMap[226] = "Assets/Prefabs/Projectiles/AcidProjectile.prefab";`. Run in order via batchmode `-executeMethod`: `DataImportMenu.ImportSkillDataOnly` → `DataImportMenu.LinkSkillPrefabs`. Must NOT use `ImportAll` (it re-imports enemy/biome data — outside scope, trips F4); must NOT touch preset.csv / Player.prefab / other CSVs. Verify: `Assets/Resources/SkillData/` gains `224_DarkBolt.asset`, `225_Holy.asset`, `226_Acid.asset` (naming follows the importer's existing `<id>_<Name>.asset` convention — confirm actual names via `ImportSkillFile`'s asset naming code and adjust assertions to the REAL names), each with the CSV values (Damage/ManaCost/Cooldown/SkillType=Projectile/Speed) and non-null `ProjectilePrefab` pointing at the new prefabs.
  Parallelization: Wave 3 | Blocked by: 3 | Blocks: 5
  References: `tiger/datafiles/skill/magicskill.csv:1-4` (header + existing rows); `Assets/Editor/DataImportMenu.cs:173` (skillMagicPath), `:209` (ImportSkillFile), `:310-320` (prefabMap — add after 223), `:332-334` (LinkSkillPrefabs iteration); Todo-3 prefab paths; asset naming: `DataImportMenu.cs` GetOrCreateAsset pattern (rework Todo 4, line ~371) — verify exact `<id>_<Name>` naming before asserting.
  Acceptance criteria (agent-executable): batchmode runs exit 0 with no errors in log; 3 new .asset files exist at `Assets/Resources/SkillData/` root with CSV values + linked prefabs (assert via a throwaway EditMode check or Todo-5 assertions); `Assets/Resources/SkillData` root count = 14.
  QA scenarios: happy — run pipeline, load the 3 new assets, assert fields, evidence `.omo/evidence/task-4-magic-skill-vfx.txt`; failure — temporarily set 224's Speed to a non-numeric value in CSV, run importer, assert it logs an error and skips the row without crashing, revert CSV.
  Commit: N (사용자 직접 커밋) | feat(data): add DarkBolt/Holy/Acid magic skills and wire prefabs

- [ ] 5. 무결성 스위트 갱신: 11→14 루트 / 10→13 캐노니컬 + MagicVFXAnimatorWired 신규
  What to do / Must NOT do: In `Assets/Tests/EditMode/SkillDataIntegrityTests.cs`: (a) `CanonicalAssetNames` (lines 18-23) += `"224_DarkBolt.asset", "225_Holy.asset", "226_Acid.asset"` (match Todo-4 real names); (b) `CanonicalPrefabLinks` (lines 26-38) += `{224, ".../DarkBoltProjectile.prefab"}, {225, ".../HolyProjectile.prefab"}, {226, ".../AcidProjectile.prefab"}`; (c) `SkillInventoryClean` (line 48) `Assert.AreEqual(11, ...)` → `14`; comment line 46-47 update; (d) `SkillIdsUnique` (line 80) `Assert.GreaterOrEqual(guids.Length, 11, ...)` → `14`; (e) `CanonicalSkillsWired` type expectation (line 110) — 224/225/226 are `>= 211` so already `SkillType.Projectile`, bubble expectation line 117 — all three `None` → `UseBubbleEffect=false` (no change needed but verify); (f) NEW test `MagicVFXAnimatorWired`: for ids 221-226, load prefab from `skill.ProjectilePrefab`, assert `GetComponentInChildren<SpriteVFXAnimator>(true)` non-null, `loopFrames.Length > 0`, `hitFrames.Length > 0`, `fps > 0`; and for 221/222/225 assert `startFrames.Length > 0` (223/224/226 start may be empty by design). Must NOT weaken any existing assertion; must NOT add scene/InputSystem dependencies. Also add the PlayMode impact test in Todo 2 (already there). Run full EditMode + PlayMode suites.
  Parallelization: Wave 4 | Blocked by: 2, 3, 4 | Blocks: —
  References: `Assets/Tests/EditMode/SkillDataIntegrityTests.cs:18-38` (lists), `:43-91` (counts), `:96-129` (wiring), `:134-167` (prefab structure); `Assets/Script/Combat/SpriteVFXAnimator.cs` (Todo 1); Todo-4 asset/prefab names; `Assets/Tests/EditMode/ClubGame.EditModeTests.asmdef` (must reference ClubGame.Combat asmdef — already does via Assembly-CSharp? verify: IntegrityTests already uses SkillData/Projectile from Combat, so reference exists).
  Acceptance criteria (agent-executable): EditMode run: XML `result="Passed" failed="0"` with all existing + new tests (8 existing → 9 with MagicVFXAnimatorWired); PlayMode green; `Assets/Resources/SkillData` root = 14 (asserted).
  QA scenarios: happy — full suites green, evidence `.omo/evidence/task-5-magic-skill-vfx.xml` + PlayMode `.omo/evidence/task-5-magic-skill-vfx-playmode.xml`; failure — temporarily strip `hitFrames` from IceBlastProjectile prefab, prove `MagicVFXAnimatorWired` fails, restore via builder rerun.
  Commit: N (사용자 직접 커밋) | test(vfx): update integrity suite for 14-skill end-state and VFX wiring

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit — for every todo 1-5: checkbox checked, evidence file present at the documented path, worktree diff contains exactly the planned file set (SpriteVFXAnimator.cs, Projectile.cs, MagicVFXBuilder.cs, 3 prefab edits + 3 new prefabs + .meta, magicskill.csv, DataImportMenu.cs, 3 new SkillData .asset + .meta, test files, FILE_MAP.md) vs. the task-0 snapshot; no file outside scope changed.
- [ ] F2. Code quality review — SpriteVFXAnimator state machine correct (no out-of-bounds, Done once, guard fps), Projectile hook idempotent (`_deactivated`), builder natural-sorts frames and errors on missing files, no dead code, names consistent, no `Debug.Log` noise in hot paths, no Animator/Particle anywhere in the 6 prefabs.
- [ ] F3. Real manual QA — open the Unity editor, play, fire FireBall/IceBlast/ThunderBolt: projectile shows Start→loop flight→hit burst; DarkBolt/Holy/Acid are selectable in the SkillData folder (equip temporarily in the Inspector via combatSettings only if the user wants — default: verify prefabs play correctly by dragging them into a test scene); no console errors.
- [ ] F4. Scope fidelity — walk the Must NOT have list line by line against the final diff: TimeStop files byte-identical (`git diff` empty), no Animator/ParticleSystem, no texture .meta changes under Assets/Sprite/vfx/, no preset/Player/bubble/enemy changes, no edits outside Assets/+magicskill.csv+FILE_MAP.md, excluded packs untouched.

## Commit strategy
- Commits are made by the USER directly (project convention — same as the rework plan). The executor stages nothing and commits nothing; it reports the exact per-todo file list so the user can commit atomically: (1) SpriteVFXAnimator.cs + test; (2) Projectile.cs + PlayMode test; (3) MagicVFXBuilder.cs + 6 prefabs (+.meta); (4) magicskill.csv + DataImportMenu.cs + 3 SkillData assets; (5) test updates + FILE_MAP.md.
- FILE_MAP.md must be updated IMMEDIATELY after each todo's verification passes — never deferred (user requirement). List new files (SpriteVFXAnimator.cs, MagicVFXBuilder.cs, 3 new prefabs, new tests), update Projectile.cs responsibilities, add the 3 new skills to the skill table, remove nothing from TimeStop notes.
- IMPORTANT: the rework's 97 uncommitted files are already in the working tree — commit the rework first (user's call) so the VFX changes are separable, and never `git add -A` (stage explicit paths per todo).
- Evidence files under `.omo/evidence/` are NOT committed.

## Session status (2026-08-03 — PAUSED by user request "걍 저장만하고 오늘은 끝내")

- **State**: Plan approved 2026-08-03 ("승인 — 바로 실행"). Execution started, then paused. 0/9 checkboxes complete.
- **Done so far**: Boulder `magic-skill-vfx` registered active; task-0 worktree snapshot at `.omo/evidence/task-0-dirty-worktree.txt` (104 porcelain entries, incl. rework's ~97 uncommitted files); Todo 1 attempted 3× (each aborted — see below).
- **Todo 1 blocker** (marked `- [~]` above): user's live Unity Editor (PID 5100, Hub-launched) holds the project → batchmode `-runTests`/compile fails ("Aborting batchmode due to fatal error"). Do NOT kill user processes. Diagnosis from attempt 3 was inconclusive (log was piped, not captured to file) — could be the lock OR a CS compile error in the stub/test. On resume, rerun with `-logFile <real file>` to distinguish (A) CS compile error vs (B) project lock; if (B), ask user to close the Editor first.
- **Confirmed facts for resume** (do not re-derive):
  - Unity exe: `D:\coding\6000.3.12f1\Editor\Unity.exe` (NOT C:\Program Files\...).
  - GOTCHA: do NOT pass `-quit` together with `-runTests` (quits before tests run). Always write `-logFile` to a real file, never pipe.
  - Asmdef: `Assets/Script/Combat/Combat.asmdef` (internal name `ClubGame.Combat`); EditModeTests references it correctly. Conventions: namespace-less, `using UnityEngine;`, NUnit namespace-less tests.
  - EditMode baseline 8/8 green; `.omo/evidence` is gitignored; pre-existing dirty set ~104-107 entries.
  - Attempt 2 may have left files: check `Assets/Script/Combat/SpriteVFXAnimator.cs` + `Assets/Tests/EditMode/SpriteVFXAnimatorTests.cs` before recreating.
- **Resume steps**: (1) user closes Unity Editor or approves lock-diagnosis run; (2) re-dispatch Todo 1 with the confirmed Unity path + log-file capture; (3) continue Wave 2 (Todos 2+3 in parallel) → Wave 3 (Todo 4) → Wave 4 (Todo 5) → F1-F4. User commits directly per project convention.

## Success criteria
- All 5 todo checkboxes AND F1-F4 checked, each with evidence under `.omo/evidence/`.
- Batchmode runs green: compile check (no `error CS`), EditMode suite (`result="Passed" failed="0"`, 9 tests), PlayMode suite green including the impact test.
- All 6 magic prefabs play 3-stage VFX in-editor (F3): Start (where defined) → loop flight → hit burst on impact; projectile no longer vanishes instantly on hit when VFX is present.
- 14 SkillData assets at root (13 canonical + 301), each canonical skill wired to its prefab; new skills 224/225/226 in CSV with linked prefabs.
- `git diff` on `301_TimeStop.asset`, `TimeStop_Effect.prefab`, `TimeStopEffect.cs` is EMPTY; `git diff` under `Assets/Sprite/vfx/**/*.meta` is EMPTY (no import changes).
- FILE_MAP.md reflects the final state.
