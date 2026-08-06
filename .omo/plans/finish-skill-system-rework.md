# finish-skill-system-rework - Work Plan

## TL;DR (For humans)

**What you'll get:** skill-system-rework 플랜(스킬 시스템 재작업 — SkillType 디스패치, MeleeHitbox, VFX 프리팹, CSV 파이프라인, 무결성 테스트)의 **마무리 완료 처리**입니다. 이미 구현+검증된 작업(Todo 1~6, evidence 6건 존재)의 커밋 상태를 재확인하고, 아직 구버전인 `FILE_MAP.md`를 v1.3으로 갱신하고, 최종 검증 웨이브(F1~F4)를 돌려서 플랜을 공식 완료로 확정합니다.

**Why this approach:** rework는 지난 세션에서 "커밋 승인 대기" 상태로 멈췄고, 그 사이 사용자가 직접 커밋(gg → fe93e5bd)했고 magic-skill-vfx 플랜도 완료됐습니다. 남은 일은 (1) 커밋 상태 재확인 (2) 문서 갱신 (3) 검증 + 체크박스 확정뿐입니다. 코드를 새로 쓰지 않으므로 리스크가 낮고, 모든 검증은 기존 evidence + 재실행 테스트 + 서브에이전트 리뷰로 수행됩니다.

**What it will NOT do:** 게임 코드를 수정하지 않습니다 (`FILE_MAP.md` 문서 1개 제외 — 커밋 확인 결과 남은 게임 파일이 있으면 커밋만 안내). TimeStop 3파일은 바이트 단위로 그대로 둡니다. `.omo/`는 커밋하지 않습니다. B/C/D 항목(스킬 프리셋 시스템, 지하 바이옴 등)은 이 플랜 범위 밖입니다.

**Effort:** Small — 9개 작업 (대부분 진단/문서/검증, 코드 변경 0개, 사용자 터미널 1회 + 사용자 Unity QA 1회 필요).
**Risk:** Low — 코드 미변경, 테스트 재실행 확인용.
**Decisions to sanity-check:** (1) 커밋 상태 재확인 결과에 따라 "커밋" 단계(Todo 3)가 생략될 수 있음. (2) rework 플랜이 요구한 "6 atomic commits"는 이미 다른 커밋에 섞여 들어가서 불가 — 기존 커밋 수용. (3) F3 QA는 네가 직접 Unity에서 확인 (이전 결정 유지). (4) FILE_MAP.md에 반영할 "Animator Controller 연결 유실" 이슈는 현재 해결 여부를 확인해서 반영.

Your next move: approve, or run a high-accuracy review. Full execution detail follows below.

---

> TL;DR (machine): Small effort/low risk; 9 todos, 0 code changes (FILE_MAP.md only) — Todo 1 커밋 상태 재확인(사용자 터미널→.omo\*.txt), Todo 2 FILE_MAP.md v1.3 갱신, Todo 3 (조건부) 남은 게임 파일 커밋 안내, Todo 4 회귀 테스트 재실행(EditMode+PlayMode batchmode), Todo 5 F1 plan compliance(evidence 6건+편차 5건 수용+TimeStop diff), Todo 6 F2 코드 품질(oracle), Todo 7 F3 사용자 수동 QA(체크리스트), Todo 8 F4 scope fidelity(Must-NOT 8항목), Todo 9 rework 플랜 체크박스 [x] + 드래프트 completed; 커밋은 사용자 직접(관례), .omo 미커밋.

## Scope
### Must have
- **커밋 상태 재확인**: 사용자 터미널에서 git 상태/로그/추적 목록/diff를 `.omo\*.txt`로 저장 → 실행자가 읽고 rework·vfx 산출물의 커밋 여부를 판정.
- **FILE_MAP.md v1.3 갱신**: `Assets/Script/Combat/` 폴더(7개 컴포넌트), SkillData SkillType, 스킬 14종(201-203/211-214/221-226/301), `Assets/Prefabs/Projectiles/` 프리팹 8종, Editor `MagicVFXBuilder.cs`, Tests 폴더, 미해결 이슈 섹션 갱신.
- **회귀 테스트 재실행**: EditMode + PlayMode batchmode green (현재 통합 상태 검증).
- **F1~F4 최종 검증 웨이브**: F1 plan compliance · F2 코드 품질(oracle) · F3 사용자 수동 QA · F4 scope fidelity — 전원 APPROVE 후 확정.
- **플랜 체크박스 확정**: `skill-system-rework.md` Todo 1-6 + F1-F4 `[x]`, 드래프트 `completed` 처리.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- NO 게임 코드 변경 — `FILE_MAP.md` 문서 1개만 수정. 어떤 .cs/.asset/.prefab/.csv도 건드리지 않음.
- NO `301_TimeStop.asset`, `TimeStop_Effect.prefab`, `TimeStopEffect.cs` — byte-identical (`git diff` 빈 결과 필수, F1/F4에서 검증).
- NO `.omo/**` 커밋 (플랜·evidence·세션 기록 — 영구 제외).
- NO rework 플랜의 Must-NOT 재검토 없이 위반 (F4에서 diff로 검증: 매나 시스템 없음, TryFire/ObjectPooler/버블 프리팹 미변경, InstantArea 미구현 유지).
- NO 스코프 확장 — B(버그 후보 중 FirePoint는 비버그로 판명됨), C(푸시), D(새 기능) 항목은 이 플랜 밖.
- NO `git checkout -- .omo` 등 되돌리기 명령 (체크박스·승인 기록 보존).
- NO F3 QA를 자동화로 대체 — 사용자가 직접 Unity에서 확인 (이전 결정).

## Verification strategy
> 모든 검증은 agent-executed + 사용자 터미널 보조 (git 권한 없음).
- **Test decision**: tests-after 재실행 (코드 변경 없음 → 결과 동일해야 함). Framework: com.unity.test-framework 1.6.0 (기설치).
- Unity binary (6000.3.12f1): `"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe"` — 없으면 `where Unity.exe`로 탐색.
- Compile check: `Unity.exe -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile -` → 로그에 `error CS` 없음.
- Test run template: `Unity.exe -batchmode -runTests -projectPath "D:\coding\github c\clubgame" -testPlatform EditMode -testResults "D:\coding\github c\clubgame\.omo\evidence\finish-4-editmode.xml" -logFile -` (PlayMode 동일, 파일명 finish-4-playmode.xml). Pass = XML `<test-run result="Passed" failed="0">`.
- **git 검증은 사용자 터미널 패턴**: 사용자가 아래 명령 출력을 `.omo\`로 저장 → 실행자가 읽고 판정. (실행자에게 git 명령 권한 없음 — 기존 관례)
- Evidence: `.omo/evidence/finish-<todo>-finish-skill-system-rework.<ext>` 패턴.

## Execution strategy
### Parallel execution waves
- **Wave 1**: Todo 1 (커밋 상태 재확인 — 사용자 터미널 요청 + 판정). 조건부 분기: 게임 파일 미커밋 → Todo 3 활성화.
- **Wave 2** (병렬): Todo 2 (FILE_MAP.md v1.3 — 사용자 규칙) · Todo 4 (회귀 테스트 — 배치모드) — 서로 독립.
- **Wave 3** (병렬): Todo 5 (F1 감사) · Todo 6 (F2 oracle) · Todo 7 (F3 사용자 QA — 사용자 의존, 체크리스트 제공 후 대기) · Todo 8 (F4 diff 검증) — Todo 4 결과를 전제.
- **Wave 4**: Todo 3 (조건부 커밋 — 사용자 직접, Todo 2 완료 후) + Todo 9 (체크박스 확정 — F1~F4 전원 APPROVE 후).

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 커밋 상태 재확인 | — | 2, 3, 4 | — |
| 2 FILE_MAP v1.3 | 1 | 3, 9 | 4 |
| 3 (조건부) 커밋 | 2 | 9 | — |
| 4 회귀 테스트 | 1 | 5, 6, 7, 8 | 2 |
| 5 F1 감사 | 4 | 9 | 6, 7, 8 |
| 6 F2 코드 품질 | 4 | 9 | 5, 7, 8 |
| 7 F3 사용자 QA | 4 | 9 | 5, 6, 8 |
| 8 F4 scope fidelity | 4 | 9 | 5, 6, 7 |
| 9 체크박스 확정 | 2, 3, 5, 6, 7, 8 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [ ] 1. 커밋 상태 재확인 (사용자 터미널 → .omo\*.txt → 실행자 판정)
  What to do / Must NOT do: 사용자에게 아래 PowerShell 블록을 터미널에서 실행하도록 안내 (결과는 모두 .omo\ 폴더에 저장됨, .omo는 커밋 대상 아님):
  ```powershell
  cd "D:\coding\github c\clubgame"
  git status --porcelain | Out-File -Encoding utf8 .omo\git-status.txt
  git log --oneline -8 | Out-File -Encoding utf8 .omo\git-log.txt
  git ls-files | Select-String -Pattern "Combat/|MeleeHitbox|SkillData|MagicVFXBuilder|Tests/" | Out-File -Encoding utf8 .omo\git-ls-files.txt
  git diff HEAD --stat | Out-File -Encoding utf8 .omo\git-diff-stat.txt
  git diff HEAD -- Assets/Resources/SkillData/301_TimeStop.asset Assets/Prefabs/Projectiles/TimeStop_Effect.prefab Assets/Script/TimeStopEffect.cs | Out-File -Encoding utf8 .omo\git-diff-timestop.txt
  ```
  실행자가 5개 파일을 읽고 판정: (a) `git-status.txt`에 `.omo/` 외 게임 파일(`Assets/`, `tiger/`, `FILE_MAP.md`, `clubgame.slnx`)이 보이면 → **미커밋 존재** = Todo 3 활성화 + 목록을 `.omo/evidence/finish-1-uncommitted.txt`로 기록. (b) `.omo/`만 남아 있으면 → **전부 커밋됨** = Todo 3 생략. (c) `git-ls-files.txt`로 rework 산출물(Combat/ 폴더, MeleeHitbox, SkillDataIntegrityTests 등)이 추적 중인지 확인 — 추적 안 된 rework 산출물이 보이면 Todo 3 활성화. (d) `git-diff-timestop.txt`가 비어 있으면 TimeStop byte-identical 확인 (F1/F4의 사전 증거로 기록). Must NOT: git 명령을 직접 실행하려 하지 않기 (권한 없음 — 사용자 안내가 유일한 경로). Must NOT: 판정 결과와 관계없이 `.omo`를 커밋하려 하지 않기.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2, 3, 4
  References: 기존 `.omo/git-status.txt` (fe93e5bd 이전 스냅샷 — 갱신 필요), `.omo/evidence/task-0-dirty-worktree.txt` (magic-skill-vfx 시작 시점 — rework 97파일 미커밋이었던 기록), `.git/logs/HEAD` (마지막 커밋 fe93e5bd "feat(vfx)").
  Acceptance criteria (agent-executable): 5개 출력 파일이 `.omo/`에 생성됐고 (사용자 확인), 판정 결과가 명시됨 — "전부 커밋됨" 또는 "미커밋 N건: [목록]". git-status.txt에 `.omo/` 외 항목이 없으면 PASS.
  QA scenarios: happy — `.omo`만 남음 → Todo 3 생략 명시; failure — 게임 파일 미커밋 발견 → Todo 3 활성화, 목록 보존.
  Commit: N (진단 — 커밋 안 함)

- [ ] 2. FILE_MAP.md v1.3 갱신 (사용자 규칙: "고치면 바로바로 파일맵에 저장")
  What to do / Must NOT do: `FILE_MAP.md`를 v1.3으로 갱신. 기존 v1.2 구조(섹션 1 루트 / 2.1 Scripts / 2.2 Editor / 2.3 Prefabs / 2.4 Data / 3 진행 상태)는 유지하되 다음을 반영:
  (1) **섹션 2.1 Scripts**: `Assets/Script/Combat/` 하위 항목 7개를 명시 — Health.cs, ObjectPooler.cs, Projectile.cs(임팩트 훅 설명 유지), SkillData.cs(**SkillType enum: Projectile/Melee/MeleeAoE/InstantArea + 타입별 필드**), MeleeHitbox.cs(짧은 생명주기 트리거 히트박스, 1회 타격), IBubbleAffectable.cs, SpriteVFXAnimator.cs(3단계 VFX). PlayerController.cs 설명에 "ZXCV 스킬 디스패치(SkillType 기반) + 슬롯별 쿨다운 게이트" 추가.
  (2) **섹션 2.2 Editor Tools**: MagicVFXBuilder.cs (마법 VFX 프리팹 일괄 생성, Custom Tools/tiger/Magic VFX) 추가. DataImportMenu.cs 설명에 "SkillType/Bubble/Speed/MeleeRange/MeleeArc 컬럼 파싱, LinkSkillPrefabs, EquipGumMasterOnPlayer" 추가.
  (3) **섹션 2.3 Prefabs**: `Assets/Prefabs/Projectiles/` 폴더 — FireBallProjectile, IceBlastProjectile, ThunderBoltProjectile, DarkBoltProjectile, HolyProjectile, AcidProjectile, MeleeHitbox, TimeStop_Effect (8종) 명시. BubbleProjectile_{blue,red,yellow}는 211-214 재사용 언급.
  (4) **섹션 2.4 Data**: SkillData 루트 14종을 그룹으로 명시 — 근접 201-203, 거품 211-214, 마법 221-226, TimeStop 301. 하위 폴더 중복 제거 완료 언급.
  (5) **Tests**: `Assets/Tests/EditMode/` (SkillDataModelTests, SkillDataIntegrityTests — 무결성 14루트/13캐노니컬+MagicVFXAnimatorWired, SpriteVFXAnimatorTests), `Assets/Tests/PlayMode/` (SkillExecutionTests) 섹션 신설.
  (6) **섹션 3 진행 상태**: "진행 완료"에 스킬 시스템 재작업(2026-08-03~04), 마법 VFX 6종(2026-08-04) 추가. "미해결 이슈"의 Animator Controller 연결 유실 항목은 **현재 해결 여부를 확인해서** (a) 해결됐으면 완료 처리 + 해결 내용, (b) 미해결이면 그대로 유지 + 현재 상태 주석. 날짜를 2026-08-04로 갱신, 버전 v1.3.
  Must NOT: 파일 구조를 새로 설계하지 않기 (기존 v1.2 레이아웃 유지). Must NOT: 다른 게임 파일 수정. Must NOT: 스킬 데이터를 문서에 하드코딩한 값과 다르게 기록 (ID/이름은 14종 실제 상태 기준).
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 3, 9
  References: `FILE_MAP.md` (현재 v1.2, 81줄), `Assets/Script/Combat/` (glob 7파일), `Assets/Resources/SkillData/` (glob 14 .asset), `Assets/Prefabs/Projectiles/` (glob 8 .prefab), `Assets/Tests/` (glob 4 .cs), `Assets/Editor/` (MagicVFXBuilder.cs + DataImportMenu.cs), `DEV_DOC.md` (v1.2 탐험형 컨셉 — 파일맵과 대조).
  Acceptance criteria (agent-executable): 파일맵 v1.3에 위 6개 반영 항목이 전부 존재 (grep으로 확인: "Combat", "SkillType", "MeleeHitbox", "MagicVFXBuilder", "224_DarkBolt" 등). "미해결 이슈" 섹션의 Animator 항목 상태가 명시됨. 날짜/버전 갱신 확인.
  QA scenarios: happy — grep으로 신규 키워드 6종 검출; failure — v1.2 그대로인 채로 통과시키지 않기 (버전 문자열 "v1.2"가 남아 있으면 실패 처리).
  Commit: N (Todo 3에서 사용자 커밋)

- [ ] 3. (조건부) 남은 게임 파일 커밋 — 사용자 직접 (Todo 1 판정 결과에 따라)
  What to do / Must NOT do: Todo 1 판정 결과 (a) 미커밋 게임 파일이 있으면 → 사용자에게 아래 명령 안내:
  ```powershell
  cd "D:\coding\github c\clubgame"
  git add -A -- . ':(exclude).omo'
  git commit -m "docs(filemap): FILE_MAP v1.3 갱신 + 남은 게임 파일 커밋"
  ```
  커밋 후 `.git/logs/HEAD`로 새 커밋 hash 확인 (마지막 줄). (b) 전부 커밋됨이면 → 이 todo는 "생략 (판정 결과)"로 체크 처리, 커밋 명령 안내하지 않기. Must NOT: 실행자가 커밋/스테이지 명령 실행 (사용자 직접 커밋 관례). Must NOT: `.omo/**` 스테이지 (`:(exclude).omo` 가드). Must NOT: 사용자가 `git checkout -- .` 등 되돌리기 하도록 안내.
  Parallelization: Wave 4 | Blocked by: 2 | Blocks: 9
  References: Todo 1 판정 결과, `.git/logs/HEAD` (검증용), 이전 커밋 패턴 fe93e5bd.
  Acceptance criteria (agent-executable): 커밋 완료 시 `.git/logs/HEAD` 마지막 줄에 새 hash + "docs(filemap)" 메시지 확인. 생략 시 판정 근거 파일(`finish-1-uncommitted.txt` 또는 git-status.txt) 존재.
  QA scenarios: happy — 사용자 커밋 후 HEAD 갱신 확인; failure — 커밋 실패/누락 시 사용자에게 다시 안내 (명령 재실행).
  Commit: Y (사용자가 직접, 메시지 위 예시)

- [ ] 4. 회귀 테스트 재실행 (EditMode + PlayMode batchmode) — 현재 통합 상태 green 확인
  What to do / Must NOT do: (1) 컴파일 체크: `"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\coding\github c\clubgame" -logFile -` → 로그에 `error CS` 없음 (출력 .omo/evidence/finish-4-compile.log). (2) EditMode: `-runTests -testPlatform EditMode -testResults "D:\coding\github c\clubgame\.omo\evidence\finish-4-editmode.xml"` → `<test-run result="Passed" failed="0">`. (3) PlayMode: `-testPlatform PlayMode -testResults "D:\coding\github c\clubgame\.omo\evidence\finish-4-playmode.xml"` → 동일. 기대: EditMode ≥ 16 (SkillDataModelTests + SkillDataIntegrityTests 8종 + SpriteVFXAnimatorTests), PlayMode ≥ 4 (SkillExecutionTests). Must NOT: 테스트 수정, 코드 수정, 테스트 통과를 위해 assertion 완화. Must NOT: `.omo/evidence/` 기존 파일 삭제.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 5, 6, 7, 8
  References: `Packages/manifest.json` (test-framework 1.6.0), `ProjectSettings/ProjectVersion.txt` (6000.3.12f1), 마지막 기록: EditMode 16/16, PlayMode 4/4 (magic-skill-vfx task-5 evidence), skill-system-rework evidence task-6 (EditMode 8/8 당시).
  Acceptance criteria (agent-executable): XML 2개 `result="Passed" failed="0"`; 컴파일 로그 `error CS` 0건.
  QA scenarios: happy — 2개 스위트 green, evidence 저장; failure — 실패 시 실패 테스트명을 플랜에 보고하고, 코드 변경이 없으므로 (마지막 커밋 대비) 상태 이상임을 플랜 수준에서 기록 — 무단 수정 금지, 사용자에게 보고.
  Commit: N (코드 변경 없음 — 확인용)

- [ ] 5. F1. Plan compliance audit — rework 플랜 6개 todo의 evidence·커밋·범위 검증
  What to do / Must NOT do: (1) `.omo/evidence/`에서 task-1..6-skill-system-rework.* 6건 전부 존재 확인 (glob). (2) Todo 1의 git-ls-files/git-status 판정 결과로 rework 산출물 커밋 존재 확인. (3) 범위 검증: `git-diff-stat.txt`를 읽고, 마지막 커밋(fe93e5bd) 이후 변경이 `.omo/` 외 게임 파일에 있는지 — 있으면 Todo 3 대상(의도된 것)인지, 플랜 범위 밖 변경인지 구분. (4) **편차 5건 수용 판정** (사전 합의 — 증거와 함께 기록): ① `Assets/Script/` → `Combat/` 폴더 이동 + Combat.asmdef (플랜 지시 밖이었으나 후속 플랜 magic-skill-vfx가 동일 구조에 의존 → 수용) ② 무결성 테스트 11→14 루트/10→13 캐노니컬 + MagicVFXAnimatorWired (magic-skill-vfx 의도적 갱신 → 수용) ③ TimeStop 테스트: SkillName "Time Stop" (에셋 필드값) vs 플랜 "TimeStop" (파일명 약칭 → 수용) ④ PlayerEquipsGumMaster: EquippedSkills를 combatSettings 네스팅으로 접근 (필드 구조상 필수 → 수용) ⑤ importer Yellow 버블 파싱 브랜치 추가 (213=Yellow에 필요, 기존 파싱 버그 수정 → 수용). (5) `git-diff-timestop.txt`가 비어 있는지 확인 — 비어 있으면 TimeStop byte-identical PASS. 결과를 `.omo/evidence/finish-5-F1.txt`로 기록 (체크 항목별 PASS/편차 수용 근거).
  Parallelization: Wave 3 | Blocked by: 4 | Blocks: 9
  References: rework 플랜 Todo 1-6 명세, `.omo/evidence/task-{1..6}-skill-system-rework.*`, `.omo/drafts/skill-system-rework.md` (KNOWN DEVIATIONS 섹션), Todo 1 출력 5종.
  Acceptance criteria (agent-executable): evidence 6건 존재, 편차 5건 각각 수용 근거 기록, TimeStop diff empty 확인, 기록 파일 finish-5-F1.txt 존재. 판정 APPROVE/CONDITIONAL.
  QA scenarios: happy — 전 항목 PASS + 편차 수용 → APPROVE; failure — evidence 누락 또는 범위 밖 변경 발견 시 CONDITIONAL로 보고 (수정 지시가 아닌 보고 — 코드 수정 금지).
  Commit: N

- [ ] 6. F2. Code quality review — oracle 서브에이전트 (rework 코드 품질)
  What to do / Must NOT do: oracle 서브에이전트에 리뷰 위임: `task(subagent_type="oracle", description="F2 code quality review", prompt="...")`. 리뷰 대상: `Assets/Script/Combat/MeleeHitbox.cs`, `Assets/Script/player/PlayerController.cs` UseSkill 디스패치(206-275행), `Assets/Editor/DataImportMenu.cs` 추가분(타입/버블/근접 파싱, LinkSkillPrefabs, EquipGumMasterOnPlayer), `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` + `Assets/Tests/PlayMode/SkillExecutionTests.cs`. 체크 항목 (rework 플랜 F2 명세): `SpawnFromPool("Projectile"`이 PlayerController에 없음, MeleeHitbox에 ObjectPooler 미사용, 파싱 로직 중복 없음, 죽은 필드/메서드 없음, 기존 코드와 네이밍 일관성, 핫패스에 Debug.Log 잔존 없음. 리뷰 결과를 `.omo/evidence/finish-6-F2.txt`로 기록 (APPROVE / 비차단 권고 목록). Must NOT: oracle의 지적을 코드 수정으로 처리 (이 플랜은 코드 미변경 — 권고는 기록만 하고 사용자에게 보고).
  Parallelization: Wave 3 | Blocked by: 4 | Blocks: 9
  References: rework 플랜 F2 항목(145행), 리뷰 대상 파일 4종, 이전 F2 패턴 (magic-skill-vfx ses_0340b77e3ffehyOfbLkAliByIe — 빈 Awake() 1건 권고).
  Acceptance criteria (agent-executable): oracle 리뷰 receipt 존재, 체크 항목별 판정 기록, APPROVE 또는 비차단 권고 명시.
  QA scenarios: happy — APPROVE + 비차단 권고 0~N건; failure — 차단 이슈 발견 시 CONDITIONAL로 보고 (수정은 사용자 결정).
  Commit: N

- [ ] 7. F3. 사용자 수동 QA — Unity에서 ZXCV 스킬 직접 확인 (이전 결정: "커밋 후 내가 직접 확인")
  What to do / Must NOT do: 사용자에게 아래 체크리스트를 제공하고 Unity에서 확인 요청 (mainscene 열고 Play):
  1. **Z** → GumShot(파란 버블) 발사 — 마우스 방향으로 날아감
  2. **X** → StickyBlob(빨간 버블) 발사 — 적에 맞으면 **슬로우**(Red, 3초)
  3. **C** → BigBubble(노란 버블) 발사 — 적에 맞으면 **스턴**(Yellow, 1초)
  4. **V** → PopTrap 발사
  5. 각 스킬 발사 후 **HUD 쿨다운 오버레이**가 도는지 (SkillHUDManager)
  6. 연타 시 쿨다운이 끝나기 전엔 발사 안 되는지 (쿨다운 게이트)
  7. 콘솔에 **에러/경고 없음** (스킬 코드에서 나오는 로그만 허용)
  8. (선택) 근접 스킬 201-203은 키 미할당 상태가 정상 (프리셋 시스템에서 장착 예정)
  사용자가 결과("QA 통과" 또는 실패 항목)를 보고하면 → 실행자가 `.omo/evidence/finish-7-F3.txt`로 기록 (통과/실패 항목, 날짜). 실패 항목이 있으면 플랜 수준에서 보고 — **코드 수정은 이 플랜 밖** (별도 플랜/사용자 결정). Must NOT: 자동화 테스트로 대체. Must NOT: 결과를 사용자 보고 없이 추측으로 기록.
  Parallelization: Wave 3 | Blocked by: 4 | Blocks: 9
  References: `Assets/Prefabs/Player.prefab` (EquippedSkills 4종 — 211-214 확인됨), PlayerController.cs:149-153 (ZXCV 매핑), `Assets/Script/SkillHUDManager.cs`, rework 플랜 F3 항목(146행).
  Acceptance criteria (agent-executable): 사용자 QA 결과 보고 수신, finish-7-F3.txt 기록. 통과 시 F3 APPROVE.
  QA scenarios: happy — 사용자 "통과" → F3 APPROVE + 체크박스; failure — 실패 항목 보고 → 사용자에게 어떤 스킬이 어떻게 안 됐는지 상세 요청, 수정은 후속 작업으로.
  Commit: N

- [ ] 8. F4. Scope fidelity — rework Must-NOT 8항목 diff 검증
  What to do / Must NOT do: rework 플랜 Must NOT have(31-39행)를 최종 diff 대비 1:1 검증:
  1. TimeStop 3파일 byte-identical → `git-diff-timestop.txt` 비어 있음 (Todo 1에서 확보)
  2. 매나 시스템 없음 → grep `Mana` (ManaCost 필드 존재는 OK, UseSkill에 매나 로직 없음 확인 — PlayerController.cs에서 `ManaCost` 사용처 없음)
  3. 신규 VFX 프리팹 6종에 Animator 컴포넌트 없음 → prefab YAML grep (`Assets/Prefabs/Projectiles/{FireBall,IceBlast,ThunderBolt,DarkBolt,Holy,Acid}Projectile.prefab`에 `Animator:` 없음)
  4. TryFire(PlayerController.cs:289-) / ObjectPooler / Blue-Red-Yellow 버블 프리팹 미변경 → `git-diff-stat.txt`에서 해당 파일 미포함 (또는 rework 커밋 시점 diff 확인)
  5. 적/대미지 시스템/멀티샷 스프레드 미변경 → diff-stat에서 Enemy*/Health 외 파일 미포함
  6. Assets 밖 편집 없음 → 변경이 `tiger/datafiles/skill/{magic,melee,ranged}skill.csv` + `FILE_MAP.md`만 (기존 수정분 + 이 플랜 문서) — clubgame.slnx 등 사전 변경은 task-0 스냅샷 기록 대조
  7. InstantArea 미구현 → PlayerController.cs:265-267 `Debug.LogWarning("InstantArea skill type is not implemented yet.")` 유지 확인
  8. MeleeHitbox에 ObjectPooler 미사용 → MeleeHitbox.cs 소스에 `ObjectPooler` 없음 (확인됨 — 유지)
  결과를 `.omo/evidence/finish-8-F4.txt`로 기록 (항목별 PASS/FAIL). Must NOT: 검증을 위해 코드 수정. Must NOT: 미확인 항목을 PASS로 표기.
  Parallelization: Wave 3 | Blocked by: 4 | Blocks: 9
  References: rework 플랜 Must NOT have 목록(31-39행), Todo 1의 git-diff-stat.txt/git-diff-timestop.txt, PlayerController.cs, MeleeHitbox.cs, 프리팹 6종 YAML, task-0-dirty-worktree.txt (사전 변경 대조).
  Acceptance criteria (agent-executable): 8항목 각각 PASS/FAIL 판정 기록, finish-8-F4.txt 존재, 전부 PASS 시 F4 APPROVE.
  QA scenarios: happy — 8/8 PASS → APPROVE; failure — FAIL 항목 1건이라도 있으면 CONDITIONAL로 보고 (수정 지시 아님).
  Commit: N

- [ ] 9. 플랜 체크박스 확정 + 상태 갱신 (skill-system-rework completed 처리)
  What to do / Must NOT do: (1) `.omo/plans/skill-system-rework.md`의 Todo 1-6 + F1-F4 체크박스를 `[ ]` → `[x]`로 확정. 각 F 체크박스 아래에 VERDICT 노트 추가: "APPROVE (2026-08-04, finish-skill-system-rework 플랜)" — F3는 "사용자 수동 QA 통과 (2026-08-04)". (2) `.omo/drafts/skill-system-rework.md` status: `executing` → `completed`, pending-action 정리, "Session 2026-08-03 — EXECUTION STATUS"에 마무리 완료 기록 추가. (3) `.omo/drafts/finish-skill-system-rework.md` status: `completed`. (4) F1~F4 전원 APPROVE일 때만 실행 (Todo 5/6/7/8 결과 확인). (5) 최종 요약 출력: 체크박스 6+4 = 10개 [x] 확인 (grep). Must NOT: F-wave가 미완료인 상태에서 체크박스 확정. Must NOT: `.omo` 커밋. Must NOT: rework 플랜의 본문/명세 수정 (체크박스와 노트만).
  Parallelization: Wave 4 | Blocked by: 2, 3, 5, 6, 7, 8 | Blocks: —
  References: `.omo/plans/skill-system-rework.md` (체크박스 10개), `.omo/drafts/skill-system-rework.md`, `.omo/drafts/finish-skill-system-rework.md`, magic-skill-vfx 플랜의 확정 패턴 (F1-F4 [x] + "승인 완료" 노트).
  Acceptance criteria (agent-executable): rework 플랜 체크박스 10개 전부 `[x]`, 드래프트 2개 status: completed, 최종 grep으로 `[x]` 10건 확인.
  QA scenarios: happy — grep `^\s*- \[x\]` 10건; failure — `[ ]` 잔존 시 미확정 원인 보고.
  Commit: N (.omo — 커밋 금지)

## Final verification wave
> Todos 5-8이 F1~F4. 전원 APPROVE 후 Todo 9에서 체크박스 확정. 사용자 승인은 F3(수동 QA)로 대체 — 추가 승인 불필요.
- [ ] F1. Plan compliance — Todo 5: evidence 6건, 커밋 존재, 편차 5건 수용, TimeStop diff empty.
- [ ] F2. Code quality — Todo 6: oracle 리뷰, rework 플랜 F2 6개 체크 항목.
- [ ] F3. Manual QA — Todo 7: 사용자 Unity 확인 8항목 (ZXCV 발사, 쿨다운, 버블 효과, 콘솔).
- [ ] F4. Scope fidelity — Todo 8: Must-NOT 8항목 diff 검증.

## Commit strategy
- 커밋은 **사용자 직접** (관례). 실행자는 커밋 명령을 실행하지 않음.
- Todo 3 (조건부): `git add -A -- . ':(exclude).omo'` + 커밋 메시지 `docs(filemap): FILE_MAP v1.3 갱신 + 남은 게임 파일 커밋`.
- `.omo/**`는 절대 스테이지/커밋 금지 (플랜·evidence·세션 기록 — 커밋 제외 영구 규칙).
- Todo 1의 `.omo\*.txt` 출력물은 진단용 임시 파일 — 확인 후 남겨둬도 커밋 안 됨 (선택 삭제).

## Success criteria
- skill-system-rework 플랜 체크박스 6(Todo) + 4(F1-F4) = 10개 전부 `[x]`, 드래프트 2개 `completed`.
- FILE_MAP.md v1.3 — Combat/ 폴더, SkillType, 스킬 14종, 프리팹 8종, 에디터 툴, 테스트, 미해결 이슈 상태 반영.
- 회귀 테스트 green: EditMode ≥ 16/16, PlayMode ≥ 4/4 (finish-4-*.xml).
- TimeStop 3파일 `git diff` 빈 결과 (git-diff-timestop.txt).
- 커밋 상태 판정 완료: 게임 파일 전부 커밋됨 (또는 Todo 3 커밋 완료).
- `.omo/evidence/finish-{1,4,5,6,7,8}-*.{txt,xml}` 존재.
