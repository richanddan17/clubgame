using UnityEngine;

public class BiomeZone : MonoBehaviour
{
    public BiomeData biomeData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (BiomeManager.Instance != null && biomeData != null)
            {
                BiomeManager.Instance.EnterZone(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.ExitZone(this);
            }
        }
    }

    // 에디터에서 영역을 시각적으로 표시 (Gizmos)
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, (Vector3)box.size);
            
            if (biomeData != null)
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, $"Biome: {biomeData.biomeName}");
                #endif
            }
        }
    }
}
