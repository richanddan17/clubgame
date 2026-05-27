using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private bool isFacingRight = true;
    private GameObject owner; // 투사체를 쏜 주인

<<<<<<< HEAD
    public void Initialize(float damageAmount, bool facingRight, float customSpeed = -1f, Vector3? customScale = null)
=======
    public void Initialize(float damageAmount, bool facingRight, GameObject shooter = null)
>>>>>>> origin/PBE
    {
        damage = damageAmount;
        isFacingRight = facingRight;
        owner = shooter;
        
<<<<<<< HEAD
        if (customSpeed > 0) speed = customSpeed;
        if (customScale.HasValue) transform.localScale = customScale.Value;
        else transform.localScale = Vector3.one; // 기본 크기 복구 (풀링 대응)
        
        // 방향에 따라 스프라이트 뒤집기 또는 회전
        // 주의: 이미 PlayerController에서 회전값을 주어 생성할 수 있으므로, 
        // 여기서는 isFacingRight에 따른 추가 처리가 필요한지 확인이 필요함.
        // 기존 로직 유지:
        if (!isFacingRight && transform.rotation.eulerAngles.z == 0)
=======
        // [수정] Spawner가 이미 Rotation을 설정했으므로 여기서 강제로 덮어쓰지 않음.
        // 스프라이트가 뒤집혀 보이지 않도록 상하 반전만 처리 (필요시)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) 
>>>>>>> origin/PBE
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
<<<<<<< HEAD
        // 플레이어 자신의 투사체에 데미지를 입지 않도록 플레이어 태그 무시
        if (collision.CompareTag("Player")) return;
=======
        // 주인(쏜 사람)은 무시
        if (owner != null && collision.gameObject == owner) return;
>>>>>>> origin/PBE

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
