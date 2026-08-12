using UnityEngine;

public class PoppingBullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float damage = 1f;
    public float lifeTime = 5f;
    public string poolTag = "PoppingBullet";

    [Header("References")]
    private Rigidbody2D _rb;
    private Animator _anim;
    private bool _hasHit = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        
        if (_rb == null) Debug.LogError("[PoppingBullet] Rigidbody2D가 투사체에 없습니다!");
    }

    void OnEnable()
    {
        _hasHit = false;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }

    public void Launch(Vector2 direction)
    {
        if (_rb != null)
        {
            _rb.linearVelocity = direction * speed;
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit) return;

        if (collision.CompareTag("Player") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            collision.CompareTag("Ground") || 
            collision.CompareTag("Wall"))
        {
            Explode(collision.gameObject);
        }
    }

    void Explode(GameObject target)
    {
        _hasHit = true;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        if (target.CompareTag("Player"))
        {
            if (target.TryGetComponent<Health>(out var health))
                health.TakeDamage(damage);
        }

        if (_anim != null)
        {
            _anim.SetTrigger("Explode");
            Invoke(nameof(ReturnToPool), 0.5f);
        }
        else
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        else
            Destroy(gameObject);
    }
}
