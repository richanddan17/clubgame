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
