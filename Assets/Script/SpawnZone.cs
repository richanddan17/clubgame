using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 특정 구역 내에서 몬스터를 스폰하고 관리합니다.
/// 플레이어가 구역에 진입하거나 특정 조건에서 몬스터를 리젠시킵니다.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject[] enemyPrefabs;
    public int maxEnemies = 3;
    public float spawnRadius = 5f;
    public float respawnCooldown = 10f;

    [Header("활성화 조건")]
    public float activationRange = 20f; // 플레이어와의 거리

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Transform player;
    private float respawnTimer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 플레이어가 너무 멀지 않을 때만 스폰 로직 작동
        if (distance <= activationRange)
        {
            // 리스트에서 죽은 적 제거
            activeEnemies.RemoveAll(e => e == null);

            if (activeEnemies.Count < maxEnemies)
            {
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnCooldown)
                {
                    SpawnEnemy();
                    respawnTimer = 0;
                }
            }
        }
        else
        {
            // 플레이어가 구역을 완전히 벗어나면 몬스터들을 정리할 수도 있음 (메모리 절약)
            // 혹은 그대로 두어 나중에 다시 돌아왔을 때 만나게 함
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPos = transform.position + (Vector3)Random.insideUnitCircle * spawnRadius;

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.transform.SetParent(this.transform);
        activeEnemies.Add(enemy);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}
