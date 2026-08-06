# timestop-style-magic-skills - Work Plan

## TL;DR (For humans)

**What you'll get:** 타임스톱 스킬(301)이 실제로 발동됩니다. 지금은 스킬 타입을 게임 코드가 아직 지원하지 않아 '아무 일도 안 일어나는' 상태인데, 이번에 발동 처리를 넣어 적들이 5초간 멈추는 연출이 작동합니다. 그리고 같은 스타일의 새 스킬 '타임워프'(227)가 추가됩니다 — 초록 시계 이펙트가 플레이어 주위를 감싸며 적을 3초 멈춥니다. 두 스킬 모두 코드 기반 스프라이트 애니메이션이라 가볍고, 타임워프는 기존 타임스톱 이펙트 부품을 그대로 재사용합니다.

**Why this approach:** 301 스킬의 데이터 파일에 '즉시 발동형(InstantArea)' 표시가 빠져 있어서(필드 자체가 없음) 게임이 이 스킬을 발사체로 오인해 아무것도 스폰하지 않았습니다. 데이터 파일에 타입 한 줄만 추가하고, 발동 케이스를 기존 근접 광역 스킬(MeleeAoE)과 똑같은 패턴으로 구현하면 됩니다. 새 스킬은 기존 타임스톱 이펙트 컴포넌트와 애니메이션 부품을 재사용하므로 새 게임 로직이 전혀 필요 없습니다. 프리팹은 수작업 대신 에디터 빌더 스크립트가 자동 생성해 실수 위험이 없고, 무결성 테스트가 "모든 스킬이 제대로 연결됐는지"를 검증합니다.

**What it will NOT do:** 타임스톱 이펙트 프리팹과 그 스크립트 자체는 건드리지 않습니다(301 데이터 파일에 타입 한 줄만 추가). 플레이어 장착 스킬(4슬롯), 스킬 프리셋, 적, 대미지 시스템은 그대로입니다. 새 타임워프 스킬은 게임 데이터와 테스트에만 존재하고 플레이어에게 장착되지 않습니다. 전체 재임포트(Import All)는 사용하지 않습니다.

**Effort:** Short-Medium — 코드 케이스 1개 + 에디터 빌더 1개 + 프리팹 1종 + 데이터 1행 + 테스트 3건 갱신/추가.
**Risk:** Low-Medium — 301 어셋의 '바이트 그대로' guardrail을 이번 스코프 한정으로 폐기하고 타입 필드 한 줄을 추가하지만, 나머지(이펙트 프리팹/스크립트)는 바이트 그대로 유지하고 테스트로 계약을 고정합니다.
**Decisions to sanity-check:** (1) 301 어셋에 SkillType 한 줄 추가 (전 플랜의 byte-identical guardrail 의도적 폐기). (2) 227 타임워프 스탯/이펙트 수치(마나 30, 쿨 10, 스턴 3초, 반경 15)는 제가 잡은 기본값. (3) 새 스킬은 장착 안 됨 — 데이터로만 존재.

Your next move: `$start-work`. Full execution detail follows below.

---

> TL;DR (machine): Short-Medium effort/risk; 4 commits — PlayerController InstantArea 케이스+301_TimeStop.asset SkillType:3+PlayMode 스턴 테스트, magicskill.csv 227행+prefabMap+ImportSkillDataOnly, TimeStopEffectBuilder.cs+TimeWarp_Effect.prefab(209 시트 15프레임)+227 Icon, LinkSkillPrefabs+무결성 14→15+TimeStopUntouched SkillType 단언+InstantAreaSkillsWired; 헤드리스 batchmode EditMode+PlayMode 검증; TimeStop_Effect.prefab/TimeStopEffect.cs byte-identical, Player.prefab/preset.csv 불변, ImportAll 금지.

## Scope
### Must have
- `PlayerController.cs:265-267` InstantArea 스텁을 MeleeAoE 패턴(253-263) 미러링으로 구현: null 프리팹 가드 + `Instantiate(skill.ProjectilePrefab, transform.position, Quaternion.identity)` + `spawned = true`. TimeStopEffect는 자가구동(Start/Update/Destroy)이라 Initialize 불필요.
- `Assets/Resources/SkillData/301_TimeStop.asset`에 `SkillType: 3` 한 줄 추가 (현재 YAML에 SkillType 필드 없음 = enum 기본값 0/Projectile로 디스패치되는 루트 원인).
- `tiger/datafiles/skill/magicskill.csv`에 `227,TimeWarp,0,30,10,InstantArea,None,0,0,0` 1행 추가 + `DataImportMenu.cs` prefabMap에 `prefabMap[227] = "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab"` 추가 + `ImportSkillDataOnly` 배치 실행으로 `227_TimeWarp.asset` 생성.
- 신규 `Assets/Editor/TimeStopEffectBuilder.cs` (MagicVFXBuilder 패턴): 209 시트(guid `3014f866b966d1240bfb57efd1ac6ac0`)의 15개 서브스프라이트(`_0`~`_14`, 숫자 자연 정렬)를 로드해 `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab` 생성 — Transform scale (5,5,1), SpriteRenderer sortingOrder 20, TimeStopEffect(radius 15/stunDuration 3/lifeTime 1.5), SimpleSpriteAnimator(15프레임, fps 12, loop false). 배치 `-executeMethod TimeStopEffectBuilder.BuildTimeWarpEffect` 진입점. 동시에 227 어셋 `Icon`을 209 시트 스프라이트(`AssetDatabase.LoadAssetAtPath<Sprite>` 반환값, 301 어셋:20 Icon 형식과 동일)로 설정.
- `LinkSkillPrefabs` 배치 실행(227 프리팹 링크, Linked=14) + `SkillDataIntegrityTests.cs` 갱신: CanonicalAssetNames +`"227_TimeWarp.asset"`, 루트 카운트 14→15(2곳), TimeStopUntouched +SkillType==InstantArea 단언, 신규 `InstantAreaSkillsWired`(301+227: 타입/프리팹 경로/TimeStopEffect 스크립트 존재). CanonicalPrefabLinks는 13개 유지(227 미등록).
- `SkillExecutionTests.cs`: TestBubbleAffectable.ApplyStun에 `StunCount++`(빈 메서드 268행), 신규 `InstantAreaSkill_SpawnsEffectAndStunsEnemy` PlayMode 테스트.
- FILE_MAP.md 증분 갱신 (사용자 요구: "고치면 바로바로 파일맵에 저장" — 전면 개편 아님).

### Must NOT have (guardrails, anti-slop, scope boundaries)
- `TimeStop_Effect.prefab` / `TimeStopEffect.cs` 수정 금지 — `git diff` EMPTY 필수 (301 어셋 필드 한 줄만 허용).
- `301_TimeStop.asset` diff는 `SkillType: 3` 한 줄만 (다른 필드/값 불변).
- `Player.prefab` EquippedSkills / `preset.csv` / 다른 CSV 수정 금지 (PlayerEquipsGumMaster 4슬롯=211-214 보호).
- `ImportAll` 사용 금지 — `ImportSkillDataOnly` → (빌더) → `LinkSkillPrefabs`만.
- 새 효과 컴포넌트/데미지 시스템/Animator/ParticleSystem 도입 금지 — TimeStopEffect+SimpleSpriteAnimator 재사용.
- vfx .meta/임포트 설정 변경 금지, 480x480 시트/다른 Pipoya 이펙트 사용 금지 (209 192x192만).
- 227을 `CanonicalPrefabLinks`에 추가 금지 (CanonicalSkillsWired:114가 `id>=211 → Projectile` 기대와 충돌).
- 커밋 금지 (사용자 직접 커밋 — 프로젝트 관례), `.omo/` 미커밋.
- finish-skill-system-rework(FILE_MAP v1.3 전면 개편) 범위 흡수 금지 — 이번엔 증분 갱신만.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after for code (구현 후 테스트 — 기존 계약 테스트가 end-state를 고정: TimeStopUntouched/CanonicalSkillsWired/PlayerEquipsGumMaster). Framework: com.unity.test-framework (기존 스위트 재사용).
- Unity binary: `"D:\coding\6000.3.12f1\Editor\Unity.exe"` (존재 확인됨 2026-08-05). 사용자 Unity 에디터가 켜져 있으면 프로젝트 락 — 실행 전에 닫혀 있는지 확인.
- Compile check (코드 변경 todo마다): `"D:\coding\6000.3.12f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile "D:\coding\github c\clubgame\.omo\evidence\task-<N>-compile.log"` → 로그에 `error CS` 없음.
- Test run template: `... -batchmode -runTests -projectPath "D:\coding\github c\clubgame" -testPlatform EditMode|PlayMode -testResults "D:\coding\github c\clubgame\.omo\evidence\<file>.xml" -logFile "D:\coding\github c\clubgame\.omo\evidence\<file>.log"` → PASS = XML 루트 `<test-run result="Passed" failed="0"`.
- executeMethod template: `... -batchmode -quit -executeMethod <Class.Method> -projectPath "D:\coding\github c\clubgame" -logFile "D:\coding\github c\clubgame\.omo\evidence\<file>.log"` → exit 0.
- ⚠️ 예정된 중간 상태: Todo 2 완료(227 에셋 생성, 루트 14→15) 후 Todo 4 갱신 전까지 EditMode `SkillInventoryClean` 1개만 실패(기대 14, 실제 15). 다른 테스트는 영향 없음(CanonicalSkillsWired/SkillPrefabStructure는 CanonicalPrefabLinks 13개 키만 순회). Todo 3 검증은 컴파일+빌더 로그로 하고, 전체 EditMode green은 Todo 4에서 확정.
- GOTCHAs (magic-skill-vfx 선례): `-runTests`와 `-quit` 동시 전달 금지(테스트 전 종료); `-logFile` 항상 실제 파일 경로(파이프 금지); 프로젝트 락 경합 시 exit 1 + 로그에 `error CS` 없음 → Unity 에디터 종료 확인 후 대기+재시도.
- Evidence: 모든 todo는 `.omo/evidence/task-<N>-timestop-style-magic-skills.<ext>`; F-태스크는 `.omo/evidence/final-<N>-timestop-style-magic-skills.<ext>`.
- 첫 단계: `git status --porcelain` → 사전 dirty/untracked 파일을 `.omo/evidence/task-0-dirty-worktree.txt`에 스냅샷 (magic-skill-vfx ~59개 미커밋 항목 포함 — 건드리지 않음, 커밋은 사용자 몫).

## Execution strategy
### Parallel execution waves
- **Wave 1** (순서 고정: Todo 1 → Todo 2): Todo 1 (PlayerController+301 어셋+PlayMode 테스트) · Todo 2 (CSV+prefabMap+ImportSkillDataOnly) — 파일 완전 분리(Assets/Script+Resources+Tests/PlayMode vs tiger/datafiles+Assets/Editor), 상호 의존 없음. 단, 둘 다 Unity 배치모드를 쓰므로 실제 실행은 직렬화 (락 경합 시 대기+재시도). **순서가 중요**: Todo 1의 EditMode 검증(파일 수 불변 14)이 Todo 2의 Import(14→15)보다 먼저 실행되어야 "green" 판정이 성립. Todo 2부터 실행하면 SkillInventoryClean 실패가 예정 상태로 섞이므로 반드시 1 → 2 순서.
- **Wave 2**: Todo 3 (TimeStopEffectBuilder+프리팹+Icon) — Todo 2 의존 (Icon을 설정할 227 어셋이 먼저 존재해야 함).
- **Wave 3**: Todo 4 (LinkSkillPrefabs+무결성 스위트) — Todo 1/2/3 의존 (301 타입 라인, 227 에셋+프리팹, 링크가 모두 있어야 최종 green).

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 PlayerController+301+PlayMode 테스트 | — | 4 | 2 |
| 2 CSV+prefabMap+Import | — | 3, 4 | 1 |
| 3 Builder+프리팹+Icon | 2 | 4 | — |
| 4 Link+무결성 스위트 | 1, 2, 3 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. PlayerController.cs InstantArea 케이스 구현 + 301_TimeStop.asset SkillType:3 + PlayMode InstantArea 테스트
  DONE (2026-08-05): PlayerController.cs:265-273 InstantArea 케이스(MeleeAoE 미러, null 가드+Instantiate+spawned=true, 하단 spawned 처리 불변) / 301_TimeStop.asset `SkillType: 3` 한 줄(그 외 22줄 동일) / SkillExecutionTests.cs — StunCount+CreateTimeStopSkill+InstantAreaSkill_SpawnsEffectAndStunsEnemy(StartsWith 스캔). 컴파일 error CS 0, PlayMode 5/5(신규 테스트 Passed), EditMode 16/16 회귀 green. QA 주입 (A) stub → 신규 테스트 라인 292 스폰 단언 실패(failed=1) 증명 후 revert → 재실행 5/5. 증거: task-1-compile.log, task-1-timestop-style-magic-skills.xml/.log, task-1-...-editmode.xml, task-1-qa-fail.xml, task-1-...-rerun.xml. Gotcha: PS 5.1은 & Unity.exe 대기 안 함 → Start-Process -Wait -PassThru. TimeStopEffect.cs 실제 기본값 radius 5/lifeTime 1 (프리팹 직렬화값 radius 20/stun 5/life 1.5가 우선, 테스트엔 무관).
  What to do / Must NOT do: (a) `Assets/Script/player/PlayerController.cs:265-267`의 스텁을 아래로 교체 (MeleeAoE 케이스 253-263 미러링 — TimeStopEffect가 자가구동이므로 Initialize 호출 없음):
  ```csharp
  case SkillType.InstantArea:
      if (skill.ProjectilePrefab == null)
      {
          Debug.LogWarning($"[Skill] '{skill.SkillName}' has no area effect prefab assigned. (slot {slotIndex})");
          return;
      }
      Instantiate(skill.ProjectilePrefab, transform.position, Quaternion.identity);
      spawned = true;
      break;
  ```
  (b) `Assets/Resources/SkillData/301_TimeStop.asset` 22행 `spreadAngle: 0` 아래에 `SkillType: 3` 한 줄 추가 (YAML 필드 순서 무관, 다른 필드/값 불변). (c) `Assets/Tests/PlayMode/SkillExecutionTests.cs`: ① `TestBubbleAffectable`(263-275)에 `public int StunCount;` 추가 + 빈 메서드 `ApplyStun`(268) 본문을 `StunCount++;`로 채움 (기존 테스트는 ApplyCount/LastBubbleType만 단언하므로 안전), ② `_timeStopSkill` 필드 + TearDown(102-105 부근)에서 `DestroyImmediate(_timeStopSkill)` + `CreateTimeStopSkill()` 헬퍼 (CreateFireBallSkill 122-134 미러링: ID 301, Name "TimeStop", Damage 0, Cooldown 0.1f, `SkillType = SkillType.InstantArea`, `ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/TimeStop_Effect.prefab")`, UseBubbleEffect false), ③ 신규 테스트 `InstantAreaSkill_SpawnsEffectAndStunsEnemy`: EquipSkill(0, _timeStopSkill) → InvokeUseSkill(0) → 스폰 검증 = `UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)` 순회해 `name.StartsWith("TimeStop_Effect")` 1개 존재 — ⚠️ **정확 일치 금지**: `Object.Instantiate`는 클론 이름에 `"(Clone)"`을 붙이므로 실제 이름은 `"TimeStop_Effect (Clone)"` (TimeStopEffect는 Assembly-CSharp 타입이라 asmdef PlayMode 테스트에서 직접 참조 불가 — 이름 스캔으로 우회) → `Assert.AreEqual(0, _bubbleAffectable.StunCount)` → `yield return new WaitForSeconds(0.2f)` (Start→ApplyEffect 대기) → `Assert.AreEqual(1, _bubbleAffectable.StunCount)` (OverlapCircleAll(플레이어 위치=원점, radius 20)이 적(원점, CircleCollider2D radius 1) 타격) → `yield return new WaitForSeconds(2.0f)` (lifeTime 1.5 자가 파괴 대기 — 다음 테스트 오염 방지). Must NOT: TryFire/ObjectPooler/MeleeHitbox/다른 UseSkill 케이스/다른 어셋 수정; Player.prefab·preset.csv 불변.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 4
  References: `Assets/Script/player/PlayerController.cs:253-267` (MeleeAoE 패턴+스텁), `:270-274` (spawned 후 `_skillLastUsed`/TriggerCooldown — 이번 변경과 무관, 건드리지 않음); `Assets/Resources/SkillData/301_TimeStop.asset:15-22` (필드 배치 — SkillType 없음 확인됨), `Assets/Script/Combat/SkillData.cs:3` (enum SkillType { Projectile, Melee, MeleeAoE, InstantArea } → 3); `Assets/Tests/PlayMode/SkillExecutionTests.cs:122-134` (CreateFireBallSkill 헬퍼 패턴), `:167-189` (EquipSkill/InvokeUseSkill/ResetSkillCooldowns), `:234-260` (WaitForSeconds 대기 패턴), `:263-275` (TestBubbleAffectable — ApplyStun 빈 메서드 268행), `:152-165` (Assembly-CSharp 타입 우회 패턴 참고); `Assets/Prefabs/Projectiles/TimeStop_Effect.prefab` (프리팹 루트 이름 "TimeStop_Effect").
  Acceptance criteria (agent-executable): 컴파일 체크 green (`error CS` 0); PlayMode 스위트 green (기존 4 + 신규 1 = 5, XML `failed="0"`); EditMode 스위트 여전히 green (301 어셋 SkillType 추가는 TimeStopUntouched·SkillInventoryClean에 영향 없음 — 파일 수 불변).
  QA scenarios: happy — PlayMode XML에 `InstantAreaSkill_SpawnsEffectAndStunsEnemy` 포함 green, 증거 `.omo/evidence/task-1-timestop-style-magic-skills.xml`; failure — (A) InstantArea 케이스를 임시로 `return;`만 남기고 스폰 제거 → 테스트가 스폰 단언에서 실패함을 증명 후 복구, 또는 (B) `CreateTimeStopSkill()`의 `ProjectilePrefab`을 일시적으로 null로 설정 → null 가드 경고 분기로 스폰 단언 실패 증명 후 복구. (⚠️ 301 어셋 SkillType 라인 제거는 이 테스트를 실패시키지 못함 — 테스트는 어셋을 읽지 않고 인메모리 SkillData를 사용하므로 주입 수단으로 부적합. 어셋 계약은 Todo 4 QA (B)가 검증.) 실패 주입 XML은 `.omo/evidence/task-1-qa-fail.xml`로 보존.
  Commit: N (사용자 직접 커밋) | feat(skill): implement InstantArea skill type and enable 301 TimeStop

- [x] 2. magicskill.csv 227행 + DataImportMenu.prefabMap + ImportSkillDataOnly 배치 (227_TimeWarp.asset 생성)
  DONE (2026-08-05): magicskill.csv 8행 `227,TimeWarp,0,30,10,InstantArea,None,0,0,0` (헤더 불변) / DataImportMenu.cs:324 `prefabMap[227] = "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab";` / ImportSkillDataOnly 배치 exit 0 → `227_TimeWarp.asset`(+.meta) 생성, YAML 검증: ID 227, SkillName TimeWarp, Damage 0, ManaCost 30, Cooldown 10, SkillType 3(InstantArea), UseBubbleEffect 0, ProjectilePrefab {fileID: 0} 미링크(Todo 4). 루트 SkillData 15(14+227). QA 주입: Type→Projectile 재임포트 시 SkillType 0 확인 → revert 후 SkillType 3 복구(3→0→3). 증거: task-2-timestop-style-magic-skills.log/.final.log, task-2-qa-fail.log. Gotcha 4: PS 5.1 Start-Process -ArgumentList는 공백 인자 자동 인용 안 함 → `'-projectPath "D:\coding\github c\clubgame"'` 수동 인용 필요.
  What to do / Must NOT do: (a) `tiger/datafiles/skill/magicskill.csv` 7행(226) 뒤에 `227,TimeWarp,0,30,10,InstantArea,None,0,0,0` 추가 (10열 헤더 `ID,Name,Damage,ManaCost,Cooldown,Type,Bubble,Speed,MeleeRange,MeleeArc` 유지 — Type="InstantArea"는 `Enum.TryParse`로 파싱됨). (b) `Assets/Editor/DataImportMenu.cs` prefabMap(310-323) 226행 뒤에 `prefabMap[227] = "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab";` 추가. (c) 배치 실행 `-executeMethod DataImportMenu.ImportSkillDataOnly` → 로그 "Skill Data Import Complete!" + `Assets/Resources/SkillData/227_TimeWarp.asset` 생성 확인 (명명 규칙 `{id}_{Name}.asset` = DataImportMenu.cs:241; SkillType==InstantArea, Damage 0 / ManaCost 30 / Cooldown 10, UseBubbleEffect false). ⚠️ 예정된 중간 상태: 이 todo 완료 후 EditMode `SkillInventoryClean` 1개만 실패(14 vs 15) — Todo 4에서 갱신, 다른 테스트는 green. Must NOT: `ImportAll` 금지 (적/바이옴/프리셋 재파싱 = 범위 위반), preset.csv/Player.prefab/다른 CSV 불변, 이 todo에서 `LinkSkillPrefabs` 실행 금지 (프리팹이 아직 없어 227 링크 실패 로그 발생 — Todo 4에서).
  Parallelization: Wave 1 | Blocked by: — | Blocks: 3, 4
  References: `tiger/datafiles/skill/magicskill.csv:1-7` (헤더+기존 행); `Assets/Editor/DataImportMenu.cs:205-291` (ImportSkillFile — 헤더 동적 파싱:224-230, Enum.TryParse:255, Bubble 파싱:260-269, Speed:271-275), `:241` (에셋 명명), `:294-300` (ImportSkillDataOnly 배치 진입점), `:310-323` (prefabMap); `Assets/Script/Combat/SkillData.cs:5-35` (필드).
  Acceptance criteria (agent-executable): 배치 실행 exit 0 + 로그에 error 없음; `227_TimeWarp.asset`(+.meta) 존재 — throwaway EditMode 체크 또는 `-executeMethod` 1회성 검증으로 필드 확인(SkillType==3, Damage==0, ManaCost==30, Cooldown==10); `Assets/Resources/SkillData` 루트 = 15.
  QA scenarios: happy — import 로그 + 에셋 필드 검증, 증거 `.omo/evidence/task-2-timestop-style-magic-skills.log`; failure — CSV Type 열을 임시로 "Projectile"로 변경 → 재임포트 후 227 어셋 SkillType이 Projectile이 됨을 확인(테스트가 잡는 회귀 경로 증명) → CSV 복구 재실행, 증거 `.omo/evidence/task-2-qa-fail.log`.
  Commit: N (사용자 직접 커밋) | feat(data): add 227 TimeWarp magic skill and prefab map entry

- [x] 3. TimeStopEffectBuilder.cs + TimeWarp_Effect.prefab 생성(209 시트) + 227 Icon 설정
  DONE (2026-08-06): 신규 `Assets/Editor/TimeStopEffectBuilder.cs`(223줄, MagicVFXBuilder 패턴 — 209 시트 `pipo-btleffect209_192.png` 15 서브스프라이트 `int.Parse` 자연 정렬, SaveAsPrefabAsset 멱등, 227 Icon 설정) / `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab`(+.meta, guid `6a0f50631e104e9458759451f1ef339f`) 생성 — Transform scale (5,5,1), SpriteRenderer sortingOrder 20, TimeStopEffect(script guid `713e57099bbd717488c4b272836d43ec`: radius 15/stunDuration 3/lifeTime 1.5), SimpleSpriteAnimator(script guid `d844848d6c8749245820249fbcd9b4a1`: 15프레임 `_0`~`_14` 자연 정렬, fps 12, loop false) / `227_TimeWarp.asset` Icon 설정 → 209 시트 guid `3014f866b966d1240bfb57efd1ac6ac0` 첫 스프라이트(`fileID: 7241654760395862158`) 참조. 컴파일 error CS 0, 빌더 배치 exit 0 + PASSED 로그 정확 일치. 독립 검증(misleading_success_output 프로브 — PASSED 로그만 신뢰하지 않음): 프리팹 YAML 4개 컴포넌트 + 프레임 15개 fileID→시트 meta 매핑 `_0`..`_14` 순서 OK + Icon guid 일치. QA(stale_state 프로브): 프리팹 삭제(meta 유지) → 재실행 → GUID 유지 + 재생성(멱등) + PASSED 재로그. 증거: task-3-dirty-worktree.txt(사전 스냅샷 62줄), task-3-compile.log, task-3-timestop-style-magic-skills.log/.txt, task-3-qa-idempotency.log. Gotcha 5: Unity 6 메타 `internalIDToNameTable`은 `213: <fileID>` + `second: <이름>` 구조 (이전 포맷의 internalID/name와 다름), PS 5.1 Get-Content 줄 끝 `\r` 때문에 `$` 앵커 실패 → `.Trim()` 필요.
  What to do / Must NOT do: 신규 `Assets/Editor/TimeStopEffectBuilder.cs` (MagicVFXBuilder.cs 패턴 — 319줄 참조). (a) 209 시트 `"Assets/Sprite/vfx/Pipoya VFX TimeMagic/Pipoya VFX TimeMagic/192x192/pipo-btleffect209_192.png"`를 `AssetDatabase.LoadAllAssetsAtPath<Sprite>`로 로드, 이름 `pipo-btleffect209_192_(\d+)$` 정규식 필터 후 **숫자 자연 정렬**(`int.Parse`, 문자열 정렬 금지 — `_10`이 `_2`보다 앞에 오는 오류) → 15프레임. 15개 미만이면 `Debug.LogError` + 중단. (b) GO "TimeWarp_Effect" 구성: Transform localScale (5,5,1), SpriteRenderer sortingOrder 20 + sprite=첫 프레임, `TimeStopEffect` 컴포넌트(AddComponent 후 SerializedObject로 `radius=15`/`stunDuration=3`/`lifeTime=1.5` — TimeStop_Effect.prefab:107-109 필드명 그대로), `SimpleSpriteAnimator` 컴포넌트(`frames`=정렬된 15개 Sprite 순서대로, `fps=12`, `loop=false` — TimeStop_Effect.prefab:121-139 형식). (c) `PrefabUtility.SaveAsPrefabAsset` → `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab` (멱등: 기존 있으면 덮어쓰기 — GUID 유지). (d) 227 어셋 Icon: `AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Resources/SkillData/227_TimeWarp.asset")` → SerializedObject → `Icon` = `AssetDatabase.LoadAssetAtPath<Sprite>(209 시트 경로)` (301 어셋:20의 Icon 형식 — `{fileID: 21300000, guid: <시트 guid>, type: 3}` — 과 동일하게 209 시트 guid `3014f866b966d1240bfb57efd1ac6ac0`를 참조하는 유효한 Sprite; LoadAssetAtPath가 반환하는 Sprite를 그대로 사용). (e) `public static void BuildTimeWarpEffect()` (배치 `-executeMethod TimeStopEffectBuilder.BuildTimeWarpEffect` 진입점) + MenuItem "Custom Tools/tiger/Time Stop/Build TimeWarp Effect Prefab". 빌드 후 자체 검증 로그 `"PASSED: TimeWarp_Effect.prefab built (15 frames, fps=12, radius=15, stun=3, life=1.5, icon set)"`. Must NOT: 209/210 시트 .meta·임포트 설정 변경, TimeStop_Effect.prefab/TimeStopEffect.cs 수정, 480x480 시트 사용, Animator/ParticleSystem, 다른 프리팹/어셋 접촉.
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 4
  References: `Assets/Editor/MagicVFXBuilder.cs` 전체 (LoadAllAssetsAtPath+자연 정렬+SaveAsPrefabAsset+SerializedObject 패턴, 배치 진입점 구조); `Assets/Prefabs/Projectiles/TimeStop_Effect.prefab:22-36` (Transform scale 5,5,1), `:83-85` (sortingOrder 20), `:95-109` (TimeStopEffect — script guid `713e57099bbd717488c4b272836d43ec`, 필드 radius/stunDuration/lifeTime), `:110-139` (SimpleSpriteAnimator — script guid `d844848d6c8749245820249fbcd9b4a1`, frames/fps/loop); `Assets/Resources/SkillData/301_TimeStop.asset:20` (Icon 형식 `{fileID: 21300000, guid: ..., type: 3}`); `Assets/Sprite/vfx/Pipoya VFX TimeMagic/Pipoya VFX TimeMagic/192x192/pipo-btleffect209_192.png.meta:2` (시트 guid), `:4-39` (서브스프라이트 명명 `_0`~`_14`); `Assets/Script/TimeStopEffect.cs:9-11` (필드 선언).
  Acceptance criteria (agent-executable): 컴파일 체크 green; 배치 `-executeMethod TimeStopEffectBuilder.BuildTimeWarpEffect` exit 0 + 로그에 PASSED; `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab`(+.meta) 존재. (전체 EditMode green은 이 todo가 아닌 Todo 4에서 확정 — 227 에셋이 루트에 있어 SkillInventoryClean 1개 실패는 예정 상태.)
  QA scenarios: happy — 빌더 실행 + 프리팹 로드로 컴포넌트/프레임 수 확인(SerializedObject: frames 15, fps 12, radius 15, stun 3, life 1.5), 증거 `.omo/evidence/task-3-timestop-style-magic-skills.txt`; failure — (A) 자연 정렬을 string sort로 임시 변경 → 로그로 `_10` 순서 오류 입증 후 복구, (B) 프리팹 삭제 후 재실행 → 재생성·GUID 유지 확인(멱등), 증거 `.omo/evidence/task-3-qa-idempotency.log`.
  Commit: N (사용자 직접 커밋) | feat(vfx): build TimeWarp_Effect prefab from 209 sheet and set icon

- [x] 4. LinkSkillPrefabs 배치 + 무결성 스위트 갱신 (14→15, TimeStopUntouched+SkillType, InstantAreaSkillsWired)
  DONE (2026-08-06): LinkSkillPrefabs 배치 exit 0 → 로그 `Linked=14` + 227 어셋 YAML `ProjectilePrefab: {fileID: 5684612262389042782, guid: 6a0f50631e104e9458759451f1ef339f, type: 3}` (TimeWarp_Effect 프리팹 — 로그만 믿지 않고 YAML 독립 검증) / SkillDataIntegrityTests.cs 5곳 갱신: CanonicalAssetNames +227(14개), SkillInventoryClean 14→15(주석 동기화), SkillIdsUnique 14→15, TimeStopUntouched +SkillType==InstantArea 단언, 신규 InstantAreaSkillsWired(301/227 × 프리팹 경로 + MonoScript m_Script 비교) — CanonicalPrefabLinks 13개 유지. 컴파일 error CS 0, EditMode 17/17(failed=0, 기존 16+신규 1, 예정된 SkillInventoryClean 실패 해소), PlayMode 5/5. QA 주입 (A) 227 기대 경로 오염 → EditMode failed=1(InstantAreaSkillsWired) 후 revert → 17/17 / (B) 301 SkillType 라인 제거 → failed=2(TimeStopUntouched+InstantAreaSkillsWired) 후 revert → 17/17 (타입 계약 실고정 증명). Guardrail: TimeStop_Effect.prefab/TimeStopEffect.cs/Player.prefab/preset.csv diff EMPTY, ImportAll 미사용, 루트 15, PlayerEquipsGumMaster/PresetsResolveToRoot 불변. 증거: task-4-timestop-style-magic-skills-link.log/.compile.log/.xml/.playmode.xml, task-4-qa-fail.xml, task-4-qa-fail-301.xml (+rerun). Gotcha 6: PS 5.1 Set-Content -Encoding UTF8은 BOM 삽입 → .asset 편집 시 [System.Text.UTF8Encoding]::new($false)로 BOM 없이 복원, git diff --text로 301 어셋 = +SkillType: 3 한 줄만 확인.
  What to do / Must NOT do: (a) 배치 실행 `-executeMethod DataImportMenu.LinkSkillPrefabs` → 로그 `Linked=14` (13→14, 227 포함) 확인. (b) `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` 갱신: ① `CanonicalAssetNames`(18-24)에 `"227_TimeWarp.asset"` 추가(14개), ② `SkillInventoryClean`(52) `Assert.AreEqual(14, ...)` → `15` + 주석(50) 갱신, ③ `SkillIdsUnique`(84) `GreaterOrEqual(guids.Length, 14)` → `15`, ④ `TimeStopUntouched`(269-283)에 `Assert.AreEqual(SkillType.InstantArea, timeStop.SkillType, "301 스킬의 SkillType 이 InstantArea(3)가 아닙니다.");` 추가, ⑤ `CanonicalPrefabLinks`(27-42)는 **변경 금지** (13개 유지 — 227 추가 시 CanonicalSkillsWired:114 `id >= 211 → Projectile` 기대와 충돌), ⑥ 신규 테스트 `InstantAreaSkillsWired`: ids {301, 227} × 기대 프리팹 {"Assets/Prefabs/Projectiles/TimeStop_Effect.prefab", "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab"} — `LoadRootSkillById`로 로드, `SkillType==SkillType.InstantArea` + `ProjectilePrefab` non-null + `AssetDatabase.GetAssetPath` 매치 + 프리팹에 TimeStopEffect 스크립트 존재 확인 (`AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Script/TimeStopEffect.cs")`와 각 컴포넌트 SerializedObject `m_Script` objectReferenceValue 비교 — Assembly-CSharp 타입 직접 참조 불가 우회). (c) FILE_MAP.md 증분 갱신 (신규: TimeStopEffectBuilder.cs, TimeWarp_Effect.prefab, 227_TimeWarp.asset; 변경: PlayerController.cs InstantArea, 301_TimeStop.asset SkillType, magicskill.csv 227행, DataImportMenu.cs prefabMap, 테스트 2파일). Must NOT: 기존 단언 약화 금지, CanonicalPrefabLinks 변경 금지, PlayerEquipsGumMaster/PresetsResolveToRoot 건드리기 금지.
  Parallelization: Wave 3 | Blocked by: 1, 2, 3 | Blocks: —
  References: `Assets/Editor/DataImportMenu.cs:303-363` (LinkSkillPrefabs — prefabMap 310-323, 루트 순회 327-358, Linked 로그 362); `Assets/Tests/EditMode/SkillDataIntegrityTests.cs:18-24` (CanonicalAssetNames), `:27-42` (CanonicalPrefabLinks — 유지), `:47-74` (SkillInventoryClean:52), `:79-95` (SkillIdsUnique:84), `:100-133` (CanonicalSkillsWired:114 — 227 충돌 이유), `:269-283` (TimeStopUntouched), `:325-349` (helpers LoadRootSkillById/GetRootSkillDataPaths); `Assets/Script/TimeStopEffect.cs`; `Assets/Script/Combat/SkillData.cs:3` (enum).
  Acceptance criteria (agent-executable): Link 로그 `Linked=14`; EditMode 스위트 전체 green (기존 16 + InstantAreaSkillsWired 1 = 17, XML `failed="0"`); PlayMode 스위트 green (5); 컴파일 체크 green; `Assets/Resources/SkillData` 루트 = 15 (단언 확인).
  QA scenarios: happy — EditMode/PlayMode XML green, 증거 `.omo/evidence/task-4-timestop-style-magic-skills.xml` + `.omo/evidence/task-4-timestop-style-magic-skills-playmode.xml`; failure — (A) InstantAreaSkillsWired의 227 기대 프리팹 경로를 틀리게 넣고 실패 확인 후 복구, (B) 301 어셋 SkillType 라인을 임시 제거 → TimeStopUntouched 새 단언이 실패함을 입증 후 복구 (타입 계약이 실제로 고정됨을 증명), 증거 `.omo/evidence/task-4-qa-fail.xml`.
  Commit: N (사용자 직접 커밋) | test(skill): update integrity suite for 15-skill end-state and InstantArea wiring

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [x] F1. Plan compliance audit — for every todo 1-4: checkbox checked, evidence file present at the documented path, worktree diff contains exactly the planned file set (PlayerController.cs, 301_TimeStop.asset, SkillExecutionTests.cs, magicskill.csv, DataImportMenu.cs, 227_TimeWarp.asset(+.meta), TimeStopEffectBuilder.cs, TimeWarp_Effect.prefab(+.meta), SkillDataIntegrityTests.cs, FILE_MAP.md) vs. the task-0 snapshot; no file outside scope changed.
- [x] F2. Code quality review — InstantArea 케이스가 MeleeAoE 패턴 미러 + null 가드 + spawned 정확성; 빌더 자연 정렬(int.Parse) + 15프레임 검증 + 멱등; 테스트 실질적(스폰+스턴 행동 증명); 데드 코드/로그 노이즈 없음; Animator/Particle 어디에도 없음.
- [x] F3. Real manual QA — 헤드리스 런타임 하네스 (PlayMode 스위트 = 301 발동→TimeStop_Effect 스폰→적 스턴 증명). ⚠️ 남은 사용자 단계: 에디터 열고 301 발동 + TimeWarp_Effect 초록 클락 애니메이션 눈으로 확인 (프리팹을 씬에 드래그하거나 301 장착).
- [x] F4. Scope fidelity — Must NOT 목록 walk (diff-검증 가능 8항목; 커밋 규칙·FILE_MAP 스코프 제한은 Commit strategy/F1에서 별도 커버): `TimeStop_Effect.prefab`/`TimeStopEffect.cs` `git diff` EMPTY, 301 어셋 diff = `SkillType: 3` 한 줄만, Player.prefab/preset.csv/다른 CSV diff EMPTY, ImportAll 미사용(로그 확인), vfx .meta 무변경, 480x480/다른 팩 미사용, 새 효과 컴포넌트 없음, CanonicalPrefabLinks 13개 유지.

## Commit strategy
- Commits are made by the USER directly (project convention). The executor stages nothing and commits nothing; it reports the exact per-todo file list so the user can commit atomically: (1) PlayerController.cs + 301_TimeStop.asset + SkillExecutionTests.cs; (2) magicskill.csv + DataImportMenu.cs + 227_TimeWarp.asset(+.meta); (3) TimeStopEffectBuilder.cs + TimeWarp_Effect.prefab(+.meta); (4) SkillDataIntegrityTests.cs + FILE_MAP.md.
- FILE_MAP.md must be updated IMMEDIATELY after each todo's verification passes — never deferred (user requirement). Incremental only (finish-skill-system-rework의 v1.3 전면 개편은 별도 플랜 범위).
- magic-skill-vfx의 ~59개 미커밋 파일이 작업 트리에 이미 존재 — 그쪽 먼저 커밋하는 것은 사용자 몫이며, 이 플랜의 변경과 섞이지 않도록 `git add -A` 금지 (todo별 명시 경로만 스테이징 안내).
- Evidence files under `.omo/evidence/` are NOT committed.

## Session status
- **State**: Draft 승인 2026-08-05 (사용자: "승인 — 플랜 작성 진행"). 이 플랜 작성 완료, 실행 대기 (`$start-work`).
- **Confirmed facts** (do not re-derive):
  - Unity exe: `D:\coding\6000.3.12f1\Editor\Unity.exe` (exists).
  - GOTCHA: `-runTests`와 `-quit` 동시 전달 금지; `-logFile` 항상 실제 파일; 프로젝트 락 경합 시 exit 1 + `error CS` 없음 → 에디터 종료 확인 후 대기+재시도.
  - `301_TimeStop.asset`: SkillType 필드 없음(23줄 YAML) → Todo 1에서 `SkillType: 3` 추가. Icon 형식 `{fileID: 21300000, guid: 58f146e2...}` (210 시트 메인 스프라이트).
  - 209 시트: guid `3014f866b966d1240bfb57efd1ac6ac0`, 서브스프라이트 15개 `pipo-btleffect209_192_0`~`_14`.
  - TimeStop_Effect.prefab 구조: scale (5,5,1), sortingOrder 20, TimeStopEffect(radius 20/stun 5/life 1.5), SimpleSpriteAnimator(15프레임, fps 12, loop 0).
  - EditMode 스위트 현재 green (16: 무결성 8 + SpriteVFXAnimatorTests 7 + SkillDataModelTests 1), PlayMode 4. Todo 4 후 EditMode 17 / PlayMode 5.
  - `TestBubbleAffectable.ApplyStun`은 빈 메서드(SkillExecutionTests.cs:268) — 기존 테스트는 ApplyStun 결과를 단언하지 않으므로 `StunCount++` 추가 안전.
  - CanonicalSkillsWired(SkillDataIntegrityTests.cs:114)는 `id >= 211 → SkillType.Projectile` 기대 — 227(InstantArea)를 CanonicalPrefabLinks에 넣으면 충돌 (D4 근거).
- **Known planned intermediate state**: Todo 2 완료~Todo 4 갱신 전까지 EditMode `SkillInventoryClean` 1개만 실패 (루트 14→15).

## Success criteria
- All 4 todo checkboxes AND F1-F4 checked, each with evidence under `.omo/evidence/`.
- Batchmode runs green: compile check (no `error CS`), EditMode suite (`result="Passed" failed="0"`, 17 tests), PlayMode suite green (5 tests) including the InstantArea stun test.
- 15 SkillData assets at root (14 canonical + 301); 301 fires as InstantArea in PlayMode (stun applied); 227_TimeWarp in CSV with SkillType=InstantArea and linked TimeWarp_Effect.prefab.
- `git diff` on `TimeStop_Effect.prefab` and `TimeStopEffect.cs` is EMPTY; `git diff` on `301_TimeStop.asset` shows exactly one added line `SkillType: 3`; `git diff` under `Assets/Sprite/vfx/**/*.meta` is EMPTY.
- CanonicalPrefabLinks still 13 entries; PlayerEquipsGumMaster still 4 = 211-214.
- FILE_MAP.md reflects the final state (incremental).
