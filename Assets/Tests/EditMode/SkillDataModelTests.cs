using NUnit.Framework;
using UnityEngine;

public class SkillDataModelTests
{
    [Test]
    public void FreshSkillData_HasExpectedDefaults()
    {
        SkillData data = ScriptableObject.CreateInstance<SkillData>();

        Assert.AreEqual(SkillType.Projectile, data.SkillType);
        Assert.AreEqual(15f, data.ProjectileSpeed);
        Assert.AreEqual(1.5f, data.MeleeRange);
        Assert.AreEqual(120f, data.MeleeArc);
        Assert.AreEqual(0.15f, data.HitboxLifetime);
        Assert.AreEqual(3f, data.AoERadius);
        Assert.IsFalse(data.UseBubbleEffect);
    }
}
