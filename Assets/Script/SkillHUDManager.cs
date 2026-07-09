using UnityEngine;
using System.Collections.Generic;

public class SkillHUDManager : MonoBehaviour
{
    public static SkillHUDManager Instance { get; private set; }

    [SerializeField] private SkillSlotUI[] slots; // Size 4 (Z, X, C, V)
    private string[] keys = { "Z", "X", "C", "V" };

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].Setup(keys[i]);
        }
    }

    public void UpdateSkillIcon(int index, SkillData data)
    {
        if (index >= 0 && index < slots.Length && slots[index] != null)
        {
            slots[index].SetSkill(data);
        }
    }

    public void TriggerCooldown(int index)
    {
        if (index >= 0 && index < slots.Length && slots[index] != null)
        {
            slots[index].StartCooldown();
        }
    }
}
