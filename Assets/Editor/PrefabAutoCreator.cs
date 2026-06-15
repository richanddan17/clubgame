using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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
        if (GUILayout.Button("Create Enemy Prefabs", GUILayout.Height(40))) CreatePrefabs(EnemySpritePath, EnemyPrefabPath, true);
        if (GUILayout.Button("Create Player Prefabs", GUILayout.Height(40))) CreatePrefabs(PlayerSpritePath, PlayerPrefabPath, false);
    }

    private void CreatePrefabs(string spriteRoot, string saveRoot, bool isEnemy)
    {
        if (!Directory.Exists(saveRoot)) Directory.CreateDirectory(saveRoot);
        if (isEnemy && !Directory.Exists(ProjectilePrefabPath)) Directory.CreateDirectory(ProjectilePrefabPath);
        AssetDatabase.Refresh();

        string[] subDirs = Directory.GetDirectories(spriteRoot);
        foreach (string dir in subDirs)
        {
            CreateSinglePrefab(dir, Path.GetFileName(dir), saveRoot, isEnemy);
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "프리팹 및 애니메이션 생성이 완료되었습니다.", "확인");
    }

    private void CreateSinglePrefab(string dir, string folderName, string saveRoot, bool isEnemy)
    {
        string prefabName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(folderName.Replace("_", " ")).Replace(" ", "");
        string savePath = Path.Combine(saveRoot, prefabName + ".prefab").Replace("\\", "/");

        GameObject go = new GameObject(prefabName);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FindBestSprite(dir, folderName);
        sr.sortingLayerName = isEnemy ? "Default" : "player";

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        bool isFlying = prefabName.ToLower().Contains("bat") || prefabName.ToLower().Contains("bird");
        rb.gravityScale = isFlying ? 0f : (isEnemy ? 3.5f : 1f);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.simulated = true;

        if (isEnemy)
        {
            go.transform.localScale = new Vector3(10, 10, 10);
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;

            if (prefabName.Contains("CandyTankSlime"))
            {
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.3f, 0.13f);
            }
            else
            {
                CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
                if (sr.sprite != null) col.size = sr.sprite.bounds.size * 0.8f;
            }
            SetupEnemyAI(go, dir, folderName, prefabName);
        }
        else
        {
            go.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) go.layer = playerLayer;
            go.AddComponent<PlayerController>();
            go.AddComponent<Health>();
        }

        // [Fix] 프리팹 저장 전 에셋 데이터베이스와 동기화 및 Dirty 설정
        EditorUtility.SetDirty(go);
        PrefabUtility.SaveAsPrefabAsset(go, savePath);
        DestroyImmediate(go);
    }

    private Sprite FindBestSprite(string dir, string folderName)
    {
        string[] files = Directory.GetFiles(dir, "*.png");
        string best = files.FirstOrDefault(f => {
            string n = Path.GetFileNameWithoutExtension(f).ToLower();
            return n.Contains("idle") && !n.Contains("elite") && !n.Contains("bullet");
        });
        if (string.IsNullOrEmpty(best)) best = files.FirstOrDefault(f => !f.ToLower().Contains("bullet") && !f.ToLower().Contains("elite"));
        return string.IsNullOrEmpty(best) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(best.Replace("\\", "/"));
    }

    private void SetupEnemyAI(GameObject go, string dir, string folderName, string prefabName)
    {
        go.tag = "Enemy";
        go.AddComponent<Health>();
        Animator anim = go.AddComponent<Animator>();
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        // 1. 애니메이터 컨트롤러 생성
        CreateAutoAnimator(dir);
        
        // 2. [Fix] 해당 컨트롤러 에셋만 강제로 확실히 임포트
        string animPath = Path.Combine(dir, folderName + ".controller").Replace("\\", "/");
        AssetDatabase.ImportAsset(animPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets(); // 모든 변경사항 디스크 저장
        
        // 3. 확실하게 로드해서 할당
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animPath);
        
        if (controller != null)
        {
            anim.runtimeAnimatorController = controller;
            // [Fix] 컴포넌트 Dirty 설정 (직렬화 강제)
            EditorUtility.SetDirty(anim);
            Debug.Log($"<color=green>[PrefabCreator]</color> Assigned Controller to {prefabName}");
        }
        else
        {
            Debug.LogError($"<color=red>[PrefabCreator]</color> Failed to load Controller for {prefabName} at {animPath}");
        }

        string[] files = Directory.GetFiles(dir, "*.png");
        bool hasBullet = files.Any(f => f.ToLower().Contains("bullet"));
        EnemyData data = FindEnemyData(folderName);

        if (prefabName.Contains("Haribo") || prefabName.Contains("Melting"))
        {
            MeltingHaribo haribo = go.AddComponent<MeltingHaribo>();
            // MeltingHaribo는 자체 스크립트 내부에서 Animator를 사용하거나 수동 제어하도록 확장 가능
            // 여기서는 스크립트만 정상 부착하고 에러 방지
            Debug.Log($"<color=orange>[PrefabCreator]</color> Added MeltingHaribo component to {prefabName}");
        }
        else if (hasBullet)
        {
            RangedEnemy ranged = go.AddComponent<RangedEnemy>();
            ranged.data = data;
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform);
            fp.transform.localPosition = new Vector3(-0.135f, 0.02f, 0f);
            ranged.firePoint = fp.transform;
            string bulletPath = files.First(f => f.ToLower().Contains("bullet"));
            ranged.projectilePrefab = CreateProjectilePrefab(bulletPath, prefabName);
        }
        else
        {
            EnemyController ec = go.AddComponent<EnemyController>();
            ec.data = data;
            ec.autoInitialize = true;
        }
    }

    private void CreateAutoAnimator(string dir)
    {
        string folderName = Path.GetFileName(dir);
        string animPath = Path.Combine(dir, folderName + ".controller").Replace("\\", "/");
        
        // 기존 파일 삭제 후 재생성
        AssetDatabase.DeleteAsset(animPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(animPath);
        var root = controller.layers[0].stateMachine;

        controller.AddParameter("Walk", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var idle = CreateClip(dir, "idle", "Idle", true) ?? CreateClip(dir, "", "Idle", true);
        var walk = CreateClip(dir, "moving", "Walk", true) ?? CreateClip(dir, "walk", "Walk", true) ?? CreateClip(dir, "heating", "Walk", true);
        var attack = CreateClip(dir, "attack", "Attack", false);
        var die = CreateClip(dir, "dead", "Die", false);

        var sIdle = root.AddState("Idle"); sIdle.motion = idle;
        root.defaultState = sIdle;

        if (walk != null) 
        { 
            var sWalk = root.AddState("Walk"); sWalk.motion = walk; 
            sIdle.AddTransition(sWalk).AddCondition(AnimatorConditionMode.If, 0, "Walk"); 
            sWalk.AddTransition(sIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "Walk"); 
        }
        if (attack != null) 
        { 
            var sAtk = root.AddState("Attack"); sAtk.motion = attack; 
            var t = root.AddAnyStateTransition(sAtk);
            t.AddCondition(AnimatorConditionMode.If, 0, "Attack"); 
            t.canTransitionToSelf = false;
            sAtk.AddTransition(sIdle).hasExitTime = true; 
        }
        if (die != null) 
        { 
            var sDie = root.AddState("Die"); sDie.motion = die; 
            root.AddAnyStateTransition(sDie).AddCondition(AnimatorConditionMode.If, 0, "Die"); 
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private AnimationClip CreateClip(string dir, string kw, string name, bool loop)
    {
        string[] files = Directory.GetFiles(dir, "*.png");
        var textureFiles = files.Where(f => {
            string n = Path.GetFileNameWithoutExtension(f).ToLower();
            if (n.Contains("elite") || n.Contains("bullet")) return false;
            return string.IsNullOrEmpty(kw) || n.Contains(kw.ToLower());
        }).OrderBy(f => f).ToList();

        if (textureFiles.Count == 0) return null;

        string savePath = Path.Combine(dir, name + ".anim").Replace("\\", "/");
        AssetDatabase.DeleteAsset(savePath);

        AnimationClip clip = new AnimationClip { name = name };
        if (loop) 
        { 
            var s = AnimationUtility.GetAnimationClipSettings(clip); 
            s.loopTime = true; 
            AnimationUtility.SetAnimationClipSettings(clip, s); 
        }

        EditorCurveBinding cb = new EditorCurveBinding { 
            type = typeof(SpriteRenderer), 
            path = "", 
            propertyName = "m_Sprite" 
        };

        // [Fix] 스프라이트 시트(Sliced Sprite) 대응 로직
        List<Sprite> allSprites = new List<Sprite>();
        foreach (var file in textureFiles)
        {
            string path = file.Replace("\\", "/");
            // 해당 경로의 모든 서브 에셋(스프라이트 포함)을 로드
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var spritesInFile = assets.OfType<Sprite>().OrderBy(s => s.name).ToList();
            allSprites.AddRange(spritesInFile);
        }

        if (allSprites.Count == 0) return null;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[allSprites.Count + 1];
        for (int i = 0; i < allSprites.Count; i++) 
        {
            keys[i] = new ObjectReferenceKeyframe { time = i * 0.15f, value = allSprites[i] };
        }
        
        // 마지막 프레임 고정
        keys[allSprites.Count] = new ObjectReferenceKeyframe { 
            time = allSprites.Count * 0.15f, 
            value = allSprites[allSprites.Count - 1] 
        };

        AnimationUtility.SetObjectReferenceCurve(clip, cb, keys);
        AssetDatabase.CreateAsset(clip, savePath);
        return clip;
    }

    private GameObject CreateProjectilePrefab(string path, string name)
    {
        string savePath = $"{ProjectilePrefabPath}/{name}_Bullet.prefab";
        GameObject go = new GameObject(name + "_Bullet");
        go.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace("\\", "/"));
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        go.AddComponent<Projectile>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
        DestroyImmediate(go);
        return prefab;
    }

    private EnemyData FindEnemyData(string folderName)
    {
        if (!Directory.Exists(EnemyDataPath)) return null;
        string searchName = folderName.Replace("_", "").ToLower();
        string[] files = Directory.GetFiles(EnemyDataPath, "*.asset");
        string path = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains(searchName));
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<EnemyData>(path.Replace("\\", "/"));
    }
}
