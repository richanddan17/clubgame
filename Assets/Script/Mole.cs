using System.Collections;
using UnityEngine;

/// <summary>
/// Mole 몹: 0~16번 스프라이트로 숨고, 플레이어 밑으로 이동 후 16~0번으로 나타나 공격합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class Mole : MonoBehaviour
{
    public enum MoleState { Idle, DiggingDown, Underground, PoppingUp, Attack, Cooldown }

    [Header("설정")]
    public float moveSpeed = 5f;
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    public float attackDamage = 15f;
    public float stateDelay = 1f;

    [Header("스프라이트 설정")]
    public Sprite[] moleSprites; // 0~16번 스프라이트 할당 필요
    public float animationSpeed = 0.05f;

    [Header("참조")]
    private Transform _player;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Health _health;
    
    private MoleState _currentState = MoleState.Idle;
    private bool _isDead = false;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _health = GetComponent<Health>();

        _rb.freezeRotation = true;
        _rb.gravityScale = 0; // 두더지는 땅속을 다니므로 중력 영향 제외 (혹은 필요에 따라 조절)
    }

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_health != null) _health.OnDie.AddListener(OnDie);
        
        StartCoroutine(MoleRoutine());
    }

    IEnumerator MoleRoutine()
    {
        while (!_isDead)
        {
            switch (_currentState)
            {
                case MoleState.Idle:
                    yield return HandleIdle();
                    break;
                case MoleState.DiggingDown:
                    yield return HandleDiggingDown();
                    break;
                case MoleState.Underground:
                    yield return HandleUnderground();
                    break;
                case MoleState.PoppingUp:
                    yield return HandlePoppingUp();
                    break;
                case MoleState.Attack:
                    yield return HandleAttack();
                    break;
                case MoleState.Cooldown:
                    yield return new WaitForSeconds(stateDelay);
                    _currentState = MoleState.Idle;
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleIdle()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= detectionRange)
        {
            _currentState = MoleState.DiggingDown;
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleDiggingDown()
    {
        // 0 -> 16 애니메이션
        for (int i = 0; i <= 16; i++)
        {
            if (moleSprites != null && i < moleSprites.Length)
                _sr.sprite = moleSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }
        
        _col.enabled = false; // 땅속에서는 충돌 무시
        _currentState = MoleState.Underground;
    }

    private IEnumerator HandleUnderground()
    {
        float duration = 2f; // 최대 2초간 이동
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_player == null) break;

            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist < 0.5f) break; // 플레이어 바로 밑 도착

            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * moveSpeed;

            // 방향에 따른 스프라이트 반전
            if (dir.x > 0.01f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else if (dir.x < -0.01f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        _currentState = MoleState.PoppingUp;
    }

    private IEnumerator HandlePoppingUp()
    {
        // 16 -> 0 애니메이션
        for (int i = 16; i >= 0; i--)
        {
            if (moleSprites != null && i < moleSprites.Length)
                _sr.sprite = moleSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }

        _col.enabled = true; // 다시 지상으로 나오면 충돌 활성화
        _currentState = MoleState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange)
        {
            Health playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
                Debug.Log("Mole Attacked Player!");
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        _currentState = MoleState.Cooldown;
    }

    private void OnDie()
    {
        _isDead = true;
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
