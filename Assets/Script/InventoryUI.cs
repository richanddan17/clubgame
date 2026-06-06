using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 데이터를 실제 UI에 표시해주는 클래스
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("그리드 참조")]
    public Transform skillGrid;
    public Transform itemGrid;

    [Header("슬롯 프리팹 (임시 생성)")]
    private GameObject slotPrefab;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged.AddListener(UpdateUI);
        }
        
        CreateDefaultSlotPrefab();
        UpdateUI();
    }

    private void CreateDefaultSlotPrefab()
    {
        // 코드상에서 간단한 슬롯 프리팹 생성 (이미지 표시용)
        slotPrefab = new GameObject("InventorySlot");
        slotPrefab.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
        slotPrefab.AddComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotPrefab.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true;
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = new Vector2(-10, -10);

        slotPrefab.SetActive(false);
    }

    public void UpdateUI()
    {
        if (InventoryManager.Instance == null) return;

        // 기존 슬롯 제거
        ClearGrid(skillGrid);
        ClearGrid(itemGrid);

        // 스킬 표시
        foreach (var skill in InventoryManager.Instance.acquiredSkills)
        {
            CreateSlot(skillGrid, skill.Icon);
        }

        // 아이템 표시
        foreach (var item in InventoryManager.Instance.acquiredItems)
        {
            CreateSlot(itemGrid, item.Icon);
        }
    }

    private void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateSlot(Transform grid, Sprite icon)
    {
        if (grid == null) return;
        GameObject newSlot = Instantiate(slotPrefab, grid);
        newSlot.SetActive(true);
        
        Image iconImg = newSlot.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg != null)
        {
            if (icon != null) iconImg.sprite = icon;
            else iconImg.color = new Color(1, 1, 1, 0.1f); // 아이콘 없으면 투명하게
        }
    }
}
