using UnityEngine;

/// <summary>
/// 필드에 드랍된 아이템 오브젝트. 플레이어가 닿으면 획득합니다.
/// </summary>
public class LootDroppedItem : MonoBehaviour
{
    public ScriptableObject itemData;
    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(ScriptableObject data)
    {
        itemData = data;
        
        // 아이콘 설정
        if (data is SkillData skill)
        {
            if (_sr != null) _sr.sprite = skill.Icon;
            gameObject.name = "Skill_" + skill.SkillName;
        }
        else if (data is ShopItemData item)
        {
            if (_sr != null) _sr.sprite = item.Icon;
            gameObject.name = "Item_" + item.ItemName;
        }
        else if (data is ClueData clue)
        {
            if (_sr != null) _sr.sprite = clue.ClueIcon;
            gameObject.name = "Clue_" + clue.ClueTitle;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (itemData is SkillData skill)
            {
                InventoryManager.Instance.AddSkill(skill);
                Debug.Log($"Acquired Skill: {skill.SkillName}");
            }
            else if (itemData is ShopItemData item)
            {
                InventoryManager.Instance.AddItem(item);
                Debug.Log($"Acquired Item: {item.ItemName}");
            }
            else if (itemData is ClueData clue)
            {
                InventoryManager.Instance.AddClue(clue);
                Debug.Log($"Acquired Clue: {clue.ClueTitle}");
            }

            Destroy(gameObject);
        }
    }
}
