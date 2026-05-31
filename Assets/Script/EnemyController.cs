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
        
        // Health 이벤트 연결
        if (_health != null)
        {
            _health.OnDie.AddListener(Die);
        }
    }

    private void Start()
    {
        if (autoInitialize && data != null)
        {
            Initialize(data);
        }
    }

    private void Update()
    {
        if (_isDead) return;

        // 타이머 감소
        if (_slowTimer > 0) _slowTimer -= Time.deltaTime;
        if (_stunTimer > 0) _stunTimer -= Time.deltaTime;

        // 시각적 피드백 (상태에 따른 색상 변화)
        if (_sr != null)
        {
            if (_stunTimer > 0) _sr.color = Color.gray; // 스턴: 회색
            else if (_slowTimer > 0) _sr.color = new Color(0.5f, 0f, 0.5f); // 슬로우: 보라색
            else ApplyVisualSettings(); // 평소
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || _target == null || data == null) return;

        HandleMovement();
    }
    #endregion

    #region Initialization
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        _target = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Health 초기화 (데이터 기반)
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
    private void HandleMovement()
    {
        // 스턴 상태면 움직임 중지
        if (_stunTimer > 0)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateAnimation(false);
            return;
        }

        float distance = Vector2.Distance(transform.position, _target.position);

        if (distance <= data.DetectionRange)
        {
            Vector2 direction = (_target.position - transform.position).normalized;
            
            // 슬로우 상태면 속도 50% 감소
            float currentSpeed = _slowTimer > 0 ? data.Speed * 0.5f : data.Speed;

            if (distance > 0.5f)
            {
                _rb.linearVelocity = new Vector2(direction.x * currentSpeed, _rb.linearVelocity.y);
                UpdateAnimation(true);
            }
            else
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                UpdateAnimation(false);
            }
            ApplyFlip(direction.x);
        }
        else
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            UpdateAnimation(false);
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
