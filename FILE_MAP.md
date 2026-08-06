# ClubGame 프로젝트 파일 맵 (v1.2)

## 1. 루트 디렉토리
- `clubgame.slnx`: 비주얼 스튜디오 솔루션 파일
- `DEV_DOC.md`: 개발 상세 문서 (껌 공장 탐험 업데이트)
- `FILE_MAP.md`: 프로젝트 구조 안내 (본 파일)
- `README.md`: 프로젝트 개요

---

## 2. 주요 폴더 구조 (Assets)

### 2.1 Scripts (`Assets/Script/`)
- **Core & Systems**:
  - `PlayerMoving.cs`: 플레이어 물리 이동 및 점프
  - `PlayerController.cs`: 플레이어 전투, 슈팅, 패링 및 인벤토리 토글 로직
  - `BiomeData.cs`: 바이옴별 생성 규칙(노이즈, 타일), 배경, 몬스터 정보를 담는 데이터 (SO)
  - `BiomeManager.cs`: 현재 플레이어 위치(청크)에 따른 바이옴 상태 관리 및 전환
  - `WorldGenerator.cs`: (설계 중) 청크 기반 절차적 맵 생성 (Perlin Noise 동굴 생성)
  - `Projectile.cs`: 투사체 충돌 및 데미지 처리 + 임팩트 훅 (VFX 보유 시 이동 정지·콜라이더 비활성·PlayHit 후 HitDuration 지연 Deactivate, 멱등 가드)
  - `SpriteVFXAnimator.cs`: 마법 스킬 3단계(Start→Loop→Hit) 코드 기반 스프라이트 애니메이션 컴포넌트 (신규)
  - `InventoryManager.cs`: 스킬/아이템/단서(Clue) 목록 관리
  - `InventoryUI.cs`: 인벤토리 데이터의 시각적 표시 및 슬롯 관리
- **Exploration & Loot**:
  - `SpawnZone.cs`: 구역 기반 몬스터 스폰 및 리젠 시스템
  - `LootTable.cs`: 아이템 드랍 확률 정의 (ScriptableObject)
  - `LootDroppedItem.cs`: 필드에 드랍된 획득 가능한 아이템 오브젝트
  - `EnemyLootDropper.cs`: 적 사망 시 전리품 생성을 담당하는 컴포넌트
  - `Chest.cs`: 상호작용 가능한 보물상자 파밍 시스템
- **Environment**:
  - `ParallaxBackground.cs`: 바이옴별 자동 전환을 지원하는 패럴랙스 배경
  - `LevelPortal.cs`: 스테이지 이동 포탈
- **Enemy AI**:
  - `MeltingHaribo.cs`: 녹아내리는 하리보 기습 AI
  - `SugarOctopusBoss.cs`: 첫 번째 보스 '설탕 문어' 기초 로직
  - `EnemyController.cs`: 적 기본 추격 및 근접 공격 AI
  - `RangedEnemy.cs`: 마법사 등 원거리/근접 하이브리드 AI
- **Common**:
  - `Health.cs` / `HealthBar.cs`: 체력 시스템 및 UI
  - `ObjectPooler.cs`: 오브젝트 풀링 시스템
### 2.2 Editor Tools (`Assets/Editor/`)
- **컨벤션**: 모든 툴은 `EditorWindow`를 상속받으며 `Custom Tools` 메뉴를 통해 접근 가능해야 함.
- `PrefabAutoCreator.cs`: 스프라이트 기반 프리팹 일괄 생성 및 AI 자동 설정 도구 (신규)
- `DataImportMenu.cs`: Tiger 데이터 임포터 (CSV[몹, 스킬] -> ScriptableObject 자동 변환)
- `ShootingSetupHelper.cs`: 슈팅 환경 자동 세팅
- `WizardSetupHelper.cs`: 마법사 AI 설정 도구
- `InventorySetupHelper.cs`: 인벤토리 UI 구조 자동 연결 도구

### 2.3 Prefabs (`Assets/Prefabs/`)
- `Player.prefab`: 플레이어 캐릭터
- **Enemy/**: 적 프리팹 (PrefabAutoCreator로 생성)
  - `CandyTankSlime.prefab`: 박스 콜라이더(0.3, 0.13) 적용, 지상 추격형.
  - `PoppingCandyBat.prefab`: 비행형(중력 0), 플레이어 머리 위 2.5m 호버링 추격.
  - `MeltingHaribo.prefab`: 녹아내리는 하리보 적
- `BubbleProjectile_*.prefab`: 3색 버블껌 투사체 (Red, Yellow, Blue)

---

## 3. 현재 진행 상태 및 미해결 이슈 (2026-06-14)
### 진행 완료
- **PrefabAutoCreator 고도화**: 스프라이트 폴더만으로 애니메이터, 클립, 프리팹(물리/AI 포함)을 일괄 생성하는 시스템 구축.
- **비행 AI 구현**: 박쥐 적이 공중에서 플레이어를 추격하고 고도를 유지하는 로직 완성.
- **데이터 동기화**: `unit.csv`를 통해 모든 적의 능력치(속도, 사거리 등)를 통합 관리.

### 미해결 이슈 (내일 작업 예정)
- **Animator Controller 연결 유실**: 프리팹 생성 로그에는 '성공'으로 뜨지만, 실제 실행 시 `Animator is not playing an AnimatorController` 에러 발생.
  - **원인 가설**: 유니티 에셋 데이터베이스의 비동기 생성 속도 문제 또는 프리팹 저장(`SaveAsPrefabAsset`) 과정에서 참조가 유실됨.
  - **해결 방안**: 내일 프리팹 생성 후 `PrefabUtility.RecordPrefabInstancePropertyModifications`를 통한 강제 직렬화 및 씬 내 인스턴스 강제 교체 로직 점검 예정.

### 2.4 Data (`Assets/Resources/`)
- `EnemyData/`: 적 능력치 데이터
- `SkillData/`: 스킬 정보 데이터
- `ShopItemData/`: 상점 아이템 데이터
- `ClueData/`: 공장 비밀 단서 데이터 (신규)

---

## 3. 리소스 및 기타
- `Assets/Scenes/`: 메인 스테이지 및 로비 씬
- `Assets/Sprite/`: 캐릭터, 배경, UI 스프라이트 리소스 (하리보 이미지 포함)
- `Assets/Animation/`: 캐릭터 애니메이션 클립 및 컨트롤러

---

## 부록: Timestop 스타일 마법 스킬 작업 (2026-08-05, v1.3 증분)
- `Assets/Script/player/PlayerController.cs`: `SkillType.InstantArea` 케이스 구현 (프리팹 null 가드 → `transform.position`에 즉시 `Instantiate` → `spawned = true`). 기존 MeleeAoE 패턴 미러링.
- `Assets/Resources/SkillData/301_TimeStop.asset`: `SkillType: 3`(InstantArea) 필드 추가 — 이전엔 필드가 없어 enum 0(Projectile)으로 직렬화되어 스킬이 발사되지 않던 루트 원인 해결.
- `Assets/Tests/PlayMode/SkillExecutionTests.cs`: `InstantAreaSkill_SpawnsEffectAndStunsEnemy` 테스트 추가 (InstantArea 스킬이 TimeStop_Effect를 스폰하고 범위 내 적에게 스턴 1회 적용 검증). `TestBubbleAffectable`에 `StunCount` 카운터 추가, `CreateTimeStopSkill()` 헬퍼 추가.

- (Todo 2, 2026-08-05) Skill 227 TimeWarp added to data pipeline:
  - `tiger/datafiles/skill/magicskill.csv`: appended row `227,TimeWarp,0,30,10,InstantArea,None,0,0,0` (Type=InstantArea -> SkillType 3).
  - `Assets/Editor/DataImportMenu.cs`: prefabMap[227] = `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab` (linked in Todo 4).
  - Generated `Assets/Resources/SkillData/227_TimeWarp.asset` (+.meta) via DataImportMenu.ImportSkillDataOnly (SkillType 3, Damage 0, ManaCost 30, Cooldown 10, UseBubbleEffect 0).

- (Todo 3, 2026-08-06) TimeWarp_Effect prefab built + 227 icon set:
  - `Assets/Editor/TimeStopEffectBuilder.cs` (신규, 223줄): 209 시트(`pipo-btleffect209_192.png`, guid `3014f866b966d1240bfb57efd1ac6ac0`) 서브스프라이트 15개를 `int.Parse` 자연 정렬로 로드 → `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab` 생성 (Transform scale (5,5,1) / SpriteRenderer sortingOrder 20 / TimeStopEffect radius 15·stunDuration 3·lifeTime 1.5 / SimpleSpriteAnimator frames 15·fps 12·loop false) + 227 어셋 Icon 설정. 배치 진입점 `TimeStopEffectBuilder.BuildTimeWarpEffect()`.
  - `Assets/Prefabs/Projectiles/TimeWarp_Effect.prefab` (+.meta, guid `6a0f50631e104e9458759451f1ef339f`): 빌더가 자동 생성. 멱등 — 프리팹 삭제(meta 유지) 후 재실행 시 재생성 + GUID 유지 확인.
  - `Assets/Resources/SkillData/227_TimeWarp.asset`: `Icon` 설정 — 209 시트 guid `3014f866b966d1240bfb57efd1ac6ac0`의 첫 스프라이트(`fileID: 7241654760395862158`, LoadAssetAtPath<Sprite> 반환값) 참조.

- (Todo 4, 2026-08-06) LinkSkillPrefabs 배치 + 무결성 스위트 15-스킬 end-state:
  - `Assets/Resources/SkillData/227_TimeWarp.asset`: `ProjectilePrefab` 링크 — `{fileID: 5684612262389042782, guid: 6a0f50631e104e9458759451f1ef339f, type: 3}` (TimeWarp_Effect.prefab). `DataImportMenu.LinkSkillPrefabs` 배치 실행, 로그 `Linked=14` (13→14).
  - `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` (변경): `CanonicalAssetNames` +`"227_TimeWarp.asset"`(14개) / `SkillInventoryClean` 루트 14→15 / `SkillIdsUnique` 14→15 / `TimeStopUntouched` +SkillType==InstantArea 단언 / 신규 `InstantAreaSkillsWired` (301+227: 타입·프리팹 경로·TimeStopEffect 스크립트 존재 — MonoScript↔m_Script SerializedObject 비교). `CanonicalPrefabLinks`는 13개 유지(227 미등록).
-   증거: `task-4-timestop-style-magic-skills-link.log`, `task-4-timestop-style-magic-skills-compile.log`, `task-4-timestop-style-magic-skills.xml`(EditMode 17/17), `task-4-timestop-style-magic-skills-playmode.xml`(PlayMode 5/5), `task-4-qa-fail.xml`, `task-4-qa-fail-301.xml` (+ rerun XMLs).

---

## 부록: Brackeys 마법 스킬 8종 (2026-08-06, brackeys-skills 플랜)

- (Todo 1, 2026-08-06) BrackeysVFXBuilder + 프리팹 8종 (ID 231-238, 전부 Projectile):
  - `Assets/Editor/BrackeysVFXBuilder.cs` (신규, 507줄): Brackeys 단일텍스처 슬라이스 시트(13종)에서 `^{Prefix}_(\d+)$` 정규식 필터 + `int.Parse` 자연 정렬 + `[First..Last]` 0-based 슬라이스(경계 초과 클램프+경고)로 Loop/Hit 프레임을 로드해 투사체 프리팹을 생성하는 에디터 빌더. 스킬별 스케일 `s = clamp(21/maxFramePx, 0.05, 1.0)` (FireBall 7×7px × scale 3 = 0.21 유닛 정합 — blind scale 3 상속 금지), Loop = 처음 min(시트프레임, 60)프레임(5초@12fps 상한), Hit = [0..29], fps 12, startFrames 빈 배열, F8 시각 연속성 게이트(자연정렬+연속+첫≠마지막, 위반 시 throw). 멱등 — 기존 프리팹 제자리 갱신(GUID 유지). 배치 진입점 `BrackeysVFXBuilder.BuildAndVerifyAllBrackeysVFX`, 메뉴 `Custom Tools/tiger/Magic VFX/Build Brackeys VFX Prefabs`.
  - 프리팹 8종 `Assets/Prefabs/Projectiles/{FireOrb,FireRing,ElectricRing,Vortex,LightStreak,WavyBolt,Charge,BloodBolt}Projectile.prefab` (+.meta): SpriteRenderer sortingOrder 10 / CircleCollider2D isTrigger radius 0.2 / Projectile(speed 15, lifeTime 3 — 템플릿 기본값, 런타임 속도는 CSV ProjectileSpeed로 덮어씀) / SpriteVFXAnimator(loop=처음≤60프레임, hit=30, fps 12, autoPlay). 빌드+자체 검증 `8/8 prefabs OK` 로그 (증거: `.omo/evidence/task-1-brackeys-skills.log`).

- (Todo 2, 2026-08-06) magicskill.csv 8행 + prefabMap 8건 + ImportSkillDataOnly:
  - `tiger/datafiles/skill/magicskill.csv`: 227행 뒤에 8행 추가 `231,FireOrb,40,30,3.0,Projectile,None,15,0,0` … `238,BloodBolt,35,28,3.2,Projectile,None,15,0,0` (밸런스 밴드 내).
  - `Assets/Editor/DataImportMenu.cs` (변경): `prefabMap[231..238]` 8건 추가 (prefabMap 총 22건). `ImportSkillDataOnly` 배치 실행 — 로그 "Skill Data Import Complete!".
  - 에셋 8개 `Assets/Resources/SkillData/{231_FireOrb..238_BloodBolt}.asset` (+.meta) 생성 — 루트 23개. 필드 CSV 일치 검증됨 (SkillType 0=Projectile, UseBubbleEffect 0) (증거: `.omo/evidence/task-2-brackeys-skills.log`).

- (Todo 4, 2026-08-06) LinkSkillPrefabs 배치 (Linked=22):
  - `Assets/Resources/SkillData/{231..238}_*.asset`: `ProjectilePrefab` 링크 — guid 전수 교차 검증 완료 (프리팹 meta guid 8/8 일치). `DataImportMenu.LinkSkillPrefabs` 배치 실행, 로그 `Linked=22` (14→22) (증거: `.omo/evidence/task-4-brackeys-skills-link.log`).

- (Todo 3, 2026-08-06) PlayMode 임팩트 테스트 `BrackeysVFX_PlaysHitAndDelaysDeactivation`:
  - `Assets/Tests/PlayMode/SkillExecutionTests.cs` (변경): 231 스킬 발사 → 적 충돌 → 히트 VFX 재생 → 비활성화 지연 증명 (Passed 4.059s). 즉 F3 헤드리스 자동화 = 231 발사→적 충돌→히트 VFX→파괴 증명을 담당 (증거: `.omo/evidence/task-3-brackeys-skills-playmode.xml`, PlayMode 6/6).
  - F3의 사용자 수동 확인(에디터에서 8종 드래그해 Loop·Hit 애니메이션 눈 확인)은 게이트 결정에 포함되지 않고 후속 절차로 사용자 몫.

- (Todo 5, 2026-08-06) EditMode 무결성 스위트 확장 + 플레이크 수정 + QA(A)/(B) 실패 주입 검증:
  - `Assets/Tests/EditMode/SkillDataIntegrityTests.cs` (변경): `CanonicalAssetNames` 14→22 / `CanonicalPrefabLinks` 13→21 / `SkillInventoryClean` 15→23 / `SkillIdsUnique` 15→23, 신규 `BrackeysVFXAnimatorWired` (케이스 231-238: 루프=처음 min(시트,60) 프레임·hit=30·fps 12·startFrames 빈 배열·sortingOrder 10·스케일 캐노니컬).
  - `Assets/Tests/EditMode/SpriteVFXAnimatorTests.cs` (변경, 플레이크 수정): `ForceFrames()`에서 `SetField(animator, "_timer", frameTime - Time.deltaTime + 1e-6f)` — EditMode에서도 dt가 1/fps(≈83ms)를 넘으면 `Update()`의 `while (_timer >= frameTime)` 루프가 한 번에 여러 프레임을 진행해 `_frameIndex % N`이 어긋나는 비결정적 실패 제거. 수정 전 실패 이력 15건, 수정 후 `LoopOnlyWhenStartFramesEmpty` 연속 Passed.
  - QA(A) 실패 주입 (증거: `.omo/evidence/task-5-brackeys-skills-qa-fail.xml`): `CanonicalPrefabLinks[231]` → `WrongProjectile.prefab` 변조 → EditMode 17/18 — `CanonicalSkillsWired`만 실패(`ID 231 스킬의 ProjectilePrefab 경로가 캐노니컬 링크와 다릅니다`), `LoopOnlyWhenStartFramesEmpty` Passed. 이후 경로 복구.
  - QA(B) 실패 주입 (증거: `.omo/evidence/task-5-brackeys-skills-qa-fail-b.xml`): 빌더에서 231 hit 스테이지 제거 변조 → 재빌드 시 빌더 자체 Verify 실패(scale (0.13) != (0.07692308)) 확인, 231 prefab hitFrames=[] 재빌드 → EditMode 17/18 — `BrackeysVFXAnimatorWired`만 실패(`ID 231 프리팹의 hitFrames 가 30이 아닙니다. Expected: 30`). 이후 빌더 원복 + 재빌드 → `VerifyAllBrackeysVFX PASSED: 8/8`, 231 loop=45 hit=30 scale=(0.077,0.077,1) (증거: `.omo/evidence/task-5-brackeys-skills-qab-restore-build.log`).
  - 최종 green (증거: `.omo/evidence/task-5-brackeys-skills-editmode-final.xml` EditMode 18/18, `.omo/evidence/task-5-brackeys-skills-playmode-final.xml` PlayMode 6/6, 둘 다 EXIT=0).
