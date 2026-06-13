using UnityEngine;

/// <summary>
/// 프로페셔널 버전의 적 컨트롤러
/// - 데이터 주입형 설계
/// - Health 컴포넌트와 연동
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class EnemyController : MonoBehaviour
{
    #region Serialized Fields
    [Header("적 설정")]
    [SerializeField] private EnemyData data;
    [SerializeField] private bool autoInitialize = false;
    #endregion

    #region Private Variables
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;
    private Health _health;
    private Transform _target;
    private bool _isDead = false;
    private Vector3 _initialScale;
    private float _nextAttackTime;

    // 상태 이상 타이머
    private float _slowTimer = 0f;
    private float _stunTimer = 0f;

    // 애니메이션 해시
    private static readonly int AnimWalk = Animator.StringToHash("Walk");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    #endregion

    #region Lifecycle Methods
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();

        SetupPhysics();
        _initialScale = transform.localScale;
        
        if (_rb != null) _rb.simulated = true;
        
        // Health 이벤트 연결
        if (_health != null)
        {
            _health.OnDie.AddListener(Die);
        }
    }

    private void Start()
    {
        FindPlayer();
        if (autoInitialize && data != null)
        {
            Initialize(data);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) 
        {
            _target = playerObj.transform;
            Debug.Log($"[{name}] Target set to Player at {_target.position}");
        }
    }

    private void Update()
    {
        if (_isDead) return;
        if (_target == null) FindPlayer();

        // 타이머 감소
        if (_slowTimer > 0) _slowTimer -= Time.deltaTime;
        if (_stunTimer > 0) _stunTimer -= Time.deltaTime;

        // 시각적 피드백
        if (_sr != null)
        {
            if (_stunTimer > 0) _sr.color = Color.gray;
            else if (_slowTimer > 0) _sr.color = new Color(0.5f, 0f, 0.5f);
            else ApplyVisualSettings();
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || _target == null || data == null) return;

        HandleMovementAndAttack();
    }
    #endregion

    #region Initialization
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        FindPlayer();

        // Health 초기화
        if (_health != null && data != null)
        {
            _health.Initialize(data.HP);
        }

        ApplyVisualSettings();
    }

    private void SetupPhysics()
    {
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // 레이어 설정
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;
    }

    private void ApplyVisualSettings()
    {
        if (_sr == null || data == null) return;
        if (data.EnemyName.Contains("Yellow")) _sr.color = Color.yellow;
        else if (data.EnemyName.Contains("Red")) _sr.color = Color.red;
        else _sr.color = Color.white;
    }
    #endregion

    #region Core Logic
    private void HandleMovementAndAttack()
    {
        if (data == null)
        {
            Debug.LogError($"[{name}] EnemyData is missing!");
            return;
        }

        if (_stunTimer > 0)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateAnimation(false);
            return;
        }

        float distance = Vector2.Distance(transform.position, _target.position);
        float detectionRange = data.DetectionRange > 0 ? data.DetectionRange : 30f;
        float attackRange = data.AttackRange > 0 ? data.AttackRange : 10f;

        // [Debug Log] 상태 실시간 파악
        // Debug.Log($"[{name}] Dist: {distance:F1}, Detection: {detectionRange}, Attack: {attackRange}");

        if (distance <= attackRange)
        {
            // 공격 범위 내
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateAnimation(false);
            TryMeleeAttack();
        }
        else if (distance <= detectionRange)
        {
            // 추격 범위 내
            Vector2 direction = (_target.position - transform.position).normalized;
            float currentSpeed = _slowTimer > 0 ? data.Speed * 0.5f : data.Speed;

            _rb.linearVelocity = new Vector2(direction.x * currentSpeed, _rb.linearVelocity.y);
            UpdateAnimation(true);
            ApplyFlip(direction.x);
            
            if (Time.frameCount % 60 == 0) // 너무 자주 찍히지 않게 60프레임마다 출력
                Debug.Log($"[{name}] Chasing player. Speed: {currentSpeed}");
        }
        else
        {
            // 범위를 벗어남
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateAnimation(false);
            
            if (Time.frameCount % 120 == 0)
                Debug.Log($"[{name}] Player too far ({distance:F1} > {detectionRange}). Idling.");
        }
    }

    private void TryMeleeAttack()
    {
        if (Time.time < _nextAttackTime) return;

        Health playerHealth = _target.GetComponent<Health>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(data.Damage, transform.position);
            
            if (_animator != null) _animator.SetTrigger(AnimAttack);
            _nextAttackTime = Time.time + (data.AttackInterval > 0 ? data.AttackInterval : 1.5f);
            
            Debug.Log($"[{name}] Attacked player! Damage: {data.Damage}");
        }
    }

    public void ApplyEffect(Projectile.BubbleType type)
    {
        switch (type)
        {
            case Projectile.BubbleType.Red:
                _slowTimer = 3f;
                Debug.Log($"<color=red>[Effect]</color> {gameObject.name} SLOWED for 3s");
                break;
            case Projectile.BubbleType.Yellow:
                _stunTimer = 1f;
                Debug.Log($"<color=yellow>[Effect]</color> {gameObject.name} STUNNED for 1s");
                break;
        }
    }

    private void ApplyFlip(float horizontalDirection)
    {
        if (horizontalDirection > 0.01f) transform.localScale = new Vector3(-_initialScale.x, _initialScale.y, _initialScale.z);
        else if (horizontalDirection < -0.01f) transform.localScale = new Vector3(_initialScale.x, _initialScale.y, _initialScale.z);
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (_animator == null) return;
        _animator.SetBool(AnimWalk, isMoving);
    }

    // 이제 Projectile이 Health.TakeDamage를 직접 호출하므로 이 메서드는 레거시 지원용
    public void TakeDamage(float amount)
    {
        if (_health != null) _health.TakeDamage(amount);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        
        if (_animator != null)
        {
            _animator.SetTrigger(AnimDie);
            Destroy(gameObject, 1.0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
