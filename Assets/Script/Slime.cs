using UnityEngine;

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
    private Health health;
    private bool isGrounded;
    private bool isDead = false;

    static int AnimatorWalk = Animator.StringToHash("Walk");
    static int AnimatorAttack = Animator.StringToHash("Attack");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
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

    void FixedUpdate()
    {
        if (isDead) return;
        if (player == null) { FindPlayer(); return; }

        UpdateGroundState();

        float distance = Vector2.Distance(transform.position, player.position);
        float diffX = player.position.x - transform.position.x;
        float directionX = diffX > 0 ? 1 : -1;

        Debug.DrawLine(transform.position, player.position, (distance <= attackRange) ? Color.magenta : Color.gray);
        
        if (distance <= attackRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, false);
            TryAttack();
        }
        else if (distance < 15f) 
        {
            rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
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

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            Debug.Log($"<color=cyan>[Combat]</color> {name}이(가) 공격을 시도합니다!");
            playerHealth.TakeDamage(attackDamage);
            PlayAttackAnimation();
            _nextAttackTime = Time.time + attackCooldown;
        }
        else if (playerHealth == null)
        {
            Debug.LogWarning($"<color=yellow>[Combat]</color> 플레이어에게 Health 컴포넌트가 없습니다!");
        }
    }

    public void PlayAttackAnimation()
    {
        if (anim != null) 
        {
            Debug.Log($"<color=magenta>[Animation]</color> {name}의 Attack 트리거를 작동시킵니다.");
            anim.SetTrigger(AnimatorAttack);
        }
        else
        {
            Debug.LogError($"<color=red>[Animation]</color> {name}의 Animator가 연결되지 않아 애니메이션을 재생할 수 없습니다!");
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
        Debug.Log($"<color=red>[Status]</color> {name} 처치됨!");
        Destroy(gameObject);
    }
}
