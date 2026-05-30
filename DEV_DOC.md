# ClubGame 개발 문서 (v1.0)

본 문서는 ClubGame 프로젝트의 주요 시스템 구조와 데이터 관리 방법을 설명합니다.

## 1. 프로젝트 환경
- **엔진**: Unity 6 (또는 최신 버전)
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **입력 시스템**: New Input System
- **주요 경로**:
  - 스크립트: `Assets/Script/`
  - 에디터 툴: `Assets/Editor/`
  - 데이터 에셋: `Assets/Resources/`
  - 기획 데이터(CSV): `tiger/datafiles/`

---

## 2. 플레이어 시스템
### 2.1 플레이어 조작 (`PlayerMoving.cs`)
- **이동**: Rigidbody2D 기반 좌우 이동.
- **점프**: 지면 체크(GroundCheck)를 통한 물리 점프.
- **웅크리기**: C 키 입력 시 콜라이더 크기 및 오프셋 조정.
- **방향 전환**: 이동 방향에 따른 `transform.localScale.x` 반전.

---

## 3. 데이터 자동화 시스템 (Tiger Import Tool)
기획자가 엑셀(CSV)에서 편집한 데이터를 유니티 ScriptableObject 에셋으로 자동 변환하는 시스템입니다.

### 3.1 메뉴 경로
- `Custom Tools > tiger > Data Import > Open Import Window`

### 3.2 주요 기능
- **상태 표시**: `tiger/datafiles/` 경로 내 파일 존재 여부를 초록/빨강 표시등으로 시각화.
- **인라인 미리보기**: 유니티 에디터 내에서 CSV 파일의 텍스트 내용을 즉시 확인.
- **일괄 임포트**: `IMPORT ALL` 버튼으로 모든 카테고리 데이터를 한 번에 업데이트.

### 3.3 데이터 규격 및 경로
| 카테고리 | 파일 경로 | 에셋 저장 위치 |
| :--- | :--- | :--- |
| 유닛(Enemy) | `unit/unit.csv` | `Resources/EnemyData/` |
| 원거리 스킬 | `skill/rangedskill.csv` | `Resources/SkillData/Ranged/` |
| 근거리 스킬 | `skill/meleeskill.csv` | `Resources/SkillData/Melee/` |
| 마법 스킬 | `skill/magicskill.csv` | `Resources/SkillData/Magic/` |
| 상점 아이템 | `shop/shop.csv` | `Resources/ShopItemData/` |

---

## 4. 데이터 구조 (Data Structure)
### 4.1 EnemyData
- `ID`, `EnemyName`, `HP`, `Speed`, `Damage`, `DetectionRange`, `AttackInterval`

### 4.2 SkillData
- `ID`, `SkillName`, `Damage`, `ManaCost`, `Cooldown`

### ---

## 6. 전투 및 슈팅 시스템 (`Projectile.cs`, `PlayerController.cs`)
### 6.1 3색 버블껌 슈팅
- **전환**: `R` 키를 눌러 파랑 -> 빨강 -> 노랑 순으로 탄종 교체.
- **발사**: 마우스 조준 방향으로 발사. 발사 시 캐릭터가 조준 방향을 바라봄.
- **조준 보정**: 발사 직후 짧은 시간(0.3s) 동안 회전을 고정하여 조준 안정성 확보.
- **투사체 로직**:
  - `Owner` 태그 확인을 통한 자폭 방지.
  - 적 충돌 시 데미지 처리 및 소멸.

---

## 7. 배경 시스템 (`ParallaxBackground.cs`)
### 7.1 5단계 레이어 패럴랙스
- 카메라의 이동 속도에 비례하여 각 배경 레이어가 서로 다른 속도로 이동.
- **무한 루프**: 좌우 양방향으로 복제본을 생성하여 끊김 없는 무한 루핑 구현.

---

## 8. 적(Enemy) 시스템
### 8.1 기본 AI (`EnemyController.cs`, `Slime.cs`)
- 플레이어 감지 시 추격 및 기본 공격.
- 거대 슬라임: 총알 한 방에 처치되는 기믹 적용.

### 8.2 마법사 AI (`RangedEnemy.cs`)
- **하이브리드 패턴**: 원거리 마법 공격과 근접 지팡이 공격 병행.
- **애니메이션 이벤트**: 마법 구체가 생성되는 타이밍을 애니메이션 모션과 동기화.

---

## 9. 에디터 자동화 도구 (Custom Tools)
### 9.1 주요 헬퍼 클래스
- `ShootingSetupHelper.cs`: 플레이어 슈팅 프리팹 및 총구 설정 자동화.
- `WizardSetupHelper.cs`: 마법사 적 프리팹 및 데이터 자동 세팅.
- `TilemapSetupHelper.cs`: Ground 레이어 및 타일맵 환경 구축.
- `EnemyFixHelper.cs`: 기존 적 프리팹의 컴포넌트 누락 및 데이터 복구.
