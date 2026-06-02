using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class InventorySetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Setup Inventory UI")]
    public static void SetupInventoryUI()
    {
        // 1. 캔버스 찾기 또는 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. 인벤토리 패널(부모) 생성
        GameObject invPanel = GameObject.Find("InventoryPanel");
        if (invPanel != null) DestroyImmediate(invPanel);

        invPanel = new GameObject("InventoryPanel");
        invPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = invPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.sizeDelta = Vector2.zero;

        Image panelImg = invPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.85f);

        // 3. 제목 추가
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(invPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 40;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;

        // 4. 스킬 그리드 영역 생성
        CreateGridArea(invPanel.transform, "SkillGrid", new Vector2(0, 0.5f), new Vector2(1, 0.9f), "Skills");
        
        // 5. 아이템 그리드 영역 생성
        CreateGridArea(invPanel.transform, "ItemGrid", new Vector2(0, 0.1f), new Vector2(1, 0.5f), "Items");

        // 6. InventoryUI 스크립트 추가 및 연결 (비활성화 전에 연결!)
        InventoryUI invUI = invPanel.AddComponent<InventoryUI>();
        
        // 계층 구조에서 자식 찾기
        Transform skillContainer = invPanel.transform.Find("SkillGridContainer");
        if (skillContainer != null) invUI.skillGrid = skillContainer.Find("SkillGrid");

        Transform itemContainer = invPanel.transform.Find("ItemGridContainer");
        if (itemContainer != null) invUI.itemGrid = itemContainer.Find("ItemGrid");

        // 7. 플레이어 컨트롤러에 연결
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.inventoryPanel = invPanel;
            EditorUtility.SetDirty(player);
        }

        // 이제 비활성화
        invPanel.SetActive(false);

        // 8. 빈 InventoryManager 생성 (씬에 없으면)
        if (FindFirstObjectByType<InventoryManager>() == null)
        {
            new GameObject("InventoryManager").AddComponent<InventoryManager>();
        }

        Debug.Log("인벤토리 UI 기본 구조 및 스크립트 설정 완료!");
    }

    private static void CreateGridArea(Transform parent, string name, Vector2 min, Vector2 max, string label)
    {
        GameObject area = new GameObject(name + "Container");
        area.transform.SetParent(parent, false);
        RectTransform areaRect = area.AddComponent<RectTransform>();
        areaRect.anchorMin = min;
        areaRect.anchorMax = max;
        areaRect.sizeDelta = Vector2.zero;

        // 배경 살짝 추가 (영역 구분용)
        Image areaImg = area.AddComponent<Image>();
        areaImg.color = new Color(1, 1, 1, 0.05f);

        // 라벨 추가
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(area.transform, false);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 25;
        labelText.fontStyle = FontStyles.Bold;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.02f, 0.85f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = Vector2.zero;

        // 그리드 영역 (실제 슬롯들이 들어갈 곳)
        GameObject grid = new GameObject(name);
        grid.transform.SetParent(area.transform, false);
        RectTransform gridRect = grid.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.02f, 0.05f);
        gridRect.anchorMax = new Vector2(0.98f, 0.85f);
        gridRect.sizeDelta = Vector2.zero;

        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(100, 100); // 크기 약간 키움
        layout.spacing = new Vector2(15, 15);    // 간격 넓힘
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        
        // 자동 줄바꿈 설정 (가로 개수 제한)
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 6; // 한 줄에 6개씩 고정
    }
}
