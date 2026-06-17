using UnityEngine;

public class PoppingBullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float damage = 30f;
    public float lifeTime = 5f;

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
        
        Destroy(gameObject, lifeTime);
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

        // 무엇과 부딪혔는지 로그 출력
        Debug.Log("[PoppingBullet] 충돌 감지: " + collision.gameObject.name);

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
        Debug.Log("[PoppingBullet] 폭발! 데미지 처리 및 애니메이션 재생");
        
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        if (target.CompareTag("Player"))
        {
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (_anim != null)
        {
            _anim.SetTrigger("Explode");
            Destroy(gameObject, 0.5f); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
