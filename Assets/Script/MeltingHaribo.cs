using System.Collections;
using UnityEngine;

/// <summary>
/// MeltingHaribo 몹: 곰젤리 몬스터. 
/// - 평소 Idle 상태였다가 플레이어가 반경 안에 들어오면 땅으로 들어감 (Burrow).
/// - 땅 속에서는 무적이며 플레이어의 X축을 추적 (4초).
/// - 땅에서 튀어나와 공격 (20 데미지, 플레이어 3초 경직).
/// - 다시 땅으로 들어감.
/// - 땅에 들어가는 애니메이션 중 풀차징 공격을 받으면 캔슬되고 2초간 경직.
/// - 중력 효과가 적용됩니다.
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
    public float playerStunDuration = 3f;
    public float gravityScale = 3.5f;

    [Header("스프라이트 설정")]
    public Sprite[] hariboSprites; // 0~18번 스프라이트 (녹는 연출)
    public float animationSpeed = 0.05f;

    [Header("참조")]
    private Transform _player;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Health _health;
    
    private HariboState _currentState = HariboState.Idle;
    private bool _isDead = false;
    private Coroutine _currentRoutine;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _health = GetComponent<Health>();

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
                    _currentState = HariboState.MeltingDown;
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleIdle()
    {
        _rb.gravityScale = gravityScale;
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= detectionRange)
        {
            _currentState = HariboState.MeltingDown;
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleMeltingDown()
    {
        if (_health != null) _health.DamageMultiplier = 0f;
        
        // 땅으로 들어갈 때는 중력 해제 및 속도 정지 (바닥 통과 방지)
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        
        int frameCount = hariboSprites != null ? hariboSprites.Length : 0;
        for (int i = 0; i < frameCount; i++)
        {
            if (hariboSprites != null && i < hariboSprites.Length)
                _sr.sprite = hariboSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }

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
            
            if (Mathf.Abs(xDir) > 0.1f)
            {
                _rb.linearVelocity = new Vector2(moveDir * moveSpeed, 0f);
                ApplyFlip(moveDir);
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;
        _currentState = HariboState.Solidifying;
    }

    private IEnumerator HandleSolidifying()
    {
        int frameCount = hariboSprites != null ? hariboSprites.Length : 0;
        for (int i = frameCount - 1; i >= 0; i--)
        {
            if (hariboSprites != null && i < hariboSprites.Length)
                _sr.sprite = hariboSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }

        if (_col != null) _col.isTrigger = false;
        if (_health != null) _health.DamageMultiplier = 1f;
        _rb.gravityScale = gravityScale; // 다시 중력 적용
        
        _currentState = HariboState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange)
        {
            Health playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage, transform.position);

            MonoBehaviour playerCtrl = _player.GetComponent("PlayerController") as MonoBehaviour;
            Rigidbody2D playerRb = _player.GetComponent<Rigidbody2D>();
            
            if (playerCtrl != null)
            {
                playerCtrl.enabled = false;
                if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
                StartCoroutine(ReEnablePlayer(playerCtrl, playerStunDuration));
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        _currentState = HariboState.Cooldown;
    }

    private IEnumerator ReEnablePlayer(MonoBehaviour ctrl, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ctrl != null) ctrl.enabled = true;
    }

    private IEnumerator HandleStunned()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = gravityScale; // 스턴 중에도 중력 적용
        
        if (_health != null) _health.DamageMultiplier = 1f;
        if (_col != null) _col.isTrigger = false;

        yield return new WaitForSeconds(stunDuration);
        
        _currentState = HariboState.MeltingDown;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDead) return;

        if (_currentState == HariboState.MeltingDown || _currentState == HariboState.Underground)
        {
            if (collision.CompareTag("Projectile") || collision.gameObject.name.Contains("Bubble"))
            {
                // 풀차징 샷(크기 3.4 이상) 감지
                if (collision.transform.localScale.x >= 3.4f)
                {
                    CancelBurrow();
                }
            }
        }
    }

    private void CancelBurrow()
    {
        Debug.Log("<color=orange>[Cancel]</color> Melting Haribo burrow cancelled by Fully Charged Attack!");
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentState = HariboState.Stunned;
        _currentRoutine = StartCoroutine(HariboRoutine());
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
        StopAllCoroutines();
        Destroy(gameObject, 0.5f);
    }
}
