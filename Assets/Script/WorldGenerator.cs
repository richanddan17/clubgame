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

    public void GenerateChunk(int chunkX)
    {
        Vector2Int chunkKey = new Vector2Int(chunkX, 0);
        if (generatedChunks.Contains(chunkKey)) return;

        // 해당 청크의 바이옴 결정 (일단은 기본 바이옴 사용, 추후 확장 가능)
        BiomeData biome = GetBiomeForChunk(chunkX);
        
        int startX = chunkX * chunkSize;

        for (int x = startX; x < startX + chunkSize; x++)
        {
            for (int y = -worldHeight / 2; y < worldHeight / 2; y++)
            {
                // Perlin Noise를 이용한 동굴 생성 로직
                float noiseValue = Mathf.PerlinNoise((x + seed) * biome.noiseScale, (y + seed) * biome.noiseScale);
                
                // 노이즈 값이 밀도보다 높으면 블록 배치 (1: 꽉 참, 0: 텅 빔)
                if (noiseValue < biome.caveDensity)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), biome.mainTile);
                }
            }
        }

        generatedChunks.Add(chunkKey);
        Debug.Log($"청크 생성 완료: {chunkX} (바이옴: {biome.biomeName})");
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
