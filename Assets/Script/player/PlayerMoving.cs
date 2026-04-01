using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    /// <summary>
    /// 게임 시작 시 1번만 실행되는 초기 설정 함수
    /// </summary>

    [Header("이동 설정")]
    public float speed = 5f;
    public float jumpForce = 7f;

    private Rigidbody2D rb;
    private Animator animator;
    private float moveInput;
    private bool isGrounded;

    private static readonly KeyCode[] leftKeys  = { KeyCode.A, KeyCode.LeftArrow };
    private static readonly KeyCode[] rightKeys = { KeyCode.D, KeyCode.RightArrow };
    private static readonly KeyCode[] jumpKeys  = { KeyCode.W, KeyCode.Space };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 매 프레임 호출되는 업데이트 함수
    /// </summary>
    void Update()
    {
        moveInput = 0f;
        if (AnyKey(leftKeys))  moveInput = -1f;
        if (AnyKey(rightKeys)) moveInput =  1f;

        if (isGrounded && AnyKeyDown(jumpKeys))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // 애니메이션 파라미터 설정
        animator.SetFloat("Speed", Mathf.Abs(moveInput) * speed);
        animator.SetBool("Grounded", isGrounded);
        
        // 디버그
        Debug.Log("Speed: " + (Mathf.Abs(moveInput) * speed) + ", Grounded: " + isGrounded);
    }

    /// <summary>
    /// 물리 연산 전용 업데이트 함수
    /// </summary>
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
    }

    /// <summary>
    /// 땅 감지 - 콜라이더 태그가 Ground인 오브젝트에 닿으면 점프 가능
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }

    private bool AnyKey(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
            if (Input.GetKey(keys[i])) return true;
        return false;
    }

    private bool AnyKeyDown(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
            if (Input.GetKeyDown(keys[i])) return true;
        return false;
    }

}