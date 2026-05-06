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

### 4.3 ShopItemData
- `ID`, `ItemName`, `Price`, `Description`

---

## 5. 작업 흐름 (Workflow)
1. **데이터 수정**: `tiger/datafiles/` 내부의 CSV 파일을 엑셀로 수정 후 저장.
2. **데이터 반영**: 유니티에서 `Open Import Window` 실행 후 `IMPORT ALL` 클릭.
3. **확인**: `Assets/Resources/` 폴더 내의 `.asset` 파일 수치가 변경되었는지 확인.
4. **사용**: 게임 로직에서 `Resources.Load<T>()`를 통해 데이터 호출.
