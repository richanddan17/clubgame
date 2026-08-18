---
slug: level-gimmicks
status: drafting
intent: clear
pending-action: write .omo/plans/level-gimmicks.md
approach: 플랫포머 레벨 기믹 시스템 구축 - 이동식 플랫폼, 함정, 스위치/문, 체크포인트, 그래플링 훅, 스프링, 보스 페이즈 전환
---

# Draft: level-gimmicks

## Components (topology ledger)
<!-- Lock the SHAPE before depth. One row per top-level component that can succeed or fail independently. -->

| id | outcome | status | evidence path |
|----|---------|--------|---------------|
| C1 | 이동식 플랫폼 시스템 - 플레이어가 올라타면 같이 움직이는 발판 | active | PlayerController.cs:11-23 (MovementSettings) |
| C2 | 함정 시스템 - 가시/폭발 등 데미지 오브젝트 (즉사/HP데미지 설정 가능) | active | Health.cs:50-78 (TakeDamage) |
| C3 | 스위치/문 시스템 - E키로 상호작용하는 인터랙티브 오브젝트 | active | LevelPortal.cs:44-46 (E키 입력 패턴) |
| C4 | 체크포인트/리스폰 시스템 - 현재 위치 저장, 사망 시 해당 지점 복귀 | active | PlayerController.cs:469-476 (Respawn 메서드) |
| C5 | 그래플링 훅 시스템 - 산나비 기계팔 스타일, 벽에 훅 쏘고 그네 타기 | active | PlayerController.cs (새 기믹) |
| C6 | 스프링/점프 패드 - 접촉 시 위로 튕겨올림 | active | PlayerController.cs:403-407 (Jump 메서드) |
| C7 | 보스 페이즈 전환 시스템 - 기믹 연동 가능한 보스 베이스 클래스 | active | SugarOctopusBoss.cs:9 (PhaseTransition enum) |

## Open assumptions (announced defaults)
<!-- Record any default you adopt instead of asking, so the user can veto it at the gate. -->

| assumption | adopted default | rationale | reversible? |
|------------|----------------|-----------|-------------|
| 이동식 플랫폼 물리 | Parenting 방식 | 가장 안정적이고 구현 쉬움, PlayerController 변경 최소화 | Yes |
| 함정 데미지 모델 | 설정 가능 (즉사/HP) | 레벨 디자인 유연성 확보 | Yes |
| 보스 시스템 | 새 베이스 클래스 설계 | 기믹과 연동되는 유연한 구조 필요 | Yes |
| 그래플링 훅 | 산나비 기계팔 스타일 | 사용자 요청사항 | No |
| 스위치 키 | E키 (기존 LevelPortal 패턴) | 일관성 유지 | Yes |
| 스프링 물리 | 점프력 × N배 (기본 2배) | 플랫포머 표준 | Yes |
| 체크포인트 영속성 | 씬 전환 시 리셋 | 구현 복잡도 최소화 | Yes |
| 테스트 전략 | tests-after (에디터 테스트) | Unity 프로젝트 특성상 | Yes |

## Findings (cited - path:lines)

### 기존 시스템 분석
1. **PlayerController.cs:14-23** — MovementSettings: WalkSpeed=6, RunSpeed=10, CrouchSpeed=3, JumpForce=14
2. **PlayerController.cs:403-407** — Jump(): _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, moveSettings.JumpForce)
3. **PlayerController.cs:469-476** — Respawn(): transform.position = _startPosition; _health.Initialize(_health.MaxHealth)
4. **Health.cs:50-78** — TakeDamage(): IsParrying 체크 후 데미지 적용, OnDie 이벤트 발생
5. **SugarOctopusBoss.cs:9** — BossState enum에 PhaseTransition 존재하나 미구현
6. **LevelPortal.cs:44** — Keyboard.current.eKey.wasPressedThisFrame 패턴
7. **WorldGenerator.cs:60-107** — 절차적 동굴 생성 (chunk 기반, Perlin 노이즈)

### 물리적 제약
1. **PlayerController.cs:120** — FixedUpdate에서 _rb.linearVelocity 직접 세팅
2. Moving platform은 이 방식과 충돌함 → parenting 방식으로 해결 필요

## Decisions (with rationale)

1. **Parenting 방식 채택**: PlayerController가 매 프레임 velocity를 세팅하므로, 플랫폼에 parent하면 로컬 좌표계가 자동으로 따라감. 물리 충돌 최소화.
2. **새 보스 베이스 클래스**: SugarOctopusBoss를 리팩토링하기보다, 기믹 연동이 가능한 새로운 BossBase 클래스를 만들어서 상속받는 구조로 설계.
3. **그래플링 훅 = DistanceJoint2D + LineRenderer**: 벽에 훅 쏘면 DistanceJoint2D로 연결, 당기기 힘 적용, 놓으면 해제. 산나비 기계팔 느낌.
4. **체크포인트 = static 변수**: PlayerController에 static Vector2 respawnPoint 추가, 체크포인트 오브젝트가 OnTriggerEnter2D에서 갱신.

## Scope IN
- 이동식 플랫폼 (위아래/좌우)
- 함정 (가시/폭발, 설정 가능)
- 스위치/문 (E키 인터랙션)
- 체크포인트/리스폰 시스템
- 그래플링 훅 (DistanceJoint2D + LineRenderer)
- 스프링/점프 패드
- 보스 페이즈 전환 베이스 클래스
- 각 기믹용 프리팹 + 에디터 세팅 도구

## Scope OUT (Must NOT have)
- 절차적 기믹 배치 (WorldGenerator 연동)
- 다중 플레이어 기믹
- save/load 시스템 연동
- 애니메이션 시스템 (기믹 비주얼만)
- 사운드 시스템
- UI/HUD 개선

## Open questions
- 없음 (사용자가 모든 포크 결정 완료)

## Approval gate
status: awaiting-approval
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
