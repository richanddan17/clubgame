using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class PrefabAutoCreator : EditorWindow
{
    private const string EnemySpritePath = "Assets/Sprite/enemy";
    private const string PlayerSpritePath = "Assets/Sprite/player";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemy";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player";
    private const string EnemyDataPath = "Assets/Resources/EnemyData";

    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectiles";

    [MenuItem("Custom Tools/Prefab Auto Creator", false, -100)]
    public static void ShowWindow()
    {
        GetWindow<PrefabAutoCreator>("Prefab Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Auto Creator Settings", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Enemy Prefabs", GUILayout.Height(40)))
        {
            CreatePrefabs(EnemySpritePath, EnemyPrefabPath, true);
        }

        if (GUILayout.Button("Create Player Prefabs", GUILayout.Height(40)))
        {
            CreatePrefabs(PlayerSpritePath, PlayerPrefabPath, false);
        }
    }

    private void CreatePrefabs(string spriteRoot, string saveRoot, bool isEnemy)
    {
        if (!Directory.Exists(saveRoot))
        {
            Directory.CreateDirectory(saveRoot);
        }
        if (isEnemy && !Directory.Exists(ProjectilePrefabPath))
        {
            Directory.CreateDirectory(ProjectilePrefabPath);
        }
        AssetDatabase.Refresh();

        string[] subDirs = Directory.GetDirectories(spriteRoot);
        int count = 0;

        foreach (string dir in subDirs)
        {
            string folderName = Path.GetFileName(dir);
            string prefabName = folderName.Replace("_", " ");
            prefabName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(prefabName).Replace(" ", "");
            
            string savePath = Path.Combine(saveRoot, prefabName + ".prefab").Replace("\\", "/");

            GameObject go = new GameObject(prefabName);
            
            // 1. SpriteRenderer 및 스프라이트 설정
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FindBestSprite(dir, folderName);
            sr.sortingLayerName = isEnemy ? "Default" : "player";

            // 2. 물리 컴포넌트 추가
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = isEnemy ? 3f : 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.simulated = true;

            if (isEnemy)
            {
                go.transform.localScale = new Vector3(10, 10, 10);
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1) go.layer = enemyLayer;
            }
            else
            {
                go.tag = "Player";
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1) go.layer = playerLayer;
            }

            CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
            if (sr.sprite != null)
            {
                col.size = sr.sprite.bounds.size * 0.8f;
            }

            // 3. 타입별 특화 설정 (공격 로직 포함)
            if (isEnemy)
            {
                SetupEnemyAI(go, dir, folderName, prefabName);
            }
            else
            {
                SetupPlayer(go);
            }

            // 4. 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(go, savePath);
            DestroyImmediate(go);
            count++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"{count}개의 프리팹이 {saveRoot}에 생성되었습니다.", "확인");
    }

    private Sprite FindBestSprite(string dir, string folderName)
    {
        string[] files = Directory.GetFiles(dir, "*.png");
        if (files.Length == 0) return null;

        var fileInfos = files.Select(f => new { Path = f, Name = Path.GetFileNameWithoutExtension(f).ToLower() }).ToList();
        string folderNameLower = folderName.ToLower();

        // 1. "idle" 포함 (bullet 제외)
        string bestMatch = fileInfos.FirstOrDefault(f => f.Name.Contains("idle") && !f.Name.Contains("bullet"))?.Path;

        // 2. 폴더 이름과 정확히 일치 (뒤에 아무것도 없는 것)
        if (string.IsNullOrEmpty(bestMatch))
        {
            bestMatch = fileInfos.FirstOrDefault(f => f.Name == folderNameLower)?.Path;
        }

        // 3. "moving"으로 끝나는 것
        if (string.IsNullOrEmpty(bestMatch))
        {
            bestMatch = fileInfos.FirstOrDefault(f => f.Name.EndsWith("moving") || f.Name.Contains("walk") || f.Name.Contains("run"))?.Path;
        }

        // 4. 최후의 수단: 첫 번째 파일 (단, bullet이나 dead 등은 최대한 피함)
        if (string.IsNullOrEmpty(bestMatch))
        {
            string[] excludes = { "bullet", "dead", "attack", "hit" };
            bestMatch = fileInfos.FirstOrDefault(f => !excludes.Any(ex => f.Name.Contains(ex)))?.Path;
            if (string.IsNullOrEmpty(bestMatch)) bestMatch = files[0];
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(bestMatch.Replace("\\", "/"));
    }

    private void SetupEnemyAI(GameObject go, string dir, string folderName, string prefabName)
    {
        go.tag = "Enemy";
        Health health = go.AddComponent<Health>();
        
        // 1. Animator 추가 및 컨트롤러 검색
        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = FindAnimatorController(dir);

        string[] files = Directory.GetFiles(dir, "*.png");
        bool hasBullet = files.Any(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("bullet"));
        bool hasAttack = files.Any(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("attack"));

        EnemyData data = FindEnemyData(folderName);

        if (hasBullet)
        {
            // 원거리 공격 설정
            RangedEnemy ranged = go.AddComponent<RangedEnemy>();
            ranged.data = data;
            
            // FirePoint 생성
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(go.transform);
            firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0); // 기본 위치
            ranged.firePoint = firePointObj.transform;

            // 총알 프리팹 생성 및 연결
            string bulletSpritePath = files.First(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("bullet"));
            ranged.projectilePrefab = CreateProjectilePrefab(bulletSpritePath, prefabName);
        }
        else
        {
            // 근접 공격 및 기본 이동 설정 (EnemyController로 통합)
            EnemyController controller = go.AddComponent<EnemyController>();
            if (data != null)
            {
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("data").objectReferenceValue = data;
                so.FindProperty("autoInitialize").boolValue = true;
                so.ApplyModifiedProperties();
            }
        }
    }

    private RuntimeAnimatorController FindAnimatorController(string dir)
    {
        // 1. 현재 폴더에서 검색
        string[] controllers = Directory.GetFiles(dir, "*.controller", SearchOption.AllDirectories);
        if (controllers.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllers[0].Replace("\\", "/"));
        }

        // 2. 부모 폴더에서 검색 (한 단계 위까지만)
        string parentDir = Directory.GetParent(dir).FullName;
        controllers = Directory.GetFiles(parentDir, "*.controller", SearchOption.TopDirectoryOnly);
        if (controllers.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllers[0].Replace("\\", "/"));
        }

        return null;
    }

    private GameObject CreateProjectilePrefab(string spritePath, string enemyName)
    {
        string savePath = $"{ProjectilePrefabPath}/{enemyName}_Bullet.prefab";
        
        // 이미 있으면 반환
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
        if (existing != null) return existing;

        GameObject go = new GameObject(enemyName + "_Bullet");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath.Replace("\\", "/"));
        
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        if (sr.sprite != null) col.radius = Mathf.Max(sr.sprite.bounds.extents.x, sr.sprite.bounds.extents.y);

        // Projectile 스크립트가 있다면 추가 (기존 Projectile.cs 참고)
        var proj = go.AddComponent<Projectile>();
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
        DestroyImmediate(go);
        return prefab;
    }

    private EnemyData FindEnemyData(string folderName)
    {
        string[] dataFiles = Directory.GetFiles(EnemyDataPath, "*.asset");
        string searchName = folderName.Replace("_", "").ToLower();
        string dataPath = dataFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains(searchName));
        
        if (!string.IsNullOrEmpty(dataPath))
        {
            return AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath.Replace("\\", "/"));
        }
        return null;
    }


    private void SetupPlayer(GameObject go)
    {
        go.tag = "Player";
        // PlayerController 추가 (Health는 PlayerController 내부에 있거나 별도로 있을 수 있음)
        // 기존 코드 분석 결과 PlayerController는 RequireComponent로 Health를 가질 수도 있음
        var pc = go.AddComponent<PlayerController>();
        if (go.GetComponent<Health>() == null) go.AddComponent<Health>();
    }
}
