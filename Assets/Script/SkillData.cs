using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    public int ID;
    public string SkillName;
    public float Damage;
    public float ManaCost;
    public float Cooldown;
    public GameObject ProjectilePrefab;
}
