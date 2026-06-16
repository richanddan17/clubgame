using UnityEngine;

/// <summary>
/// 마법사 적 컨트롤러
/// - 원거리/근접 하이브리드 공격
/// - 슬로우/스턴 상태 이상 대응 추가
/// </summary>
public class RangedEnemy : MonoBehaviour
{
    [Header("기본 설정")]
    public EnemyData data;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("인스펙터 조절용")]
    public float speedMultiplier = 1f;
    public float damageMultiplier = 1f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _sr;
    private Health _health;
    private Transform _player;
    private bool _isDead = false;
    private float _nextAttackTime;
    private Vector3 _initialScale;

    // 상태 이상 타이머
    private float _slowTimer = 0f;
    private float _stunTimer = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _health = GetComponent<Health>();
        _initialScale = transform.localScale;
        
        if (firePoint == null) 
        {
            firePoint = transform;
        }
        if (_health != null) _health.OnDie.AddListener(Die);
    }

    private void FindPlayer()
    {
        // 'player'라는 이름을 가진 오브젝트를 가장 먼저 찾음
        GameObject playerObj = GameObject.Find("player");
        
        // 없으면 'Player' (대문자) 찾기
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    private void Start()
    {
        FindPlayer();

        if (data != null)
        {
            if (_health != null) _health.Initialize(data.HP);
        }
    }

    private void Update()
    {
        if (_isDead) return;
        if (_player == null) FindPlayer();

        // 타이머 감소
        if (_slowTimer > 0) _slowTimer -= Time.deltaTime;
        if (_stunTimer > 0) _stunTimer -= Time.deltaTime;

        // 시각적 피드백
        if (_sr != null)
        {
            if (_stunTimer > 0.01f) _sr.color = Color.gray;
            else if (_slowTimer > 0.01f) _sr.color = new Color(0.5f, 0f, 0.5f);
            else _sr.color = Color.white;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || _player == null || data == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        float attackRange = data.AttackRange;
        float detectionRange = data.DetectionRange;

        bool isFlying = _rb.gravityScale == 0;

        // 1. 비행 적 전용 타겟 위치 계산 (플레이어 머리 위 2.5m)
        Vector2 targetPos = _player.position;
        if (isFlying) targetPos.y += 2.5f; 

        // [디버그] 타겟 위치 시각화
        Debug.DrawLine(transform.position, targetPos, Color.yellow);

        // 2. 스턴 상태 처리
        if (_stunTimer > 0)
        {
            _rb.linearVelocity = isFlying ? Vector2.zero : new Vector2(0, _rb.linearVelocity.y);
            UpdateWalkAnimation(false);
            return;
        }

        // 3. 행동 로직
        if (distance <= attackRange) 
        {
            if (isFlying)
            {
                float yDiff = targetPos.y - transform.position.y;
                _rb.linearVelocity = new Vector2(0, yDiff * 5f);
            }
            else
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            }
            
            UpdateWalkAnimation(false);
            TryAttack();
        }
        else if (distance <= detectionRange)
        {
            Vector2 moveDir = (targetPos - (Vector2)transform.position).normalized;
            float currentSpeed = (_slowTimer > 0 ? data.Speed * 0.5f : data.Speed) * speedMultiplier;
            
            if (isFlying)
            {
                _rb.linearVelocity = moveDir * currentSpeed;
            }
            else
            {
                _rb.linearVelocity = new Vector2(moveDir.x * currentSpeed, _rb.linearVelocity.y);
            }

            UpdateWalkAnimation(true);
            ApplyFlip(moveDir.x);
        }
        else
        {
            if (isFlying)
            {
                 _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
            }
            else
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            }
            UpdateWalkAnimation(false);
        }

        // 4. 화면 밖 이탈 방지
        if (isFlying)
        {
            float clampedY = Mathf.Clamp(transform.position.y, _player.position.y - 2f, _player.position.y + 10f); // 범위를 조금 더 넓힘
            if (transform.position.y != clampedY)
            {
                transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
            }
        }
    }

    private void UpdateWalkAnimation(bool isWalking)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            if (Time.frameCount % 200 == 0 && _animator == null) Debug.LogError($"<color=red>[AnimDebug]</color> {name} Animator is MISSING!");
            return;
        }

        if (!_animator.isActiveAndEnabled)
        {
            if (Time.frameCount % 200 == 0) Debug.LogWarning($"<color=yellow>[AnimDebug]</color> {name} Animator is DISABLED!");
            return;
        }

        _animator.SetBool("Walk", isWalking);
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        _didShootThisAttack = false;

        Debug.Log($"<color=cyan>[AttackDebug]</color> {name} Attack Triggered!");

        float directionX = _player.position.x - transform.position.x;
        ApplyFlip(directionX);

        if (_animator != null && _animator.runtimeAnimatorController != null && _animator.isActiveAndEnabled)
        {
            _animator.SetTrigger("Attack");
            StartCoroutine(AutoShootFallback(0.3f));
            _nextAttackTime = Time.time + data.AttackInterval;
        }
        else
        {
            Shoot();
            _nextAttackTime = Time.time + data.AttackInterval;
        }
    }

    private bool _didShootThisAttack = false;

    private System.Collections.IEnumerator AutoShootFallback(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 투사체가 있을 때만 실행
        if (!_didShootThisAttack && projectilePrefab != null)
        {
            Shoot();
        }
    }

    public void ApplyEffect(Projectile.BubbleType type)
    {
        switch (type)
        {
            case Projectile.BubbleType.Red:
                _slowTimer = 3f;
                break;
            case Projectile.BubbleType.Yellow:
                _stunTimer = 1f;
                break;
        }
    }

    // 애니메이션 이벤트에서 호출할 함수
    [Header("근접 공격(휘두르기) 설정")]
    public float meleeDamage = 25f;
    public float meleeRange = 2.0f;
    public Vector2 meleeOffset = new Vector2(1.2f, 0.5f);

    public void MeleeSwing()
    {
        if (_isDead || _player == null) return;
        _didShootThisAttack = true; // 이벤트 실행됨 표시

        float side = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 checkPos = (Vector2)transform.position + new Vector2(side * meleeOffset.x, meleeOffset.y);
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPos, meleeRange);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                if (col.TryGetComponent<Health>(out var h))
                {
                    h.TakeDamage(meleeDamage * damageMultiplier, transform.position);
                }
            }
        }
    }

    public void Shoot()
    {
        if (_isDead || projectilePrefab == null || firePoint == null || _player == null) 
        {
            return;
        }

        if (_didShootThisAttack) return; 
        _didShootThisAttack = true;

        Vector2 direction = (_player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        
        // 총알 크기 강제 확대 (5, 5, 5)
        projectile.transform.localScale = new Vector3(5f, 5f, 5f);

        if (projectile.TryGetComponent<Projectile>(out var proj))
        {
            proj.Initialize(data.Damage * damageMultiplier, transform.localScale.x < 0, shooter: gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // 탱크일 때만 사각형으로 표시, 아니면 원형으로 표시
        bool isTank = name.Contains("CandyTankSlime");

        // 1. 탐지 범위 (하늘색)
        Gizmos.color = Color.cyan;
        if (isTank) Gizmos.DrawWireCube(transform.position, new Vector3(data.DetectionRange * 2, data.DetectionRange * 2, 1));
        else Gizmos.DrawWireSphere(transform.position, data.DetectionRange);

        // 2. 원거리 공격 사거리 (빨간색)
        Gizmos.color = Color.red;
        if (isTank) Gizmos.DrawWireCube(transform.position, new Vector3(data.AttackRange * 2, data.AttackRange * 2, 1));
        else Gizmos.DrawWireSphere(transform.position, data.AttackRange);

        // 3. 근접 공격 범위 (노란색)
        float side = transform.localScale.x > 0 ? 1f : -1f;
        Vector3 checkPos = transform.position + new Vector3(side * meleeOffset.x, meleeOffset.y, 0);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, meleeRange);
    }

    private void ApplyFlip(float x)
    {
        // x > 0 (플레이어가 오른쪽)일 때 -스케일, x < 0 (플레이어가 왼쪽)일 때 +스케일로 반전
        if (x > 0.1f) transform.localScale = new Vector3(-Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
        else if (x < -0.1f) transform.localScale = new Vector3(Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
    }

    public void ApplyStun(float duration)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
        Debug.Log($"<color=yellow>[RangedEnemy]</color> {name} STUNNED for {duration}s");
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        if (_animator != null && _animator.runtimeAnimatorController != null && _animator.isActiveAndEnabled) 
            _animator.SetTrigger("Die");
        Destroy(gameObject, 1.5f);
    }
}
