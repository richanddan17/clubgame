using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("설정")]
    public GameObject enemyPrefab; // 적 기본 프리팹
    public float spawnInterval = 3f;
    public float spawnRange = 10f;
    public int maxEnemyCount = 5;

    private List<EnemyData> enemyDataList = new List<EnemyData>();
    private float timer;

    private void Start()
    {
        // Resources에서 로드된 모든 적 데이터를 리스트에 담음
        EnemyData[] loadedData = Resources.LoadAll<EnemyData>("EnemyData");
        enemyDataList.AddRange(loadedData);

        if (enemyDataList.Count == 0)
        {
            Debug.LogWarning("EnemyData를 찾을 수 없습니다. Import를 먼저 진행하세요.");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0;
            if (GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemyCount)
            {
                SpawnRandomEnemy();
            }
        }
    }

    private void SpawnRandomEnemy()
    {
        // BiomeManager에서 현재 바이옴의 적 리스트 가져오기
        EnemyData[] currentPossibleEnemies = null;
        if (BiomeManager.Instance != null && BiomeManager.Instance.currentBiome != null)
        {
            currentPossibleEnemies = BiomeManager.Instance.currentBiome.allowedEnemies;
        }

        if (currentPossibleEnemies == null || currentPossibleEnemies.Length == 0)
        {
            // 바이옴에 적이 설정되어 있지 않으면 전역 리스트(Resources/EnemyData) 사용
            if (enemyDataList.Count == 0)
            {
                // 실시간으로 다시 로드 시도
                EnemyData[] loadedData = Resources.LoadAll<EnemyData>("EnemyData");
                enemyDataList.AddRange(loadedData);
                if (enemyDataList.Count == 0) return;
            }
            currentPossibleEnemies = enemyDataList.ToArray();
        }

        // 랜덤 데이터 선택
        EnemyData randomData = currentPossibleEnemies[UnityEngine.Random.Range(0, currentPossibleEnemies.Length)];

        // 특수 프리팹 확인 (ID_Name 또는 Name 형태)
        string prefabName = randomData.EnemyName;
        if (prefabName.Contains("_")) prefabName = prefabName.Split('_')[1];

        GameObject specializedPrefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
        
        // Resources에 없으면 Assets/Prefabs에서 직접 로드 시도 (Editor 전용)
        if (specializedPrefab == null)
        {
            #if UNITY_EDITOR
            specializedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{randomData.EnemyName}.prefab");
            if (specializedPrefab == null && randomData.EnemyName.Contains("Slime"))
                specializedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Slime.prefab");
            #endif
        }

        if (specializedPrefab != null) prefabToSpawn = specializedPrefab;
        if (prefabToSpawn == null) return;

        // 플레이어 주변 랜덤 위치 계산
        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-spawnRange, spawnRange), 0, 0);
        
        // 생성 및 데이터 주입
        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        enemyObj.tag = "Enemy";
        
        // Slime 스크립트가 있으면 속도 동기화
        Slime slimeScript = enemyObj.GetComponent<Slime>();
        if (slimeScript != null) slimeScript.speed = randomData.Speed;

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.Initialize(randomData);
        }
    }
}
