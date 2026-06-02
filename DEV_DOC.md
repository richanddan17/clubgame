# ClubGame 개발 문서 (v1.1)

본 문서는 ClubGame 프로젝트의 주요 시스템 구조와 데이터 관리 방법을 설명합니다.

## 1. 프로젝트 환경
- **엔진**: Unity 6 (또는 최신 버전)
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **입력 시스템**: New Input System

---

## 2. 플레이어 시스템
### 2.1 플레이어 조작 (`PlayerMoving.cs`)
- **이동/점프**: Rigidbody2D 기반 물리 이동 및 지면 체크 점프.
- **특수 조작**: 웅크리기(C), 패링(F), 인벤토리(E).

### 2.2 전투 시스템 (`PlayerController.cs`)
- **3색 버블껌**: R키로 Blue, Red, Yellow 탄종 교체.
- **차징 모드**: Q키로 토글. 마우스 왼쪽 버튼 유지 시 투사체 크기 및 데미지 증가.
- **패링 (Parry)**: F키 입력 시 0.3초간 무적 및 반사 상태. 성공 시 공격 방향 반대쪽으로 넉백 발생.

### 2.3 인벤토리 시스템 (`InventoryManager.cs`, `InventoryUI.cs`)
- **토글**: E키 입력 시 게임 일시 정지(`Time.timeScale = 0`) 및 UI 표시.
- **관리**: ScriptableObject 기반의 스킬 및 아이템 데이터를 리스트로 관리.
- **자동화**: `Custom Tools > Setup Inventory UI`를 통해 UI 구조 자동 생성 및 연결.

---

## 3. 데이터 자동화 시스템 (Tiger Import Tool)
기획자가 엑셀(CSV)에서 편집한 데이터를 유니티 ScriptableObject 에셋으로 자동 변환하는 시스템입니다.
- **경로**: `Custom Tools > tiger > Data Import > Open Import Window`

---

## 4. 데이터 구조 (Data Structure)
### 4.1 EnemyData
- `ID`, `EnemyName`, `HP`, `Speed`, `Damage`, `DetectionRange`, `AttackInterval`

### 4.2 SkillData / ShopItemData
- `ID`, `Name`, `Damage/Price`, `Cooldown`, `Icon(Sprite)`

---

## 5. 적(Enemy) 시스템
### 5.1 주요 몬스터 AI
- **Slime**: 기본 추격 및 근접 공격.
- **RangedEnemy (Wizard)**: 원거리 마법 및 근접 지팡이 휘두르기 하이브리드 패턴.
- **Mole (두더지)**: 땅속 이동 후 플레이어 발밑에서 기습. 가시성 개선을 위해 `moleColor` 적용 가능.

---

## 6. 배경 시스템 (`ParallaxBackground.cs`)
- 카메라 이동 속도에 비례한 5단계 레이어 패럴랙스 무한 루프 구현.

---

## 7. 에디터 자동화 도구 (Custom Tools)
- `ShootingSetupHelper`: 플레이어 슈팅 환경 세팅.
- `WizardSetupHelper`: 마법사 적 프리팹 데이터 설정.
- `InventorySetupHelper`: 인벤토리 UI 생성 및 스크립트 연결.
- `HUDSetupHelper`: 플레이어 체력바 UI 생성 및 연결.

---

## 8. 향후 과제 및 개선 사항
- **UI**: 인벤토리 슬롯 레이아웃 최적화 (겹침 문제 해결 필요).
- **비주얼**: 두더지 스프라이트 밝기 조정 및 배경 가시성 확보.
