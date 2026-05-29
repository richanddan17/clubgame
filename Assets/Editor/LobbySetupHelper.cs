using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbySetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Lobby/Setup Portal UI")]
    public static void SetupPortalUI()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("포탈로 만들 오브젝트를 하이어라키에서 선택하고 실행해주세요!");
            return;
        }

        // 1. LevelPortal 스크립트 추가 (없을 경우)
        LevelPortal portal = selected.GetComponent<LevelPortal>();
        if (portal == null)
        {
            portal = selected.AddComponent<LevelPortal>();
        }

        // 2. 이미 UI가 있는지 확인
        if (portal.instructionUI != null)
        {
            Debug.LogWarning("이미 Instruction UI가 설정되어 있습니다.");
            return;
        }

        // 3. Canvas 생성
        GameObject canvasObj = new GameObject("Portal_UI_Canvas");
        canvasObj.transform.SetParent(selected.transform);
        canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0); // 머리 위 위치

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // RectTransform 설정
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 4. TextMeshPro 생성
        GameObject textObj = new GameObject("PressE_Text");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Press E to Enter";
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.raycastTarget = false;

        // 5. 스크립트에 연결
        portal.instructionUI = canvasObj;
        
        // [수정] 에디터에서 보일 수 있도록 활성화 상태로 생성 (플레이 시에는 스크립트가 제어함)
        canvasObj.SetActive(true);

        Undo.RegisterCreatedObjectUndo(canvasObj, "Setup Portal UI");
        Debug.Log(selected.name + " 포탈 UI 설정 완료!");
    }

    [MenuItem("Custom Tools/Lobby/Setup ObjectPooler")]
    public static void SetupObjectPooler()
    {
        // 1. 이미 존재하는지 확인
        if (ObjectPooler.Instance != null || GameObject.Find("ObjectPooler") != null)
        {
            Debug.LogWarning("이미 ObjectPooler가 씬에 존재합니다.");
            return;
        }

        // 2. ObjectPooler 생성
        GameObject poolerObj = new GameObject("ObjectPooler");
        ObjectPooler pooler = poolerObj.AddComponent<ObjectPooler>();

        // 3. 풀 리스트 설정 (Blue, Red, Yellow)
        pooler.pools = new List<ObjectPooler.Pool>();

        string[] colors = { "blue", "red", "yellow" };
        string[] tags = { "Blue", "Red", "Yellow" };

        for (int i = 0; i < 3; i++)
        {
            ObjectPooler.Pool newPool = new ObjectPooler.Pool();
            newPool.tag = tags[i];
            
            // 프리팹 찾기
            string prefabPath = $"Assets/Prefabs/BubbleProjectile_{colors[i]}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"{prefabPath} 경로에서 프리팹을 찾을 수 없습니다! 경로를 확인해주세요.");
            }
            
            newPool.prefab = prefab;
            newPool.size = 20; // 기본 20개 생성
            pooler.pools.Add(newPool);
        }

        Undo.RegisterCreatedObjectUndo(poolerObj, "Setup ObjectPooler");
        Debug.Log("Lobby Scene에 ObjectPooler 설정 완료! (Blue, Red, Yellow)");
    }
}
