using UnityEngine;

public interface IBubbleAffectable
{
    void ApplyStun(float duration);
    void ApplyBubbleEffect(Projectile.BubbleType type);
}
