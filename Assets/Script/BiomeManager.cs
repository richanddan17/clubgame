using UnityEngine;
using System;

public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("바이옴 데이터")]
    public BiomeData currentBiome;
    public BiomeData undergroundBiome; // 지하 공장 (시작)
    public BiomeData surfaceBiome;     // 지상

    [Header("전환 설정")]
    public float transitionTriggerX = 100f; // 지상으로 전환될 X 좌표 (임시)

    public event Action<BiomeData> OnBiomeChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 지하 공장에서 시작
        if (undergroundBiome != null)
        {
            ChangeBiome(undergroundBiome);
        }
    }

    private void Update()
    {
        // 플레이어 위치 체크 (추후 트리거 오브젝트 방식으로 변경 가능)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentBiome == undergroundBiome)
        {
            if (player.transform.position.x > transitionTriggerX)
            {
                ChangeBiome(surfaceBiome);
            }
        }
    }

    public void ChangeBiome(BiomeData newData)
    {
        if (newData == null || currentBiome == newData) return;

        currentBiome = newData;
        Debug.Log($"바이옴 변경: {currentBiome.biomeName}");
        
        OnBiomeChanged?.Invoke(currentBiome);
    }
}
