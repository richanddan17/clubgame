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
        Health health = collision.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Deactivate();
            return;
        }

        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Deactivate();
        }
    }
}
