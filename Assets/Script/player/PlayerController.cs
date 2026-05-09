using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 프로페셔널 버전의 플레이어 컨트롤러
/// - 관심사 분리 (이동/전투)
/// - 성능 최적화 (애니메이션 해시)
/// - 확장성 고려
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    #region Internal Classes & Enums
    [System.Serializable]
    public class MovementSettings
    {
        public float WalkSpeed = 6f;
        public float RunSpeed = 10f;
        public float CrouchSpeed = 3f;
        public float JumpForce = 14f;
        [Range(0, 1)] public float CrouchScaleMultiplier = 0.5f;
        public LayerMask GroundLayer;
    }

    [System.Serializable]
    public class CombatSettings
    {
        public Transform FirePoint;
        public List<SkillData> EquippedSkills = new List<SkillData>();
        public GameObject[] ColorProjectilePrefabs; // 0:Blue, 1:Red, 2:Yellow
    }
    #endregion

    #region Serialized Fields
    [Header("설정 데이터")]
    [SerializeField] private MovementSettings moveSettings;
    [SerializeField] private CombatSettings combatSettings;
    #endregion

    #region Private Variables
    private Rigidbody2D _rb;
    private Animator _animator;
    private CapsuleCollider2D _collider;

    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _isRunning;
    private bool _isCrouching;
    private bool _isFacingRight = true;
    private Vector3 _originalScale;

    private int _currentSkillIndex = 0;
    private int _currentColorIndex = 0;
    private float _lastFireTime;

    // 애니메이션 해시 (성능 최적화)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsRunning = Animator.StringToHash("isRunning");
    #endregion

    #region Lifecycle Methods
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider2D>();
        _originalScale = transform.localScale;

        SetupPhysics();
    }

    private void Update()
    {
        UpdateGroundStatus();
        HandleInput();
        ApplyFlip();
        ApplyCrouch();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }
    #endregion

    #region Core Logic
    private void SetupPhysics()
    {
        _rb.gravityScale = 3.5f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 마찰력 0 재질 적용
        PhysicsMaterial2D mat = new PhysicsMaterial2D("Frictionless") { friction = 0, bounciness = 0 };
        _collider.sharedMaterial = mat;
    }

    private void HandleInput()
    {
        // New Input System과 연동하거나 Legacy를 깔끔하게 래핑
        float h = Input.GetAxisRaw("Horizontal");
        _moveInput.x = Mathf.Abs(h) > 0.01f ? h : 0f;

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) Jump();
        
        _isRunning = Input.GetKey(KeyCode.LeftShift);
        _isCrouching = Input.GetKey(KeyCode.S);

        if (Keyboard.current.rKey.wasPressedThisFrame) CycleColor();
        if (Mouse.current.leftButton.wasPressedThisFrame) TryFire();
    }

    private void ApplyMovement()
    {
        float targetSpeed = _isCrouching ? moveSettings.CrouchSpeed : (_isRunning ? moveSettings.RunSpeed : moveSettings.WalkSpeed);
        _rb.linearVelocity = new Vector2(_moveInput.x * targetSpeed, _rb.linearVelocity.y);
    }

    private void UpdateGroundStatus()
    {
        Bounds b = _collider.bounds;
        Vector2 feetPos = new Vector2(b.center.x, b.min.y);
        // OverlapBox가 박스캐스트보다 가벼우며 판정이 관대함
        _isGrounded = Physics2D.OverlapBox(feetPos, new Vector2(b.size.x * 0.8f, 0.1f), 0f, moveSettings.GroundLayer);
    }

    private void Jump()
    {
        if (_isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, moveSettings.JumpForce);
        }
    }

    private void CycleColor()
    {
        _currentColorIndex = (_currentColorIndex + 1) % 3;
        // 프로는 UI 이벤트를 따로 던지지만, 여기서는 로그로 대체
    }

    private void TryFire()
    {
        if (combatSettings.EquippedSkills.Count == 0) return;
        
        SkillData skill = combatSettings.EquippedSkills[_currentSkillIndex];
        if (Time.time < _lastFireTime + skill.Cooldown) return;

        GameObject prefab = (combatSettings.ColorProjectilePrefabs.Length > _currentColorIndex) 
            ? combatSettings.ColorProjectilePrefabs[_currentColorIndex] 
            : skill.ProjectilePrefab;

        if (prefab && combatSettings.FirePoint)
        {
            var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.identity);
            if (obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(skill.Damage, _isFacingRight);
            }
        }
        _lastFireTime = Time.time;
    }
    #endregion

    #region Helpers & Polish
    private void ApplyFlip()
    {
        if ((_moveInput.x > 0 && !_isFacingRight) || (_moveInput.x < 0 && _isFacingRight))
        {
            _isFacingRight = !_isFacingRight;
            transform.Rotate(0, 180, 0); // Scale 조절보다 Rotation이 나중에 자식 오브젝트 관리하기 편함
        }
    }

    private void ApplyCrouch()
    {
        float targetY = _isCrouching ? _originalScale.y * moveSettings.CrouchScaleMultiplier : _originalScale.y;
        transform.localScale = new Vector3(_originalScale.x, targetY, _originalScale.z);
    }

    private void UpdateAnimations()
    {
        _animator.SetFloat(AnimSpeed, Mathf.Abs(_moveInput.x));
        _animator.SetBool(AnimIsGrounded, _isGrounded);
        _animator.SetBool(AnimIsRunning, _isRunning && Mathf.Abs(_moveInput.x) > 0.1f);
    }

    private void OnDrawGizmos()
    {
        if (!_collider) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Bounds b = _collider.bounds;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f));
    }
    #endregion
}
