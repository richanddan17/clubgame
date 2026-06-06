using System.Collections;
using UnityEngine;

/// <summary>
/// MeltingHaribo 몹: 몸이 녹아내려(MeltingDown) 플레이어 밑으로 이동 후, 다시 굳어져(Solidifying) 공격합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class MeltingHaribo : MonoBehaviour
{
    public enum HariboState { Idle, MeltingDown, Underground, Solidifying, Attack, Cooldown }

    [Header("설정")]
    public float moveSpeed = 5f;
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    public float attackDamage = 15f;
    public float stateDelay = 1f;

    [Header("스프라이트 설정")]
    public Sprite[] hariboSprites; // 0~16번 스프라이트 할당 필요 (녹는 연출)
    public float animationSpeed = 0.05f;

    [Header("참조")]
    private Transform _player;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Health _health;
    
    private HariboState _currentState = HariboState.Idle;
    private bool _isDead = false;

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
        if (_health != null) _health.OnDie.AddListener(OnDie);
        
        StartCoroutine(HariboRoutine());
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
                case HariboState.Cooldown:
                    yield return new WaitForSeconds(stateDelay);
                    _currentState = HariboState.Idle;
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
        // 녹기 시작할 때 콜라이더를 트리거로 변경 (물리 충돌은 없지만 공격은 받음)
        if (_col != null) _col.isTrigger = true;

        // 녹아내리는 애니메이션 (0 -> 18)
        int frameCount = hariboSprites != null ? hariboSprites.Length : 19;
        for (int i = 0; i < frameCount; i++)
        {
            if (hariboSprites != null && i < hariboSprites.Length)
                _sr.sprite = hariboSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }
        
        _currentState = HariboState.Underground;
    }

    private IEnumerator HandleUnderground()
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_player == null) break;

            float dist = Vector2.Distance(transform.position, _player.position);
            // 공격 범위 근처까지 가면 멈추고 올라옴
            if (dist < attackRange * 0.8f) break;

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
        // 다시 나타나는 애니메이션 (18 -> 0)
        int frameCount = hariboSprites != null ? hariboSprites.Length : 19;
        for (int i = frameCount - 1; i >= 0; i--)
        {
            if (hariboSprites != null && i < hariboSprites.Length)
                _sr.sprite = hariboSprites[i];
            yield return new WaitForSeconds(animationSpeed);
        }

        // 완전히 올라온 후 콜라이더를 다시 물리 모드로 복구
        if (_col != null) _col.isTrigger = false;
        
        _currentState = HariboState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange)
        {
            Health playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
                Debug.Log("Melting Haribo Attacked Player!");
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        _currentState = HariboState.Cooldown;
    }

    private void OnDie()
    {
        _isDead = true;
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
