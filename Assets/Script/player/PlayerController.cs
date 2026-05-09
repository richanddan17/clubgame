using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 프로페셔널 버전의 플레이어 컨트롤러
/// - 관심사 분리 (이동/전투)
/// - 성능 최적화 (애니메이션 해시)
/// - Health 시스템 연동 및 리스폰 기능
/// - 마우스 방향 조준 발사 시스템
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
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
    private Collider2D _collider;
    private Health _health;

    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _isRunning;
    private bool _isCrouching;
    private bool _isFacingRight = true;
    private Vector3 _originalScale;
    private Vector3 _startPosition;

    private int _currentSkillIndex = 0;
    private int _currentColorIndex = 0;
    private float _lastFireTime;

    // 애니메이션 해시 (성능 최적화)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    #endregion

    #region Lifecycle Methods
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _health = GetComponent<Health>();
        _originalScale = transform.localScale;
        _startPosition = transform.position;

        SetupPhysics();
        
        if (_health != null)
        {
            _health.OnDie.AddListener(OnDeath);
        }
    }

    private void Update()
    {
        if (_health.IsDead) return;

        UpdateGroundStatus();
        HandleInput();
        ApplyRotationToMouse(); // 마우스 방향에 따른 캐릭터 회전
        ApplyCrouch();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (_health.IsDead) return;
        ApplyMovement();
    }
    #endregion

    #region Core Logic
    private void SetupPhysics()
    {
        if (_rb == null) return;
        _rb.gravityScale = 3.5f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (_collider != null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("Frictionless") { friction = 0, bounciness = 0 };
            _collider.sharedMaterial = mat;
        }
    }

    private void HandleInput()
    {
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
        if (_collider == null) return;
        Bounds b = _collider.bounds;
        Vector2 checkPos = new Vector2(b.center.x, b.min.y);
        _isGrounded = Physics2D.OverlapBox(checkPos, new Vector2(b.size.x * 0.9f, 0.2f), 0f, moveSettings.GroundLayer);
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
    }

    private void ApplyRotationToMouse()
    {
        // 마우스 월드 좌표 계산
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
        // 캐릭터와 마우스 사이의 방향 벡터
        float directionX = mousePos.x - transform.position.x;

        // 마우스 위치에 따른 좌우 반전
        if (directionX > 0.1f && !_isFacingRight) Flip();
        else if (directionX < -0.1f && _isFacingRight) Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        // 캐릭터를 180도 회전
        transform.Rotate(0f, 180f, 0f);
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
            // 마우스 방향으로 각도 계산
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;
            Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 총알 생성 시 계산된 각도 적용
            var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
            if (obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(skill.Damage, _isFacingRight);
            }
        }
        _lastFireTime = Time.time;
    }

    private void OnDeath()
    {
        _rb.linearVelocity = Vector2.zero;
        if (_animator != null) _animator.SetTrigger(AnimDie);
        Invoke(nameof(Respawn), 2f);
    }

    private void Respawn()
    {
        transform.position = _startPosition;
        _health.Initialize(_health.MaxHealth);
        _isFacingRight = true;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
    #endregion

    #region Helpers & Polish
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
        if (!_collider) _collider = GetComponent<Collider2D>();
        if (!_collider) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Bounds b = _collider.bounds;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.9f, 0.2f));
    }
    #endregion
}
