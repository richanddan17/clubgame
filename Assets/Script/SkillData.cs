using UnityEngine;

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
}
