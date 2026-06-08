using UnityEngine;
using UnityEditor;
using System.IO;

public class BiomeSetupHelper : EditorWindow
{
    [MenuItem("Custom Tools/Generate Preliminary Biomes", false, -84)]
    public static void GenerateBiomes()
    {
        string folderPath = "Assets/Resources/BiomeData";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 1. 시럽 동굴 (Syrup Cave)
        CreateBiomeAsset(folderPath, "Underground_SyrupCave", "시럽 동굴", new Color(0.3f, 0.1f, 0.5f));
        
        // 2. 지하 정원 (Overgrown Basement)
        CreateBiomeAsset(folderPath, "Underground_Overgrown", "지하 정원", new Color(0.1f, 0.4f, 0.2f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Biome Generation", "예비 바이옴 데이터 2종이 Assets/Resources/BiomeData에 생성되었습니다.", "확인");
    }

    private static void CreateBiomeAsset(string path, string fileName, string biomeName, Color ambient)
    {
        string fullPath = $"{path}/{fileName}.asset";
        
        // 이미 존재하면 건너뜀
        if (File.Exists(fullPath)) return;

        BiomeData data = ScriptableObject.CreateInstance<BiomeData>();
        data.biomeName = biomeName;
        data.ambientColor = ambient;

        // Karsiori 에셋 자동 연결 시도 (예시: 배경 이미지 하나라도 찾아 넣기)
        string backgroundPath = "Assets/Sprite/tilemap/karsiori/TileMap/Backgrounds/BACKGROUND.png";
        Sprite defaultBg = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
        if (defaultBg != null)
        {
            data.backgroundLayers = new Sprite[] { defaultBg };
        }

        AssetDatabase.CreateAsset(data, fullPath);
        Debug.Log($"바이옴 생성 완료: {fullPath}");
    }
}
