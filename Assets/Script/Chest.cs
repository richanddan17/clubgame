using UnityEngine;

/// <summary>
/// 상호작용 시 전리품을 드랍하는 보물상자
/// </summary>
public class Chest : MonoBehaviour
{
    [Header("설정")]
    public LootTable lootTable;
    public GameObject droppedItemPrefab;
    public int dropCount = 1;

    [Header("시각 효과")]
    public Sprite openSprite;
    private SpriteRenderer _sr;
    private bool _isOpened = false;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_isOpened) return;

        if (collision.CompareTag("Player"))
        {
            // 플레이어가 E키를 누르면 상자 오픈 (New Input System 사용 여부 확인 필요)
            // 일단 단순화를 위해 플레이어 스크립트의 인터랙션 로직과 연결하거나 
            // 여기서는 간단히 F키 또는 자동 오픈으로 구현
            if (Input.GetKeyDown(KeyCode.F)) // 기존 패링 키와 겹칠 수 있으나 일단 예시
            {
                OpenChest();
            }
        }
    }

    public void OpenChest()
    {
        if (_isOpened) return;
        _isOpened = true;

        if (openSprite != null) _sr.sprite = openSprite;

        // 전리품 생성
        for (int i = 0; i < dropCount; i++)
        {
            DropLoot();
        }

        Debug.Log("Chest Opened!");
    }

    private void DropLoot()
    {
        if (lootTable == null || droppedItemPrefab == null) return;

        ScriptableObject data = lootTable.GetRandomDrop();
        if (data != null)
        {
            GameObject dropObj = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
            LootDroppedItem lootItem = dropObj.GetComponent<LootDroppedItem>();
            if (lootItem != null) lootItem.Initialize(data);

            // 상자에서 튀어나오는 연출
            Rigidbody2D rb = dropObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 burstDir = new Vector2(Random.Range(-1f, 1f), 1f).normalized;
                rb.AddForce(burstDir * 7f, ForceMode2D.Impulse);
            }
        }
    }
}
