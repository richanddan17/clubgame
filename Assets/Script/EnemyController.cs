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
    }
    #endregion

    #region Core Logic
    private void HandleMovement()
    {
        float distance = Vector2.Distance(transform.position, _target.position);

        if (distance <= data.DetectionRange)
        {
            Vector2 direction = (_target.position - transform.position).normalized;
            if (distance > 0.5f)
            {
                _rb.linearVelocity = new Vector2(direction.x * data.Speed, _rb.linearVelocity.y);
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

    private void ApplyFlip(float horizontalDirection)
    {
        if (horizontalDirection > 0.01f) transform.localScale = new Vector3(-1, 1, 1);
        else if (horizontalDirection < -0.01f) transform.localScale = new Vector3(1, 1, 1);
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
