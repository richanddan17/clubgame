using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance { get; private set; }

    [Header("설정")]
    public Tilemap tilemap;
    public int chunkSize = 16;
    public int worldHeight = 64;
    public int seed;

    [Header("바이옴")]
    public BiomeData defaultBiome;
    public List<BiomeData> possibleBiomes = new List<BiomeData>();
    public int chunksPerBiome = 10; // 몇 청크마다 바이옴이 바뀔 가능성이 있는지

    private Dictionary<int, BiomeData> chunkBiomeMap = new Dictionary<int, BiomeData>();
    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (seed == 0) seed = Random.Range(1, 99999);
    }

    private void Start()
    {
        // 시작 지점 주변 생성
        GenerateAround(Vector3.zero);
    }

    public void GenerateAround(Vector3 position)
    {
        int centerX = Mathf.FloorToInt(position.x / chunkSize);
        
        // 플레이어 주변 3개 청크 생성
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            GenerateChunk(x);
        }
    }

    [Header("동굴 세부 설정")]
    public int minTunnelHeight = 5;  // 최소 동굴 높이
    public int maxTunnelHeight = 10; // 최대 동굴 높이
    public int groundLevel = 0;      // 중앙 높이 기준점
    public float pathCurvature = 0.02f; // 길이 굽어지는 정도
    public int maxPathOffset = 15;   // 위아래로 얼마나 크게 요동칠지

    [Header("고급 랜덤 설정 (테라리아 스타일)")]
    [Range(0, 1)]
    public float wallDensity = 0.5f; // 벽이 채워질 확률 (낮을수록 구멍이 많음)
    public float secondaryNoiseScale = 0.08f; // 벽 안의 구멍들 크기

    public void GenerateChunk(int chunkX)
    {
        Vector2Int chunkKey = new Vector2Int(chunkX, 0);
        if (generatedChunks.Contains(chunkKey)) return;

        BiomeData biome = GetBiomeForChunk(chunkX);
        int startX = chunkX * chunkSize;

        for (int x = startX; x < startX + chunkSize; x++)
        {
            // 1. 주 통로(Main Path) 계산
            float pathNoise = Mathf.PerlinNoise((x + seed) * pathCurvature, seed * 0.1f);
            int currentCenterY = groundLevel + Mathf.FloorToInt((pathNoise - 0.5f) * 2f * maxPathOffset);

            float heightNoise = Mathf.PerlinNoise((x + seed) * biome.noiseScale, seed * 0.5f);
            int currentTunnelHeight = Mathf.FloorToInt(Mathf.Lerp(minTunnelHeight, maxTunnelHeight, heightNoise));

            int topY = currentCenterY + (currentTunnelHeight / 2);
            int bottomY = currentCenterY - (currentTunnelHeight / 2);

            for (int y = -worldHeight / 2; y < worldHeight / 2; y++)
            {
                // 2. 주 통로 안쪽은 무조건 비움 (플레이어 이동 보장)
                if (y <= topY && y >= bottomY)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
                else
                {
                    // 3. 주 통로 밖은 2D 노이즈로 덩어리 지형 생성 (벽에 구멍 뚫기)
                    float wallNoise = Mathf.PerlinNoise((x + seed) * secondaryNoiseScale, (y + seed) * secondaryNoiseScale);
                    
                    // 노이즈 값이 밀도보다 낮을 때만 블록 배치 (덩어리진 느낌)
                    if (wallNoise < wallDensity)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), biome.mainTile);
                    }
                    else
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), null);
                    }
                }
            }
        }

        generatedChunks.Add(chunkKey);
        Debug.Log($"고급 랜덤 동굴 생성 완료: {chunkX}");
    }

    public BiomeData GetBiomeForChunk(int chunkX)
    {
        if (chunkBiomeMap.TryGetValue(chunkX, out BiomeData cachedBiome))
            return cachedBiome;

        // 바이옴 그룹 인덱스 (chunksPerBiome 개수마다 하나씩 결정)
        int biomeIndex = Mathf.FloorToInt((float)chunkX / chunksPerBiome);
        
        // 시드와 바이옴 인덱스를 조합하여 랜덤 값 생성
        Random.State prevState = Random.state;
        Random.InitState(seed + biomeIndex);
        
        BiomeData selectedBiome = defaultBiome;
        if (possibleBiomes != null && possibleBiomes.Count > 0)
        {
            selectedBiome = possibleBiomes[Random.Range(0, possibleBiomes.Count)];
        }

        Random.state = prevState;
        chunkBiomeMap[chunkX] = selectedBiome;
        return selectedBiome;
    }
}
