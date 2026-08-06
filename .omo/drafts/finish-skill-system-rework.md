---
slug: finish-skill-system-rework
status: awaiting-approval
intent: clear
review_required: false
pending-action: waiting for user approval to execute .omo/plans/finish-skill-system-rework.md (then /start-work)
approach: skill-system-rework 마무리 — 커밋 상태 재확인, FILE_MAP.md v1.3 갱신, 회귀 테스트, F1~F4 최종 검증 웨이브, 플랜 체크박스 확정
---

# Draft: finish-skill-system-rework

## Why this plan exists
skill-system-rework 플랜은 Todo 1-6 **구현+검증 완료** (evidence 존재) 상태로, 커밋 게이트에서 멈췄다 (드래프트: "EXECUTION STALLED AT COMMIT GATE — 0/6 commits, FILE_MAP.md not updated, F1-F4 wave incomplete"). 이후 사용자가 직접 커밋(gg → fe93e5bd)했고 magic-skill-vfx 플랜이 완료되면서, 남은 것은 **마무리**: (1) 커밋 상태 재확인 (2) FILE_MAP.md 갱신 (3) F1~F4 검증 (4) 체크박스 확정.

## Pre-verified facts (2026-08-04 탐색 결과 — 플랜 근거)
- **rework 구현 완료 확인**: `Assets/Script/Combat/`에 SkillData.cs (SkillType enum+필드), MeleeHitbox.cs, Projectile.cs, Health.cs, ObjectPooler.cs, IBubbleAffectable.cs, SpriteVFXAnimator.cs 존재. PlayerController.cs:206-275 UseSkill 디스패치+쿨다운 게이트 구현 (InstantArea만 LogWarning). 테스트 4종 존재 (EditMode: SkillDataModelTests/SkillDataIntegrityTests/SpriteVFXAnimatorTests, PlayMode: SkillExecutionTests). SkillData 루트 14개 에셋, 하위 폴더 없음.
- **evidence 존재 확인**: `.omo/evidence/task-{1..6}-skill-system-rework.*` 전부 존재 (glob).
- **커밋 상태 (추정)**: 기존 `.omo/git-status.txt` (fe93e5bd 이전 스냅샷)에서 rework 산출물이 전혀 안 보임 (Combat/ 파일은 "M" = 이미 추적 중) → rework 파일은 사용자 커밋(gg 또는 fe93e5bd)에 포함된 것으로 보임. **단, 이 스냅샷은 fe93e5bd 이전 것이므로 현재 상태 재확인 필요** (Todo 1).
- **무결성 테스트는 magic-skill-vfx가 11→14/10→13으로 갱신 완료** — 현재 상태(14 루트)와 일치 (SkillDataIntegrityTests.cs:52 `Assert.AreEqual(14, ...)`, CanonicalAssetNames 13개 + 301).
- **FirePoint = {fileID: 0} 은 버그 아님**: PlayerController.cs:91-92 Awake에서 `if (combatSettings.FirePoint == null) combatSettings.FirePoint = transform.Find("FirePoint") ?? transform;` — prefab의 FirePoint 자식(Transform fileID 1595743548204521575)이 런타임에 자동 연결됨. 이전 세션의 B1 후보에서 제외.
- **FILE_MAP.md = 부분 갱신 상태** (v1.2, Projectile.cs/SpriteVFXAnimator.cs 설명만 반영, Combat/ 폴더·SkillType·스킬 14종·Projectiles 프리팹·테스트·미해결 이슈 미반영).
- **F2 리뷰는 이전 실행에서 보고 안 됨** (드래프트 기록). F1/F3/F4도 미실행. 체크박스 6+4개 전부 `[ ]`.
- **커밋 관례**: 사용자가 직접 커밋 (드래프트: "내가 직접 브랜치에 커밋함"). 실행자는 커밋 안 함.
- **F3 QA**: 사용자가 직접 Unity에서 확인 (드래프트: "커밋 후 내가 직접 확인").
- **Unity**: 6000.3.12f1 → `"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe"` (magic-skill-vfx 플랜과 동일).
- **툴 제약**: bash/git 명령 실행 불가 → git 검증은 사용자 터미널 → `.omo\*.txt` 저장 → 실행자가 읽는 패턴 사용 (이전 세션과 동일).

## Open assumptions (announced defaults)
- 재확인 결과 rework 파일이 이미 커밋됐다면 "커밋" 단계는 생략 (Todo 3 조건부).
- FILE_MAP.md 갱신은 v1.3, 문서 구조는 기존 v1.2 유지.
- 회귀 테스트 재실행 (EditMode/PlayMode) — 코드 변경이 없으므로 결과는 동일해야 함 (확인용).
- rework 플랜의 "6 atomic commits" 요구는 현실적으로 불가 (이미 커밋됨) → 단일 커밋 또는 기존 커밋 수용으로 처리.
- F2/F4 검증은 서브에이전트(oracle) + 파일 검증, F3만 사용자 수동.

## Decisions (with rationale)
1. **새 플랜 슬러그 `finish-skill-system-rework`** — 기존 rework 플랜은 "구현 완료"로 두고, 마무리 작업을 별도 플랜으로. 실행자가 명확한 지시를 받음.
2. **git 검증 = 사용자 터미널 → 파일 저장 → 실행자 읽기** — 실행자에 git 권한이 없으므로 (기존 관례).
3. **F3 QA 체크리스트를 플랜에 명시** — 사용자가 Unity에서 직접 확인할 항목을 구체적으로 제공, 완료 보고 시 체크박스 확정.
4. **회귀 테스트를 F1-F4 이전에 실행** — 현재 통합 상태(rework+vfx)에서의 green을 증거로 확보 후 리뷰 진행.
5. **FILE_MAP.md 갱신은 사용자 규칙 준수** ("고치면 바로바로 파일맵에 저장") — 이 플랜의 유일한 게임 파일 변경 (문서).
6. **편차 5건 사전 수용** (드래프트 KNOWN DEVIATIONS + 폴더 이동): (1) Combat/ 폴더+asmdef 이동 (후속 플랜이 의존, 수용) (2) 무결성 테스트 14/13 갱신 (3) TimeStop SkillName "Time Stop" (에셋 필드값) (4) PlayerEquipsGumMaster 네스팅 접근 (5) importer Yellow 버블 파싱 추가. F1 감사에서 수용 판정 명시.

## Approval gate
status: awaiting-approval
<!-- 사용자 승인 → .omo/plans/finish-skill-system-rework.md 작성 → /start-work -->
