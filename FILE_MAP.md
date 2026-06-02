# ClubGame 프로젝트 파일 맵

## 1. 루트 디렉토리
- `clubgame.slnx`: 비주얼 스튜디오 솔루션 파일
- `DEV_DOC.md`: 개발 상세 문서
- `FILE_MAP.md`: 프로젝트 구조 안내 (본 파일)
- `README.md`: 프로젝트 개요

---

## 2. 주요 폴더 구조 (Assets)

### 2.1 Scripts (`Assets/Script/`)
- **Core & Systems**:
  - `PlayerMoving.cs`: 플레이어 물리 이동 및 점프
  - `PlayerController.cs`: 플레이어 전투, 슈팅, 패링 및 인벤토리 토글 로직
  - `Projectile.cs`: 투사체 충돌 및 데미지 처리
  - `InventoryManager.cs`: 스킬/아이템 목록 관리 (싱글톤)
  - `InventoryUI.cs`: 인벤토리 데이터의 시각적 표시 및 슬롯 관리
- **Environment**:
  - `ParallaxBackground.cs`: 5단계 패럴랙스 무한 배경
  - `LevelPortal.cs`: 스테이지 이동 포탈
- **Enemy AI**:
  - `EnemyController.cs`: 적 기본 추격 AI
  - `RangedEnemy.cs`: 마법사 등 원거리/근접 하이브리드 AI
  - `Slime.cs`: 슬라임 전용 로직
  - `Mole.cs`: 두더지 전용 잠행/기습 AI 및 가시성 개선용 색상 적용
  - `EnemySpawner.cs`: 적 생성 관리
- **Common**:
  - `Health.cs` / `HealthBar.cs`: 체력 시스템(패링 판정 포함) 및 UI
  - `ObjectPooler.cs`: 오브젝트 풀링 시스템

### 2.2 Editor Tools (`Assets/Editor/`)
- `DataImportMenu.cs`: Tiger 데이터 임포터 (CSV -> ScriptableObject)
- `ShootingSetupHelper.cs`: 슈팅 환경 자동 세팅
- `WizardSetupHelper.cs`: 마법사 AI 설정 도구
- `TilemapSetupHelper.cs`: 지면 타일맵 레이어 자동 구성
- `InventorySetupHelper.cs`: 인벤토리 UI 구조 및 스크립트 자동 연결 도구
- `HUDSetupHelper.cs`: 플레이어 HUD(체력바) 자동 설정 도구

### 2.3 Prefabs (`Assets/Prefabs/`)
- `Player.prefab`: 플레이어 캐릭터
- `Wizard2.prefab`: 마법사 적
- `Slime.prefab`: 슬라임 적
- `Mole.prefab`: 두더지 적
- `BubbleProjectile_*.prefab`: 3색 버블껌 투사체

### 2.4 Data (`Assets/Resources/`)
- `EnemyData/`: 적 능력치 데이터
- `SkillData/`: 스킬 정보 데이터 (Icon 필드 포함)
- `ShopItemData/`: 상점 아이템 데이터 (Icon 필드 포함)

### 2.5 External Data (`tiger/datafiles/`)
- 기획자가 편집하는 원본 CSV 데이터 파일들 (unit, skill, shop)

---

## 3. 리소스 및 기타
- `Assets/Scenes/`: 메인 스테이지 및 로비 씬
- `Assets/Sprite/`: 캐릭터, 배경, UI 스프라이트 리소스
- `Assets/Animation/`: 캐릭터 애니메이션 클립 및 컨트롤러
