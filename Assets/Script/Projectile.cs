using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private bool isFacingRight = true;

    public void Initialize(float damageAmount, bool facingRight)
    {
        damage = damageAmount;
        isFacingRight = facingRight;
        
        // 방향에 따라 스프라이트 뒤집기 또는 회전
        if (!isFacingRight)
        {
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Slime 스크립트가 있는지 확인
        Slime slime = collision.GetComponent<Slime>();
        if (slime != null)
        {
            slime.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 기존 EnemyController가 있는지 확인
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
