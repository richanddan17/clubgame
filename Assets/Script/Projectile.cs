using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum BubbleType { Blue, Red, Yellow }

    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private bool isFacingRight = true;
    private GameObject owner; // 투사체를 쏜 주인
    private BubbleType bubbleType;
    private bool isSpecial;

    public void Initialize(float damageAmount, bool facingRight, float customSpeed = 15f, Vector3? customScale = null, GameObject shooter = null, BubbleType type = BubbleType.Blue, bool special = false)
    {
        damage = damageAmount;
        isFacingRight = facingRight;
        speed = customSpeed;
        owner = shooter;
        bubbleType = type;
        isSpecial = special;

        if (customScale.HasValue)
        {
            transform.localScale = customScale.Value;
        }
        
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) 
        {
            float angle = transform.eulerAngles.z;
            sr.flipY = (angle > 90 && angle < 270);
        }
    }

    private void OnEnable()
    {
        CancelInvoke(nameof(Deactivate));
        Invoke(nameof(Deactivate), lifeTime);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        transform.Translate(transform.right * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 주인(쏜 사람)은 무시
        if (owner != null && collision.gameObject == owner) return;

        Health health = collision.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);

            // 특수 효과 적용 (적에게만)
            if (isSpecial && collision.CompareTag("Enemy"))
            {
                EnemyController enemy = collision.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplyEffect(bubbleType);
                }
            }

            Deactivate();
            return;
        }

        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // 적끼리 맞거나 땅에 닿으면 제거 (플레이어가 쏜 경우 적을 맞추기 위해)
            if (owner != null && owner.CompareTag("Player") && collision.CompareTag("Enemy"))
            {
                // Health가 없는 적 처리 (있을 경우 위에서 처리됨)
                EnemyController enemy = collision.GetComponent<EnemyController>();
                if (isSpecial && enemy != null) enemy.ApplyEffect(bubbleType);
                Deactivate();
            }
            else
            {
                Deactivate();
            }
        }
    }
}
