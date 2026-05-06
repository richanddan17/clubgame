using UnityEngine;
using UnityEditor;

public class EnemyFixHelper : EditorWindow
{
    [MenuItem("Custom Tools/Fix Enemy (Slime)", false, -80)]
    public static void FixSlime()
    {
        string slimePath = "Assets/Prefabs/Slime.prefab";
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(slimePath);

        if (slimePrefab != null)
        {
            GameObject instance = PrefabUtility.LoadPrefabContents(slimePath);
            
            // 1. 크기 키우기 (기존 3배에서 더 키워 총 10배로 설정)
            instance.transform.localScale = new Vector3(10, 10, 10);
            
            // 태그 설정 (총알 충돌 감지용)
            instance.tag = "Enemy";

            // 2. 잘못된 스크립트 제거 및 올바른 스크립트 추가
            var oldScript = instance.GetComponent("Mobs");
            if (oldScript != null) DestroyImmediate(oldScript);

            var slimeScript = instance.GetComponent<Slime>();
            if (slimeScript == null) slimeScript = instance.AddComponent<Slime>();
            
            // 3. 컴포넌트 설정 최적화
            var rb = instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 3f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            var col = instance.GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(0.16f, 0.16f); // 스프라이트 크기에 맞춤
            }

            PrefabUtility.SaveAsPrefabAsset(instance, slimePath);
            PrefabUtility.UnloadPrefabContents(instance);
            
            Debug.Log("슬라임 프리팹 수정 완료: 크기 확대 및 AI 스크립트 교체");
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Enemy Fix Complete", "슬라임 몬스터 설정이 완료되었습니다!\n1. 크기가 3배로 커졌습니다.\n2. 이제 플레이어를 추격합니다.", "확인");
    }
}
