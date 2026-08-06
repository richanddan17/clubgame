---
slug: brackeys-skills
status: approved
intent: unclear
review_required: true
pending-action: write .omo/plans/brackeys-skills.md (승인됨 2026-08-06 — Phase 3 진행: Metis 갭 분석 + 듀얼 고정밀 리뷰(momus+oracle) → 플랜 완성 → $start-work 대기)
approach: brackeys predrawn 시트(슬라이스 완료) 기반 신규 마법 스킬 8종(231-238, Projectile) 대량 추가 — 신규 BrackeysVFXBuilder(시트 서브스프라이트 자연정렬 로드 + 범위 클램프) + magicskill.csv 8행 + prefabMap 8건 + 무결성 테스트 갱신. 장착/프리셋 변경 없음(데이터만), 기존 221-226 파이프라인 무접촉.
---

# Draft: brackeys-skills

## Components (topology ledger)
<!-- id | outcome (one line) | status: active|deferred | evidence path -->
- C1 BrackeysVFXBuilder.cs + 프리팹 8종 | 시트 슬라이스 자연정렬 로드 → SpriteVFXAnimator 프리팹 생성/검증(멱등) | active | Assets/Editor/BrackeysVFXBuilder.cs + Assets/Prefabs/Projectiles/{8개}.prefab
- C2 데이터 파이프라인 | magicskill.csv +8행, DataImportMenu.prefabMap +8, ImportSkillDataOnly→LinkSkillPrefabs 배치 | active | tiger/datafiles/skill/magicskill.csv, Assets/Editor/DataImportMenu.cs
- C3 무결성 테스트 갱신 | CanonicalAssetNames/PrefabLinks +8, 카운트 15→23, BrackeysVFXAnimatorWired 신규 | active | Assets/Tests/EditMode/SkillDataIntegrityTests.cs
- C4 PlayMode 임팩트 테스트 1종 | 231 프리팹 히트→지연 비활성화 검증 (기존 패턴 미러) | active | Assets/Tests/PlayMode/SkillExecutionTests.cs
- C5 FILE_MAP.md 갱신 | 사용자 규칙("고치면 바로바로 파일맵") — 추가 기재만, finish-rework와 공유 | active | FILE_MAP.md

## Open assumptions (announced defaults)
<!-- assumption | adopted default | rationale | reversible? -->
- 신규 스킬 수 | 8종 (231-238) | "더더 많이"의 실용적 배치; 시트 13종 사용(loop 8 + hit 5), 미사용 1장(wavy_purple)은 후속 플랜에 | reversible (CSV 행만)
- 스킬 타입 | 전부 Projectile | 기존 Projectile+SpriteVFXAnimator 파이프라인 재사용, 런타임 코드 0줄 | reversible
- VFX 매핑 | Loop=고유 시트 전체 / Hit=폭발계열 시트 [First..Last] — 범위 클램프 설계로 미검증 카운트 안전 | reversible (빌더 테이블)
- 밸런스 | 기존 마법 밴드 내 (대미지 25-45, 마나 15-35, 쿨 1.5-4.0, 속도 12-20), 버블 None | 221-226과 일관 | reversible (CSV 값)
- 장착/프리셋 | 변경 없음 — Player.prefab·preset.csv·EquippedSkills 유지, 데이터만 추가 | 기존 관례 (magic-skill-vfx와 동일) | reversible
- 아이콘 | 미설정(null) 유지 | 기존 221-226과 동일 (임포터가 Icon 미설정) | reversible
- ID 대역 | 231-238 (221-227+301과 무충돌, 228-230은 여유분) | 기존 연속 대역 관례 | reversible
- 커밋 | 사용자 직접 (프로젝트 관례) — 실행자 스테이징/커밋 없음 | 모든 선행 플랜과 동일 | N/A

## Findings (cited - path:lines)
- brackeys predrawn 14시트 전부 Multiple-mode(spriteMode:2) 슬라이스, 서브스프라이트 명명 `{base}_{N}` (explosion_6x5.png.meta:4-97, charge_7x6.png.meta:4-59).
- 시트별 실제 프레임 수는 파일명 그리드(6x5/7x6)와 무관 — explosion_6x5=284, charge_7x6=256, fire_point_6x5=45, star_explosion_6x5=34 (grep count, `second: {base}_\d+`).
- flipbooks 14 TGA 전부 .meta 존재 (glob) — 슬라이스 상태이나 이번 범위 제외 후보.
- particles/opague 92 + alpha 93 단일 스프라이트 (이전 세션 검증) — 아이콘 후보, 이번 범위 제외.
- 기존 MagicVFXBuilder는 분리 PNG 폴더 자연정렬 로드 (MagicVFXBuilder.cs:266-318 LoadStage) — brackeys 단일텍스처 슬라이스와 구조 상이 → 신규 빌더 필요.
- 프리팹 템플릿: Transform localScale 3 + SpriteRenderer sortingOrder 10 + CircleCollider2D isTrigger radius 0.2 + Projectile speed 15 / lifeTime 3 (FireBallProjectile.prefab:22-36,84,96-131,132-145; BuildSkillPrefab MagicVFXBuilder.cs:179-248).
- Projectile 히트 훅: VFX 보유 시 콜라이더 비활성 + 이동 정지 + HitDuration 지연 Deactivate (Projectile.cs:59-74) — 신규 프리팹에 자동 적용.
- magicskill.csv 10열 스키마, 221-227 존재 (tiger/datafiles/skill/magicskill.csv:1-8). prefabMap 201-227 (DataImportMenu.cs:310-324). ImportSkillDataOnly(:294-300) / LinkSkillPrefabs(:303-364) 배치 진입점 존재.
- 무결성 테스트 현재: CanonicalAssetNames 14개(227 포함, SkillDataIntegrityTests.cs:18-25), CanonicalPrefabLinks 13(28-43), SkillInventoryClean=15(52-54), SkillIdsUnique≥15(85), MagicVFXAnimatorWired 221-226(139-177), InstantAreaSkillsWired 301/227(290-328).
- SkillType enum {Projectile, Melee, MeleeAoE, InstantArea} (SkillData.cs:3); SkillData 필드 17개.
- Unity 6000.3.12f1 → `D:\coding\6000.3.12f1\Editor\Unity.exe`; EditMode 17/17 + PlayMode 5/5 green 기준 (task-5 증거 — 16/16+4/4 아님).
- dirty worktree: **PixelArtRPGVFXLite 이동** (magic-skill-vfx·timestop 산출물은 커밋됨 — git-status.txt); finish-skill-system-rework awaiting-approval → FILE_MAP.md v1.3 전체 재작성 예정, brackeys 증분은 그 위에 병합.

## Decisions (with rationale)
1. **신규 `BrackeysVFXBuilder.cs`** — 기존 221-226 검증된 빌더 무접촉(회귀 0), 로더 방식이 근본적으로 다름(단일텍스처 슬라이스 vs 분리 PNG). 구조는 MagicVFXBuilder 미러: SkillSpec 테이블 + `BuildAndVerifyAllBrackeysVFX` 배치 진입점 + MenuItem + `VerifyAllBrackeysVFX`(실패 시 throw → batchmode exit non-zero).
2. **SheetStageSpec(SheetPath, Prefix, First?, Last?)** — 서브스프라이트를 이름 자연정렬 후 [First..Last] 슬라이스, 경계 초과 시 클램프+경고. 미검증 카운트 시트에 안전, Hit 지속시간 상한 보장(12fps 기준 2.5s=30프레임 등).
3. **8종 전부 Projectile** — 런타임 코드 추가 0, 최대 재사용. MeleeAoE/InstantArea는 신규 런타임 필요 → 범위 밖.
4. **ID 231-238, magicskill.csv 배치** — 데이터 파이프라인 기존 그대로 (ImportSkillDataOnly→LinkSkillPrefabs).
5. **무결성 테스트**: CanonicalAssetNames/PrefabLinks +8, SkillInventoryClean 15→23, SkillIdsUnique ≥15→≥23, `BrackeysVFXAnimatorWired` 신규 (231-238). 기존 MagicVFXAnimatorWired(221-226)는 유지.
6. **PlayMode 임팩트 테스트 1종 (231)** — magic-skill-vfx Todo-2 패턴 미러 (스킵 가능 프리팹 조건 포함).
7. **FILE_MAP.md 병합 기재** — finish-skill-system-rework v1.3이 전체 재작성하므로, brackeys 항목은 재작성 후 버전 위에 병합 (덮어쓰기 금지, 항목 보존).

## Scope IN
- `Assets/Editor/BrackeysVFXBuilder.cs` 신규 (SheetStageSpec 로더 + 8종 SkillSpec 테이블 + 빌드/검증 진입점).
- 프리팹 8종: `{FireOrb,FireRing,ElectricRing,Vortex,LightStreak,WavyBolt,Charge,BloodBolt}Projectile.prefab` — 템플릿 공통 (sortingOrder 10, 트리거 collider radius 0.2, speed 15, lifeTime 3, SpriteVFXAnimator) + **Transform localScale은 스킬별 계산 `(s_i, s_i, 1)`, `s_i = clamp(21 / maxFramePx_i, 0.05, 1.0)`** (FireBall 7×7px×scale 3=0.21 유닛과 최대 변 정합; brackeys 프레임은 FireBall 대비 4-20배 커서 blind scale 3 상속 금지 — Metis F1 반영).
- magicskill.csv 8행 (231-238), DataImportMenu.prefabMap 8건, 배치 파이프라인 실행 → SkillData 에셋 8개 생성·링크.
- 테스트: SkillDataIntegrityTests 갱신(14→22 캐노니컬, 13→21 링크, 15→23 카운트, BrackeysVFXAnimatorWired), SkillExecutionTests 임팩트 1종.
- FILE_MAP.md 갱신, .omo/evidence QA 산출물.

## Scope OUT (Must NOT have)
- NO 기존 221-226 파이프라인 변경: MagicVFXBuilder.cs·기존 프리팹 6종·기존 SkillData 에셋·기존 테스트 약화 (SkillDataIntegrityTests는 추가만, 기존 단정값 221-226/227/301 유지).
- NO 런타임 코드: SpriteVFXAnimator.cs·Projectile.cs·SkillData.cs·PlayerController.cs·MeleeHitbox.cs 무변경.
- NO 장착/프리셋: preset.csv·Player.prefab·EquippedSkills·SkillPresets 에셋 무변경.
- NO brackeys 텍스처 .meta/임포트 설정 변경 (리슬라이싱·재설정 금지), flipbooks/particles 미사용.
- NO 301/227 (TimeStop/TimeWarp) 관련 변경 — byte-identical.
- NO CSVs 외 tiger 데이터 변경; NO 제외 팩(VividMotion/Pipoya 등) 사용.
- NO Assets 밖 편집 (magicskill.csv + FILE_MAP.md 제외).
- NO 실행자 커밋 (사용자 직접).

## Open questions
- None blocking. (스킬 구성/밸런스/VFX 매핑은 게이트에서 veto 가능한 기본값으로 기록됨.)

## Approval gate
status: awaiting-approval
<!-- 사용자 승인 → Phase 3: Metis 갭 분석 + 듀얼 고정밀 리뷰(자동) → .omo/plans/brackeys-skills.md 완성 → $start-work -->
- 대기 중인 행동: 사용자가 아래 brief의 채택 기본값 검토 후 승인 (또는 특정 기본값 veto).
- 참고: finish-skill-system-rework이 awaiting-approval 상태 — v1.3이 FILE_MAP.md를 전체 재작성하므로 brackeys 항목은 재작성 후 버전 위에 **병합** (덮어쓰기 금지, "추가 기재만이라 충돌 없음" 주장은 철회 — Metis F5 반영).
