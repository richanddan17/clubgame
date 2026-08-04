---
slug: magic-skill-vfx
status: approved
intent: clear
review_required: false
pending-action: EXECUTING — user approved 2026-08-03 ("승인 — 바로 실행"). $start-work triggered. Executor builds per .omo/plans/magic-skill-vfx.md; user commits directly (project convention). Rework 97 files still uncommitted — user may commit rework first or after; VFX commits must stay separable.
approach: 코드 기반 3단계 스프라이트 애니메이션(SpriteVFXAnimator)으로 마법 스킬 6종에 VFX 연출 — 기존 221/222/223 업그레이드 + 신규 224/225/226 추가, CSV/임포터/프리팹 파이프라인 확장, 무결성 테스트 갱신.
---

# Draft: magic-skill-vfx

## Session 2026-08-03 — USER DECISIONS (interview, 3 questions answered)
- **범위**: "3개 + 새 스킬 추가" — 기존 221 FireBall / 222 IceBlast / 223 ThunderBolt 업그레이드 + Dark-Bolt(어둠), Holy(성스러움), Acid(산성) 신규 3종 추가.
- **재생 방식**: "코드 기반 스프라이트 애니메이션 (Recommended)" — Animator 클립/파티클 없음. C# 컴포넌트가 Sprite[] 프레임 배열을 순차 재생.
- **이펙트 단계**: "3단계 전부 (Recommended)" — 발사 Start → 이동 Repeatable 루프 → 히트 Hit/Ending. 팩 폴더 구조(Ice/Holy/Acid는 Start/Repeatable/Hit 폴더가 이미 분리됨)와 정확히 일치.

## Components (topology ledger)
- SpriteVFXAnimator 컴포넌트 | 3단계(start/loop/hit) 코드 스프라이트 애니메이션 | active | new Assets/Script/Combat/SpriteVFXAnimator.cs
- Projectile 임팩트 훅 | 히트 시 충돌 비활성 + 이동 정지 + 히트 애니메이션 후 지연 Deactivate | active | Assets/Script/Combat/Projectile.cs
- 마법 프리팹 6종 | 기존 3종 프레임 부여 + 신규 3종 생성 | active | Assets/Prefabs/Projectiles/
- MagicVFXBuilder 에디터 툴 | 프레임 배열 자동 부여 + 프리팹 생성/갱신(멱등) | active | new Assets/Editor/MagicVFXBuilder.cs
- 스킬 데이터 파이프라인 | magicskill.csv +3행, prefabMap +3, ImportSkillDataOnly+LinkSkillPrefabs | active | tiger/datafiles/skill/magicskill.csv, Assets/Editor/DataImportMenu.cs
- 테스트 | SpriteVFXAnimator 단위(EditMode) + 무결성 갱신(14루트/13캐노니컬) + PlayMode 임팩트 1종 | active | Assets/Tests/EditMode|PlayMode/

## Open assumptions (announced defaults — veto allowed at gate)
- 신규 스킬 ID 224/225/226 (기존 201-203, 211-214, 221-223, 301과 충돌 없음). | magic ID 대역 연장
- 신규 스킬 밸런스: 224 DarkBolt 35/25/3.0/None/16, 225 Holy 30/22/2.5/None/15, 226 Acid 25/20/2.0/None/12 (대미지/마나/쿨다운/버블/속도). 기존 마법 밴드(25-45/1.5-4.0) 안쪽. | reversible (CSV 값)
- 버블 효과: 신규 3종 전부 None (VFX 플랜은 연출 목적; 버블은 이후 밸런스 작업). | reversible
- 프리셋/장착 변경 없음: preset.csv·Player.prefab·EquippedSkills 그대로 (GumMaster 유지). 신규 스킬은 데이터에만 존재, 향후 "스킬 프리셋 시스템"에서 장착. | user 언급한 미래 기획
- 커밋: 실행자 안 함, 사용자 직접 커밋 (리워크와 동일 관례). 단, 리워크 97파일 미커밋 상태이므로 VFX 커밋 전 리워크 커밋 먼저 권고.
- 223 ThunderBolt: Start 없음(즉시 낙뢰), Loop=Lightning 11프레임, Hit=범용 Hit Effect 01 3프레임. | 시트에 비행 루프 없음(시각 확인)
- 226 Acid: Acid VFX 2(Repeatable 12 + Ending 6) 사용. 221 FireBall은 분리 프레임 15장 기준(시트 14 vs 분리 15 — 빌더가 자연 정렬로 대입, 테스트는 비어있지 않음만 검증).
- 제외 팩: VividMotion 포탈(ZIP 미압축), Brackeys(파티클 시트 — 코드 애니메이션 선택과 상충), Pipoya TimeMagic/HEXShield, Smoke/Smear/Wood (이번 범위 아님).

## Findings (cited - path:lines)
- 기존 발사체 프리팹 3종 동일 구조: Transform localScale 3 + SpriteRenderer sortingOrder 10 + CircleCollider2D isTrigger radius 0.2 + Projectile(speed 15, lifeTime 3). 정적 1프레임. (Assets/Prefabs/Projectiles/FireBallProjectile.prefab:22-36, 84, 96-131, 132-145; Projectile 스크립트 guid 748bc7fe4f5592044adef09a9696c5a8)
- Projectile.cs: OnTriggerEnter2D가 히트 즉시 Deactivate() 호출(72, 80) → 히트 애니메이션 재생 여지 없음. lifeTime 만료 시 Invoke(Deactivate) (Assets/Script/Combat/Projectile.cs:38-50, 57-82).
- magicskill.csv: 10열 스키마 확정 (ID,Name,Damage,ManaCost,Cooldown,Type,Bubble,Speed,MeleeRange,MeleeArc), 221 FireBall 30/15/1.5/None/18, 222 IceBlast 25/20/2.0/Red/14, 223 ThunderBolt 45/35/4.0/None/20. (tiger/datafiles/skill/magicskill.csv:1-4)
- DataImportMenu.prefabMap: 221→FireBallProjectile.prefab, 222→IceBlastProjectile.prefab, 223→ThunderBoltProjectile.prefab (Assets/Editor/DataImportMenu.cs:310-320); skillMagicPath=magicskill.csv(:173,:209); ImportSkillDataOnly 존재(리워크에서 추가, 배치모드 -executeMethod 사용 가능).
- VFX 시트 시각 분석(look_at): Fire-bomb 1x14 — Start 1-3(청색 에너지), Travel 4-7(청색 발사체), Hit 8-14(주황 폭발). Lightning 1x14 — Start 1-6(낙뢰), Hit 7-14(임팩트/스파크). Dark-Bolt 1x13 — Start 1-4(보라 번개 이동), Hit 5-13(다크 폭발). Dark VFX 2 1x14 — 지속형 연기(루프용). Holy VFX 02 1x13 — 광기둥. Acid VFX 01 1x12 — Start 1-8, Hit 9-12.
- 분리 프레임 파일(전부 개별 PNG, 슬라이스 불필요): sprites/FireBomb/Fire-bomb1..15.png, sprites/Lightning/Lightning1..11.png, sprites/DarkBolt/Dark-Bolt1..12.png, sprites/spark/spark1..8.png (Assets/Sprite/vfx/Magic Pack 9 files/Magic Pack 9 files/); Ice VFX 1/VFX 1 Start1..3, Repeatable1..10, Hit1..8 (Ice Effect 01/.../Ice VFX 1/Separated Frames/); Holy VFX 01 Initial1..2, Repeatable1..8, Impact1..7 (Holy VFX 01-02/.../Holy VFX 01/Separated Frames/); Acid VFX 2/Acid VFX 02Repeatable1..12, 02Ending1..6 (Acid VFX 01-02/.../Acid VFX 2/Separated Frames/); Hit Effect 01 1..3 (Hit Effect 01/Hit Effect 01/).
- 기존 무결성 테스트가 11루트/10캐노니컬 가정 → 신규 3종 추가 시 14루트/13캐노니컬로 갱신 필수. CanonicalAssetNames/CanonicalPrefabLinks/SkillInventoryClean(48), SkillIdsUnique(80) 수정 대상. (Assets/Tests/EditMode/SkillDataIntegrityTests.cs:18-38, 43-91)
- 프리팹 자동 생성 관례 존재: PrefabAutoCreator가 AssetDatabase로 스프라이트 로드→컴포넌트 부착→SaveAsPrefabAsset. (Assets/Editor/PrefabAutoCreator.cs:272-283)
- 리워크 상태: 97파일 미커밋(사용자 직접 커밋 예정), 테스트 EditMode 8/8 green 유지 중. 이 플랜은 리워크 결과물(CSV 10열, prefabMap, Combat 폴더) 위에 얹힘.

## Decisions (with rationale)
1. **SpriteVFXAnimator.cs 신규 컴포넌트** — 코드 기반 3단계 상태머신(Start→Loop 루프→PlayHit 시 Hit 1회). 프레임 배열 + fps + autoPlay + HitDuration 속성. Animator/파티클 금지. (user-confirmed 방식)
2. **Projectile 훅**: 히트 시 콜라이더 비활성 + speed=0 + CancelInvoke 후 `Invoke(Deactivate, HitDuration)` — 히트 연출 후 풀 복귀. 이벤트 구독 대신 Invoke 지연으로 풀링 안전. `_deactivated` 가드로 중복 Deactivate 방지. VFX 없으면 기존 즉시 Deactivate.
3. **3단계 프레임 매핑 테이블(확정)**: 221 FireBall=Start 1-3/Loop 4-7/Hit 8-15(분리15장), 222 IceBlast=VFX 1 Start1-3/Repeatable1-10/Hit1-8, 223 ThunderBolt=Start 없음/Loop Lightning1-11/Hit HitEffect1-3, 224 DarkBolt=Start 없음/Loop Dark-Bolt1-4/Hit Dark-Bolt5-12, 225 Holy=Initial1-2/Repeatable1-8/Impact1-7, 226 Acid=Start 없음/Repeatable1-12/Ending1-6.
4. **MagicVFXBuilder.cs 에디터 툴** — 분리 PNG를 숫자 자연 정렬로 Sprite 로드, 6개 프리팹 생성/갱신(멱등), MenuItem + static 배치모드 진입점. 수동 YAML guid 대입 대신 코드 생성(관례 일치, 신뢰성↑).
5. **CSV+prefabMap 확장** 후 ImportSkillDataOnly→LinkSkillPrefabs 배치 실행으로 224/225/226 에셋 생성·링크.
6. **테스트**: SpriteVFXAnimator EditMode 단위 테스트 신규, 무결성 스위트 11→14/10→13 갱신 + MagicVFXAnimatorWired 신규, PlayMode 임팩트 테스트 1종 추가.

## Scope IN
- SpriteVFXAnimator.cs (Combat asmdef), Projectile.cs 임팩트 훅(_vfx 캐시, HandleImpact, _deactivated 가드).
- 프리팹 6종: FireBall/IceBlast/ThunderBolt 프레임 부여 + DarkBolt/Holy/Acid 신규 생성 (scale 3, sortingOrder 10, 트리거 콜라이더 radius 0.2, speed 15, lifeTime 3 유지).
- MagicVFXBuilder.cs (프레임 로드 + 프리팹 생성/갱신, 자연 정렬).
- magicskill.csv 3행 추가, DataImportMenu.prefabMap 3건 추가, 배치 파이프라인 실행.
- 테스트: SpriteVFXAnimatorTests(EditMode), SkillDataIntegrityTests 갱신+MagicVFXAnimatorWired, SkillExecutionTests 임팩트 1종(PlayMode).
- FILE_MAP.md 갱신 (사용자 요구사항: 고치면 바로바로).
- .omo/evidence QA 산출물.

## Scope OUT (Must NOT have)
- NO Animator 컴포넌트 / AnimationClip / ParticleSystem (코드 애니메이션 선택).
- NO VFX 텍스처 .meta/임포트 설정 변경 (리슬라이싱 금지 — 분리 PNG만 사용, TimeStop이 쓰는 Ice VFX 2 포함 건드리지 않음).
- NO 301_TimeStop 관련 변경 (byte-identical 유지), TimeStop_Effect.prefab/TimeStopEffect.cs 터치 금지.
- NO preset.csv, Player.prefab, EquippedSkills, bubble 시스템, 적/대미지 코드 변경.
- NO BubbleProjectile_{blue,red,yellow}.prefab, MeleeHitbox, ObjectPooler, TryFire/UseSkill 로직 변경 (Projectile.cs 훅만).
- NO 제외 팩(VividMotion ZIP/Brackeys/Pipoya/Smoke/Smear/Wood) 임포트·사용.
- NO Assets 밖 편집 (magicskill.csv + FILE_MAP.md 제외).

## Open questions
- None blocking. (밸런스 숫자/버블 여부는 게이트에서 veto 가능한 기본값으로 기록됨.)

## Approval gate
status: awaiting-approval
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
- 대기 중인 행동: 사용자가 플랜 승인 또는 고정밀 리뷰 선택 → 승인 시 $start-work로 실행.
- 참고: 리워크 97파일 미커밋 상태 — VFX 실행 전 리워크 커밋을 먼저 권고 (사용자 커밋, 실행자 아님).
