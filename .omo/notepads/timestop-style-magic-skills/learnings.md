# Learnings - timestop-style-magic-skills

## Task 1 (2026-08-05) — SkillType.InstantArea 구현 + PlayMode 테스트
- **변경 3파일**:
  1. `Assets/Script/player/PlayerController.cs` UseSkill `case SkillType.InstantArea`: 프리팹 null 가드 → `Instantiate(prefab, transform.position, Quaternion.identity)` → `spawned = true`. MeleeAoE 패턴 미러링. `spawned` 처리(`_skillLastUsed`/`TriggerCooldown`)는 건드리지 않음.
  2. `Assets/Resources/SkillData/301_TimeStop.asset`: `spreadAngle: 0` 뒤에 `SkillType: 3` 한 줄 추가. 루트 원인: 필드 부재 → enum 0(Projectile) 직렬화 → 스킬 발사 안 됨.
  3. `Assets/Tests/PlayMode/SkillExecutionTests.cs`: (①)`TestBubbleAffectable`에 `public int StunCount;` + `ApplyStun(){ StunCount++; }`, (②)`_timeStopSkill` 필드/TearDown destroy/`CreateTimeStopSkill()` 헬퍼(ID 301, SkillType.InstantArea, 프리팹 TimeStop_Effect), (③)신규 `InstantAreaSkill_SpawnsEffectAndStunsEnemy` 테스트.
- **테스트명**: `InstantAreaSkill_SpawnsEffectAndStunsEnemy` — InstantArea 사용 직후 "TimeStop_Effect (Clone)" 게임오브젝트 1개 스폰 검증(`StartsWith` 매칭, exact equality 금지 — Instantiate가 "(Clone)" 붙임), Start() 전 StunCount 0, 0.2s 후 StunCount 1 (적은 원점, radius 커버). TimeStopEffect는 Assembly-CSharp 타입이라 asmdef에서 타입 참조 불가 → 이름 스캔.
- **증거 파일**: `task-1-compile.log`, `task-1-timestop-style-magic-skills.xml/.log`(PlayMode 5 green), `task-1-timestop-style-magic-skills-editmode.xml`(EditMode 16 green), `task-1-qa-fail.xml`(주입 실패 증명: 5중 1 실패), `task-1-timestop-style-magic-skills-rerun.xml`(revert 후 5 green).
- **QA 주입**: 옵션 (A) InstantArea 케이스를 stub(`Debug.LogWarning; return`)으로 되돌림 → 신규 테스트가 라인 292 spawn 단언에서 실패(`failed="1"`) 확인 → 즉시 revert.
- **Gotcha 1**: PowerShell 5.1에서 `& Unity.exe`는 GUI 앱이라 대기하지 않아 `$LASTEXITCODE`가 비어 있음 → `Start-Process -Wait -PassThru`로 `.ExitCode` 취득 (0=성공, 2=테스트 실패).
- **Gotcha 2**: 계획서엔 "radius 20 / lifeTime 1.5"라 써 있으나 실제 `TimeStopEffect.cs`는 `radius = 5f`, `lifeTime = 1f`. 테스트엔 영향 없음(적은 원점 → 반경 상관없이 커버, 2.0s 대기는 lifeTime 1.0을 초과). 메시지 문자열은 계획 원문 그대로 유지.
- **Gotcha 3**: 계획서가 "Line 22 = spreadAngle: 0"이라 했지만 실제 파일은 line 23이 spreadAngle. 편집 위치는 값 기준으로 명확(insert after spreadAngle).
- **실행 컨벤션**: `-runTests`에 `-quit` 금지. `-logFile`은 항상 실제 파일 경로로. Unity 실행 후 프로세스/`Temp/UnityLockfile` 유무 확인으로 락 해제 확인.

## Task 2 (2026-08-05) — Skill 227 TimeWarp 데이터 파이프라인 추가
- **변경 2파일 + 생성 1파일**:
  1. `tiger/datafiles/skill/magicskill.csv`: 8번째 줄 `227,TimeWarp,0,30,10,InstantArea,None,0,0,0` 추가 (헤더 불변).
  2. `Assets/Editor/DataImportMenu.cs` `LinkSkillPrefabs()`: `prefabMap[227] = "Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab";` 한 줄 추가 (실제 라인 324, 계획서 "~310-323"과 다름 — 값 기준 위치 확인).
  3. 생성: `Assets/Resources/SkillData/227_TimeWarp.asset`(+.meta). YAML 검증: `ID: 227`, `SkillName: TimeWarp`, `Damage: 0`, `ManaCost: 30`, `Cooldown: 10`, `SkillType: 3`, `UseBubbleEffect: 0`, `ProjectilePrefab: {fileID: 0}`(아직 미링크 — Todo 4). 루트 SkillData 15개(14+227).
- **증거 파일**: `task-2-timestop-style-magic-skills.log`(최초 성공), `task-2-qa-fail.log`(주입), `task-2-timestop-style-magic-skills-final.log`(revert 후 최종).
- **Gotcha 4 (중요)**: PowerShell 5.1 `Start-Process -ArgumentList`는 공백 포함 인자를 자동 인용하지 않음 → `-projectPath D:\coding\github c\clubgame`이 공백에서 분리되어 `-projectPath D:\coding\github`로 해석 → 프로젝트 열기 실패, exit 1, 로그가 `D:\coding\github`에 생성됨. 해결: 인자 문자열에 수동 인용 `'-projectPath "D:\coding\github c\clubgame"'` (single-quote 안에 double-quote). 스트레이 파일 `D:\coding\github` 삭제함.
- **QA 주입**: CSV `Type`을 `InstantArea`→`Projectile`로 변경 → 재임포트 → 227 YAML `SkillType: 0` 확인 → CSV revert → 재임포트 → `SkillType: 3` 복구 (3→0→3 사이클 증명). 완료 후 프로젝트 락 자유 상태.
- **알려진 계획 상태**: 루트 에셋 14→15로 증가 → Todo 4에서 갱신 전까지 EditMode `SkillInventoryClean` 테스트 1건 실패 예상 (수정 금지).

## Task 3 (2026-08-06) — TimeWarp_Effect 프리팹 빌드 + 227 Icon 설정
- **빌더는 기존에 작성 완료** (직전 세션이 빌더 작성 직후 종료 — 프리팹은 생성 전). 이번 세션은 컴파일 체크 → 빌드 실행 → 독립 검증 → 멱등성 QA 순으로 진행.
- **실행 순서 (모두 batchmode, exit 0)**:
  1. 컴파일 체크: `-batchmode -quit` → `task-3-compile.log`, `error CS` 0건.
  2. 빌드: `-executeMethod TimeStopEffectBuilder.BuildTimeWarpEffect` → `task-3-timestop-style-magic-skills.log`에 정확히 `PASSED: TimeWarp_Effect.prefab built (15 frames, fps=12, radius=15, stun=3, life=1.5, icon set)`.
  3. 멱등성 QA: 프리팹 삭제(meta 유지) → 재실행 → GUID `6a0f50631e104e9458759451f1ef339f` 유지 + 재생성 → `task-3-qa-idempotency.log`.
- **독립 검증 결과** (`task-3-timestop-style-magic-skills.txt`): `m_LocalScale {x:5,y:5,z:1}`, `m_SortingOrder: 20`, TimeStopEffect script guid `713e57099bbd717488c4b272836d43ec`(radius 15/stun 3/life 1.5), SimpleSpriteAnimator script guid `d844848d6c8749245820249fbcd9b4a1`(frames 15, fps 12, loop 0). 프레임 15개 fileID → 시트 meta 매핑으로 `_0`~`_14` 자연 정렬 확인 (NUMERIC_ORDER=OK). 227 `Icon: {fileID: 7241654760395862158, guid: 3014f866b966d1240bfb57efd1ac6ac0, type: 3}`.
- **Gotcha 5**: Unity 6 텍스처 .meta의 `internalIDToNameTable`은 이전 버전과 구조가 다름 — fileID가 `internalID:` 한 줄이 아니라 `213: <fileID>` 줄 + 다음 줄 `second: <스프라이트명>` (내부 파일ID). 프레임 순서 검증 스크립트를 이 구조에 맞게 파싱해야 함. 또한 PS 5.1 Get-Content는 줄 끝 `\r`을 남기므로 정규식 `$` 앵커가 실패 → `.Trim()` 필요.
- **변경 파일**: 신규 `TimeStopEffectBuilder.cs`(기존), 신규 `TimeWarp_Effect.prefab`(+.meta), 변경 `227_TimeWarp.asset`(Icon만). 증거: `task-3-dirty-worktree.txt`(사전 스냅샷, 62줄), `task-3-compile.log`, `task-3-timestop-style-magic-skills.log/.txt`, `task-3-qa-idempotency.log`.

## Task 4 (2026-08-06) · LinkSkillPrefabs + 무결성 스위트 15-스킬 end-state
- **변경 파일**: `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` (무결성 스위트 14→15 end-state), `Assets/Resources/SkillData/227_TimeWarp.asset` (ProjectilePrefab 링크, `{fileID: 5684612262389042782, guid: 6a0f50631e104e9458759451f1ef339f, type: 3}`).
- **실행 순서 (모두 batchmode)**: ① `-executeMethod DataImportMenu.LinkSkillPrefabs` → exit 0, 로그 `[DataImportMenu] LinkSkillPrefabs complete. Linked=14 MeleeHitboxExists=True` (13→14, 227 포함). ② 컴파일 체크 → `error CS` 0. ③ EditMode 17/17 (`InstantAreaSkillsWired` 포함, 기존 16 + 신규 1), XML `result="Passed" failed="0"`. ④ PlayMode 5/5 (`InstantAreaSkill_SpawnsEffectAndStunsEnemy` 포함).
- **테스트 갱신 5곳**: `CanonicalAssetNames` +`"227_TimeWarp.asset"`(14개) / `SkillInventoryClean` 루트 14→15 (주석 동기화) / `SkillIdsUnique` `GreaterOrEqual 14→15` / `TimeStopUntouched` +`Assert.AreEqual(SkillType.InstantArea, timeStop.SkillType, "301 스킬의 SkillType 이 InstantArea(3)가 아닙니다.")` / 신규 `InstantAreaSkillsWired` (ids {301,227} × prefab 경로, SkillType==InstantArea, ProjectilePrefab non-null + 경로 매치 + MonoScript `TimeStopEffect.cs` ↔ 각 컴포넌트 `SerializedObject.FindProperty("m_Script").objectReferenceValue` 비교). `CanonicalPrefabLinks`는 13개 유지 (227 미등록 — CanonicalSkillsWired:114 `id>=211 → Projectile` 충돌 방지).
- **QA 주입 (misleading_success_output 프로브)**: (A) `InstantAreaSkillsWired`의 227 기대 경로를 `TimeWarp_Effect_WRONG.prefab`으로 임시 변경 → EditMode `failed="1"` (`InstantAreaSkillsWired` 단독 실패, 실패 메시지 "ID 227 스킬의 ProjectilePrefab 경로가 기대값과 다릅니다") → revert → 17/17 green. 증거 `task-4-qa-fail.xml`, `task-4-qa-fail-rerun-a.xml`. (B) `301_TimeStop.asset`의 `SkillType: 3` 라인 임시 제거 → EditMode `failed="2"` (`TimeStopUntouched` + `InstantAreaSkillsWired` 둘 다 "301 스킬의 SkillType 이 InstantArea(3)가 아닙니다"로 실패 — 타입 계약이 실제로 고정됨을 증명) → revert → 17/17 green. 증거 `task-4-qa-fail-301.xml`, `task-4-qa-fail-rerun-b.xml`.
- **Gotcha 6 (중요)**: PS 5.1 `Set-Content -Encoding UTF8`은 BOM(EF BB BF)을 붙인다 — `.asset`처럼 git이 바이너리 취급하는 파일을 편집할 때 BOM이 들어가면 diff가 오염된다. 301 어셋 QA (B) 복구 시 `git checkout --`로 HEAD 복원 → `spreadAngle: 0` 뒤에 `SkillType: 3` 한 줄 재삽입(`[System.Text.UTF8Encoding]::new($false)`로 BOM 없이 WriteAllText) → `git diff --text`가 정확히 `+  SkillType: 3` 한 줄만 표시. guardrail: 파일 최종 상태 = Todo 1 diff 그대로.
- **Gotcha 7**: `git diff`가 `.asset`을 "Binary files differ"로 취급하면 diff를 신뢰 못함 → `git diff --text`로 강제. 301 어셋 최종 diff = `+  SkillType: 3` 한 줄 (23줄 YAML).
- **Guardrail 확인**: `TimeStop_Effect.prefab`/`TimeStopEffect.cs`/`Player.prefab`/`preset.csv` git diff EMPTY, `ImportAll` 미사용 (로그 확인, `LinkSkillPrefabs`만), 루트 SkillData 15, `CanonicalPrefabLinks` 13, `PlayerEquipsGumMaster`(4=211-214)/`PresetsResolveToRoot` 단언 불변.
- **증거 파일**: `task-4-timestop-style-magic-skills-link.log`, `task-4-timestop-style-magic-skills-compile.log`, `task-4-timestop-style-magic-skills.xml`(EditMode 17/17), `task-4-timestop-style-magic-skills-playmode.xml`(PlayMode 5/5), `task-4-qa-fail.xml`(+rerun-a), `task-4-qa-fail-301.xml`(+rerun-b). (기존 magic-skill-vfx 플랜의 `task-4-compile.log` 등 08-04 증거는 미덮어씀.)


