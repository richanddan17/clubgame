using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Slime : MonoBehaviour
{
    public float speed = 2f; // 추적 속도
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 0.2f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private bool isGrounded;

    // 애니메이션 파라미터 해시값 (Mobs.cs 스타일)
    static int AnimatorWalk = Animator.StringToHash("Walk");
    static int AnimatorAttack = Animator.StringToHash("Attack");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("Player 태그가 설정된 오브젝트를 찾을 수 없습니다. Player 태그를 확인하세요.");
        }

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
        }

        // 지면 레이어 자동 설정 (없을 경우 Ground 레이어 사용)
        if (groundLayer == ~0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1) groundLayer = 1 << layer;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }

        UpdateGroundState();

        // 플레이어 방향으로 이동
        float diffX = player.position.x - transform.position.x;
        float directionX = diffX > 0 ? 1 : -1;
        
        // 너무 가까우면 멈춤 (공격 사거리 등)
        if (Mathf.Abs(diffX) > 0.5f)
        {
            rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, true);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(AnimatorWalk, false);
        }
        
        // 방향 전환 (Flip) - 부모의 scale 부호를 바꿈
        if (directionX > 0) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void UpdateGroundState()
    {
        Bounds bounds = col.bounds;
        float castDistance = bounds.extents.y + groundCheckDistance;

        Vector2 centerOrigin = new Vector2(bounds.center.x, bounds.min.y);
        isGrounded = Physics2D.Raycast(centerOrigin, Vector2.down, castDistance, groundLayer);

        Debug.DrawRay(centerOrigin, Vector2.down * castDistance, isGrounded ? Color.green : Color.red);
    }

    // 공격 애니메이션을 실행하고 싶을 때 외부에서 호출 가능
    public void PlayAttackAnimation()
    {
        if (anim != null) anim.SetTrigger(AnimatorAttack);
    }

    // 총알에 맞았을 때 호출되는 메서드
    public void TakeDamage(float amount)
    {
        Debug.Log("슬라임 처치!");
        Destroy(gameObject);
    }
}
