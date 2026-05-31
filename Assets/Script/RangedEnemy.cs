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
        
        if (firePoint == null) firePoint = transform;
        if (_health != null) _health.OnDie.AddListener(Die);
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (data != null && _health != null) _health.Initialize(data.HP);
    }

    private void Update()
    {
        if (_isDead) return;

        // 타이머 감소
        if (_slowTimer > 0) _slowTimer -= Time.deltaTime;
        if (_stunTimer > 0) _stunTimer -= Time.deltaTime;

        // 시각적 피드백
        if (_sr != null)
        {
            if (_stunTimer > 0) _sr.color = Color.gray;
            else if (_slowTimer > 0) _sr.color = new Color(0.5f, 0f, 0.5f);
            else _sr.color = Color.white;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || _player == null || data == null) return;

        // 스턴 상태면 행동 불가
        if (_stunTimer > 0)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null) 
                _animator.SetBool("Walk", false);
            return;
        }

        float distance = Vector2.Distance(transform.position, _player.position);

        // 1. 공격 사거리(5f) 안에 있으면 멈추고 공격
        if (distance <= 5f) 
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateWalkAnimation(false);
            TryAttack();
        }
        // 2. 공격 사거리 밖이지만 탐지 범위(data.DetectionRange) 안이면 추격
        else if (distance <= data.DetectionRange)
        {
            Vector2 direction = (_player.position - transform.position).normalized;
            
            // 슬로우 효과 적용
            float currentSpeed = _slowTimer > 0 ? data.Speed * 0.5f : data.Speed;
            
            _rb.linearVelocity = new Vector2(direction.x * currentSpeed, _rb.linearVelocity.y);
            UpdateWalkAnimation(true);
            ApplyFlip(direction.x);
        }
        // 3. 둘 다 아니면 정지
        else
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateWalkAnimation(false);
        }
    }

    private void UpdateWalkAnimation(bool isWalking)
    {
        // Animator가 초기화되지 않았거나 활성화되지 않았을 때의 에러 방지
        if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null)
        {
            _animator.SetBool("Walk", isWalking);
        }
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        // 플레이어 방향 보기
        float directionX = _player.position.x - transform.position.x;
        ApplyFlip(directionX);

        if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null)
        {
            _animator.SetTrigger("Attack");
            _nextAttackTime = Time.time + data.AttackInterval;
        }
    }

    public void ApplyEffect(Projectile.BubbleType type)
    {
        switch (type)
        {
            case Projectile.BubbleType.Red:
                _slowTimer = 3f;
                Debug.Log($"<color=red>[Wizard Effect]</color> SLOWED for 3s");
                break;
            case Projectile.BubbleType.Yellow:
                _stunTimer = 1f;
                Debug.Log($"<color=yellow>[Wizard Effect]</color> STUNNED for 1s");
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

        float side = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 checkPos = (Vector2)transform.position + new Vector2(side * meleeOffset.x, meleeOffset.y);
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPos, meleeRange);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                if (col.TryGetComponent<Health>(out var h))
                {
                    h.TakeDamage(meleeDamage);
                    Debug.Log($"<color=orange>[Wizard Melee]</color> 지팡이 휘두르기 적중! 데미지: {meleeDamage}");
                }
            }
        }
    }

    public void Shoot()
    {
        if (_isDead || projectilePrefab == null || firePoint == null || _player == null) return;

        Vector2 direction = (_player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        if (projectile.TryGetComponent<Projectile>(out var proj))
        {
            proj.Initialize(data.Damage, transform.localScale.x > 0, shooter: gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        float side = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 checkPos = (Vector2)transform.position + new Vector2(side * meleeOffset.x, meleeOffset.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, meleeRange);
    }

    private void ApplyFlip(float x)
    {
        if (x > 0.1f) transform.localScale = new Vector3(Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
        else if (x < -0.1f) transform.localScale = new Vector3(-Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        if (_animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger("Die");
        Destroy(gameObject, 1.5f);
    }
}
