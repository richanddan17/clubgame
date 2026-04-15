using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class scr : MonoBehaviour
{
    public float speed = 2f; // 추적 속도
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 0.2f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("Player 태그가 설정된 오브젝트를 찾을 수 없습니다. Player 태그를 확인하세요.");
        }

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
        }
    }

    void FixedUpdate()
    {
        if (player == null || rb == null || col == null)
            return;

        UpdateGroundState();

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
    }

    private void UpdateGroundState()
    {
        Bounds bounds = col.bounds;
        float castDistance = bounds.extents.y + groundCheckDistance;

        Vector2 leftOrigin = new Vector2(bounds.min.x + 0.05f, bounds.min.y);
        Vector2 centerOrigin = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - 0.05f, bounds.min.y);

        isGrounded = Physics2D.Raycast(leftOrigin, Vector2.down, castDistance, groundLayer)
            || Physics2D.Raycast(centerOrigin, Vector2.down, castDistance, groundLayer)
            || Physics2D.Raycast(rightOrigin, Vector2.down, castDistance, groundLayer);

        Debug.DrawRay(leftOrigin, Vector2.down * castDistance, isGrounded ? Color.green : Color.red);
        Debug.DrawRay(centerOrigin, Vector2.down * castDistance, isGrounded ? Color.green : Color.red);
        Debug.DrawRay(rightOrigin, Vector2.down * castDistance, isGrounded ? Color.green : Color.red);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }
}
