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
    private float _chargeStartTime;
    private bool _isCharging;

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

        // 마찰력이 0인 물리 재질 적용 (벽에 달라붙는 현상 방지)
        PhysicsMaterial2D mat = new PhysicsMaterial2D("Frictionless") { friction = 0, bounciness = 0 };
        if (_collider != null) _collider.sharedMaterial = mat;

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

        // 차징 로직
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && _isCharging)
        {
            float chargeDuration = Time.time - _chargeStartTime;
            TryFire(chargeDuration);
            _isCharging = false;
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = _isRunning ? moveSettings.RunSpeed : moveSettings.WalkSpeed;
        
        // 속도 우선순위: 차징+웅크리기(1.0) > 차징(2.0) > 웅크리기(4.0)
        if (_isCharging && _isCrouching)
        {
            targetSpeed = 1f;
        }
        else if (_isCharging)
        {
            targetSpeed = 2f;
        }
        else if (_isCrouching)
        {
            targetSpeed = 4f;
        }

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

    private void TryFire(float chargeTime = 0f)
    {
        // 웅크리기 중에는 공격 불가 (차징은 가능하지만 발사는 서서 해야 함)
        if (_isCrouching) return;
        
        float baseDamage = 10f;
        float cooldown = 0.2f;

        if (combatSettings.EquippedSkills.Count > 0)
        {
            var skill = combatSettings.EquippedSkills[_currentSkillIndex];
            baseDamage = skill.Damage;
            cooldown = skill.Cooldown;
        }

        if (Time.time < _lastFireTime + cooldown) return;

        // 차징 효과 계산 (최대 5초 차징 기준)
        float chargeRatio = Mathf.Clamp01(chargeTime / 5f);
        
        // 데미지: 기본 데미지 ~ 50
        float finalDamage = Mathf.Lerp(baseDamage, 50f, chargeRatio);
        
        // 크기: 2.0 ~ 1.0 (더 크게 상향)
        float finalScale = Mathf.Lerp(2.0f, 1.0f, chargeRatio);
        Vector3 projectileScale = new Vector3(finalScale, finalScale, 1f);

        // 속도: 15 (기본) ~ 8 (너무 느리지 않게)
        float finalSpeed = Mathf.Lerp(15f, 8f, chargeRatio);

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
<<<<<<< HEAD
                proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale);
=======
                proj.Initialize(damage, _isFacingRight, gameObject);
>>>>>>> origin/PBE
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
<<<<<<< HEAD
                if (obj.TryGetComponent<Projectile>(out var proj)) 
                    proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale);
=======
                if (obj.TryGetComponent<Projectile>(out var proj)) proj.Initialize(damage, _isFacingRight, gameObject);
>>>>>>> origin/PBE
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

        // 웅크리기 시 데미지 50% 절감 (Health 컴포넌트의 DamageMultiplier 활용)
        if (_health != null)
        {
            _health.DamageMultiplier = _isCrouching ? 0.5f : 1.0f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!_collider) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Bounds b = _collider.bounds;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f));
    }
}
