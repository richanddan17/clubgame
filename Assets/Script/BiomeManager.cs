using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;

public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("바이옴 데이터")]
    public BiomeData currentBiome;
    public BiomeData defaultBiome; // 기본 바이옴

    [Header("참조")]
    public Tilemap[] targetTilemaps; // 틴트를 적용할 타일맵들

    public event Action<BiomeData> OnBiomeChanged;

    private Stack<BiomeZone> zoneStack = new Stack<BiomeZone>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 초기 바이옴 설정
        if (defaultBiome != null)
        {
            ChangeBiome(defaultBiome);
        }

        // 씬 내의 모든 타일맵 자동 검색 (설정되지 않은 경우)
        if (targetTilemaps == null || targetTilemaps.Length == 0)
        {
            targetTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        }
    }

    public void EnterZone(BiomeZone zone)
    {
        if (zone == null || zone.biomeData == null) return;
        
        zoneStack.Push(zone);
        ChangeBiome(zone.biomeData);
    }

    public void ExitZone(BiomeZone zone)
    {
        if (zoneStack.Count == 0) return;

        // 스택에서 해당 구역 제거 (일반적으로는 가장 위쪽일 것임)
        List<BiomeZone> temp = new List<BiomeZone>(zoneStack);
        temp.Remove(zone);
        
        zoneStack.Clear();
        for (int i = temp.Count - 1; i >= 0; i--)
        {
            zoneStack.Push(temp[i]);
        }

        // 새로운 최상단 바이옴 적용
        if (zoneStack.Count > 0)
        {
            ChangeBiome(zoneStack.Peek().biomeData);
        }
        else
        {
            ChangeBiome(defaultBiome);
        }
    }

    public void ChangeBiome(BiomeData newData)
    {
        if (newData == null || currentBiome == newData) return;

        currentBiome = newData;
        Debug.Log($"바이옴 변경: {currentBiome.biomeName}");
        
        ApplyVisuals(currentBiome);
        OnBiomeChanged?.Invoke(currentBiome);
    }

    private void ApplyVisuals(BiomeData data)
    {
        if (targetTilemaps == null) return;

        foreach (var tm in targetTilemaps)
        {
            if (tm != null)
            {
                tm.color = data.tilemapTint;
            }
        }

        // 필요한 경우 라이팅(Ambient Color) 등 추가 처리
        // RenderSettings.ambientLight = data.ambientColor;
    }
}
