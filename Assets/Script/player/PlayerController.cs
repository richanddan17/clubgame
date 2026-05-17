using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 최적화 및 기능이 개선된 플레이어 컨트롤러
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    #region Settings
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

    #region Fields
    [SerializeField] private MovementSettings moveSettings;
    [SerializeField] private CombatSettings combatSettings;

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

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    #endregion

    #region Lifecycle
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _health = GetComponent<Health>();
        _originalScale = transform.localScale;
        _startPosition = transform.position;

        SetupPhysics();
        
        if (combatSettings.FirePoint == null)
            combatSettings.FirePoint = transform.Find("FirePoint") ?? transform;

        if (_health != null)
            _health.OnDie.AddListener(OnDeath);
    }

    private void Update()
    {
        if (_health.IsDead) return;

        UpdateGroundStatus();
        HandleInput();
        HandleFacingDirection();
        ApplyCrouch();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (_health.IsDead) return;
        ApplyMovement();
    }
    #endregion

    #region Mechanics
    private void SetupPhysics()
    {
        _rb.gravityScale = 3.5f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (moveSettings.GroundLayer.value == 0)
            moveSettings.GroundLayer = LayerMask.GetMask("Ground");
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
        int layerMask = moveSettings.GroundLayer.value & ~(1 << gameObject.layer);
        Bounds b = _collider.bounds;
        Vector2 checkPos = new Vector2(b.center.x, b.min.y);
        _isGrounded = Physics2D.OverlapBox(checkPos, new Vector2(b.size.x * 0.8f, 0.1f), 0f, layerMask);
    }

    private void Jump()
    {
        if (_isGrounded)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, moveSettings.JumpForce);
    }

    private void CycleColor()
    {
        _currentColorIndex = (_currentColorIndex + 1) % 3;
    }

    private void HandleFacingDirection()
    {
        // 발사 직후 0.3초 동안은 방향 전환 유예
        if (Time.time < _lastFireTime + 0.3f) return;

        if (!Mouse.current.leftButton.isPressed && Mathf.Abs(_moveInput.x) > 0.1f)
        {
            if (_moveInput.x > 0 && !_isFacingRight) Flip();
            else if (_moveInput.x < 0 && _isFacingRight) Flip();
        }
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        transform.rotation = Quaternion.Euler(0, _isFacingRight ? 0 : 180, 0);
    }

    private void TryFire()
    {
        float damage = 10f;
        float cooldown = 0.2f;

        if (combatSettings.EquippedSkills.Count > 0)
        {
            var skill = combatSettings.EquippedSkills[_currentSkillIndex];
            damage = skill.Damage;
            cooldown = skill.Cooldown;
        }

        if (Time.time < _lastFireTime + cooldown) return;

        // 조준 방향으로 즉시 회전
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;
        float directionX = mousePos.x - transform.position.x;
        if (directionX > 0.1f && !_isFacingRight) Flip();
        else if (directionX < -0.1f && _isFacingRight) Flip();

        string[] tags = { "Blue", "Red", "Yellow" };
        string poolTag = tags[_currentColorIndex];

        if (ObjectPooler.Instance != null && combatSettings.FirePoint != null)
        {
            Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var obj = ObjectPooler.Instance.SpawnFromPool(poolTag, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
            if (obj != null && obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(damage, _isFacingRight);
            }
            _lastFireTime = Time.time;
        }
        else
        {
            // 풀러가 없으면 기존 방식(Instantiate)으로 폴백
            GameObject prefab = (combatSettings.ColorProjectilePrefabs != null && combatSettings.ColorProjectilePrefabs.Length > _currentColorIndex) 
                ? combatSettings.ColorProjectilePrefabs[_currentColorIndex] : null;

            if (prefab && combatSettings.FirePoint)
            {
                Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
                if (obj.TryGetComponent<Projectile>(out var proj)) proj.Initialize(damage, _isFacingRight);
                _lastFireTime = Time.time;
            }
        }
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
        _animator?.Rebind();
    }
    #endregion

    private void UpdateAnimations()
    {
        _animator.SetFloat(AnimSpeed, Mathf.Abs(_moveInput.x));
        _animator.SetBool(AnimIsGrounded, _isGrounded);
        _animator.SetBool(AnimIsRunning, _isRunning && Mathf.Abs(_moveInput.x) > 0.1f);
    }

    private void ApplyCrouch()
    {
        float targetY = _isCrouching ? _originalScale.y * moveSettings.CrouchScaleMultiplier : _originalScale.y;
        transform.localScale = new Vector3(_originalScale.x, targetY, _originalScale.z);
    }

    private void OnDrawGizmos()
    {
        if (!_collider) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Bounds b = _collider.bounds;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f));
    }
}
