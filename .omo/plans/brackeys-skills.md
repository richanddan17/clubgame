# brackeys-skills - Work Plan

## TL;DR (For humans)

**What you'll get:** "더더 많이" 요청대로 마법 스킬 8개(화염구, 화염 고리, 전기 고리, 소용돌이, 광선, 물결 탄환, 차지, 핏빛 탄환)가 게임에 추가됩니다. 각 스킬은 Brackeys 무료 VFX 시트의 스프라이트 연속 장면을 사용해 발사→적 충돌→히트 이펙트→잠시 후 사라짐의 3단계 애니메이션으로 동작하고, 스킬 데이터/공격력/쿨다운도 기존 밸런스 범위 안에서 채워집니다. 기존 스킬·코드·설정은 하나도 건드리지 않습니다.

**Why this approach:** 기존 마법 스킬 파이프라인(빌더 스크립트 → CSV 데이터 → 자동 생성)을 그대로 따라가므로, 리뷰 3회(갭 분석 + 비평가 2인)를 거쳐 최소 침습으로 끝납니다. Brackeys 시트는 프레임이 커서(기준 FireBall 대비 최대 20배) 스킬마다 크기를 자동 계산해 실제 크기가 비슷해 보이게 합니다. 무한 반복되는 긴 시트는 5초까지만 잘라 게임 리소스를 보호합니다.

**What it will NOT do:** 기존 스킬/프리팹/런타임 코드/애니메이터·파티클 시스템/시간정지 스킬은 전혀 변경하지 않고, Brackeys 텍스처 원본도 그대로 둡니다. 커밋은 직접 하셔야 합니다.

**Effort:** Medium
**Risk:** Low - 기존 파이프라인 미러 + 2단계 자동 테스트 스위트로 회귀 방지; 실행 순서 고정으로 중간 상태가 정확히 예측됨.
**Decisions I made for you:** ① 스킬 구성: 8종 전부 투사체(Projectile) 타입 — 장착형 스킬 슬롯과 호환되고 기존 규칙과 충돌 없음. ② 밸런스: 대미지 25-45/마나 15-35/쿨 1.5-4.0/속도 12-20 기존 밴드 안. ③ 시각 크기: 스킬별 자동 계산(blind 3배 상속 금지). ④ 재생: 루프는 처음 60프레임(5초) 이하로 잘라 리소스 보호. ⑤ 애니메이션 방식: 코드 기반 스프라이트 전환(Animator 금지, 기존 시스템과 동일).

Your next move: `/start-work` 를 실행하면 승인된 이 계획이 그대로 실행됩니다 (Wave 1: Todo 1 → 2 직렬 → Wave 2/3/4 → 최종 검증 게이트 4종 → 증거 파일). 상세 실행 절차는 아래에 있습니다.

---

> TL;DR (machine): Medium effort, Low risk. Reuse existing editor-builder + CSV-import pipeline to add 8 Projectile skills (IDs 231-238) from Brackeys sprite sheets: new BrackeysVFXBuilder.cs (per-skill scale = clamp(21/maxFramePx, 0.05, 1.0), loop clamped to first 60 frames, hit [0..29], fps 12, F8 visual-continuity gate), 8 prefabs, CSV rows + prefabMap entries, ImportSkillDataOnly + LinkSkillPrefabs batch, PlayMode impact test (231), integrity suite 17→18 EditMode / 5→6 PlayMode; no runtime/MagicVFXBuilder/asset changes; evidence under .omo/evidence; commits by user.

## Scope
### Must have
- 신규 `Assets/Editor/BrackeysVFXBuilder.cs`: SheetStageSpec(SheetPath, Prefix, First?, Last?) 로더 — 서브스프라이트 이름 `^{Prefix}_(\d+)$` 정규식 필터 + `int.Parse` 자연 정렬(문자열 정렬 금지) + `[First..Last]` 슬라이스(경계 초과 시 클램프+`Debug.LogWarning`) — + 8종 SkillSpec 테이블(아래 표) + `BuildAndVerifyAllBrackeysVFX()`(배치 `-executeMethod` 진입점, 멱등 빌드 + 자체 검증 로그) + `VerifyAllBrackeysVFX()`(실패 시 throw → batchmode exit non-zero) + `[MenuItem("Custom Tools/tiger/Magic VFX/Build Brackeys VFX Prefabs")]`. 구조는 `MagicVFXBuilder.cs` 미러 (BuildSkillPrefab:179-248, 자연정렬+SaveAsPrefabAsset 패턴).
- 프리팹 8종 `Assets/Prefabs/Projectiles/{FireOrb,FireRing,ElectricRing,Vortex,LightStreak,WavyBolt,Charge,BloodBolt}Projectile.prefab` — 템플릿: Transform localScale **스킬별 계산 `(s_i, s_i, 1)` — `s_i = clamp(21 / maxFramePx_i, 0.05, 1.0)`** (FireBall 스프라이트 7×7px(meta rect) × scale 3 = 0.21 유닛과 최대 변 정합; maxFramePx_i = loop/hit 시트 최대 프레임 변 px, sprite.textureRect에서 수집 — wavy_blue 418px → s≈0.05; **blind scale 3 상속 금지**: brackeys 프레임은 FireBall 대비 4-20배 큼), SpriteRenderer sortingOrder 10 + 첫 loop 프레임, CircleCollider2D isTrigger radius 0.2, Projectile(speed 15, lifeTime 3 — SerializedObject), SpriteVFXAnimator(startFrames 빈 배열, **loopFrames=[0..min(전체,60)-1] 클램프**, hitFrames=[0..29] 클램프, fps 12, autoPlay true). `PrefabUtility.SaveAsPrefabAsset` 멱등(재실행 시 GUID 유지).
- 시트 소스 `Assets/Sprite/vfx/brackeys_vfx_bundle/brackeys_vfx_bundle/predrawn/` (Multiple-mode 슬라이스 완료, 서브스프라이트 `{base}_{N}`):
  | Skill | ID | Loop (처음 ≤60프레임) | Hit [0..29] | (검증된 프레임 수) |
  | --- | --- | --- | --- | --- |
  | FireOrb | 231 | fire_point_6x5 | explosion_6x5 | 45 / 284 |
  | FireRing | 232 | fire_ring_6x5 | explosion_6x5 | 30 / 284 |
  | ElectricRing | 233 | electric_ring_6x5 | star_explosion_6x5 | 55 / 34 |
  | Vortex | 234 | vortex_6x5 | explosion_6x5 | 64 / 284 |
  | LightStreak | 235 | lightstreaks_6x5 | big_hit_6x5 | 500 / 311 |
  | WavyBolt | 236 | wavy_blue_6x5 | explosion_6x5 | 121 / 284 |
  | Charge | 237 | charge_7x6 | impact_white_6x4 | 256 / 78 |
  | BloodBolt | 238 | dithered_fire_6x5 | blood_impact_6x5 | 500 / 59 |
  (wavy_purple_6x5(119)는 이번 라운드 미사용 — 후속 플랜 여유분.)
- magicskill.csv 8행(231-238, 10열 스키마 유지) + `DataImportMenu.prefabMap` 8건 + `ImportSkillDataOnly`→`LinkSkillPrefabs` 배치 실행으로 에셋 8개 생성·링크 (에셋 명명 `{id}_{Name}.asset` = DataImportMenu.cs:241).
- 테스트: `SkillDataIntegrityTests.cs` 갱신(CanonicalAssetNames 14→22, CanonicalPrefabLinks 13→21, SkillInventoryClean 15→23, SkillIdsUnique ≥15→≥23, 신규 `BrackeysVFXAnimatorWired` 231-238) + `SkillExecutionTests.cs` PlayMode 임팩트 테스트 1종(231).
- FILE_MAP.md 증분 갱신 (각 todo 검증 통과 직후 — "고치면 바로바로 파일맵" 사용자 요구). ⚠️ finish-skill-system-rework v1.3이 FILE_MAP.md를 **전체 재작성**함 — brackeys 항목은 재작성 후 버전에 병합해 보존 (실행 시점 현재 파일 기준, 덮어쓰기 금지).

### Must NOT have (guardrails, anti-slop, scope boundaries)
- NO 기존 221-226 파이프라인 변경: `MagicVFXBuilder.cs`·기존 프리팹 6종·기존 SkillData 에셋 무접촉 (`git diff` 검증 가능).
- NO 런타임 코드: `SpriteVFXAnimator.cs`·`Projectile.cs`·`SkillData.cs`·`PlayerController.cs`·`MeleeHitbox.cs` 무변경.
- NO 장착/프리셋: preset.csv·Player.prefab·EquippedSkills·SkillPresets 에셋 무변경 (신규 8종은 데이터에만 존재).
- NO brackeys 텍스처 .meta·임포트 설정 변경 (리슬라이싱·재설정 금지), flipbooks(14 TGA)·particles(185장) 미사용.
- NO 301/227 (TimeStop/TimeWarp) 관련 변경 — byte-identical (`git diff` EMPTY).
- NO 다른 CSV·tiger 데이터 변경; NO 제외 팩(VividMotion/Pipoya 등) 사용.
- NO Assets 밖 편집 (magicskill.csv + FILE_MAP.md 제외).
- NO 실행자 커밋 (사용자 직접), `ImportAll` 사용 금지 — `ImportSkillDataOnly` → `LinkSkillPrefabs`만.
- NO Animator 컴포넌트/AnimationClip/ParticleSystem — SpriteVFXAnimator 코드 애니메이션만.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after (기존 계약 테스트가 end-state를 고정: CanonicalAssetNames/PrefabLinks/SkillInventoryClean/BrackeysVFXAnimatorWired). Framework: com.unity.test-framework (기존 스위트 재사용).
- Unity binary: `"D:\coding\6000.3.12f1\Editor\Unity.exe"` (존재 확인됨 — timestop/magic-skill-vfx 선례). 사용자 Unity 에디터가 켜져 있으면 프로젝트 락 — 실행 전에 종료 확인.
- Compile check (코드 변경 todo마다): `Unity.exe -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile "D:\coding\github c\clubgame\.omo\evidence\task-<N>-brackeys-skills-compile.log"` → 로그에 `error CS` 없음.
- Test run template: `... -batchmode -runTests -projectPath "D:\coding\github c\clubgame" -testPlatform EditMode|PlayMode -testResults "...\task-<N>-brackeys-skills-<platform>.xml" -logFile "...\task-<N>-brackeys-skills-<platform>.log"` → PASS = XML 루트 `<test-run result="Passed" failed="0"`.
- executeMethod template: `... -batchmode -quit -executeMethod <Class.Method> -projectPath "D:\coding\github c\clubgame" -logFile "...\task-<N>-brackeys-skills.log"` → exit 0 + 로그 검증.
- ⚠️ 예정된 중간 상태: Todo 2 완료(231-238 에셋 생성, 루트 15→23) 후 Todo 5 갱신 전까지 EditMode `SkillInventoryClean` 1개만 실패(기대 15, 실제 23). 다른 테스트는 영향 없음(CanonicalSkillsWired/SkillPrefabStructure는 CanonicalPrefabLinks 21개 키만 순회 — Todo 5에서 21로 갱신됨). 전체 EditMode green은 Todo 5에서 확정.
  - GOTCHAs (magic-skill-vfx/timestop 선례 — 배치 고찰: `.omo/plans/magic-skill-vfx.md` · `.omo/plans/timestop-style-magic-skills.md`): `-runTests`와 `-quit` 동시 전달 금지; `-logFile` 항상 실제 파일 경로; PS 5.1은 `& Unity.exe` 대기 안 함 → `Start-Process -Wait -PassThru` + `.ExitCode`; `-ArgumentList` 공백 인자 수동 인용 `'-projectPath "D:\coding\github c\clubgame"'`; 프로젝트 락 경합 시 exit 1 + 로그에 `error CS` 없음 → 에디터 종료 확인 후 대기+재시도.
- Evidence: 모든 todo는 `.omo/evidence/task-<N>-brackeys-skills.<ext>`; F-태스크는 `.omo/evidence/final-<N>-brackeys-skills.<ext>`.
- 첫 단계: `git status --porcelain` → 사전 dirty/untracked 파일을 `.omo/evidence/task-0-brackeys-skills-dirty-worktree.txt`에 스냅샷 (실제 현재 dirty: **PixelArtRPGVFXLite 이동** — magic-skill-vfx·timestop 산출물은 이미 커밋됨; 스냅샷 항목은 건드리지 않음, 커밋은 사용자 몫).

## Execution strategy
### Parallel execution waves
- **Wave 1** (순서 고정: Todo 1 → Todo 2): Todo 1 (BrackeysVFXBuilder.cs + 프리팹 8종 빌드) · Todo 2 (CSV 8행 + prefabMap + ImportSkillDataOnly 배치) — 파일 완전 분리(Assets/Editor + Assets/Prefabs vs tiger/datafiles + Assets/Editor/DataImportMenu.cs), 상호 의존 없음. 단, 둘 다 Unity 배치모드를 쓰므로 실제 실행은 직렬화 (락 경합 시 대기+재시도). **순서 중요**: Todo 1의 EditMode 검증(파일 수 불변 15)이 Todo 2의 Import(15→23)보다 먼저 실행되어야 "green" 판정이 성립. Todo 2부터 실행하면 SkillInventoryClean 실패가 예정 상태로 섞이므로 반드시 1 → 2 순서.
- **Wave 2**: Todo 3 (PlayMode 임팩트 테스트 231) — Todo 1 의존 (231 프리팹이 있어야 실제 실행; 없으면 스킵 가드).
- **Wave 3**: Todo 4 (LinkSkillPrefabs 배치 — 231-238 프리팹 링크) — Todo 1/2 의존 (프리팹 8종 + 에셋 8개가 모두 있어야 링크 가능).
- **Wave 4**: Todo 5 (무결성 스위트 갱신 + BrackeysVFXAnimatorWired) — Todo 2/3/4 의존, 최종 상태 명세 (EditMode 18 / PlayMode 6 green 확정).

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 BrackeysVFXBuilder+프리팹 8종 | — | 3, 4 | 2 |
| 2 CSV+prefabMap+Import | — | 4, 5 | 1 |
| 3 PlayMode 임팩트 테스트 | 1 | 5 | 4 |
| 4 LinkSkillPrefabs 배치 | 1, 2 | 5 | 3 |
| 5 무결성 스위트 갱신 | 2, 3, 4 | — | — |

> ⚠️ 'Can parallelize with' 셀 = 파일 충돌 없음(그래프상)만 의미 — 실제 실행은 Wave 1의 강제 직렬 순서(Todo 1 → 2)를 따른다 (RED 판정 정합, 위 Wave 1 주석 참조).

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. BrackeysVFXBuilder.cs 작성 + 프리팹 8종 빌드 (231-238)
  What to do / Must NOT do: 신규 `Assets/Editor/BrackeysVFXBuilder.cs` (MagicVFXBuilder.cs 패턴 미러 — 320줄 참조, 기존 파일 무접촉). (a) `SheetStageSpec(string SheetPath, string Prefix, int? First = null, int? Last = null)` 구조체/클래스: `AssetDatabase.LoadAllAssetsAtPath<Sprite>(SheetPath)` 로드 → 이름 `^{Prefix}_(\d+)$` 정규식 필터 → **`int.Parse` 자연 정렬**(문자열 정렬 금지 — `_10`이 `_2` 앞에 오는 오류) → `[First..Last]` 슬라이스, 경계 초과 시 **클램프 + `Debug.LogWarning`** (미검증 카운트 시트에 안전; First/Last null = 전체; **인덱스는 0-based — `[0..29]` = 처음 30프레임**). (b) 8종 SkillSpec 테이블 (아래 Scope 표 그대로 — SheetPath = `Assets/Sprite/vfx/brackeys_vfx_bundle/brackeys_vfx_bundle/predrawn/{base}.png`, Prefix = base 이름, Loop=처음 min(시트프레임수,60) 프레임 [0-based], Hit=처음 30프레임 [0..29]): 231 FireOrb (fire_point_6x5 / explosion_6x5), 232 FireRing (fire_ring_6x5 / explosion_6x5), 233 ElectricRing (electric_ring_6x5 / star_explosion_6x5), 234 Vortex (vortex_6x5 / explosion_6x5), 235 LightStreak (lightstreaks_6x5 / big_hit_6x5), 236 WavyBolt (wavy_blue_6x5 / explosion_6x5), 237 Charge (charge_7x6 / impact_white_6x4), 238 BloodBolt (dithered_fire_6x5 / blood_impact_6x5). (c) 각 프리팹: GO `{Name}Projectile` → Transform localScale **(s_i, s_i, 1)** — s_i = clamp(21 / maxFramePx_i, 0.05, 1.0), maxFramePx_i = 해당 스킬 loop/hit 시트 최대 프레임 변 px (sprite.textureRect에서 수집, **blind scale 3 상속 금지** — FireBall 7×7px × scale 3 = 0.21 유닛 정합 목표) → SpriteRenderer sortingOrder 10 + sprite=첫 loop 프레임 → CircleCollider2D isTrigger radius 0.2 → `Projectile`(speed 15, lifeTime 3 — SerializedObject; **템플릿 기본값일 뿐 — 런타임 속도는 `PlayerController.Initialize`가 CSV `ProjectileSpeed`(12-18)로 덮어씀**, oracle minor 8; `ObjectPooler.ReturnToPool`은 SetActive(false)만 — 기본 poolTag Projectile 안전) → `SpriteVFXAnimator`(startFrames **빈 배열**, loopFrames=**처음 min(시트프레임수,60) 프레임** [0-based 클램프, 60프레임 = 5초@12fps 상한], hitFrames=[0..29] 클램프, fps 12, autoPlay true) → `PrefabUtility.SaveAsPrefabAsset`(`Assets/Prefabs/Projectiles/{Name}Projectile.prefab`, 멱등 — 재실행 시 GUID 유지). (d) `public static void BuildAndVerifyAllBrackeysVFX()` (배치 `-executeMethod` 진입점 — 빌드 후 자체 검증: 8개 프리팹 로드 + SpriteVFXAnimator 존재 + loop/hit 비어있지 않음 + **시각 연속성 게이트(F8): loopFrames 인덱스가 자연정렬 순서로 연속(인접 차이 1)이고 첫≠마지막 프레임**, 실패 시 `Debug.LogError`+throw → exit non-zero) + `[MenuItem("Custom Tools/tiger/Magic VFX/Build Brackeys VFX Prefabs")]` + `public static void VerifyAllBrackeysVFX()`. Must NOT: `MagicVFXBuilder.cs`/기존 프리팹 6종/TimeStop_Effect.prefab 수정, .meta·임포트 설정 변경, Animator/ParticleSystem, flipbooks/particles 사용, 14개 시트 중 wavy_purple_6x5(119) 미사용 유지.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 3, 4
  References: `Assets/Editor/MagicVFXBuilder.cs:179-248` (BuildSkillPrefab — 컴포넌트 구성+SaveAsPrefabAsset+SerializedObject 패턴), `:266-318` (LoadStage — 분리 PNG 로더, brackeys는 단일텍스처 슬라이스라 재사용 불가 근거), `:97-124` (구조/배치 진입점 BuildAndVerifyAllMagicVFX:100, BuildAllMagicVFX:114, VerifyAllMagicVFX:130); 단일텍스처 슬라이스 자연정렬 로더 선례 `Assets/Editor/TimeStopEffectBuilder.cs:116-135` (LoadSortedFrames:116, ParseFrameNumber:132 — brackeys와 동일 패턴: 이름 `_(\d+)` 파싱 + 자연정렬); 프리팹 템플릿 `Assets/Prefabs/Projectiles/FireBallProjectile.prefab:22-36` (scale 3 — m_LocalScale :33), `:85` (m_SortingOrder: 10), `:96-132` (CircleCollider2D trigger radius 0.2 — m_IsTrigger :127, m_Radius :132), `:133-146` (Projectile guid `748bc7fe4f5592044adef09a9696c5a8` :142 — `Projectile.cs.meta`와 일치 확인, speed 15 :145, lifeTime 3 :146); 시트 슬라이스 증거 `Assets/Sprite/vfx/brackeys_vfx_bundle/brackeys_vfx_bundle/predrawn/explosion_6x5.png.meta:4-97` (spriteMode:2 + `second: explosion_6x5_\d+` 명명), `charge_7x6.png.meta:4-59`; `Assets/Script/Combat/SpriteVFXAnimator.cs` (필드 startFrames/loopFrames/hitFrames/fps/autoPlay); FireBall 템플릿 스프라이트 크기 근거 `Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/sprites/FireBomb/Fire-bomb4.png.meta:104-109` (rect 7×7px → scale 3 = 0.21 유닛).
  Acceptance criteria (agent-executable): 컴파일 체크 green (`error CS` 0); 배치 `-executeMethod BrackeysVFXBuilder.BuildAndVerifyAllBrackeysVFX` exit 0 + 로그에 PASSED; `Assets/Prefabs/Projectiles/`에 8개 프리팹(+.meta) 존재 — 각각 GameObject로 로드 시 SpriteVFXAnimator 보유, **loopFrames.Length == min(시트프레임수,60) (>0)**, hitFrames.Length == 30, fps == 12, sortingOrder == 10, **localScale == 로그 기록된 (s_i, s_i, 1) (스킬별 계산값과 일치)**, CircleCollider2D isTrigger (Todo 5 `BrackeysVFXAnimatorWired`가 최종 고정, 이 wave에선 빌더 자체 검증 + 1회성 throwaway EditMode 체크로 확인).
  QA scenarios: happy — 배치 빌더 실행 + 로그 PASSED + 프리팹 8개 SerializedObject 검증, 증거 `.omo/evidence/task-1-brackeys-skills.txt`; failure — (A) `SheetStageSpec.Prefix`를 틀리게(예: "explosion_6x") 설정 → 프레임 0개 → `Debug.LogError`+중단 확인 후 복구, (B) **`.prefab` 파일만 삭제(`.meta` 보존)** 후 재실행 → 재생성+GUID 유지(멱등) 확인 — ⚠️ `AssetDatabase.DeleteAsset`은 meta까지 제거해 GUID가 새로 발급되므로, GUID 유지 검증은 **파일시스템에서 .prefab만 삭제**(meta 유지) 방식으로 수행 (oracle minor 6), 증거 `.omo/evidence/task-1-brackeys-skills-qa-fail.log`.
  Commit: N (사용자 직접 커밋) | feat(vfx): build 8 brackeys projectile prefabs with 3-stage sprite VFX

- [x] 2. magicskill.csv 8행 (231-238) + DataImportMenu.prefabMap 8건 + ImportSkillDataOnly 배치
  What to do / Must NOT do: (a) `tiger/datafiles/skill/magicskill.csv` 227행(8행) 뒤에 정확히 8행 추가 (10열 헤더 `ID,Name,Damage,ManaCost,Cooldown,Type,Bubble,Speed,MeleeRange,MeleeArc` 유지): `231,FireOrb,40,30,3.0,Projectile,None,15,0,0` / `232,FireRing,35,28,3.5,Projectile,None,14,0,0` / `233,ElectricRing,35,28,3.5,Projectile,None,14,0,0` / `234,Vortex,45,35,4.0,Projectile,None,12,0,0` / `235,LightStreak,30,22,2.5,Projectile,None,18,0,0` / `236,WavyBolt,30,25,2.8,Projectile,None,16,0,0` / `237,Charge,25,20,2.0,Projectile,None,12,0,0` / `238,BloodBolt,35,28,3.2,Projectile,None,15,0,0` (전부 밸런스 밴드 내: 대미지 25-45/마나 15-35/쿨 1.5-4.0/속도 12-20, Bubble None). (b) `Assets/Editor/DataImportMenu.cs` prefabMap(310-324) 227행 뒤에 8건 추가: `prefabMap[231] = "Assets/Prefabs/Projectiles/FireOrbProjectile.prefab";` … `prefabMap[238] = "Assets/Prefabs/Projectiles/BloodBoltProjectile.prefab";` (스킬명 = 프리팹명 매핑 정확히 일치). (c) 배치 실행 `-executeMethod DataImportMenu.ImportSkillDataOnly` → 로그 "Skill Data Import Complete!" + `Assets/Resources/SkillData/` 루트에 `{231..238}_{Name}.asset` 8개 생성 확인 (명명 `{id}_{Name}.asset` = DataImportMenu.cs:241). ⚠️ `ImportSkillDataOnly`(DataImportMenu.cs:294-300)는 내부적으로 `ImportSkillData()`(205-212)를 호출해 **ranged/melee/magic CSV 3종 전부**를 멱등 재임포트함 — 기존 에셋은 동일 값 덮어쓰기라 파일 diff 없음 (oracle minor 7, F1의 "정확한 파일 세트" 검사에서 re-dirty로 오인 금지). ⚠️ 예정된 중간 상태: 이 todo 완료 후 EditMode `SkillInventoryClean` 1개만 실패(기대 15, 실제 23) — Todo 5에서 갱신, 다른 테스트는 green. Must NOT: `ImportAll` 금지, preset.csv/Player.prefab/다른 CSV 불변, 이 todo에서 `LinkSkillPrefabs` 실행 금지 (Todo 4에서), magicskill.csv 헤더 수정 금지.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 4, 5
  References: `tiger/datafiles/skill/magicskill.csv:1-8` (헤더+기존 221-227행 — 227 `TimeWarp` 행 뒤에 추가); `Assets/Editor/DataImportMenu.cs:214-291` (ImportSkillFile — 실제 시작 214, 헤더 동적 파싱:224-230, Enum.TryParse:255, Bubble:260-269, Speed:271-275), `:241` (에셋 명명 `{id}_{Name}.asset`), `:294-300` (ImportSkillDataOnly 배치 진입점), `:310-324` (prefabMap 201-227 — 231-238을 227 뒤에 추가); `Assets/Script/Combat/SkillData.cs:5-35` (필드 17개).
  Acceptance criteria (agent-executable): 배치 exit 0 + 로그에 error 없음 + "Skill Data Import Complete!"; `Assets/Resources/SkillData/` 루트 = 23 (231-238 에셋 8개 존재: 231_FireOrb.asset … 238_BloodBolt.asset, +.meta); 에셋 필드 CSV 일치 (throwaway EditMode 체크 또는 1회성 `-executeMethod` 검증: SkillType==Projectile, Damage/ManaCost/Cooldown/ProjectileSpeed CSV 값 일치, UseBubbleEffect false).
  QA scenarios: happy — import 로그 + 에셋 8개 필드 검증, 증거 `.omo/evidence/task-2-brackeys-skills.log`; failure — CSV Type 열을 임시로 "Melee"로 변경 → 재임포트 후 231 에셋 SkillType이 Melee가 됨을 확인(파싱 회귀 경로 증명) → CSV 복구 재실행, 증거 `.omo/evidence/task-2-brackeys-skills-qa-fail.log`.
  Commit: N (사용자 직접 커밋) | feat(data): add brackeys magic skills 231-238 to CSV and prefab map

- [x] 3. PlayMode 임팩트 테스트 1종 (231 FireOrb — VFX 보유 프리팹)
  What to do / Must NOT do: `Assets/Tests/PlayMode/SkillExecutionTests.cs`에 신규 테스트 `BrackeysVFX_PlaysHitAndDelaysDeactivation` 추가 — magic-skill-vfx Todo-2 패턴 미러: ① `CreateFireBallSkill` 헬퍼(124-136) 미러로 231 FireOrb SkillData 생성 (`SkillType = SkillType.Projectile`, `ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/FireOrbProjectile.prefab")`), ② **스킵 가드**: 프리팹이 null이거나 `GetComponentInChildren<SpriteVFXAnimator>(true)`가 null이면 `Assert.Ignore("231 prefab not built yet")` (Todo 1 전에 이 테스트가 실행돼도 실패 대신 스킵), ③ 임팩트 시나리오: EquipSkill(0, skill) → InvokeUseSkill(0) → 투사체가 적에 충돌 → 적 `Health.CurrentHealth` 감소 단언 AND 투사체가 임팩트 직후 즉시 파괴되지 않음(`HitDuration` 지연 Invoke — 존재 유지 확인) → `HitDuration + 0.5f` 이내에 Deactivate(파괴/비활성) 확인. Must NOT: Projectile.cs/PlayerController.cs/SpriteVFXAnimator.cs 수정, 기존 테스트 변경, `FindObjectsByType` 이름 스캔 대신 타입 직접 참조(Assembly-CSharp 제약 — timestop 선례 참고: `.omo/plans/timestop-style-magic-skills.md`).
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 5
  References: `Assets/Tests/PlayMode/SkillExecutionTests.cs:124-136` (CreateFireBallSkill 헬퍼 패턴 — 실제 시작 124), `:182-204` (EquipSkill/InvokeUseSkill/ResetSkillCooldowns — :167-180은 GetPlayerControllerType), `:233-302` (WaitForSeconds 대기 패턴); magic-skill-vfx 선례: `.omo/plans/magic-skill-vfx.md` Todo 2 (ProjectileWithVFX_PlaysHitAndDelaysDeactivation — 임팩트 시나리오 전체, `_stage`/파괴 타이밍 단언); `Assets/Script/Combat/Projectile.cs:38-82` (OnEnable/HandleImpact/Deactivate — `_vfx.HitDuration` 지연 Invoke, 59-74 히트 훅); `Assets/Script/Combat/SpriteVFXAnimator.cs` (`PlayHit()`/`HitDuration` API).
  Acceptance criteria (agent-executable): 컴파일 체크 green; PlayMode 스위트 green — XML `result="Passed" failed="0"`에 `BrackeysVFX_PlaysHitAndDelaysDeactivation` test-case가 **`result="Passed"`로 기록** (Skipped/Ignored 아님 — XML에서 test-case 행 grep으로 확인, PlayMode 5→6); EditMode 회귀 green (SkillInventoryClean 1개 예정 실패 제외 — Todo 2 중간 상태, Todo 5에서 해소).
  QA scenarios: happy — PlayMode XML green, 증거 `.omo/evidence/task-3-brackeys-skills-playmode.xml`; failure — (A) 스킵 가드를 제거한 채 Todo 1 프리팹이 없으면 실패함을 확인 후 복구(가드 실증), (B) 임팩트 직후 즉시 Deactivate로 임시 변경 → 지연 파괴 단언 실패 증명 후 복구, 증거 `.omo/evidence/task-3-brackeys-skills-qa-fail.xml`.
  Commit: N (사용자 직접 커밋) | test(vfx): add PlayMode impact test for brackeys FireOrb prefab

- [x] 4. LinkSkillPrefabs 배치 (231-238 프리팹 링크)
  What to do / Must NOT do: 배치 실행 `-executeMethod DataImportMenu.LinkSkillPrefabs` → 로그 `Linked=22` (14→22, 231-238 포함) 확인. 독립 검증(misleading_success_output 프로브 — 로그만 신뢰하지 않음): `231_FireOrb.asset` … `238_BloodBolt.asset` 8개의 YAML `ProjectilePrefab: {fileID: …, guid: <새 프리팹 guid>, type: 3}` — guid가 Todo 1에서 생성된 프리팹과 정확히 일치(교차 확인). Must NOT: 프리팹 경로 오타(LinkSkillPrefabs:350-354가 LogError+skip), 다른 에셋 링크 변경(기존 14개 재링크는 멱등), prefabMap 201-227 항목 수정, 이 todo에서 테스트 갱신(다음 wave).
  Parallelization: Wave 3 | Blocked by: 1, 2 | Blocks: 5
  References: `Assets/Editor/DataImportMenu.cs:303-364` (LinkSkillPrefabs — prefabMap 310-324, 루트 순회 327-358, Linked 로그 363, 프리팹 없을 때 LogError 350-354); Todo 1 프리팹 경로/guid (빌더 실행 로그에서 수집); `Assets/Resources/SkillData/231_FireOrb.asset` … `238_BloodBolt.asset` (YAML 검증 대상).
  Acceptance criteria (agent-executable): 배치 exit 0 + 로그 `Linked=22`; 8개 에셋 YAML 각각 `ProjectilePrefab` guid == 해당 프리팹 meta guid (스크립트/수동 grep 교차 확인); 컴파일 체크 불필요(에디터 코드 변경 없음) 단 EditMode 회귀 확인(예정된 SkillInventoryClean 1개 실패 외 green).
  QA scenarios: happy — link 로그 + 8개 YAML guid 교차 검증, 증거 `.omo/evidence/task-4-brackeys-skills-link.log`; failure — 231 프리팹을 임시로 이동 → LinkSkillPrefabs가 `LogError: prefab not found` + 해당 스킬 미링크(skip) → 복구 후 재실행 Linked=22, 증거 `.omo/evidence/task-4-brackeys-skills-qa-fail.log`.
  Commit: N (사용자 직접 커밋) | chore(data): link brackeys skill prefabs via LinkSkillPrefabs

- [x] 5. 무결성 스위트 갱신 (14→22/13→21/15→23 + BrackeysVFXAnimatorWired 신규)
  What to do / Must NOT do: `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` 갱신: ① `CanonicalAssetNames`(18-25) += 8개 `"231_FireOrb.asset"` … `"238_BloodBolt.asset"` (22개), ② `CanonicalPrefabLinks`(28-43) += 8건 `{231, "Assets/Prefabs/Projectiles/FireOrbProjectile.prefab"}` … `{238, …}` (21개) — ⚠️ 227/301과 달리 231-238은 전부 Projectile이라 `CanonicalSkillsWired`(102-134)의 `id >= 211 → Projectile` 기대(114-119)와 무충돌, Bubble None이므로 기대 버블(121-124: 212/213/222만)과도 무충돌 — 추가 안전함, ③ `SkillInventoryClean`(49-75) `Assert.AreEqual(15, …)`(53) → `23` + 주석(51) 갱신, ④ `SkillIdsUnique`(80-96) `GreaterOrEqual(…, 15)`(85) → `23`, ⑤ 신규 `BrackeysVFXAnimatorWired`: ids {231..238} × 기대 prefab 경로 — `LoadRootSkillById`로 로드 → `SkillType==SkillType.Projectile` + `ProjectilePrefab` non-null + `AssetDatabase.GetAssetPath` 매치 + 프리팹에 `SpriteVFXAnimator` 존재 확인 (`GetComponentInChildren<SpriteVFXAnimator>(true)` non-null — EditMode asmdef가 ClubGame.Combat 참조하므로 직접 타입 사용 가능, MagicVFXAnimatorWired 151과 동일) + SerializedObject로 loopFrames.Length > 0, hitFrames.Length > 0, fps > 0 (startFrames는 빈 배열 허용 — 223/224/226 선례 168-175). 기존 테스트 약화 금지: `MagicVFXAnimatorWired`(139-177, 221-226)·`InstantAreaSkillsWired`(290-328, 301/227)·`TimeStopUntouched`(270-285)·`PlayerEquipsGumMaster`(220-265)·`PresetsResolveToRoot`(333-363) 유지. (f) FILE_MAP.md 증분 갱신 — ⚠️ **이 todo가 첫 갱신이 아님**: "고치면 바로바로 파일맵" 규칙상 각 todo(Todo 1-4) 검증 통과 직후 그 todo의 변경 파일을 즉시 반영하고, (f)는 Todo 5 자기 변경(테스트 파일 2종) + 최종 정합만 담당. finish-skill-system-rework v1.3이 FILE_MAP.md를 전체 재작성할 수 있으므로, 실행 시점 현재 파일 기준으로 **병합**(기존 항목 보존·덮어쓰기 금지) — 순서: finish-rework 실행 후라면 그 재작성 버전 위에 증분 적용. 최종 검증: EditMode 전체 green (17→18) + PlayMode green (6).
  Parallelization: Wave 4 | Blocked by: 2, 3, 4 | Blocks: —
  References: `Assets/Tests/EditMode/SkillDataIntegrityTests.cs:18-25` (CanonicalAssetNames), `:28-43` (CanonicalPrefabLinks), `:49-75` (SkillInventoryClean — assert 53), `:80-96` (SkillIdsUnique — assert 85), `:102-134` (CanonicalSkillsWired — 231-238 무충돌 근거: 114-119 타입 기대, 121-124 버블 기대), `:139-177` (MagicVFXAnimatorWired — BrackeysVFXAnimatorWired의 미러 템플릿: SerializedObject loop/hit/fps 단언 구조, 151 GetComponentInChildren, 154-166 SerializedObject), `:290-328` (InstantAreaSkillsWired — LoadRootSkillById/경로 매치 패턴), `:370-394` (helpers GetRootSkillDataPaths/LoadRootSkillById); `Assets/Script/Combat/SpriteVFXAnimator.cs` (필드명/타입); Todo 1 프리팹 경로, Todo 2 에셋 이름.
  Acceptance criteria (agent-executable): 컴파일 체크 green; EditMode XML `result="Passed" failed="0"` (기존 17 + BrackeysVFXAnimatorWired 1 = 18, 예정된 SkillInventoryClean 실패 해소); PlayMode XML green (6); `Assets/Resources/SkillData` 루트 = 23 (단언 확인); FILE_MAP.md에 신규 17개 파일 반영.
  QA scenarios: happy — EditMode+PlayMode XML green, 증거 `.omo/evidence/task-5-brackeys-skills.xml` + `.omo/evidence/task-5-brackeys-skills-playmode.xml`; failure — (A) `BrackeysVFXAnimatorWired`의 231 기대 프리팹 경로를 틀리게 → 실패 확인 후 복구, (B) 231 프리팹에서 hitFrames 제거(빌더 수정+재빌드) → BrackeysVFXAnimatorWired hitFrames 단언 실패 입증 → 빌더 복구 재빌드, 증거 `.omo/evidence/task-5-brackeys-skills-qa-fail.xml`.
  Commit: N (사용자 직접 커밋) | test(skill): update integrity suite for 23-skill end-state and brackeys VFX wiring

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [x] F1. Plan compliance audit — for every todo 1-5: checkbox checked, evidence file present at the documented path, worktree diff contains exactly the planned file set (BrackeysVFXBuilder.cs, 8 new prefabs + .meta, magicskill.csv, DataImportMenu.cs, 8 new SkillData .asset + .meta, SkillDataIntegrityTests.cs, SkillExecutionTests.cs, FILE_MAP.md) vs. the task-0 snapshot; no file outside scope changed.
- [x] F2. Code quality review — SheetStageSpec 자연 정렬(int.Parse) + 클램프 정확성; 빌더 멱등(GUID 유지) + 자체 검증 throw; 테스트 실질적(스폰+임팩트+지연 파괴 행동 증명); 데드 코드/로그 노이즈 없음; Animator/Particle 어디에도 없음; CSV 행 8개 스키마·밸런스 밴드 준수.
- [x] F3. Real manual QA — 헤드리스 런타임 하네스 (PlayMode 스위트 = 231 발사→적 충돌→히트 VFX→지연 파괴 증명). ⚠️ 남은 사용자 단계: 에디터 열고 프리팹 8종을 씬에 드래그해 Loop→Hit 애니메이션 눈으로 확인. **사용자 육안 단계는 F-게이트 판정 대상 아님** (게이트 F1-F4는 에이전트 실행·헤드리스로만 판정; 사용자 확인은 승인 후 후속 절차 — mF5).
- [x] F4. Scope fidelity — Must NOT 목록 walk (diff-검증 가능 항목): `MagicVFXBuilder.cs`/기존 프리팹 6종/기존 SkillData 에셋 `git diff` EMPTY, `SpriteVFXAnimator.cs`/`Projectile.cs`/`SkillData.cs`/`PlayerController.cs` diff EMPTY, preset.csv/Player.prefab/다른 CSV diff EMPTY, `Assets/Sprite/vfx/brackeys_vfx_bundle/**/*.meta` 무변경, 301/227 파일 diff EMPTY, ImportAll 미사용(로그 확인), 신규 런타임 코드 없음.

## Commit strategy
- Commits are made by the USER directly (project convention). The executor stages nothing and commits nothing; it reports the exact per-todo file list so the user can commit atomically: (1) BrackeysVFXBuilder.cs + 8 prefabs (+.meta); (2) magicskill.csv + DataImportMenu.cs + 8 SkillData .asset (+.meta); (3) SkillExecutionTests.cs; (4) (LinkSkillPrefabs 배치 — 파일 변경 없음, 에셋 링크만); (5) SkillDataIntegrityTests.cs + FILE_MAP.md.
- FILE_MAP.md must be updated IMMEDIATELY after each todo's verification passes — never deferred (user requirement). Incremental only: list the new files (BrackeysVFXBuilder.cs, 8 prefabs, 8 assets), update DataImportMenu.cs responsibilities, add the 8 skills to the skill table, remove nothing from TimeStop/magic-skill-vfx notes.
- 실제 현재 dirty는 **PixelArtRPGVFXLite 이동** (magic-skill-vfx·timestop 산출물은 이미 커밋됨 — task-0 스냅샷 참조). 그쪽 먼저 커밋하는 것은 사용자 몫이며, 이 플랜의 변경과 섞이지 않도록 `git add -A` 금지 (todo별 명시 경로만 스테이징 안내).
- Evidence files under `.omo/evidence/` are NOT committed.

## Success criteria
- All 5 todo checkboxes AND F1-F4 checked, each with evidence under `.omo/evidence/`.
- Batchmode runs green: compile check (no `error CS`), EditMode suite (`result="Passed" failed="0"`, 18 tests), PlayMode suite green (6 tests) including the new brackeys impact test.
- 23 SkillData assets at root (22 canonical + 301); new skills 231-238 in CSV with SkillType=Projectile, balance-band stats, and linked prefabs.
- 8 new prefabs exist with SpriteVFXAnimator (loopFrames = first min(sheetFrames, 60) frames [0-based], hitFrames = clamped [0..29], fps 12, per-skill computed localScale logged) — BrackeysVFXAnimatorWired passes.
- `git diff` EMPTY on: `MagicVFXBuilder.cs`, existing 6 magic prefabs, `SpriteVFXAnimator.cs`, `Projectile.cs`, `SkillData.cs`, `PlayerController.cs`, `MeleeHitbox.cs`, `preset.csv`, `Player.prefab`, `Assets/Sprite/vfx/brackeys_vfx_bundle/**/*.meta`, 301/227 files.
- FILE_MAP.md reflects the final state (incremental).
