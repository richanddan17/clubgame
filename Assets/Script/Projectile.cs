using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum BubbleType { Blue, Red, Yellow }

    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private string poolTag = "Projectile";
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
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        else
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

        IBubbleAffectable affectable = collision.GetComponent<IBubbleAffectable>() ?? collision.GetComponentInParent<IBubbleAffectable>();
        Health health = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage, transform.position);

            if (isSpecial && affectable != null)
                affectable.ApplyBubbleEffect(bubbleType);

            Deactivate();
            return;
        }

        if (affectable != null || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (isSpecial && affectable != null)
                affectable.ApplyBubbleEffect(bubbleType);
            Deactivate();
        }
    }
}
