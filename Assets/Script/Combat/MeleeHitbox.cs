using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 근접 공격 판정용 짧은 생명주기 트리거 히트박스.
/// Initialize 호출 시 지정된 lifetime 이후 자동 파괴되며,
/// 각 대상(Health)에 한 번만 데미지를 입히고 선택적으로 거품 효과를 적용합니다.
/// </summary>
public class MeleeHitbox : MonoBehaviour
{
    private float damage;
    private GameObject owner;
    private bool useBubble;
    private Projectile.BubbleType bubbleType;
    private readonly HashSet<Health> _hitTargets = new HashSet<Health>();

    public void Initialize(float damage, GameObject owner, float lifeTime, float range, bool useBubble, Projectile.BubbleType bubbleType)
    {
        this.damage = damage;
        this.owner = owner;
        this.useBubble = useBubble;
        this.bubbleType = bubbleType;

        transform.localScale = new Vector3(range, range, 1f);
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 주인(플레이어) 및 주인 참조는 무시 (Projectile.cs 패턴)
        if (collision.CompareTag("Player")) return;
        if (owner != null && collision.gameObject == owner) return;

        Health health = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>();
        if (health == null) return;

        // 각 대상은 한 번만 타격
        if (!_hitTargets.Add(health)) return;

        health.TakeDamage(damage, transform.position);

        if (useBubble)
        {
            IBubbleAffectable affectable = collision.GetComponent<IBubbleAffectable>() ?? collision.GetComponentInParent<IBubbleAffectable>();
            if (affectable != null)
                affectable.ApplyBubbleEffect(bubbleType);
        }
    }
}
