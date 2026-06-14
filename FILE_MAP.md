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
  - `Projectile.cs`: 투사체 충돌 및 데미지 처리
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
  - `CandyTankSlime.prefab`: 캔디 탱크 슬라임 (신규)
  - `PoppingCandyBat.prefab`: 팝핑 캔디 박쥐 (신규)
  - `MeltingHaribo.prefab`: 녹아내리는 하리보 적
- `BubbleProjectile_*.prefab`: 3색 버블껌 투사체 (Red, Yellow, Blue)

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
