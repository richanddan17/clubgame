using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 생명체(플레이어, 적)에 공통으로 사용되는 모듈형 체력 시스템
/// </summary>
public class Health : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("이벤트")]
    public UnityEvent<float, float> OnHealthChanged; // (현재 체력, 최대 체력)
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnDie;

    private bool _isDead = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => _isDead;
    public float DamageMultiplier { get; set; } = 1f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 외부 데이터(EnemyData 등)로부터 체력을 초기화합니다.
    /// </summary>
    public void Initialize(float hp)
    {
        maxHealth = hp;
        currentHealth = hp;
        _isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool IsParrying { get; set; } = false;
    [System.Serializable] public class Vector2Event : UnityEvent<Vector2> { }
    public Vector2Event OnParry;

    /// <summary>
    /// 데미지를 입힙니다.
    /// </summary>
    public void TakeDamage(float amount, Vector2? damageSourcePos = null)
    {
        if (_isDead) return;

        bool currentFacingRight = transform.localScale.x > 0;

        if (IsParrying)
        {
            Vector2 dir = damageSourcePos.HasValue ? ((Vector2)transform.position - (Vector2)damageSourcePos.Value).normalized : (currentFacingRight ? Vector2.left : Vector2.right);
            // 대각선 위로 살짝 뜨게 설정
            dir = (dir + Vector2.up * 0.5f).normalized;
            
            OnParry?.Invoke(dir);
            Debug.Log("<color=green>[Parry]</color> Damage Negated!");
            return;
        }

        float finalDamage = amount * DamageMultiplier;
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnDamaged?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력을 회복시킵니다.
    /// </summary>
    public void Heal(float amount)
    {
        if (_isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealed?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDie?.Invoke();
    }
}
