using NUnit.Framework;
using UnityEngine;
using System.Reflection;

[TestFixture]
public class GimmickTests
{
    private System.Type GetType(string name)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    private MonoBehaviour AddComponentByName(GameObject go, string typeName)
    {
        var type = GetType(typeName);
        Assert.IsNotNull(type, $"Type '{typeName}' not found");
        return go.AddComponent(type) as MonoBehaviour;
    }

    [Test]
    public void Hazard_CanBeCreated()
    {
        var go = new GameObject("TestHazard");
        var comp = AddComponentByName(go, "Hazard");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SpringPad_CanBeCreated()
    {
        var go = new GameObject("TestSpringPad");
        var comp = AddComponentByName(go, "SpringPad");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void MovingPlatform_CanBeCreated()
    {
        var go = new GameObject("TestMovingPlatform");
        var comp = AddComponentByName(go, "MovingPlatform");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Checkpoint_CanBeCreated()
    {
        var go = new GameObject("TestCheckpoint");
        var comp = AddComponentByName(go, "Checkpoint");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Door_CanBeCreated()
    {
        var go = new GameObject("TestDoor");
        var comp = AddComponentByName(go, "Door");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Switch_CanBeCreated()
    {
        var go = new GameObject("TestSwitch");
        var comp = AddComponentByName(go, "Switch");
        Assert.IsNotNull(comp);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void BossBase_IsAbstract()
    {
        var type = GetType("BossBase");
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsAbstract);
    }

    [Test]
    public void GimmickBase_IsAbstract()
    {
        var type = GetType("GimmickBase");
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsAbstract);
    }

    [Test]
    public void SugarOctopusBoss_InheritsBossBase()
    {
        var bossType = GetType("SugarOctopusBoss");
        var baseType = GetType("BossBase");
        Assert.IsNotNull(bossType);
        Assert.IsNotNull(baseType);
        Assert.IsTrue(baseType.IsAssignableFrom(bossType));
    }

    [Test]
    public void Hazard_InheritsGimmickBase()
    {
        var hazardType = GetType("Hazard");
        var baseType = GetType("GimmickBase");
        Assert.IsNotNull(hazardType);
        Assert.IsNotNull(baseType);
        Assert.IsTrue(baseType.IsAssignableFrom(hazardType));
    }

    [Test]
    public void SpringPad_InheritsGimmickBase()
    {
        var springType = GetType("SpringPad");
        var baseType = GetType("GimmickBase");
        Assert.IsNotNull(springType);
        Assert.IsNotNull(baseType);
        Assert.IsTrue(baseType.IsAssignableFrom(springType));
    }

    [Test]
    public void MovingPlatform_InheritsGimmickBase()
    {
        var mpType = GetType("MovingPlatform");
        var baseType = GetType("GimmickBase");
        Assert.IsNotNull(mpType);
        Assert.IsNotNull(baseType);
        Assert.IsTrue(baseType.IsAssignableFrom(mpType));
    }
}
