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

        Health health = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>();
        
        // 다양한 적 스크립트 대응
        EnemyController enemy = collision.GetComponent<EnemyController>() ?? collision.GetComponentInParent<EnemyController>();
        Slime slime = collision.GetComponent<Slime>() ?? collision.GetComponentInParent<Slime>();
        RangedEnemy wizard = collision.GetComponent<RangedEnemy>() ?? collision.GetComponentInParent<RangedEnemy>();

        if (health != null)
        {
            health.TakeDamage(damage, transform.position);

            // 특수 효과 적용
            if (isSpecial)
            {
                if (enemy != null) enemy.ApplyEffect(bubbleType);
                if (slime != null) slime.ApplyEffect(bubbleType);
                if (wizard != null) wizard.ApplyEffect(bubbleType);
            }

            Deactivate();
            return;
        }

        // Health는 없지만 컨트롤러만 있는 경우나 지형 충돌 처리
        bool isAnyEnemy = (enemy != null || slime != null || wizard != null);
        if (isAnyEnemy || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (isSpecial)
            {
                if (enemy != null) enemy.ApplyEffect(bubbleType);
                if (slime != null) slime.ApplyEffect(bubbleType);
                if (wizard != null) wizard.ApplyEffect(bubbleType);
            }
            Deactivate();
        }
    }
}
