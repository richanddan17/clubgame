using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SkillHUDSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Setup Skill HUD (ZXCV)")]
    public static void SetupSkillHUD()
    {
        // 1. Canvas 찾기
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다!");
            return;
        }

        // 2. HUD Root 생성
        GameObject hudRoot = new GameObject("Skill_HUD_BottomLeft");
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 0);
        rootRect.anchorMax = new Vector2(0, 0);
        rootRect.pivot = new Vector2(0, 0);
        rootRect.anchoredPosition = new Vector2(20, 20);
        rootRect.sizeDelta = new Vector2(400, 100);

        HorizontalLayoutGroup layout = hudRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        SkillHUDManager manager = hudRoot.AddComponent<SkillHUDManager>();
        SkillSlotUI[] slots = new SkillSlotUI[4];
        string[] keys = { "Z", "X", "C", "V" };

        for (int i = 0; i < 4; i++)
        {
            // Slot Container
            GameObject slotObj = new GameObject($"SkillSlot_{keys[i]}");
            slotObj.transform.SetParent(hudRoot.transform, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(80, 80);

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(slotObj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // Icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slotObj.transform, false);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.preserveAspect = true;
            icon.GetComponent<RectTransform>().anchorMin = new Vector2(0.1f, 0.1f);
            icon.GetComponent<RectTransform>().anchorMax = new Vector2(0.9f, 0.9f);
            icon.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // Cooldown Overlay
            GameObject cd = new GameObject("CooldownOverlay");
            cd.transform.SetParent(slotObj.transform, false);
            Image cdImg = cd.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0.7f);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillOrigin = (int)Image.Origin360.Top;
            cdImg.fillAmount = 0;
            cd.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            cd.GetComponent<RectTransform>().anchorMax = Vector2.one;
            cd.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // Key Text
            GameObject txt = new GameObject("KeyText");
            txt.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = keys[i];
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.BottomRight;
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = new Vector2(-5, -5);

            SkillSlotUI slotScript = slotObj.AddComponent<SkillSlotUI>();
            
            // SerializeObject를 통한 참조 할당
            SerializedObject so = new SerializedObject(slotScript);
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("cooldownOverlay").objectReferenceValue = cdImg;
            so.FindProperty("keyText").objectReferenceValue = tmp;
            so.ApplyModifiedProperties();

            slots[i] = slotScript;
        }

        SerializedObject mso = new SerializedObject(manager);
        SerializedProperty slotsProp = mso.FindProperty("slots");
        slotsProp.arraySize = 4;
        for (int i = 0; i < 4; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        mso.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(hudRoot, "Setup Skill HUD");
        Debug.Log("스킬 HUD(ZXCV) 설정 완료! 왼쪽 하단을 확인하세요.");
    }
}
