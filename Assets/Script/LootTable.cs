using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootEntry
{
    public string name;
    public ScriptableObject itemData; // SkillData 또는 ShopItemData
    [Range(0, 100)] public float dropChance;
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Scriptable Objects/LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> lootEntries = new List<LootEntry>();

    public ScriptableObject GetRandomDrop()
    {
        float roll = Random.Range(0f, 100f);
        float cumulativeChance = 0f;

        foreach (var entry in lootEntries)
        {
            cumulativeChance += entry.dropChance;
            if (roll <= cumulativeChance)
            {
                return entry.itemData;
            }
        }
        return null;
    }
}
