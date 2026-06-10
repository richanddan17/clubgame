using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 최적화된 플레이어 컨트롤러 (Q키 토글 차징 모드 + 가변 속도/크기 적용)
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
        
        [Header("Damage Settings")]
        public float BaseDamage = 10f;
        public float MaxChargeDamage = 60f;
        public float MaxChargeTime = 5f;
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
    private bool _isChargeMode = false; // Q키 토글 상태

    // 특수 능력 및 쿨타임
    private float[] _abilityCooldowns = { 5f, 5f, 8f }; // Blue, Red, Yellow
    private float[] _lastUsedTime = { -10f, -10f, -10f };
    private float _speedBoostTimer = 0f;

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
        {
            _health.OnDie.AddListener(OnDeath);
            _health.OnParry.AddListener(ApplyParryKnockback);
        }
    }

    private float _knockbackTimer = 0f;

    private void ApplyParryKnockback(Vector2 direction)
    {
        _knockbackTimer = 0.2f; // 0.2초 동안 이동 입력 무시
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction * 15f, ForceMode2D.Impulse);
    }

    private void Update()
    {
        if (_health.IsDead) return;

        UpdateGroundStatus();
        HandleInput();
        HandleFacingDirection();
        ApplyCrouch();
        UpdateAnimations();

        // 타이머 감소
        if (_speedBoostTimer > 0) _speedBoostTimer -= Time.deltaTime;
        if (_knockbackTimer > 0) _knockbackTimer -= Time.deltaTime;
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

        PhysicsMaterial2D mat = new PhysicsMaterial2D("Frictionless") { friction = 0, bounciness = 0 };
        if (_collider != null) _collider.sharedMaterial = mat;

        if (moveSettings.GroundLayer.value == 0)
            moveSettings.GroundLayer = LayerMask.GetMask("Ground");
    }

    private float _parryCooldown = 1f;
    private float _lastParryTime = -10f;
    private float _parryDuration = 0.3f;

    [Header("Inventory Settings")]
    public GameObject inventoryPanel; // 인스펙터에서 할당 필요
    private bool _isInventoryOpen = false;

    private void HandleInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame) ToggleInventory();
        if (_isInventoryOpen) 
        {
            _moveInput = Vector2.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        _moveInput.x = Mathf.Abs(h) > 0.01f ? h : 0f;

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) Jump();
        
        _isRunning = Input.GetKey(KeyCode.LeftShift);
        _isCrouching = Input.GetKey(KeyCode.S);

        if (Keyboard.current.rKey.wasPressedThisFrame) CycleColor();

        // F키 패링
        if (Keyboard.current.fKey.wasPressedThisFrame) TryParry();

        // Q키 토글
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            _isChargeMode = !_isChargeMode;
            Debug.Log("<color=cyan>[Mode]</color> Charge Mode: " + (_isChargeMode ? "ON" : "OFF"));
        }

        // 슈팅 처리
        if (_isChargeMode)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _chargeStartTime = Time.time;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                float chargeTime = Time.time - _chargeStartTime;
                TryFire(chargeTime);
            }
        }
        else
        {
            if (Mouse.current.leftButton.isPressed)
            {
                TryFire();
            }
        }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("InventoryPanel");
                if (t != null) inventoryPanel = t.gameObject;
            }
        }

        if (inventoryPanel != null)
        {
            _isInventoryOpen = !_isInventoryOpen;
            inventoryPanel.SetActive(_isInventoryOpen);
            
            // 게임 일시 정지 / 재개
            Time.timeScale = _isInventoryOpen ? 0f : 1f;
            
            Cursor.visible = _isInventoryOpen;
            Cursor.lockState = _isInventoryOpen ? CursorLockMode.None : CursorLockMode.Confined;

            // UI 강제 갱신 시도
            if (_isInventoryOpen && inventoryPanel.TryGetComponent<InventoryUI>(out var invUI))
            {
                invUI.UpdateUI();
            }

            Debug.Log("<color=yellow>[Inventory]</color> " + (_isInventoryOpen ? "Opened (Paused)" : "Closed (Resumed)"));
        }
    }

    private void TryParry()
    {
        if (Time.time < _lastParryTime + _parryCooldown) return;
        
        StartCoroutine(ParryRoutine());
    }

    private System.Collections.IEnumerator ParryRoutine()
    {
        _lastParryTime = Time.time;
        if (_health != null) _health.IsParrying = true;

        // 비주얼 효과: 버블껌처럼 분홍색으로 변경
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = new Color(1f, 0.5f, 0.8f); // 분홍색

        yield return new WaitForSeconds(_parryDuration);

        if (_health != null) _health.IsParrying = false;
        sr.color = originalColor;
    }


    private void ApplyMovement()
    {
        if (_knockbackTimer > 0) return; // 밀려나는 중에는 이동 처리 안 함

        float targetSpeed = _isRunning ? moveSettings.RunSpeed : moveSettings.WalkSpeed;
        
        // 파랑 버블 특수 능력: 스피드 부스트
        if (_speedBoostTimer > 0) targetSpeed *= 1.5f;

        // [중요] 차징 모드이면서 실제로 마우스를 누르고 있을 때만 느려짐
        bool isActivelyCharging = _isChargeMode && Mouse.current.leftButton.isPressed;

        if (isActivelyCharging && _isCrouching) targetSpeed = 1f;
        else if (isActivelyCharging) targetSpeed = 2f;
        else if (_isCrouching) targetSpeed = 4f;

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
        float remainingCooldown = Mathf.Max(0, (_lastUsedTime[_currentColorIndex] + _abilityCooldowns[_currentColorIndex]) - Time.time);
        Debug.Log($"Bullet Color: {((ColorIndex)_currentColorIndex)} | Cooldown: {remainingCooldown:F1}s");
    }

    private enum ColorIndex { Blue = 0, Red = 1, Yellow = 2 }

    private void HandleFacingDirection()
    {
        bool isPressingFire = Mouse.current.leftButton.isPressed;

        if (isPressingFire)
        {
            // 차징 중이거나 발사 중일 때는 마우스 위치를 바라봄
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            float directionX = mousePos.x - transform.position.x;

            if (directionX > 0.1f && !_isFacingRight) Flip();
            else if (directionX < -0.1f && _isFacingRight) Flip();
            return;
        }

        if (Time.time < _lastFireTime + 0.3f) return;

        if (Mathf.Abs(_moveInput.x) > 0.1f)
        {
            // 평소 이동 중일 때는 이동 방향을 바라봄
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
        if (_isCrouching) return;
        
        float baseDamage = combatSettings.BaseDamage;
        float cooldown = 0.2f;

        if (combatSettings.EquippedSkills.Count > 0)
        {
            var skill = combatSettings.EquippedSkills[_currentSkillIndex];
            baseDamage = skill.Damage;
            cooldown = skill.Cooldown;
        }

        if (Time.time < _lastFireTime + cooldown) return;

        // 특수 능력 사용 가능 여부 체크
        bool canUseSpecial = Time.time >= _lastUsedTime[_currentColorIndex] + _abilityCooldowns[_currentColorIndex];
        bool isSpecialShot = false;

        if (canUseSpecial)
        {
            isSpecialShot = true;
            _lastUsedTime[_currentColorIndex] = Time.time;

            // 즉발 효과 (파랑: 스피드 부스트)
            if (_currentColorIndex == (int)ColorIndex.Blue)
            {
                _speedBoostTimer = 2f;
                Debug.Log("<color=blue>[Ability]</color> SPEED BOOST activated for 2s");
            }
        }

        // 데미지 및 크기 계산
        float chargeRatio = Mathf.Clamp01(chargeTime / combatSettings.MaxChargeTime);
        float finalDamage = Mathf.Lerp(baseDamage, combatSettings.MaxChargeDamage, chargeRatio);
        
        // 빨강 특수 능력: 데미지 1.5배
        if (isSpecialShot && _currentColorIndex == (int)ColorIndex.Red)
        {
            finalDamage *= 1.5f;
            Debug.Log("<color=red>[Ability]</color> POWER SHOT! Damage increased by 1.5x");
        }

        // [수정] 일반 모드(또는 차징 시작) 크기를 1.5f로 설정, 최대 3.5f까지 커짐
        float baseScale = 1.5f;
        float finalScale = _isChargeMode ? Mathf.Lerp(baseScale, 3.5f, chargeRatio) : baseScale;
        
        Vector3 projectileScale = new Vector3(finalScale, finalScale, 1f);
        float finalSpeed = _isChargeMode ? Mathf.Lerp(15f, 10f, chargeRatio) : 18f; // 일반 모드는 조금 더 빠르게

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;

        string[] tags = { "Blue", "Red", "Yellow" };
        string poolTag = tags[_currentColorIndex];
        Projectile.BubbleType bubbleType = (Projectile.BubbleType)_currentColorIndex;

        if (ObjectPooler.Instance != null && combatSettings.FirePoint != null)
        {
            Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var obj = ObjectPooler.Instance.SpawnFromPool(poolTag, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
            if (obj != null && obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale, gameObject, bubbleType, isSpecialShot);
            }
            _lastFireTime = Time.time;
        }
        else
        {
            GameObject prefab = (combatSettings.ColorProjectilePrefabs != null && combatSettings.ColorProjectilePrefabs.Length > _currentColorIndex) 
                ? combatSettings.ColorProjectilePrefabs[_currentColorIndex] : null;

            if (prefab && combatSettings.FirePoint)
            {
                Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
                if (obj.TryGetComponent<Projectile>(out var proj)) 
                    proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale, gameObject, bubbleType, isSpecialShot);
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

        if (_health != null)
        {
            _health.DamageMultiplier = _isCrouching ? 0.5f : 1.0f;
        }
    }
}
