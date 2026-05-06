using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMoving : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("웅크리기 설정")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private Vector2 crouchColliderSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, -0.5f);

    [Header("지면 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Vector2 defaultColliderSize;
    private Vector2 defaultColliderOffset;

    private Vector2 moveInput;
    private bool isGrounded;
    private bool isCrouching;
    private bool facingRight = true;

    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();

        if (capsuleCollider != null)
        {
            defaultColliderSize = capsuleCollider.size;
            defaultColliderOffset = capsuleCollider.offset;
        }
    }

    private void Update()
    {
        CheckGround();
        ApplyFlip();
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isCrouching", isCrouching);
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // Input System 메시지: Move 액션
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Input System 메시지: Jump 액션
    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    // Input System 메시지: Crouch 액션
    public void OnCrouch(InputValue value)
    {
        isCrouching = value.isPressed;
        UpdateCrouchState();
    }

    private void ApplyMovement()
    {
        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
    }

    private void ApplyFlip()
    {
        if (moveInput.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void UpdateCrouchState()
    {
        if (capsuleCollider == null) return;

        if (isCrouching)
        {
            capsuleCollider.size = crouchColliderSize;
            capsuleCollider.offset = crouchColliderOffset;
        }
        else
        {
            capsuleCollider.size = defaultColliderSize;
            capsuleCollider.offset = defaultColliderOffset;
        }
    }

    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // 에디터에서 지면 체크 범위를 시각적으로 확인
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
