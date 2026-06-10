using System.Collections;
using UnityEngine;

/// <summary>
/// MeltingHaribo 몹: 곰젤리 몬스터. 
/// - 평소 Idle 상태였다가 플레이어가 반경 안에 들어오면 땅으로 들어감 (Burrow).
/// - 땅 속에서는 무적이며 플레이어를 추적 (4초).
/// - 땅에서 튀어나와 공격 (20 데미지, 플레이어 3초 경직 및 둔화).
/// - 다시 땅으로 들어감.
/// - 땅에 들어가는 애니메이션 중 풀차징 공격을 받으면 캔슬되고 2초간 경직.
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
    public float playerSlowMultiplier = 0.5f;

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
        _rb.gravityScale = 0;
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
                    _currentState = HariboState.MeltingDown; // 공격 후 다시 땅으로
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleIdle()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= detectionRange)
        {
            _currentState = HariboState.MeltingDown;
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleMeltingDown()
    {
        // 땅으로 들어갈 때는 평타 무적 (데미지 배율 0)
        if (_health != null) _health.DamageMultiplier = 0f;
        
        // 애니메이션 (0 -> 끝)
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

            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * moveSpeed;

            if (dir.x > 0.01f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else if (dir.x < -0.01f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        _currentState = HariboState.Solidifying;
    }

    private IEnumerator HandleSolidifying()
    {
        // 다시 나타날 때도 무적 유지? (유저 요청은 땅에 들어가는 애니메이션 중 캔슬임)
        // 여기서는 다시 나타나는 애니메이션 (끝 -> 0)
        int frameCount = hariboSprites != null ? hariboSprites.Length : 0;
        for (int i = frameCount - 1; i >= 0; i--)
        {
            if (hariboSprites != null && i < hariboSprites.Length)
                _sr.sprite = hariboSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }

        if (_col != null) _col.isTrigger = false;
        if (_health != null) _health.DamageMultiplier = 1f; // 이제 데미지 받음
        
        _currentState = HariboState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange)
        {
            Health playerHealth = _player.GetComponent<Health>();
            PlayerController playerCtrl = _player.GetComponent<PlayerController>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
            }

            if (playerCtrl != null)
            {
                playerCtrl.ApplyStun(playerStunDuration);
                playerCtrl.ApplySlow(playerSlowMultiplier, playerStunDuration);
            }
            Debug.Log("Melting Haribo Attacked Player! 20 Damage + Stun + Slow");
        }
        
        yield return new WaitForSeconds(0.5f);
        _currentState = HariboState.Cooldown;
    }

    private IEnumerator HandleStunned()
    {
        _rb.linearVelocity = Vector2.zero;
        if (_sr != null) _sr.color = Color.gray;
        
        // 스턴 중에는 데미지를 입게 설정 (캔슬 되었으므로)
        if (_health != null) _health.DamageMultiplier = 1f;
        if (_col != null) _col.isTrigger = false;

        yield return new WaitForSeconds(stunDuration);
        
        if (_sr != null) _sr.color = Color.white;
        _currentState = HariboState.MeltingDown; // 스턴 풀리면 다시 땅으로 시도? 혹은 Idle? 
        // 유저 요구사항엔 없지만 자연스럽게 다시 땅으로 들어가게 함.
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDead) return;

        // 땅 속에 있거나 들어가는 중일 때 풀차징 공격 체크
        if (_currentState == HariboState.MeltingDown || _currentState == HariboState.Underground)
        {
            if (collision.TryGetComponent<Projectile>(out var proj))
            {
                if (proj.IsFullyCharged)
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

    private void OnDie()
    {
        if (_isDead) return;
        _isDead = true;
        StopAllCoroutines();
        
        // 죽는 애니메이션이나 효과 추가 가능
        Destroy(gameObject, 0.5f);
    }
}
