using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;

public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("바이옴 데이터")]
    public BiomeData currentBiome;
    public BiomeData defaultBiome;

    [Header("참조")]
    public Tilemap[] targetTilemaps;
    public Transform playerTransform;

    public event Action<BiomeData> OnBiomeChanged;

    private int lastChunkX = int.MinValue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (defaultBiome != null)
        {
            ChangeBiome(defaultBiome);
        }

        if (targetTilemaps == null || targetTilemaps.Length == 0)
        {
            targetTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        }
    }

    private void Update()
    {
        if (playerTransform == null || WorldGenerator.Instance == null) return;

        // 플레이어의 현재 청크 위치 계산
        int currentChunkX = Mathf.FloorToInt(playerTransform.position.x / WorldGenerator.Instance.chunkSize);

        if (currentChunkX != lastChunkX)
        {
            lastChunkX = currentChunkX;
            UpdateBiomeAtLocation(currentChunkX);
            
            // 주변 청크 추가 생성 요청
            WorldGenerator.Instance.GenerateAround(playerTransform.position);
        }
    }

    private void UpdateBiomeAtLocation(int chunkX)
    {
        // WorldGenerator에 청크의 바이옴을 물어봄
        BiomeData biomeAtLoc = WorldGenerator.Instance.GetBiomeForChunk(chunkX);
        if (biomeAtLoc != null)
        {
            ChangeBiome(biomeAtLoc);
        }
    }

    public void ChangeBiome(BiomeData newData)
    {
        if (newData == null || currentBiome == newData) return;

        currentBiome = newData;
        Debug.Log($"바이옴 전환: {currentBiome.biomeName} (청크 위치)");
        
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
    }

    // 구역 감지(수동) 호환성을 위한 메서드
    public void EnterZone(BiomeZone zone)
    {
        if (zone != null && zone.biomeData != null)
        {
            ChangeBiome(zone.biomeData);
        }
    }

    public void ExitZone(BiomeZone zone)
    {
        // 절차적 생성 시스템에서는 Update에서 자동으로 현재 위치의 바이옴을 체크하므로 
        // 여기서는 별도의 처리가 필요하지 않을 수 있습니다.
    }
}
