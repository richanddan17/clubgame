# level-gimmicks - Work Plan

## TL;DR (For humans)
<!-- Fill this LAST, after the detailed plan below is written, so it summarizes the REAL plan. -->
<!-- Plain English for a non-engineer: NO file paths, NO todo numbers, NO wave/agent/tool names. -->

**What you'll get:** 플랫포머 레벨에 들어갈 7가지 기믹 시스템: 이동식 플랫폼, 함정(가시/폭발), 스위치/문, 체크포인트/리스폰, 그래플링 훅(산나비 기계팔 스타일), 스프링/점프 패드, 보스 페이즈 전환 베이스 클래스. 각 기믹별 프리팹과 에디터 세팅 도구 포함.

**Why this approach:** 기존 PlayerController의 velocity 직접 세팅 방식과 충돌하지 않도록 parenting 기반으로 설계하고, 기믹마다 설정 가능한 ScriptableObject 데이터를 사용해서 레벨 디자이너가 유연하게 조절할 수 있게 했습니다.

**What it will NOT do:** 절차적 기믹 배치(WorldGenerator 연동), 다중 플레이어 기믹, save/load 시스템 연동, 사운드/음악 시스템.

**Effort:** Large
**Risk:** Medium** - 그래플링 훅의 물리 구현이 복잡할 수 있음

**Decisions to sanity-check:** 이동식 플랫폼 parenting 방식, 그래플링 훅 = DistanceJoint2D + LineRenderer, 체크포인트 = static 변수.

Your next move: 계획 승인 또는 하이어큐러시 리뷰 요청. 실행 상세는 아래에 기술.

---

> TL;DR (machine): Large effort, Medium risk - 7 level gimmick systems with prefabs and editor tools.

## Scope
### Must have
- 이동식 플랫폼 (위아래/좌우 이동, parenting 방식)
- 함정 시스템 (가시/폭발, 즉사/HP데미지 설정 가능)
- 스위치/문 (E키 인터랙션)
- 체크포인트/리스폰 시스템 (static respawnPoint)
- 그래플링 훅 (DistanceJoint2D + LineRenderer, 벽에 훅 쏘고 그네)
- 스프링/점프 패드 (접촉 시 위로 튕겨올림)
- 보스 페이즈 전환 베이스 클래스 (BossBase)
- 각 기믹용 프리팹
- 에디터 세팅 도구 (Editor Menu)

### Must NOT have (guardrails, anti-slop, scope boundaries)
- 절차적 기믹 배치 (WorldGenerator 연동)
- 다중 플레이어 기믹
- save/load 시스템 연동
- 애니메이션 시스템 (기믹 비주얼만)
- 사운드 시스템
- UI/HUD 개선
- 기존 PlayerController의 이동 로직 대규모 리팩토링

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after + Unity Test Runner (EditMode)
- Evidence: .omo/evidence/task-<N>-level-gimmicks.md

## Execution strategy
### Parallel execution waves
> Target 5-8 todos per wave. Fewer than 3 (except the final) means you under-split.

**Wave 1 (기반 시스템):** Task 1-2 (인프라 + 기반 클래스)
**Wave 2 (개별 기믹):** Task 3-8 (6개 기믹 구현, 병렬 가능)
**Wave 3 (통합):** Task 9 (프리팹 빌더 + 에디터 도구)
**Wave 4 (검증):** Task 10 (최종 테스트 + 검증)

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | - | 2-8 | - |
| 2 | 1 | 3-8 | - |
| 3 | 2 | 9 | 4,5,6,7,8 |
| 4 | 2 | 9 | 3,5,6,7,8 |
| 5 | 2 | 9 | 3,4,6,7,8 |
| 6 | 2 | 9 | 3,4,5,7,8 |
| 7 | 2 | 9 | 3,4,5,6,8 |
| 8 | 2 | 9 | 3,4,5,6,7 |
| 9 | 3-8 | 10 | - |
| 10 | 9 | - | - |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->

### Wave 1: 기반 시스템

- [ ] 1. GimmickBase 클래스 + GimmickData ScriptableObject 구현
  What to do / Must NOT do: 
  - `Assets/Script/Gimmick/GimmickBase.cs` 생성 (MonoBehaviour 베이스)
  - `Assets/Script/Gimmick/GimmickData.cs` 생성 (ScriptableObject)
  - GimmickBase: protected virtual void OnActivate(), OnDeactivate(), OnPlayerEnter(), OnPlayerExit()
  - GimmickData: string gimmickName, float activationDelay, bool isActiveByDefault
  - PlayerController.cs에 public bool isOnMovingPlatform 필드 추가
  - **Must NOT**: 기존 PlayerController의 이동 로직 변경
  
  Parallelization: Wave 1 | Blocked by: - | Blocks: 2-8
  References: PlayerController.cs:11-23 (MovementSettings), Health.cs:7-99 (Health 클래스 패턴)
  Acceptance criteria (agent-executable): GimmickBase.cs와 GimmickData.cs가 Assets/Script/Gimmick/ 폴더에 존재하고, 컴파일 에러 없음
  QA scenarios: Unity Editor에서 스크립트 컴파일 확인, Evidence .omo/evidence/task-1-level-gimmicks.md
  Commit: Y | feat(gimmick): add GimmickBase and GimmickData base classes

- [ ] 2. PlayerController 기믹 연동 포인트 추가
  What to do / Must NOT do:
  - PlayerController.cs에 기믹 시스템 연동 메서드 추가:
    - `public void SetOnMovingPlatform(Transform platform)` — 플랫폼에 parent 설정
    - `public void ClearMovingPlatform()` — parent 해제
    - `public void ApplyGrappleForce(Vector2 force)` — 그래플링 힘 적용
    - `public void ApplySpringForce(float force)` — 스프링 힘 적용
    - `public void SetRespawnPoint(Vector2 point)` — 리스폰 위치 갱신
    - `public Vector2 GetRespawnPoint()` — 리스폰 위치 반환
  - _startPosition을 static respawnPoint로 변경
  - isOnMovingPlatform 플래그로 플랫폼 이동 중かどうか 추적
  - **Must NOT**: 기존 이동 로직(걷기/달리기/점프) 변경
  
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 3-8
  References: PlayerController.cs:469-476 (Respawn 메서드), PlayerController.cs:120 (FixedUpdate)
  Acceptance criteria (agent-executable): PlayerController에 5개 새 메서드가 존재하고, Respawn()이 static respawnPoint를 사용
  QA scenarios: Unity Editor에서 PlayerController 컴파일 확인, Evidence .omo/evidence/task-2-level-gimmicks.md
  Commit: Y | feat(player): add gimmick integration points to PlayerController

### Wave 2: 개별 기믹 구현 (병렬 가능)

- [ ] 3. 이동식 플랫폼 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/MovingPlatform.cs` 생성
  - GimmickBase 상속
  - SerializeField: Transform[] waypoints, float speed, bool loop
  - OnTriggerEnter2D: Player 태그이면 SetOnMovingPlatform() 호출
  - OnTriggerExit2D: Player 태그이면 ClearMovingPlatform() 호출
  - Update: waypoint를 따라 이동 (Vector3.MoveTowards)
  - 부모-자식 관계를 이용한 이동 (플랫폼 자식으로 플레이어)
  - **Must NOT**: Rigidbody로 물리적 이동 시도
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: PlayerController.cs:120 (FixedUpdate 패턴), MovingPlatform 개념 (부모-자식)
  Acceptance criteria (agent-executable): MovingPlatform.cs가 존재하고, 플랫폼 위에 올라가면 같이 움직임
  QA scenarios: Unity Editor에서 테스트 씬에 MovingPlatform 배치, 플레이어 올려보기, Evidence .omo/evidence/task-3-level-gimmicks.md
  Commit: Y | feat(gimmick): implement MovingPlatform with parenting

- [ ] 4. 함정 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/Hazard.cs` 생성
  - GimmickBase 상속
  - SerializeField: float damage, bool isInstantKill, float damageInterval
  - OnTriggerEnter2D: Player 태그이면 데미지 적용
    - isInstantKill이면 Health.TakeDamage(9999)
    - 아니면 Health.TakeDamage(damage)
  - damageInterval로 연속 데미지 방지
  - **Must NOT**: 함정 자체가 이동하거나 애니메이션
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: Health.cs:50-78 (TakeDamage), MeleeHitbox.cs:28-48 (OnTriggerEnter2D 패턴)
  Acceptance criteria (agent-executable): Hazard.cs가 존재하고, isInstantKill에 따라 즉사/HP데미지 분기
  QA scenarios: Unity Editor에서 Hazard 프리팹 테스트, Evidence .omo/evidence/task-4-level-gimmicks.md
  Commit: Y | feat(gimmick): implement Hazard with configurable damage

- [ ] 5. 스위치/문 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/Switch.cs` 생성
  - GimmickBase 상속
  - SerializeField: Door linkedDoor, Sprite onSprite, Sprite offSprite
  - Update: E키 입력 감지 (Keyboard.current.eKey.wasPressedThisFrame)
  - 플레이어가 근처에 있고 E키 누르면 OnActivate() 호출
  - linkedDoor의 OnActivate() 호출
  - **Must NOT**: 스위치가 여러 개를 제어하는 로직 (1:1 매칭)
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: LevelPortal.cs:44 (E키 입력 패턴), BiomeZone.cs:7-16 (OnTriggerEnter2D 패턴)
  Acceptance criteria (agent-executable): Switch.cs와 Door.cs가 존재하고, E키로 문이 열림
  QA scenarios: Unity Editor에서 Switch + Door 세트 테스트, Evidence .omo/evidence/task-5-level-gimmicks.md
  Commit: Y | feat(gimmick): implement Switch and Door interaction

- [ ] 6. 체크포인트/리스폰 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/Checkpoint.cs` 생성
  - GimmickBase 상속
  - SerializeField: Sprite activatedSprite
  - OnTriggerEnter2D: Player 태그이면 PlayerController.SetRespawnPoint() 호출
  - activatedSprite로 변경 (시각적 피드백)
  - PlayerController.Respawn()이 static respawnPoint를 사용하도록 수정
  - **Must NOT**: 체크포인트가 씬 전환 시 영속되는 기능
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: PlayerController.cs:469-476 (Respawn 메서드), PlayerController.cs:49 (_startPosition)
  Acceptance criteria (agent-executable): Checkpoint.cs가 존재하고, 닿으면 리스폰 위치가 갱신됨
  QA scenarios: Unity Editor에서 Checkpoint 테스트, 죽었을 때 체크포인트에서 리스폰 확인, Evidence .omo/evidence/task-6-level-gimmicks.md
  Commit: Y | feat(gimmick): implement Checkpoint and Respawn system

- [ ] 7. 그래플링 훅 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/GrapplingHook.cs` 생성
  - GimmickBase 상속
  - SerializeField: float maxDistance, float grappleSpeed, float releaseForce, LayerMask grappleLayer
  - LineRenderer로 로프 시각화
  - 왼쪽 마우스 버튼으로 훅 발사 (Physics2D.Raycast)
  - 히트하면 DistanceJoint2D로 연결
  - 다시 누르면 해제 + 위쪽으로 힘 적용 (그네 타기)
  - 최대 거리 제한
  - **Must NOT**: 복잡한 로프 물리 시뮬레이션
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: PlayerController.cs (InputSystem 패턴), Health.cs (이벤트 패턴)
  Acceptance criteria (agent-executable): GrapplingHook.cs가 존재하고, 마우스로 벽에 훅하면 당겨지는 동작
  QA scenarios: Unity Editor에서 그래플링 훅 테스트, 벽에 훅하고 그네 타기 확인, Evidence .omo/evidence/task-7-level-gimmicks.md
  Commit: Y | feat(gimmick): implement GrapplingHook with DistanceJoint2D

- [ ] 8. 스프링/점프 패드 시스템 구현
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/SpringPad.cs` 생성
  - GimmickBase 상속
  - SerializeField: float springForce (기본 28, 점프력 14의 2배)
  - OnTriggerEnter2D: Player 태그이면 ApplySpringForce(springForce) 호출
  - **Must NOT**: 스프링이 자체적으로 애니메이션
  
  Parallelization: Wave 2 | Blocked by: 2 | Blocks: 9
  References: PlayerController.cs:403-407 (Jump 메서드), SpringPad 개념
  Acceptance criteria (agent-executable): SpringPad.cs가 존재하고, 닿으면 위로 튕겨올림
  QA scenarios: Unity Editor에서 SpringPad 테스트, 점프력 확인, Evidence .omo/evidence/task-8-level-gimmicks.md
  Commit: Y | feat(gimmick): implement SpringPad with configurable force

### Wave 3: 통합

- [ ] 9. 보스 페이즈 전환 베이스 클래스 + 프리팹 빌더
  What to do / Must NOT do:
  - `Assets/Script/Gimmick/BossBase.cs` 생성
  - MonoBehaviour 베이스 (GimmickBase 상속 안 함 - 보스는 별도 계층)
  - SerializeField: float maxHP, float[] phaseThresholds, BossPhase[] phases
  - BossPhase 클래스: float threshold, UnityEvent onPhaseStart
  - Health.OnDie 구독해서 HP% 계산
  - phaseThresholds에 따라 자동 페이즈 전환
  - `Assets/Editor/GimmickPrefabBuilder.cs` 생성
  - Custom Tools > Gimmicks > Build All Gimmick Prefabs 메뉴
  - 각 기믹별 프리팹 자동 생성
  - **Must NOT**: SugarOctopusBoss를 직접 리팩토링
  
  Parallelization: Wave 3 | Blocked by: 3-8 | Blocks: 10
  References: SugarOctopusBoss.cs:9-13 (BossState 패턴), MagicVFXBuilder.cs:100-124 (에디터 빌더 패턴)
  Acceptance criteria (agent-executable): BossBase.cs와 GimmickPrefabBuilder.cs가 존재하고, 보스 프리팹 생성 가능
  QA scenarios: Unity Editor에서 BossBase 컴파일 확인, 에디터 메뉴에서 프리팹 빌드 테스트, Evidence .omo/evidence/task-9-level-gimmicks.md
  Commit: Y | feat(boss): implement BossBase and GimmickPrefabBuilder

### Wave 4: 검증

- [ ] 10. 전체 통합 테스트 + 최종 검증
  What to do / Must NOT do:
  - 모든 기믹이 하나의 씬에서 동시 작동하는지 테스트
  - 기믹 간 상호작용 확인 (이동 플랫폼 + 함정, 체크포인트 + 리스폰 등)
  - 에디터 세팅 도구가 정상 작동하는지 확인
  - **Must NOT**: 기존 기능 회귀 유발
  
  Parallelization: Wave 4 | Blocked by: 9 | Blocks: -
  References: 모든 이전 Task 참조
  Acceptance criteria (agent-executable): 7개 기믹 모두 정상 작동, 기존 기능 유지
  QA scenarios: Unity Editor에서 전체 기믹 테스트 씬 구성 및 플레이 테스트, Evidence .omo/evidence/task-10-level-gimmicks.md
  Commit: Y | feat(gimmick): final integration test and validation

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit — 모든 기믹이 구현되었는지, 스코프 내인지 확인
- [ ] F2. Code quality review — 코드 스타일, 네이밍 컨벤션, 주석 확인
- [ ] F3. Real manual QA — Unity Editor에서 전체 기믹 플레이 테스트
- [ ] F4. Scope fidelity — Must NOT have가 준수되었는지 확인

## Commit strategy
- Task 1-2: 기본 클래스 + PlayerController 수정 (1 커밋)
- Task 3-8: 각 기믹별 독립 커밋 (6 커밋)
- Task 9: 보스 베이스 + 에디터 도구 (1 커밋)
- Task 10: 통합 테스트 (1 커밋)
- Final: 검증 완료 (1 커밋)

## Success criteria
1. 7개 기믹 모두 정상 작동
2. 기존 PlayerController 이동 로직 변경 없음
3. 에디터 세팅 도구로 프리팹 자동 생성 가능
4. 각 기믹이 독립적으로 설정 가능 (ScriptableObject)
5. 기존 테스트 통과 (SpriteVFXAnimatorTests 등)
