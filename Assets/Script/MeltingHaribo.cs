using System.Collections;
using UnityEngine;

/// <summary>
/// MeltingHaribo 몹: 곰젤리 몬스터. 
/// - Animator 기반으로 동작하며, 녹기-이동-굳기-공격 루프를 가짐.
/// - 2D 물리 시스템(Rigidbody2D, Collider2D) 사용.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class MeltingHaribo : MonoBehaviour
{
    public enum HariboState { Idle, MeltingDown, Underground, Solidifying, Attack, Stunned, Cooldown }

    [Header("설정")]
    public float moveSpeed = 4f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float attackDamage = 20f;
    public float undergroundDuration = 4f;
    public float stunDuration = 2f;
    public float playerStunDuration = 2f;
    public float gravityScale = 3.5f;

    [Header("참조")]
    private Transform _player;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Animator _anim;
    private Health _health;
    
    private HariboState _currentState = HariboState.Idle;
    private bool _isDead = false;
    private Coroutine _currentRoutine;

    // Animator Parameters
    private static readonly int AnimWalk = Animator.StringToHash("Walk");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    // 추가적인 Melting 전용 파라미터가 없을 경우를 대비해 Walk와 Attack 활용 가능

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _health = GetComponent<Health>();
        _anim = GetComponent<Animator>();

        _rb.freezeRotation = true;
        _rb.gravityScale = gravityScale;
    }

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_health != null)
        {
            _health.Initialize(100f);
            _health.OnDie.AddListener(OnDie);
        }
        
        _currentRoutine = StartCoroutine(HariboRoutine());
    }

    void Update()
    {
        if (_isDead) return;

        // 시각적 피드백 (스턴/정지 시 회색)
        if (_sr != null)
        {
            if (_currentState == HariboState.Stunned) _sr.color = Color.gray;
            else _sr.color = Color.white;
        }
    }

    IEnumerator HariboRoutine()
    {
        while (!_isDead)
        {
            switch (_currentState)
            {
                case HariboState.Idle:
                    yield return HandleIdle();
                    break;
                case HariboState.MeltingDown:
                    yield return HandleMeltingDown();
                    break;
                case HariboState.Underground:
                    yield return HandleUnderground();
                    break;
                case HariboState.Solidifying:
                    yield return HandleSolidifying();
                    break;
                case HariboState.Attack:
                    yield return HandleAttack();
                    break;
                case HariboState.Stunned:
                    yield return HandleStunned();
                    break;
                case HariboState.Cooldown:
                    yield return new WaitForSeconds(1f);
                    _currentState = HariboState.Idle;
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleIdle()
    {
        _rb.gravityScale = gravityScale;
        UpdateAnimation(false);
        
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= detectionRange)
        {
            _currentState = HariboState.MeltingDown;
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleMeltingDown()
    {
        Debug.Log($"[{name}] Melting Down...");
        if (_health != null) _health.DamageMultiplier = 0.5f; // 녹는 중에는 데미지 반감
        
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        
        // 애니메이션: 녹는 연출 (별도 파라미터 없으면 Walk를 false로 하고 잠시 대기)
        UpdateAnimation(false);
        yield return new WaitForSeconds(1.0f); 

        if (_col != null) _col.isTrigger = true;
        _currentState = HariboState.Underground;
    }

    private IEnumerator HandleUnderground()
    {
        float elapsed = 0f;
        while (elapsed < undergroundDuration)
        {
            if (_player == null) break;
            
            float xDir = _player.position.x - transform.position.x;
            float moveDir = xDir > 0 ? 1 : -1;
            
            if (Mathf.Abs(xDir) > 0.5f)
            {
                _rb.linearVelocity = new Vector2(moveDir * moveSpeed, 0f);
                ApplyFlip(moveDir);
                UpdateAnimation(true);
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                UpdateAnimation(false);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;
        _currentState = HariboState.Solidifying;
    }

    private IEnumerator HandleSolidifying()
    {
        Debug.Log($"[{name}] Solidifying...");
        UpdateAnimation(false);
        yield return new WaitForSeconds(1.0f);

        if (_col != null) _col.isTrigger = false;
        if (_health != null) _health.DamageMultiplier = 1f;
        _rb.gravityScale = gravityScale; 
        
        _currentState = HariboState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        float distance = _player != null ? Vector2.Distance(transform.position, _player.position) : 999f;
        
        if (distance <= attackRange)
        {
            if (_anim != null) _anim.SetTrigger(AnimAttack);
            
            Health playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null) 
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
            }
            Debug.Log($"[{name}] Attacked player!");
        }
        
        yield return new WaitForSeconds(1.0f);
        _currentState = HariboState.Cooldown;
    }

    private IEnumerator HandleStunned()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = gravityScale;
        UpdateAnimation(false);
        
        if (_health != null) _health.DamageMultiplier = 2.0f; // 스턴 중에는 데미지 2배
        if (_col != null) _col.isTrigger = false;

        yield return new WaitForSeconds(stunDuration);
        _currentState = HariboState.Idle;
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (_anim != null) _anim.SetBool(AnimWalk, isMoving);
    }

    private void ApplyFlip(float xDir)
    {
        if (xDir > 0.01f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (xDir < -0.01f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void OnDie()
    {
        if (_isDead) return;
        _isDead = true;
        if (_anim != null) _anim.SetTrigger(AnimDie);
        StopAllCoroutines();
        Destroy(gameObject, 1.5f);
    }

    public void ApplyStun(float duration)
    {
        stunDuration = duration; // 임시로 현재 스턴 시간 변경
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentState = HariboState.Stunned;
        _currentRoutine = StartCoroutine(HariboRoutine());
        Debug.Log($"<color=yellow>[MeltingHaribo]</color> {name} STUNNED for {duration}s");
    }

    // 외부(Projectile 등)에서 호출하여 패턴 캔슬 가능
    public void RequestCancelPattern()
    {
        ApplyStun(2.0f);
    }
}
