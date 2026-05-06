using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class TilemapSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Setup Tilemap (Ground)", false, -85)]
    public static void SetupTilemap()
    {
        Debug.Log("--- 타일맵 설정 시작 ---");

        // 1. Grid 확인 또는 생성
        Grid grid = Object.FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            GameObject gridObj = new GameObject("Grid");
            grid = gridObj.AddComponent<Grid>();
            Debug.Log("Grid 오브젝트를 생성했습니다.");
        }

        // 2. Ground Tilemap 확인 또는 생성
        Tilemap groundTilemap = null;
        foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
        {
            if (tm.gameObject.name == "Ground_Tilemap")
            {
                groundTilemap = tm;
                break;
            }
        }

        if (groundTilemap == null)
        {
            GameObject tmObj = new GameObject("Ground_Tilemap");
            tmObj.transform.SetParent(grid.transform);
            groundTilemap = tmObj.AddComponent<Tilemap>();
            tmObj.AddComponent<TilemapRenderer>();
            
            // 충돌 설정
            var collider = tmObj.AddComponent<TilemapCollider2D>();
            var composite = tmObj.AddComponent<CompositeCollider2D>();
            var rb = tmObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;

            // 레이어 설정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                Debug.LogWarning("'Ground' 레이어가 존재하지 않습니다. 먼저 'EMERGENCY FIX ALL' 도구를 실행하거나 수동으로 생성하세요.");
            }
            else
            {
                tmObj.layer = groundLayer;
            }

            Debug.Log("Ground_Tilemap 오브젝트를 생성하고 설정을 완료했습니다.");
        }
        else
        {
            Debug.Log("이미 Ground_Tilemap이 존재합니다.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Tilemap Setup Complete", "타일맵 설정이 완료되었습니다!\n1. Grid/Ground_Tilemap 오브젝트 확인\n2. 'Ground' 레이어 설정 확인\n3. Tile Palette에서 타일을 그려보세요.", "확인");
    }
}
