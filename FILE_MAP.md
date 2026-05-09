# clubgame File Map (Update: 2026-05-09)

이 문서는 프로젝트의 핵심 시스템과 파일 구조를 정의합니다. 오늘 추가된 프로급 시스템들이 반영되었습니다.

## 핵심 시스템 (Core Systems)

### 1. 체력 및 전투 (Health & Combat)
- `Assets/Script/Health.cs` (New)
  - **역할**: 모든 생명체(플레이어, 적)의 근본이 되는 모듈형 체력 시스템.
  - **기능**: 데미지/회복 처리, 사망 이벤트, 체력 변경 알림(Event) 제공.
- `Assets/Script/Projectile.cs` (Updated)
  - **역할**: 마우스 방향으로 날아가는 지능형 발사체.
  - **기능**: `Health` 컴포넌트 자동 감지 및 데미지 전달, 5프레임 애니메이션 적용.
- `Assets/Script/HealthBar.cs` (New)
  - **역할**: 시각적 피드백을 위한 UI 컨트롤러.
  - **기능**: HP 슬라이더 연동 및 숫자(TextMeshPro) 표시.

### 2. 플레이어 컨트롤 (Advanced Player)
- `Assets/Script/player/PlayerController.cs` (Updated: Pro Version)
  - **조준**: 마우스 커서 위치에 따른 캐릭터 자동 회전 및 360도 전방향 발사 시스템.
  - **리스폰**: 사망 시 시작 지점으로 자동 복귀 및 상태 초기화 기능.
  - **최적화**: Animator Hash 및 계층적 데이터 구조(Movement/Combat Settings) 적용.

### 3. 적 AI (Enemy AI)
- `Assets/Script/Slime.cs` (Updated)
  - **역할**: 추격 및 공격 지능형 슬라임.
  - **기능**: 2D 거리 기반 공격 판정, 8~15번 프레임 공격 애니메이션 적용, 씬 뷰 시각적 디버깅(보라색 선).

## 에디터 툴 (Advanced Editor Tools)

- `Assets/Editor/EmergencyFixer.cs` (Major Update)
  - **역할**: 프로젝트의 "만능 수리 도구".
  - **기능**: 태그(Player/Enemy) 자동 생성, UI 레이아웃(HP바 왼쪽 상단) 교정, 레이어 및 프리팹 연결 자동 복구.
- `Assets/Editor/SlimeAnimationHelper.cs` (New)
  - **역할**: 슬라임 전용 애니메이션 생성기.
  - **기능**: 특정 프레임(8~15)을 추출하여 공격 애니메이션 클립(`Slime_Attack.anim`) 자동 생성.

## 코딩 컨벤션 및 표준 (Pro Standard)

1. **관심사 분리**: 기능별로 스크립트를 쪼개어 재사용성을 높임 (예: 이동은 Controller, 생존은 Health).
2. **이벤트 기반 설계**: 체력이 변할 때 UI를 직접 부르지 않고 이벤트를 던져서 처리함.
3. **자동화**: 반복적인 세팅은 반드시 `EmergencyFixer`를 통해 클릭 한 번으로 해결하도록 함.
4. **성능 최적화**: 매 프레임 문자열 검색 지양, Animator Hash 사용 생활화.
5. **마찰력 제어**: 벽 붙기 현상을 방지하기 위해 코드로 `Frictionless` 재질을 자동 관리함.
