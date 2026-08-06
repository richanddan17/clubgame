---
slug: timestop-style-magic-skills
status: approved
intent: clear
review_required: false
approved-at: 2026-08-05
pending-action: execute .omo/plans/timestop-style-magic-skills.md (via $start-work)
approach: (1) PlayerController.UseSkill InstantArea 케이스 구현(플레이어 위치에서 프리팹 스폰), (2) 301_TimeStop.asset에 SkillType: 3(InstantArea) 추가 — 현재 어셋에 SkillType 필드가 없어 enum 기본값 Projectile로 디스패치됨, (3) 신규 스킬 227_TimeWarp 추가(CSV 행 + prefabMap + TimeStopEffectBuilder 프리팹 + Icon), (4) 무결성 스위트 14→15 갱신 + InstantAreaSkillsWired 추가, (5) PlayMode InstantArea 테스트, (6) FILE_MAP 갱신. 검증은 헤드리스 batchmode(컴파일 체크/EditMode/PlayMode), 커밋은 사용자 직접.
---

# Draft: timestop-style-magic-skills

## Components (topology ledger)
| id | outcome | status | evidence path |
| --- | --- | --- | --- |
| c1 | PlayerController.InstantArea 케이스: 프리팹을 플레이어 위치에 스폰, 쿨타임/HUD 소비 | active | Assets/Script/player/PlayerController.cs:265-267 |
| c2 | 301_TimeStop.asset SkillType=InstantArea (YAML 한 줄 추가) | active | Assets/Resources/SkillData/301_TimeStop.asset |
| c3 | 신규 스킬 227_TimeWarp: CSV+prefabMap+빌더 프리팹(209 프레임)+Icon | active | tiger/datafiles/skill/magicskill.csv, DataImportMenu.cs, 새 프리팹 |
| c4 | 무결성 스위트 14→15 + InstantAreaSkillsWired + TimeStopUntouched SkillType 단언 | active | Assets/Tests/EditMode/SkillDataIntegrityTests.cs |
| c5 | PlayMode InstantArea 실행 테스트 (+TestBubbleAffectable 스턴 카운터) | active | Assets/Tests/PlayMode/SkillExecutionTests.cs |

## Open assumptions (announced defaults)
| assumption | adopted default | rationale | reversible? |
| --- | --- | --- | --- |
| 301 어셋 수정(전 플랜 guardrail "byte-identical" 의도적 폐기, SkillType 필드 한정) | 301_TimeStop.asset에 SkillType: 3 추가. TimeStop_Effect.prefab/TimeStopEffect.cs는 불변 | 301이 현재 Projectile로 디스패치됨(SkillType 미시리얼라이즈=enum 0). InstantArea 구현이 301에 적용되려면 필수. TimeStopUntouched 테스트는 SkillType 미검사라 안전 | 예(원복=필드 삭제) |
| 신규 스킬 정체성 | ID 227 "TimeWarp", Damage 0, ManaCost 30, Cooldown 10, InstantArea. 프리팹: 209 그린 클락 15프레임, radius 15 / stunDuration 3 / lifeTime 1.5, scale 5 | TimeStop 스타일(순수 스턴, 데미지 없음) 유지. 209 시트는 미사용 → 301(210 주황)과 색 대비. TimeStopEffect 재사용으로 C# 변경 0 | 예(CSV/프리팹 숫자만 수정) |
| 데미지 지원 안 함 | 신규 스킬도 TimeStopEffect처럼 스턴 전용(데미지 없음) | 301=Damage 0 선례, "TimeStop 스타일" = 스턴 테마. 데미지 AoE는 별도 컴포넌트 필요 → 범위 밖 | 예 |
| 테스트 전략 | tests-after + 에이전트 QA(EditMode+PlayMode batchmode) | 구현이 작고 기존 테스트가 계약을 고정. magic-vfx 선례와 동일 | n/a |
| 장착/프리셋 불변 | Player.prefab EquippedSkills·preset.csv·Player.prefab 불변 (4슬롯=211-214 유지) | PlayerEquipsGumMaster가 정확히 4개=211-214 단언. 신규 스킬은 데이터+테스트로만 검증 | 예 |

## Findings (cited - path:lines)
- `PlayerController.cs:265-267`: `case SkillType.InstantArea: Debug.LogWarning(...); return;` — 미구현 지점. UseSkill의 다른 케이스 패턴: Projectile=FirePoint+각도, Melee=FirePoint+dir*0.6, MeleeAoE=`transform.position, Quaternion.identity`(259). 스폰 성공 시 `_skillLastUsed[slotIndex]=Time.time` + `TriggerCooldown`(270-274).
- `SkillData.cs:3`: `enum SkillType { Projectile, Melee, MeleeAoE, InstantArea }` → InstantArea=3.
- `301_TimeStop.asset`(전문): YAML에 SkillType 필드 없음 → 런타임 enum 기본값 0=Projectile. 현재 301은 InstantArea로 디스패치 불가. Icon guid `58f146e2...`=pipo-btleffect210_192, ProjectilePrefab guid `b9d01243...`=TimeStop_Effect.prefab, ManaCost 50/Cooldown 15/Damage 0.
- `TimeStopEffect.cs`: radius 5/stunDuration 5/lifeTime 1 기본값, Start()에서 플레이어 태그 탐색→ApplyEffect(OverlapCircleAll→ApplyStun), Update()에서 플레이어 추종, `Destroy(gameObject, lifeTime)`. 프리팹이 직렬화 값(radius 20/stun 5/life 1.5)으로 오버라이드.
- `TimeStop_Effect.prefab`: scale (5,5,1), SpriteRenderer sortingOrder 20, TimeStopEffect, SimpleSpriteAnimator(210 시트 15프레임, fps 12, loop 0). 콜라이더/Projectile 없음 → 프리팹 자체가 자가구동, 스폰만 하면 동작.
- `SkillData.cs:5-35`: 필드 ID/SkillName/Damage/ManaCost/Cooldown/Icon/ProjectilePrefab/.../SkillType/.../AoERadius(3f, 미사용 — 프리팹이 radius 소유).
- `magicskill.csv`: 221-226 전부 Projectile. 다음 ID 227. 헤더 10열. `DataImportMenu.ImportSkillFile`은 헤더 동적 파싱 + `Enum.TryParse`(Type) → "InstantArea" 행 파싱 가능.
- `DataImportMenu.cs:310-323` prefabMap: 201-203/211-214/221-226 → 배치 `ImportSkillDataOnly`→`LinkSkillPrefabs` 순서(전 플랜 관례). `ImportAll` 금지(적/바이옴 재파싱=F4 위반).
- `SkillDataIntegrityTests.cs`: CanonicalAssetNames 13개(201-226), 루트 14개 단언(50-53), TimeStopUntouched(269-283, SkillType 미검사), CanonicalSkillsWired/SkillPrefabStructure는 CanonicalPrefabLinks 13개 키만 순회 → 227을 거기에 넣지 않으면 안전, MagicVFXAnimatorWired 221-226만, PlayerEquipsGumMaster 4개=211-214.
- `SkillExecutionTests.cs`: 리플렉션으로 UseSkill 호출, `TestBubbleAffectable.ApplyStun`은 현재 빈 메서드(268) → 스턴 카운터 추가 필요. SetUp이 Player 태그/카메라/적 생성 → InstantArea 테스트 인프라 그대로 사용 가능.
- Pipoya 209 시트: `Assets/Sprite/vfx/Pipoya VFX TimeMagic/Pipoya VFX TimeMagic/192x192/pipo-btleffect209_192.png` — 스프라이트 15개(`_0`~`_14`, meta 확인). 480x480 버전도 존재. 시트 guid는 meta 2번째 줄(실행자가 직접 판독).
- Unity exe `D:\coding\6000.3.12f1\Editor\Unity.exe`(존재 확인). GOTCHA: `-runTests`와 `-quit` 동시 금지, `-logFile` 실제 파일 필수, 락 경합 시 exit 1+`error CS` 없음 → 대기+재시도 선례(magic-skill-vfx:146).

## Decisions (with rationale)
- D1: InstantArea 케이스는 MeleeAoE 패턴 미러링 — `Instantiate(skill.ProjectilePrefab, transform.position, Quaternion.identity)`, null 프리팹 시 경고+return, spawned=true. TimeStopEffect가 자가구동(Start/Update/Destroy)이므로 Initialize 불필요. (PlayerController.cs:253-263, 265-267 참조)
- D2: 301_TimeStop.asset에 `SkillType: 3` 한 줄 추가 — 전 플랜의 byte-identical guardrail은 그 플랜 스코프 한정이었고, 이번 플랜의 목적 자체가 301을 InstantArea로 발동시키는 것. TimeStop_Effect.prefab/TimeStopEffect.cs는 불변 유지. TimeStopUntouched에 SkillType=InstantArea 단언 추가로 계약 고정.
- D3: 신규 스킬은 프리팹 빌더(TimeStopEffectBuilder.cs, MagicVFXBuilder 패턴)로 생성 — 209 시트 스프라이트를 AssetDatabase.LoadAllAssetsAtPath로 로드(이름 숫자 자연정렬 필수: _10 vs _2 주의), SpriteRenderer+SimpleSpriteAnimator+TimeStopEffect 구성, 227 어셋 Icon 설정. 손으로 fileID를 쓰지 않음(오류 위험 회피).
- D4: 227을 CanonicalPrefabLinks(13개)에 넣지 않음 → CanonicalSkillsWired/SkillPrefabStructure 안전. 별도 `InstantAreaSkillsWired` 테스트로 301+227 계약 검증.
- D5: 파이프라인 순서 고정: CSV+prefabMap 커밋 → `ImportSkillDataOnly`(227 어셋 생성) → 빌더(프리팹+Icon) → `LinkSkillPrefabs`(프리팹 링크). Todo 3~5 사이 EditMode 실패(루트 14→15)는 예정된 중간 상태.

## Scope IN
- PlayerController.cs InstantArea 케이스 구현 (Assets/Script/player/PlayerController.cs:265-267)
- 301_TimeStop.asset SkillType: 3 (한 줄)
- magicskill.csv +1행(227), DataImportMenu.prefabMap +1, TimeStopEffectBuilder.cs 신규, TimeWarp_Effect.prefab 신규(+.meta), 227_TimeWarp.asset(+.meta)
- SkillDataIntegrityTests 갱신(14→15, TimeStopUntouched+SkillType, InstantAreaSkillsWired), SkillExecutionTests +InstantArea 테스트(+TestBubbleAffectable 스턴 카운터)
- FILE_MAP.md 갱신, 검증 evidence .omo/evidence/

## Scope OUT (Must NOT have)
- TimeStop_Effect.prefab / TimeStopEffect.cs 수정 금지 (301 어셋 필드 한 줄만 허용)
- Player.prefab EquippedSkills / preset.csv / 다른 CSV 수정 금지 (PlayerEquipsGumMaster 보호)
- `ImportAll` 사용 금지 (`ImportSkillDataOnly`→`LinkSkillPrefabs`만)
- 새 효과 컴포넌트/데미지 시스템/애니메이터·파티클 도입 금지 (TimeStopEffect+SimpleSpriteAnimator 재사용)
- vfx .meta/임포트 설정 변경 금지, 480x480 시트/다른 Pipoya 이펙트 사용 금지
- 커밋 금지 (사용자 직접 커밋 — 프로젝트 관례), .omo/ 미커밋
- finish-skill-system-rework(FILE_MAP v1.3 전면 개편) 범위 흡수 금지 — 이번엔 증분 갱신만

## Open questions
없음 — 모든 포크는 위 announced defaults로 해결. 승인 게이트에서 veto 가능.

## Approval gate
status: approved
approved-at: 2026-08-05
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
- brief 제시함: 2026-08-05 (InstantArea 구현 + 301 SkillType 픽스 + 신규 227_TimeWarp, announced defaults 5건)
- 승인됨: 2026-08-05 (사용자: "승인 — 플랜 작성 진행") → 플랜 파일 작성 완료 → Momus 리뷰 → 실행 대기

## Execution resume log
- 2026-08-05: /start-work 실행 시작 (세션 ses_03065d9b8ffegK271dwv4sbzXA). Todo 1 ✅ (PlayerController InstantArea + 301 SkillType:3 + PlayMode 테스트, 증거 6건), Todo 2 ✅ (CSV 227행 + prefabMap + 227_TimeWarp.asset, 증거 3건) 검증 완료. Todo 3 위임 직후 세션 중단 — TimeStopEffectBuilder.cs(223줄) 작성 완료, TimeWarp_Effect.prefab 미생성, task-3 증거 없음. Boulder `timestop-style-magic-skills-0246db17` active 유지.
- 2026-08-06: 사용자가 "timestop 플랜 재개" 선택. 재개 지점 = Todo 3 (빌더 배치 실행 + 프리팹 생성 + 227 Icon) → Todo 4 (LinkSkillPrefabs + 무결성 스위트) → F1~F4. Unity 에디터 닫힘 확인 필요 (Temp/UnityLockfile 없음 — 2026-08-06 확인).

## Execution completion log
- 2026-08-06: **플랜 전체 완료** (세션 ses_02a4dfe77ffeIev6l5pP92RYW3, Prometheus 오케스트레이션 + Sisyphus-Junior 워커 실행).
  - Todo 3 ✅: `TimeStopEffectBuilder.BuildTimeWarpEffect` 배치 실행 → `TimeWarp_Effect.prefab`(+.meta, guid 6a0f50631e104e9458759451f1ef339f) 생성, `227_TimeWarp.asset` Icon = 209시트(guid 3014f866b966d1240bfb57efd1ac6ac0). 컴포넌트 검증 + 멱등 QA(GUID 보존) + 컴파일 error 0.
  - Todo 4 ✅: `LinkSkillPrefabs` → Linked=14, 227 `ProjectilePrefab` 연결. `SkillDataIntegrityTests.cs` 갱신 (CanonicalAssetNames 14개, SkillInventoryClean/SkillIdsUnique → 15, `TimeStopUntouched` + SkillType==InstantArea, 신규 `InstantAreaSkillsWired` :290). **EditMode 17/17, PlayMode 5/5 green** (QA 주입 A/B 각각 failed=1/2 → 복구 확인).
  - Final verification wave ✅: F1 플랜 준수, F2 코드 품질(minor 3건 비차단), F3 헤드리스 PlayMode 런타임 QA, F4 스코프 충실도(9/9) — 전부 APPROVE. 워크트리 diff = 계획된 10개 파일 정확히 일치, guardrail 전부 유지, 에이전트 커밋 0건.
  - 남은 사용자 단계 (F3의 수동 파트): 에디터를 열어 301 발동 + TimeWarp_Effect 초록 클락 애니메이션 눈으로 확인. 이후 사용자 직접 커밋 (플랜 Commit strategy의 4개 원자 커밋 그룹 참조).
