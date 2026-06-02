using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 인벤토리 및 스킬 목록을 관리하는 싱글톤 매니저
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("인벤토리 데이터")]
    public List<SkillData> acquiredSkills = new List<SkillData>();
    public List<ShopItemData> acquiredItems = new List<ShopItemData>();

    [Header("이벤트")]
    public UnityEngine.Events.UnityEvent OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 테스트용 데이터 로드 (실제 게임에서는 획득 시 추가)
        LoadTestItems();
    }

    private void LoadTestItems()
    {
        // Resources 폴더에 스킬 데이터가 있다면 자동으로 몇 개 넣어줌
        SkillData[] skills = Resources.LoadAll<SkillData>("SkillData");
        foreach (var s in skills) AddSkill(s);

        ShopItemData[] items = Resources.LoadAll<ShopItemData>("ShopItemData");
        foreach (var i in items) AddItem(i);
    }

    public void AddSkill(SkillData skill)
    {
        if (skill != null && !acquiredSkills.Contains(skill))
        {
            acquiredSkills.Add(skill);
            OnInventoryChanged?.Invoke();
        }
    }

    public void AddItem(ShopItemData item)
    {
        if (item != null)
        {
            acquiredItems.Add(item);
            OnInventoryChanged?.Invoke();
        }
    }
}
