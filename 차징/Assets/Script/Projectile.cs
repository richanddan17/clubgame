using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private bool isFacingRight = true;

    public void Initialize(float damageAmount, bool facingRight, float customSpeed = -1f, Vector3? customScale = null)
    {
        damage = damageAmount;
        isFacingRight = facingRight;
        
        if (customSpeed > 0) speed = customSpeed;
        if (customScale.HasValue) transform.localScale = customScale.Value;
        else transform.localScale = Vector3.one; // 기본 크기 복구 (풀링 대응)
        
        // 방향에 따라 스프라이트 뒤집기 또는 회전
        // 주의: 이미 PlayerController에서 회전값을 주어 생성할 수 있으므로, 
        // 여기서는 isFacingRight에 따른 추가 처리가 필요한지 확인이 필요함.
        // 기존 로직 유지:
        if (!isFacingRight && transform.rotation.eulerAngles.z == 0)
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
        // 플레이어 자신의 투사체에 데미지를 입지 않도록 플레이어 태그 무시
        if (collision.CompareTag("Player")) return;

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
