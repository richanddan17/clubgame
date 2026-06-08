using UnityEngine;

[CreateAssetMenu(fileName = "NewBiomeData", menuName = "Scriptable Objects/BiomeData")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    
    [Header("비주얼 설정")]
    public Sprite[] backgroundLayers; // 패럴랙스 배경 레이어들
    
    [Header("생태계 설정")]
    public EnemyData[] allowedEnemies; // 해당 바이옴에서 등장하는 적 리스트
    
    [Header("기타")]
    public Color ambientColor = Color.white; // 바이옴 분위기 색상 (선택사항)
}
