using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float crouchScaleMultiplier = 0.5f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("버블껌 총 설정")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private List<SkillData> equippedSkills = new List<SkillData>();
    [SerializeField] private GameObject[] colorProjectilePrefabs; // 0:Blue, 1:Red, 2:Yellow
    
    private int currentSkillIndex = 0;
    private int currentColorIndex = 0;
    private float lastFireTime;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool facingRight = true;
    private Vector3 originalLocalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalLocalScale = transform.localScale;
        
        if (rb != null)
        {
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Update()
    {
        CheckGround();
        
        // 입력 보정
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.01f) moveInput.x = h;
        else if (moveInput.sqrMagnitude < 0.01f) moveInput.x = 0;

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) Jump();
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        isCrouching = Input.GetKey(KeyCode.S);

        // 색상 변경 (R 키)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            currentColorIndex = (currentColorIndex + 1) % 3;
            string colorName = currentColorIndex == 0 ? "파란색" : currentColorIndex == 1 ? "빨간색" : "노란색";
            Debug.Log($"총알 색상 변경: {colorName}");
        }

        // 발사 직접 감지 (마우스 왼쪽 클릭)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryFire();
        }

        ApplyFlip();
        ApplyCrouchScale();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
    }

    public void OnMove(InputValue value) 
    { 
        try { moveInput = value.Get<Vector2>(); }
        catch { moveInput = new Vector2(value.Get<float>(), 0); }
    }
    public void OnJump(InputValue value) { if (value.isPressed) Jump(); }
    public void OnRun(InputValue value) { isRunning = value.isPressed; }
    public void OnFire(InputValue value) { if (value.isPressed) TryFire(); }

    public void OnNextSkill(InputValue value)
    {
        if (value.isPressed && equippedSkills.Count > 0)
        {
            currentSkillIndex = (currentSkillIndex + 1) % equippedSkills.Count;
            Debug.Log($"현재 스킬 변경: {equippedSkills[currentSkillIndex].SkillName}");
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void TryFire()
    {
        if (equippedSkills.Count == 0) return;
        SkillData currentSkill = equippedSkills[currentSkillIndex];

        if (Time.time >= lastFireTime + currentSkill.Cooldown)
        {
            GameObject selectedPrefab = (colorProjectilePrefabs != null && colorProjectilePrefabs.Length > currentColorIndex) 
                                        ? colorProjectilePrefabs[currentColorIndex] 
                                        : currentSkill.ProjectilePrefab;

            if (selectedPrefab != null && firePoint != null)
            {
                GameObject projectileObj = Instantiate(selectedPrefab, firePoint.position, Quaternion.identity);
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(currentSkill.Damage, facingRight);
                }
            }
            lastFireTime = Time.time;
        }
    }

    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);
    }

    private void ApplyFlip()
    {
        if (moveInput.x > 0.1f && !facingRight) Flip();
        else if (moveInput.x < -0.1f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void ApplyCrouchScale()
    {
        Vector3 localScale = transform.localScale;
        localScale.y = isCrouching ? originalLocalScale.y * crouchScaleMultiplier : originalLocalScale.y;
        transform.localScale = localScale;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isRunning && Mathf.Abs(moveInput.x) > 0.1f);
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawSphere(groundCheck.position, 0.3f);
        }
    }
}
