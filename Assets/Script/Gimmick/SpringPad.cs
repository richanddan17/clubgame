using UnityEngine;

/// <summary>
/// 스프링/점프 패드. 접촉 시 플레이어를 위로 튕겨올립니다.
/// 기본 힘 28 (점프력 14의 2배).
/// </summary>
public class SpringPad : GimmickBase
{
    [SerializeField] private float springForce = 28f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.ApplySpringForce(springForce);
    }
}
