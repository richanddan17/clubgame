using UnityEngine;

/// <summary>
/// 함정 시스템. 즉사/HP데미지 설정 가능, damageInterval로 연속 히트 방지.
/// </summary>
public class Hazard : GimmickBase
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private bool isInstantKill = false;
    [SerializeField] private float damageInterval = 1f;

    private float _lastDamageTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (Time.time - _lastDamageTime < damageInterval) return;

        var health = other.GetComponent<Health>();
        if (health == null) health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return;

        health.TakeDamage(isInstantKill ? 9999f : damage, transform.position);
        _lastDamageTime = Time.time;
    }
}
