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
        
        // 1. FirePoint 자동 할당
        if (combatSettings.FirePoint == null)
        {
            combatSettings.FirePoint = transform.Find("FirePoint");
            if (combatSettings.FirePoint == null) combatSettings.FirePoint = transform;
        }

        // 2. 프리팹 자동 로드 시도 (인스펙터 할당 누락 대비)
        if (combatSettings.ColorProjectilePrefabs == null || combatSettings.ColorProjectilePrefabs.Length == 0 || combatSettings.ColorProjectilePrefabs[0] == null)
        {
            Debug.Log("Prefabs missing, attempting to load from Resources/Prefabs...");
            combatSettings.ColorProjectilePrefabs = new GameObject[3];
            string[] colors = { "blue", "red", "yellow" };
            for (int i = 0; i < 3; i++)
            {
                // Resources 폴더에 있거나, 직접 경로에서 로드 시도 (런타임에서는 Resources 권장)
                combatSettings.ColorProjectilePrefabs[i] = Resources.Load<GameObject>($"BubbleProjectile_{colors[i]}");
                
                // Resources에 없으면 에디터에서만 작동하는 경로 시도 (개발 편의성)
                #if UNITY_EDITOR
                if (combatSettings.ColorProjectilePrefabs[i] == null)
                {
                    combatSettings.ColorProjectilePrefabs[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/BubbleProjectile_{colors[i]}.prefab");
                }
                #endif
            }
        }
        
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
        HandleFacingDirection(); 
        ApplyCrouch();
        UpdateAnimations();

        // 디버그: 점프가 안될 때 바닥 체크 확인용 (개발 중에만 사용)
        if (Input.GetKeyDown(KeyCode.Space) && !_isGrounded)
        {
            Debug.Log($"Jump failed: Not Grounded. Layer: {moveSettings.GroundLayer.value}");
        }
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

        // GroundLayer가 아무것도 없으면 "Ground"만 설정 (Default 포함 시 자신을 밟고 점프할 위험 있음)
        if (moveSettings.GroundLayer.value == 0)
        {
            moveSettings.GroundLayer = LayerMask.GetMask("Ground");
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
        
        // 발사는 마우스 클릭 시 즉시 실행
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
        
        // 캐릭터 자신의 레이어는 무시하도록 체크 (무한 점프 방지)
        int layerMask = moveSettings.GroundLayer.value;
        // 만약 레이어마스크에 Player가 포함되어 있다면 제거
        layerMask &= ~(1 << gameObject.layer);

        Bounds b = _collider.bounds;
        Vector2 checkPos = new Vector2(b.center.x, b.min.y);
        _isGrounded = Physics2D.OverlapBox(checkPos, new Vector2(b.size.x * 0.8f, 0.1f), 0f, layerMask);
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

    private void HandleFacingDirection()
    {
        // 마우스 왼쪽 버튼이 눌려있지 않을 때만 이동 방향으로 회전
        if (!Mouse.current.leftButton.isPressed)
        {
            if (Mathf.Abs(_moveInput.x) > 0.1f)
            {
                if (_moveInput.x > 0 && !_isFacingRight) Flip();
                else if (_moveInput.x < 0 && _isFacingRight) Flip();
            }
        }
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        // 캐릭터를 180도 회전
        transform.rotation = Quaternion.Euler(0, _isFacingRight ? 0 : 180, 0);
    }

    private void TryFire()
    {
        // 스킬/기본 설정값 준비
        float damage = 10f;
        float cooldown = 0.2f;
        GameObject defaultPrefab = null;

        if (combatSettings.EquippedSkills.Count > 0)
        {
            SkillData skill = combatSettings.EquippedSkills[_currentSkillIndex];
            damage = skill.Damage;
            cooldown = skill.Cooldown;
            defaultPrefab = skill.ProjectilePrefab;
        }

        if (Time.time < _lastFireTime + cooldown) return;

        // 1. 발사 전 마우스 방향으로 캐릭터 즉시 회전 (이동 방향보다 우선순위 높음)
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;
        float directionX = mousePos.x - transform.position.x;

        if (directionX > 0.1f && !_isFacingRight) Flip();
        else if (directionX < -0.1f && _isFacingRight) Flip();

        // 2. 총알 발사 로직
        GameObject prefab = (combatSettings.ColorProjectilePrefabs != null && combatSettings.ColorProjectilePrefabs.Length > _currentColorIndex) 
            ? combatSettings.ColorProjectilePrefabs[_currentColorIndex] 
            : defaultPrefab;

        if (prefab && combatSettings.FirePoint)
        {
            Vector2 direction = (mousePos - combatSettings.FirePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var obj = Instantiate(prefab, combatSettings.FirePoint.position, Quaternion.Euler(0, 0, angle));
            if (obj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(damage, _isFacingRight);
            }
            _lastFireTime = Time.time;
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
