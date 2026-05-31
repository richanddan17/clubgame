using UnityEngine;

/// <summary>
/// 슬라임 전용 AI 컨트롤러
/// - 상태 이상(슬로우, 스턴) 대응 로직 추가
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class Slime : MonoBehaviour
{
    public float speed = 2f;
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 0.2f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private SpriteRenderer sr; // 색상 변화용
    private Health health;
    private bool isGrounded;
    private bool isDead = false;

    // 상태 이상 타이머
    private float _slowTimer = 0f;
    private float _stunTimer = 0f;

    static int AnimatorWalk = Animator.StringToHash("Walk");
    static int AnimatorAttack = Animator.StringToHash("Attack");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        health = GetComponent<Health>();
        FindPlayer();

        if (health != null)
        {
            health.OnDie.AddListener(Die);
        }

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
        }

        if (groundLayer == ~0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1) groundLayer = 1 << layer;
        }
        
        if (anim == null) Debug.LogError($"[{name}] Animator를 찾을 수 없습니다! 자식 오브젝트에 Animator가 있는지 확인하세요.");
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    [Header("공격 설정")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    private float _nextAttackTime;

    void Update()
    {
        if (isDead) return;

        // 타이머 감소
        if (_slowTimer > 0) _slowTimer -= Time.deltaTime;
        if (_stunTimer > 0) _stunTimer -= Time.deltaTime;

        // 시각적 피드백
        if (sr != null)
        {
            if (_stunTimer > 0) sr.color = Color.gray; // 스턴: 회색
            else if (_slowTimer > 0) sr.color = new Color(0.5f, 0f, 0.5f); // 슬로우: 보라색
            else sr.color = Color.white; // 평소
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (player == null) { FindPlayer(); return; }

        UpdateGroundState();

        // 스턴 상태면 움직임 중지
        if (_stunTimer > 0)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, false);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        float diffX = player.position.x - transform.position.x;
        float directionX = diffX > 0 ? 1 : -1;

        // 슬로우 상태면 속도 50% 감소
        float currentSpeed = _slowTimer > 0 ? speed * 0.5f : speed;

        if (distance <= attackRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, false);
            TryAttack();
        }
        else if (distance < 15f) 
        {
            rb.linearVelocity = new Vector2(directionX * currentSpeed, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, true);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, false);
        }
        
        if (directionX > 0) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    public void ApplyEffect(Projectile.BubbleType type)
    {
        switch (type)
        {
            case Projectile.BubbleType.Red:
                _slowTimer = 3f;
                Debug.Log($"<color=red>[Slime Effect]</color> SLOWED for 3s");
                break;
            case Projectile.BubbleType.Yellow:
                _stunTimer = 1f;
                Debug.Log($"<color=yellow>[Slime Effect]</color> STUNNED for 1s");
                break;
        }
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(attackDamage);
            PlayAttackAnimation();
            _nextAttackTime = Time.time + attackCooldown;
        }
    }

    public void PlayAttackAnimation()
    {
        if (anim != null) 
        {
            anim.SetTrigger(AnimatorAttack);
        }
    }

    private void UpdateGroundState()
    {
        if (col == null) return;
        Bounds bounds = col.bounds;
        float castDistance = bounds.extents.y + groundCheckDistance;
        Vector2 centerOrigin = new Vector2(bounds.center.x, bounds.min.y);
        isGrounded = Physics2D.Raycast(centerOrigin, Vector2.down, castDistance, groundLayer);
    }

    public void TakeDamage(float amount)
    {
        if (health != null) health.TakeDamage(amount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject);
    }
}
