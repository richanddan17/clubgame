using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private bool isFacingRight = true;
    private GameObject owner; // 투사체를 쏜 주인

    public void Initialize(float damageAmount, bool facingRight, GameObject shooter = null)
    {
        damage = damageAmount;
        isFacingRight = facingRight;
        owner = shooter;
        
        // [수정] Spawner가 이미 Rotation을 설정했으므로 여기서 강제로 덮어쓰지 않음.
        // 스프라이트가 뒤집혀 보이지 않도록 상하 반전만 처리 (필요시)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) 
        {
            // 발사 각도가 90~270도 사이일 때(왼쪽 방향) 스프라이트를 뒤집어줌
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
            Deactivate();
            return;
        }

        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // 적끼리 맞거나 땅에 닿으면 제거 (플레이어가 쏜 경우 적을 맞추기 위해)
            if (owner != null && owner.CompareTag("Player") && collision.CompareTag("Enemy"))
            {
                 // 이미 위에서 Health로 처리됨
            }
            else
            {
                Deactivate();
            }
        }
    }
}
