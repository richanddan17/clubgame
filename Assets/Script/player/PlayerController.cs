using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// 최적화된 플레이어 컨트롤러 (Q키 토글 차징 모드 + ZXCV 스킬 시스템 + ObjectPooler 슈팅)
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
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

    private int _currentColorIndex = 0;
    private float _lastFireTime;
    private float _chargeStartTime;
    private bool _isChargeMode = false;
    private bool _isActivelyCharging = false;

    // 스킬 슬롯별 마지막 사용 시각 (쿨타임 게이트용)
    private float[] _skillLastUsed;

    // 특수 능력 및 쿨타임
    private float[] _abilityCooldowns = { 5f, 5f, 8f }; 
    private float[] _lastUsedTime = { -10f, -10f, -10f };
    private float _speedBoostTimer = 0f;

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    [Header("Inventory & HUD")]
    public GameObject inventoryPanel;
    private bool _isInventoryOpen = false;
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

        if (_skillLastUsed == null || _skillLastUsed.Length != combatSettings.EquippedSkills.Count)
            Array.Resize(ref _skillLastUsed, combatSettings.EquippedSkills.Count);
        for (int i = 0; i < _skillLastUsed.Length; i++) _skillLastUsed[i] = -10f;
        
        if (combatSettings.FirePoint == null)
            combatSettings.FirePoint = transform.Find("FirePoint") ?? transform;

        if (_health != null)
        {
            _health.OnDie.AddListener(OnDeath);
            _health.OnParry.AddListener(ApplyParryKnockback);
        }
    }

    private void Start()
    {
        UpdateSkillHUD();
    }

    private void Update()
    {
        if (_health.IsDead) return;

        UpdateGroundStatus();
        HandleInput();
        HandleFacingDirection();
        ApplyCrouch();
        UpdateAnimations();

        if (_speedBoostTimer > 0) _speedBoostTimer -= Time.deltaTime;
        if (_knockbackTimer > 0) _knockbackTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (_health.IsDead) return;
        ApplyMovement();
    }
    #endregion

    #region Input & Mechanics
    private void HandleInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame) ToggleInventory();
        if (_isInventoryOpen) 
        {
            _moveInput = Vector2.zero;
            return;
        }

        float h = 0f;
        if (Keyboard.current.dKey.isPressed) h = 1f;
        else if (Keyboard.current.aKey.isPressed) h = -1f;
        _moveInput.x = h;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) Jump();

        _isCrouching = Keyboard.current.sKey.isPressed;
        _isRunning = Keyboard.current.leftShiftKey.isPressed && !_isCrouching;

        if (Keyboard.current.rKey.wasPressedThisFrame) CycleColor();

        // ZXCV 스킬 입력
        if (Keyboard.current.zKey.wasPressedThisFrame) UseSkill(0);
        if (Keyboard.current.xKey.wasPressedThisFrame) UseSkill(1);
        if (Keyboard.current.cKey.wasPressedThisFrame) UseSkill(2);
        if (Keyboard.current.vKey.wasPressedThisFrame) UseSkill(3);

        if (Keyboard.current.fKey.wasPressedThisFrame) TryParry();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            _isChargeMode = !_isChargeMode;
            Debug.Log("<color=cyan>[Mode]</color> Charge Mode: " + (_isChargeMode ? "ON" : "OFF"));
        }

        if (_isChargeMode)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) 
            {
                _chargeStartTime = Time.time;
                _isActivelyCharging = true;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame) 
            {
                TryFire(Time.time - _chargeStartTime);
                _isActivelyCharging = false;
            }
        }
        else
        {
            if (Mouse.current.leftButton.isPressed) TryFire();
            _isActivelyCharging = false;
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
            Time.timeScale = _isInventoryOpen ? 0f : 1f;
            Cursor.visible = _isInventoryOpen;
            Cursor.lockState = _isInventoryOpen ? CursorLockMode.None : CursorLockMode.Confined;
            if (_isInventoryOpen && inventoryPanel.TryGetComponent<InventoryUI>(out var invUI)) invUI.UpdateUI();
        }
    }

    private void UseSkill(int slotIndex)
    {
        if (combatSettings.EquippedSkills.Count <= slotIndex) return;

        SkillData skill = combatSettings.EquippedSkills[slotIndex];
        if (skill == null) return;

        // 슬롯별 쿨타임 게이트 (차단 시 쿨타임/리소스 소모 없음)
        if (_skillLastUsed == null || slotIndex >= _skillLastUsed.Length) return;
        if (Time.time - _skillLastUsed[slotIndex] < skill.Cooldown) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;

        if (combatSettings.FirePoint == null) return;
        Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        bool spawned = false;

        switch (skill.SkillType)
        {
            case SkillType.Projectile:
                if (skill.ProjectilePrefab == null)
                {
                    Debug.LogWarning($"[Skill] '{skill.SkillName}' has no ProjectilePrefab assigned. (slot {slotIndex})");
                    return;
                }
                GameObject projObj = Instantiate(skill.ProjectilePrefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
                if (projObj.TryGetComponent<Projectile>(out var proj))
                    proj.Initialize(skill.Damage, _isFacingRight, skill.ProjectileSpeed, null, gameObject, skill.BubbleEffect, skill.UseBubbleEffect);
                spawned = true;
                break;

            case SkillType.Melee:
                if (skill.ProjectilePrefab == null)
                {
                    Debug.LogWarning($"[Skill] '{skill.SkillName}' has no melee hitbox prefab assigned. (slot {slotIndex})");
                    return;
                }
                Vector3 meleeSpawnPos = combatSettings.FirePoint.position + (Vector3)(direction * (skill.MeleeRange * 0.6f));
                GameObject meleeObj = Instantiate(skill.ProjectilePrefab, meleeSpawnPos, Quaternion.Euler(0, 0, angle));
                if (meleeObj.TryGetComponent<MeleeHitbox>(out var hitbox))
                    hitbox.Initialize(skill.Damage, gameObject, skill.HitboxLifetime, skill.MeleeRange, skill.UseBubbleEffect, skill.BubbleEffect);
                spawned = true;
                break;

            case SkillType.MeleeAoE:
                if (skill.ProjectilePrefab == null)
                {
                    Debug.LogWarning($"[Skill] '{skill.SkillName}' has no AoE hitbox prefab assigned. (slot {slotIndex})");
                    return;
                }
                GameObject aoeObj = Instantiate(skill.ProjectilePrefab, transform.position, Quaternion.identity);
                if (aoeObj.TryGetComponent<MeleeHitbox>(out var aoeHitbox))
                    aoeHitbox.Initialize(skill.Damage, gameObject, skill.HitboxLifetime, skill.MeleeRange, skill.UseBubbleEffect, skill.BubbleEffect);
                spawned = true;
                break;

            case SkillType.InstantArea:
                if (skill.ProjectilePrefab == null)
                {
                    Debug.LogWarning($"[Skill] '{skill.SkillName}' has no area effect prefab assigned. (slot {slotIndex})");
                    return;
                }
                Instantiate(skill.ProjectilePrefab, transform.position, Quaternion.identity);
                spawned = true;
                break;
        }

        if (spawned)
        {
            _skillLastUsed[slotIndex] = Time.time;
            if (SkillHUDManager.Instance != null) SkillHUDManager.Instance.TriggerCooldown(slotIndex);
        }
    }

    public void UpdateSkillHUD()
    {
        if (SkillHUDManager.Instance == null) return;
        for (int i = 0; i < 4; i++)
        {
            if (combatSettings.EquippedSkills.Count > i)
                SkillHUDManager.Instance.UpdateSkillIcon(i, combatSettings.EquippedSkills[i]);
            else
                SkillHUDManager.Instance.UpdateSkillIcon(i, null);
        }
    }

    private void TryFire(float chargeTime = 0f)
    {
        if (_isCrouching) return;
        
        float baseCooldown = 0.2f;
        if (Time.time < _lastFireTime + baseCooldown) return;

        bool canUseSpecial = Time.time >= _lastUsedTime[_currentColorIndex] + _abilityCooldowns[_currentColorIndex];
        bool isSpecialShot = false;

        if (canUseSpecial)
        {
            isSpecialShot = true;
            _lastUsedTime[_currentColorIndex] = Time.time;
            if (_currentColorIndex == 0) _speedBoostTimer = 2f;
        }

        float chargeRatio = Mathf.Clamp01(chargeTime / 5f);
        float finalDamage = Mathf.Lerp(10f, 60f, chargeRatio);
        if (isSpecialShot && _currentColorIndex == 1) finalDamage *= 1.5f;

        float finalScaleVal = _isChargeMode ? Mathf.Lerp(1.5f, 3.5f, chargeRatio) : 1.5f;
        Vector3 projectileScale = new Vector3(finalScaleVal, finalScaleVal, 1f);
        float finalSpeed = _isChargeMode ? Mathf.Lerp(15f, 10f, chargeRatio) : 18f;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;

        if (combatSettings.FirePoint == null) return;

        Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        string[] tags = { "Blue", "Red", "Yellow" };
        string poolTag = tags[_currentColorIndex];
        Projectile.BubbleType bubbleType = (Projectile.BubbleType)_currentColorIndex;

        if (ObjectPooler.Instance != null)
        {
            var obj = ObjectPooler.Instance.SpawnFromPool(poolTag, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
            if (obj != null && obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale, gameObject, bubbleType, isSpecialShot);
            }
        }
        else
        {
            GameObject prefab = (combatSettings.ColorProjectilePrefabs != null && combatSettings.ColorProjectilePrefabs.Length > _currentColorIndex) 
                ? combatSettings.ColorProjectilePrefabs[_currentColorIndex] : null;

            if (prefab != null)
            {
                var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
                if (obj.TryGetComponent<Projectile>(out var proj)) 
                    proj.Initialize(finalDamage, _isFacingRight, finalSpeed, projectileScale, gameObject, bubbleType, isSpecialShot);
            }
        }

        _lastFireTime = Time.time;
    }
    #endregion

    #region Movement & Physics
    private float _knockbackTimer = 0f;

    private void SetupPhysics()
    {
        _rb.gravityScale = 3.5f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (moveSettings.GroundLayer.value == 0) moveSettings.GroundLayer = LayerMask.GetMask("Ground");
    }

    private void ApplyMovement()
    {
        if (_knockbackTimer > 0) return;

        float targetSpeed = _isRunning ? moveSettings.RunSpeed : moveSettings.WalkSpeed;

        // 웅크리기 속도 반감 (CrouchSpeed 적용)
        if (_isCrouching) targetSpeed = moveSettings.CrouchSpeed;

        // 차징 모드이면서, 실제로 마우스를 눌러 차징 중일 때만 속도 50% 감소
        if (_isChargeMode && _isActivelyCharging) targetSpeed *= 0.5f;

        if (_speedBoostTimer > 0) targetSpeed *= 1.5f;
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
        if (_isGrounded) _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, moveSettings.JumpForce);
    }

    private void HandleFacingDirection()
    {
        bool isPressingFire = Mouse.current.leftButton.isPressed;
        if (isPressingFire)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            float directionX = mousePos.x - transform.position.x;
            if (directionX > 0.1f && !_isFacingRight) Flip();
            else if (directionX < -0.1f && _isFacingRight) Flip();
            return;
        }

        if (Mathf.Abs(_moveInput.x) > 0.1f)
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

    private void ApplyCrouch()
    {
        float targetY = _isCrouching ? _originalScale.y * moveSettings.CrouchScaleMultiplier : _originalScale.y;
        transform.localScale = new Vector3(_originalScale.x, targetY, _originalScale.z);
        if (_health != null) _health.DamageMultiplier = _isCrouching ? 0.5f : 1.0f;
    }

    private void UpdateAnimations()
    {
        _animator.SetFloat(AnimSpeed, Mathf.Abs(_moveInput.x));
        _animator.SetBool(AnimIsGrounded, _isGrounded);
        _animator.SetBool(AnimIsRunning, _isRunning && Mathf.Abs(_moveInput.x) > 0.1f);
    }
    #endregion

    #region Combat Utilities
    private void CycleColor() { _currentColorIndex = (_currentColorIndex + 1) % 3; }
    private void TryParry() { StartCoroutine(ParryRoutine()); }
    private System.Collections.IEnumerator ParryRoutine()
    {
        if (_health != null) _health.IsParrying = true;
        yield return new WaitForSeconds(0.3f);
        if (_health != null) _health.IsParrying = false;
    }
    private void ApplyParryKnockback(Vector2 direction)
    {
        _knockbackTimer = 0.2f;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction * 15f, ForceMode2D.Impulse);
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
}
