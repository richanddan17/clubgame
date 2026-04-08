using UnityEngine;

public class PlayerMovement : MonoBehaviour
    {
        /// <summary>
        /// 게임 시작 시 1번만 실행되는 초기 설정 함수
        /// </summary>


        [Header("이동 설정")]
        public float speed = 40f;
        public float jumpForce = 45f;
        
        public float Speed => speed;

        private Rigidbody2D rb;
        private float moveInput;
        private bool isGrounded;
        private bool isCrouching;
        private bool isRunningLeft;
        private bool isRunningRight;
        private bool crouchBlocked;
        private float leftHoldTime;
        private float rightHoldTime;

        public float runSpeed = 55f;
        public float crouchScale = 0.5f;

        public float MoveInput => moveInput;
        public bool IsGrounded => isGrounded;

    private static readonly KeyCode[] leftKeys  = { KeyCode.A, KeyCode.LeftArrow };
    private static readonly KeyCode[] rightKeys = { KeyCode.D, KeyCode.RightArrow };
    private static readonly KeyCode[] jumpKeys  = { KeyCode.W, KeyCode.Space };


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 매 프레임 호출되는 업데이트 함수
    /// </summary>
    void Update()
    {
        // 웅크리기
        bool crouchKeyHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.DownArrow);
        if (crouchBlocked)
        {
            if (!crouchKeyHeld)
                crouchBlocked = false;

            isCrouching = false;
            transform.localScale = Vector3.one;
        }
        else if (crouchKeyHeld)
        {
            isCrouching = true;
            transform.localScale = new Vector3(1, crouchScale, 1);
        }
        else
        {
            isCrouching = false;
            transform.localScale = Vector3.one;
        }

        // 이동
        moveInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveInput = -1f;
            leftHoldTime += Time.deltaTime;
            if (leftHoldTime > 0.2f) isRunningLeft = true;
        }
        else
        {
            leftHoldTime = 0f;
            isRunningLeft = false;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveInput = 1f;
            rightHoldTime += Time.deltaTime;
            if (rightHoldTime > 0.2f) isRunningRight = true;
        }
        else
        {
            rightHoldTime = 0f;
            isRunningRight = false;
        }

        // 점프
        if (isGrounded && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)))
        {
            isCrouching = false;
            crouchBlocked = true;
            transform.localScale = Vector3.one;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 디버그
        float currentSpeed = speed;
        if ((moveInput < 0 && isRunningLeft) || (moveInput > 0 && isRunningRight)) currentSpeed = runSpeed;
        if (isCrouching) currentSpeed *= 0.5f;
        Debug.Log("Speed: " + (Mathf.Abs(moveInput) * currentSpeed) + ", Grounded: " + isGrounded + ", Running: " + (isRunningLeft || isRunningRight) + ", Crouching: " + isCrouching);
    }

    /// <summary>
    /// 물리 연산 전용 업데이트 함수
    /// </summary>
    void FixedUpdate()
    {
        float currentSpeed = speed;
        if ((moveInput < 0 && isRunningLeft) || (moveInput > 0 && isRunningRight)) currentSpeed = runSpeed;
        if (isCrouching) currentSpeed *= 0.5f;
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
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
}