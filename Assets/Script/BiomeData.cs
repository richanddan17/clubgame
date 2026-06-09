using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBiomeData", menuName = "Scriptable Objects/BiomeData")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    
    [Header("절차적 생성 설정 (Procedural Gen)")]
    public TileBase mainTile;        // 해당 바이옴의 주력 블록 타일
    [Range(0, 1)]
    public float caveDensity = 0.5f; // 동굴 밀도 (0에 가까울수록 동굴이 많아짐)
    public float noiseScale = 0.05f; // 동굴 모양의 복잡도 (Perlin Noise 스케일)
    
    [Header("비주얼 설정")]
    public Sprite[] backgroundLayers; // 패럴랙스 배경 레이어들
    public Color tilemapTint = Color.white; // 타일맵 환경 색상
    
    [Header("생태계 설정")]
    public EnemyData[] allowedEnemies; // 해당 바이옴에서 등장하는 적 리스트
    
    [Header("기타")]
    public Color ambientColor = Color.white; // 바이옴 분위기 색상 (선택사항)
}
