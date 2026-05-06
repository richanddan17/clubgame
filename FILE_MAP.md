# clubgame File Map

이 문서는 `D:\coding\github c\clubgame` Unity 프로젝트에서 자주 건드릴 파일과 역할을 빠르게 찾기 위한 지도입니다.

## 프로젝트 루트

- `Assets/`
  - 실제 게임 코드, 프리팹, 리소스, 스프라이트가 들어있는 핵심 폴더입니다.
- `Packages/`
  - Unity 패키지 의존성 설정 폴더입니다.
- `ProjectSettings/`
  - Unity 프로젝트 설정 폴더입니다. 태그, 레이어, 입력/렌더링 설정 등이 들어갑니다.
- `tiger/datafiles/`
  - CSV 원본 데이터 폴더입니다. 에디터 툴이 이 데이터를 읽어서 `Assets/Resources`의 ScriptableObject asset을 만듭니다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`
  - Unity가 자동 생성/관리하는 폴더입니다. 일반 기능 구현 때는 보통 직접 수정하지 않습니다.

## 핵심 스크립트

### Player

- `Assets/Script/player/PlayerController.cs`
  - 현재 플레이어의 주 컨트롤러로 보입니다.
  - 이동, 달리기, 점프, 방향 전환, 애니메이션 파라미터, 스킬 발사 뼈대가 들어있습니다.
  - 최근 추가된 웅크리기 기능도 여기에 있습니다.
  - gumball 발사, 색 교체 같은 플레이어 입력 기능을 붙일 때 가장 먼저 볼 파일입니다.

- `Assets/Script/player/PlayerMoving.cs`
  - 별도/이전 버전 플레이어 이동 스크립트로 보입니다.
  - 이동, 점프, 웅크리기, 콜라이더 크기 변경 로직이 있습니다.
  - `DataImportMenu`와 `EmergencyFixer`에서 legacy로 제거하는 코드가 있어, 현재 메인 플레이어에는 안 쓰는 쪽일 가능성이 큽니다.

### Camera

- `Assets/Script/camera/Camerafollow.cs`
  - 실제 클래스 이름은 `CameraFollow`입니다.
  - 카메라가 `target`을 따라가도록 `LateUpdate`에서 위치를 보간합니다.

### Enemy

- `Assets/Script/EnemyController.cs`
  - 적 기본 컨트롤러입니다.
  - `EnemyData`를 받아 HP, 속도, 감지 거리 등을 세팅하고 플레이어를 추적합니다.
  - `TakeDamage()`와 `Die()`가 있습니다.

- `Assets/Script/EnemySpawner.cs`
  - 주기적으로 적을 생성합니다.
  - `Resources/EnemyData`의 데이터를 읽고, 가능한 경우 전용 프리팹을 찾아 생성합니다.

- `Assets/Script/EnemyMarker.cs`
  - 씬에 배치한 마커가 특정 enemy ID에 맞는 적을 생성한 뒤 자기 자신을 삭제합니다.
  - 에디터에서 적 배치용으로 쓰는 스크립트로 보입니다.

- `Assets/Script/Slime.cs`
  - 슬라임 전용 추적/애니메이션 스크립트입니다.
  - 플레이어 방향으로 이동하고 `Walk`, `Attack` 애니메이션 파라미터를 사용합니다.

### Data

- `Assets/Script/EnemyData.cs`
  - 적 데이터 ScriptableObject입니다.
  - ID, 이름, HP, 속도, 데미지, 감지 거리, 공격 간격을 가집니다.

- `Assets/Script/SkillData.cs`
  - 스킬 데이터 ScriptableObject입니다.
  - ID, 이름, 데미지, 마나 비용, 쿨다운을 가집니다.

- `Assets/Script/ShopItemData.cs`
  - 상점 아이템 데이터 ScriptableObject입니다.
  - ID, 이름, 가격, 설명을 가집니다.

## 에디터 툴

- `Assets/Editor/DataImportMenu.cs`
  - Unity 메뉴 `Custom Tools/tiger/...`에 기능을 추가합니다.
  - CSV 데이터 import, 적 마커 생성, 플레이어 애니메이션 세팅, 게임 씬 초기화 기능이 있습니다.
  - `tiger/datafiles`의 CSV를 읽어서 `Assets/Resources` asset을 만드는 역할이 있습니다.

- `Assets/Editor/EmergencyFixer.cs`
  - Unity 메뉴 `Custom Tools/tiger/EMERGENCY FIX ALL`에 긴급 복구 기능을 추가합니다.
  - Enemy 태그, Ground 레이어, Slime 프리팹, EnemySpawner 연결 등을 자동 세팅하려는 도구입니다.

## 프리팹

- `Assets/Prefabs/Player.prefab`
  - 플레이어 프리팹입니다. `FirePoint`가 연결되어 있고, 3색 발사체 프리팹 리스트를 가지고 있습니다.

- `Assets/Prefabs/BubbleProjectile_blue.prefab`, `_red.prefab`, `_yellow.prefab`
  - 색상별 버블껌 발사체 프리팹입니다. 각각 애니메이션이 포함되어 있습니다.

- `Assets/Prefabs/BaseEnemy.prefab`
  - 기본 적 프리팹입니다.
  - `EnemyMarker`가 기본 적을 생성할 때 사용합니다.

- `Assets/Prefabs/Slime.prefab`
  - 슬라임 적 프리팹입니다. 거대화 설정과 AI 스크립트가 적용되어 있습니다.

## 데이터 리소스

- `Assets/Resources/EnemyData/`
  - 적 데이터 asset들이 있습니다.
  - 예: `101_YellowSlime.asset`, `103_Bat.asset`, `104_Orc.asset`

- `Assets/Resources/SkillData/Melee/`
  - 근접 스킬 데이터 asset들이 있습니다.
  - 예: `201_Slash.asset`, `202_GreatSwing.asset`, `203_Stab.asset`

- `Assets/Resources/SkillData/Ranged/`
  - 원거리 스킬 데이터 asset들이 있습니다.
  - 예: `211_ArrowShot.asset`, `212_SniperShot.asset`, `213_TripleShot.asset`

- `Assets/Resources/SkillData/Magic/`
  - 마법 스킬 데이터 asset들이 있습니다.
  - 예: `221_FireBall.asset`, `222_IceBlast.asset`, `223_ThunderBolt.asset`

- `Assets/Resources/ShopItemData/`
  - 상점 아이템 데이터 asset들이 있습니다.
  - 예: `301_SmallHPotion.asset`, `302_LargeHPotion.asset`, `303_ManaPotion.asset`, `304_IronSword.asset`

## 입력과 애니메이션

- `Assets/InputSystem_Actions.inputactions`
  - Unity Input System 액션 설정 파일입니다.
  - `PlayerInput`이 이 파일을 사용할 수 있습니다.

- `Assets/Animation/Player/PlayerController.controller`
  - 플레이어 애니메이터 컨트롤러입니다.
  - `PlayerController.cs`는 `Speed`, `isGrounded`, `isRunning` 파라미터를 세팅합니다.

## gumball 관련 파일

- `Assets/Sprite/gumball blue.png`
  - 파란 gumball 스프라이트 시트입니다.
  - 내부 sprite 이름은 `gumball blue_0`부터 여러 개로 slice되어 있습니다.

- `Assets/Sprite/gumball red.png`
  - 빨간 gumball 스프라이트 시트입니다.
  - 내부 sprite 이름은 `gumball red_0`부터 여러 개로 slice되어 있습니다.

- `Assets/Sprite/gumball yellow.png`
  - 노란 gumball 스프라이트 시트입니다.
  - 내부 sprite 이름은 `gumball yellow_0`부터 여러 개로 slice되어 있습니다.

- `Assets/Sprite/gumball blue_0.controller`
- `Assets/Sprite/gumball red_0.controller`
- `Assets/Sprite/gumball yellow_0.controller`
  - gumball sprite import 과정에서 생성된 animator controller로 보입니다.

## 외부/샘플 에셋

- `Assets/Sprite/FreePixelMob/`
  - 슬라임 관련 외부/샘플 몬스터 에셋입니다.
  - `Mobs.cs`, `StateRandom.cs`, `SlimeA.png`, `Slime.controller` 등이 있습니다.

- `Assets/Sprite/Hero Knight - Pixel Art/`
  - 히어로 나이트 외부 에셋과 데모 코드/프리팹입니다.

- `Assets/Sprite/SPUM/`
  - SPUM 캐릭터 생성/샘플/리소스 패키지입니다.
  - 프로젝트 고유 로직보다 외부 패키지 성격이 강하므로 기능 구현 때는 필요한 경우만 봅니다.

- `Assets/Sprite/Evil Wizard 2/`, `Assets/Sprite/Evil Wizard 3/`, `Assets/Sprite/Martial Hero/`, `Assets/Sprite/Monsters Creatures Fantasy/`, `Assets/Sprite/karsiori/`, `Assets/Sprite/Pixel Skies DEMO/`
  - 캐릭터, 몬스터, 배경, 타일맵 등의 외부 에셋 폴더입니다.

## 기능 추가할 때 먼저 볼 곳

- 플레이어 이동/점프/달리기/웅크리기: `Assets/Script/player/PlayerController.cs`
- 플레이어 프리팹 연결 상태: `Assets/Prefabs/Player.prefab`
- gumball 이미지: `Assets/Sprite/gumball blue.png`, `gumball red.png`, `gumball yellow.png`
- 좌클릭 발사/색 변경 구현 예정 위치: `Assets/Script/player/PlayerController.cs`
- 발사 위치를 정확히 만들려면: `Player.prefab`의 `firePoint` 연결 필요
- 적 피격 처리: `Assets/Script/EnemyController.cs`
- 적 생성/스폰: `Assets/Script/EnemySpawner.cs`, `Assets/Script/EnemyMarker.cs`
- 데이터 import/자동 세팅: `Assets/Editor/DataImportMenu.cs`, `Assets/Editor/EmergencyFixer.cs`

## 주의할 점

- `Library/`, `Temp/`, `Logs/`는 Unity 자동 생성 폴더라 일반적으로 직접 수정하지 않습니다.
- `Assets/Sprite/SPUM/`, `Hero Knight - Pixel Art/` 같은 폴더는 외부 에셋/샘플 코드가 많습니다. 프로젝트 기능을 고칠 때는 먼저 `Assets/Script/`와 `Assets/Prefabs/`를 봅니다.
- 현재 일부 한글 주석/문자열이 깨져 보입니다. 기능 수정할 때 기존 주석을 대량으로 정리하지 말고, 필요한 변경 주변에만 새 주석을 추가하는 편이 안전합니다.
Sprite/SPUM/`, `Hero Knight - Pixel Art/` 같은 폴더는 외부 에셋/샘플 코드가 많습니다. 프로젝트 기능을 고칠 때는 먼저 `Assets/Script/`와 `Assets/Prefabs/`를 봅니다.
- 현재 일부 한글 주석/문자열이 깨져 보입니다. 기능 수정할 때 기존 주석을 대량으로 정리하지 말고, 필요한 변경 주변에만 새 주석을 추가하는 편이 안전합니다.
