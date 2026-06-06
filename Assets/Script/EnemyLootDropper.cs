using UnityEngine;

/// <summary>
/// 적이 죽을 때 전리품을 떨어뜨리게 하는 컴포넌트
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyLootDropper : MonoBehaviour
{
    [Header("설정")]
    public LootTable lootTable;
    public GameObject droppedItemPrefab; // LootDroppedItem 컴포넌트가 있는 프리팹

    private Health _health;

    void Awake()
    {
        _health = GetComponent<Health>();
    }

    void Start()
    {
        _health.OnDie.AddListener(OnDie);
    }

    private void OnDie()
    {
        if (lootTable == null) return;

        ScriptableObject drop = lootTable.GetRandomDrop();
        if (drop != null)
        {
            SpawnDrop(drop);
        }
    }

    private void SpawnDrop(ScriptableObject data)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogWarning("DroppedItemPrefab이 설정되지 않았습니다.");
            return;
        }

        GameObject dropObj = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
        LootDroppedItem lootItem = dropObj.GetComponent<LootDroppedItem>();
        
        if (lootItem != null)
        {
            lootItem.Initialize(data);
        }
        
        // 살짝 튀어오르는 연출 (Rigidbody2D가 있다면)
        Rigidbody2D rb = dropObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 jumpDir = new Vector2(Random.Range(-1f, 1f), 2f).normalized;
            rb.AddForce(jumpDir * 5f, ForceMode2D.Impulse);
        }
    }
}
