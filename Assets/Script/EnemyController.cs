using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class EnemyController : MonoBehaviour
{
    public EnemyData data; // ScriptableObject 데이터
    
    private float currentHP;
    private Transform target;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        currentHP = data.HP;
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 데이터에 따른 시각적 차별화 (예: 이름에 따라 색상 변경)
        if (data.EnemyName.Contains("Yellow")) sr.color = Color.yellow;
        else if (data.EnemyName.Contains("Red")) sr.color = Color.red;
    }

    private void FixedUpdate()
    {
        if (target == null || data == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        // 감지 거리 안에 있을 때만 추격
        if (distance <= data.DetectionRange)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * data.Speed, rb.linearVelocity.y);
            
            // 방향 전환
            if (direction.x > 0) transform.localScale = new Vector3(-1, 1, 1);
            else if (direction.x < 0) transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        Debug.Log($"{data.EnemyName} 남은 체력: {currentHP}");
        
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 사망 효과 및 파괴
        Destroy(gameObject);
    }
}
