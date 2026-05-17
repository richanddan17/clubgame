using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class HUDSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Setup Player Health HUD")]
    public static void SetupPlayerHUD()
    {
        // 1. 캔버스 확인 또는 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. 플레이어 찾기
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Scene에 PlayerController가 없습니다! 플레이어를 먼저 배치해주세요.");
            return;
        }
        Health playerHealth = player.GetComponent<Health>();

        // 3. 체력바(Slider) UI 생성
        GameObject sliderObj = GameObject.Find("Player_HP_Bar");
        if (sliderObj != null) DestroyImmediate(sliderObj);

        sliderObj = new GameObject("Player_HP_Bar");
        sliderObj.transform.SetParent(canvas.transform, false);

        // 위치 설정 (좌측 상단)
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(300, 30);

        Slider slider = sliderObj.AddComponent<Slider>();

        // 배경(Background) 생성
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 채우기 영역(Fill Area) 생성
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10);

        // 실제 채우기(Fill) 이미지 생성
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.green;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImg;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // 4. 텍스트(TMP) 추가
        GameObject textObj = new GameObject("HP_Text");
        textObj.transform.SetParent(sliderObj.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 20;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.text = "100 / 100";
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // 5. HealthBar 스크립트 연결
        HealthBar hb = sliderObj.AddComponent<HealthBar>();
        
        // SerializedObject를 사용하여 강제 할당 (인스펙터 반영)
        SerializedObject so = new SerializedObject(hb);
        so.FindProperty("targetHealth").objectReferenceValue = playerHealth;
        so.FindProperty("hpSlider").objectReferenceValue = slider;
        so.FindProperty("hpText").objectReferenceValue = tmpText;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Setup Player HUD");
        Selection.activeGameObject = sliderObj;

        Debug.Log("플레이어 체력바(HUD) 설정 완료!");
        EditorUtility.DisplayDialog("Success", "플레이어 체력바가 생성되었습니다.\n좌측 상단에서 확인하세요!", "확인");
    }
}
