using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
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
    public float gravityScale = 3.5f;

    private Transform _player;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Animator _anim;
    private Health _health;
    
    private HariboState _currentState = HariboState.Idle;
    private bool _isDead = false;
    private Coroutine _currentRoutine;

    private static readonly int AnimState = Animator.StringToHash("State");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _health = GetComponent<Health>();
        _anim = GetComponent<Animator>();

        if (_anim != null && _anim.runtimeAnimatorController != null)
        {
            var overrideController = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            // 런타임에 애니메이션 자동 매핑 (파일 경로는 이전 명령으로 세팅된 Assets/Resources/... 사용)
            overrideController["Idle"] = Resources.Load<AnimationClip>("Animations/MeltingHaribo/Idle");
            overrideController["Attack"] = Resources.Load<AnimationClip>("Animations/MeltingHaribo/Attack");
            overrideController["Walk"] = Resources.Load<AnimationClip>("Animations/MeltingHaribo/Walk");
            _anim.runtimeAnimatorController = overrideController;
        }

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

    private void UpdateAnimation(HariboState state)
    {
        if (_anim == null) return;
        int stateInt = state switch
        {
            HariboState.Idle => 0,
            HariboState.MeltingDown => 1,
            HariboState.Underground => 2,
            HariboState.Solidifying => 3,
            HariboState.Attack => 4,
            HariboState.Stunned => 6,
            _ => 0
        };
        _anim.SetInteger(AnimState, stateInt);
    }

    IEnumerator HariboRoutine()
    {
        while (!_isDead)
        {
            switch (_currentState)
            {
                case HariboState.Idle: yield return HandleIdle(); break;
                case HariboState.MeltingDown: yield return HandleMeltingDown(); break;
                case HariboState.Underground: yield return HandleUnderground(); break;
                case HariboState.Solidifying: yield return HandleSolidifying(); break;
                case HariboState.Attack: yield return HandleAttack(); break;
                case HariboState.Stunned: yield return HandleStunned(); break;
                case HariboState.Cooldown: yield return new WaitForSeconds(1f); _currentState = HariboState.Idle; break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleIdle()
    {
        UpdateAnimation(HariboState.Idle);
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= detectionRange)
            _currentState = HariboState.MeltingDown;
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleMeltingDown()
    {
        if (_health != null) _health.DamageMultiplier = 0.5f;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        UpdateAnimation(HariboState.MeltingDown);
        yield return new WaitForSeconds(1.0f);
        if (_col != null) _col.isTrigger = true;
        _currentState = HariboState.Underground;
    }

    private IEnumerator HandleUnderground()
    {
        UpdateAnimation(HariboState.Underground);
        float elapsed = 0f;
        while (elapsed < undergroundDuration)
        {
            if (_player == null) break;
            float xDir = _player.position.x - transform.position.x;
            if (Mathf.Abs(xDir) > 0.5f) { _rb.linearVelocity = new Vector2(Mathf.Sign(xDir) * moveSpeed, 0f); ApplyFlip(Mathf.Sign(xDir)); }
            else _rb.linearVelocity = Vector2.zero;
            elapsed += Time.deltaTime;
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;
        _currentState = HariboState.Solidifying;
    }

    private IEnumerator HandleSolidifying()
    {
        UpdateAnimation(HariboState.Solidifying);
        yield return new WaitForSeconds(1.0f);
        if (_col != null) _col.isTrigger = false;
        if (_health != null) _health.DamageMultiplier = 1f;
        _rb.gravityScale = gravityScale;
        _currentState = HariboState.Attack;
    }

    private IEnumerator HandleAttack()
    {
        UpdateAnimation(HariboState.Attack);
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange)
            _player.GetComponent<Health>()?.TakeDamage(attackDamage, transform.position);
        yield return new WaitForSeconds(1.0f);
        _currentState = HariboState.Cooldown;
    }

    private IEnumerator HandleStunned()
    {
        UpdateAnimation(HariboState.Stunned);
        yield return new WaitForSeconds(stunDuration);
        _currentState = HariboState.Idle;
    }

    public void ApplyStun(float duration)
    {
        stunDuration = duration;
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentState = HariboState.Stunned;
        _currentRoutine = StartCoroutine(HariboRoutine());
    }

    public void RequestCancelPattern() => ApplyStun(2.0f);

    private void ApplyFlip(float xDir) => transform.localScale = new Vector3(-Mathf.Sign(xDir) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

    private void OnDie()
    {
        if (_isDead) return;
        _isDead = true;
        _anim?.SetTrigger(AnimDie);
        StopAllCoroutines();
        Destroy(gameObject, 1.5f);
    }
}
