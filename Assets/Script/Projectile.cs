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
        
        // 초기화 시 추가적인 회전 처리가 필요하다면 여기서 수행 (현재는 Instantiate의 회전값을 유지함)
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Vector2.right 대신 transform.right를 사용하여 현재 총알이 바라보는 방향(회전값)으로 이동
        transform.Translate(transform.right * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 공통 Health 컴포넌트 확인
        Health health = collision.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 2. 레거시/특수 처리 (필요시 유지)
        if (collision.CompareTag("Enemy"))
        {
            // 이제 대부분 Health 컴포넌트에서 처리되지만, 만약 없다면 기존 방식 시도
            EnemyController enemy = collision.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);
            
            Slime slime = collision.GetComponent<Slime>();
            if (slime != null) slime.TakeDamage(damage);
            
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
