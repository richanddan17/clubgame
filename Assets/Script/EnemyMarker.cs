using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    [Header("적 설정")]
    public int enemyID; // unit.csv의 ID와 일치해야 함
    
    private void Awake()
    {
        SpawnEnemy();
        // 소환 후 마커 오브젝트 자체는 파괴 (깔끔하게 정리)
        Destroy(gameObject);
    }

    private void SpawnEnemy()
    {
        // 1. 데이터 로드
        EnemyData[] allData = Resources.LoadAll<EnemyData>("EnemyData");
        EnemyData targetData = null;
        
        foreach (var data in allData)
        {
            if (data.ID == enemyID)
            {
                targetData = data;
                break;
            }
        }

        if (targetData == null)
        {
            Debug.LogError($"ID {enemyID}에 해당하는 EnemyData를 찾을 수 없습니다!");
            return;
        }

        // 2. 기본 프리팹 로드 (아까 만든 BaseEnemy 프리팹 사용)
        GameObject prefab = Resources.Load<GameObject>("Prefabs/BaseEnemy");
        if (prefab == null)
        {
            // 리소스 폴더에 없을 경우 Assets/Prefabs에서 시도 (프로젝트 구조에 따라)
            #if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BaseEnemy.prefab");
            #endif
        }

        if (prefab != null)
        {
            GameObject enemyObj = Instantiate(prefab, transform.position, Quaternion.identity);
            var controller = enemyObj.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.Initialize(targetData);
            }
        }
        else
        {
            Debug.LogError("BaseEnemy 프리팹을 찾을 수 없습니다!");
        }
    }

    // 에디터에서 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Enemy ID: {enemyID}");
        #endif
    }
}
