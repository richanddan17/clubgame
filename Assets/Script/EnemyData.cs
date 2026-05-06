using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int ID;
    public string EnemyName;
    public float HP;
    public float Speed;
    public float Damage;
    public float DetectionRange;
    public float AttackInterval;
}
