using UnityEngine;

public enum SkillType { Projectile, Melee, MeleeAoE, InstantArea }

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    public int ID;
    public string SkillName;
    public float Damage;
    public float ManaCost;
    public float Cooldown;
    public Sprite Icon;
    public GameObject ProjectilePrefab;

    [Header("멀티샷 설정 (산탄 등)")]
    public int projectileCount = 1;
    public float spreadAngle = 0f;

    [Header("스킬 타입")]
    public SkillType SkillType;

    [Header("투사체 설정")]
    public float ProjectileSpeed = 15f;
    public bool UseBubbleEffect;
    public Projectile.BubbleType BubbleEffect;

    [Header("근접 설정")]
    public float MeleeRange = 1.5f;
    public float MeleeArc = 120f;
    public float HitboxLifetime = 0.15f;

    [Header("광역 설정")]
    public float AoERadius = 3f;
}
