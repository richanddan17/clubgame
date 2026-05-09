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

    /// <summary>
    /// 데미지를 입힙니다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        currentHealth -= amount;
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
