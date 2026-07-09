using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ManualEnemyFixer : EditorWindow
{
    private const string PrefabRoot = "Assets/Prefabs/Enemy";
    private const string EnemyDataRoot = "Assets/Resources/EnemyData";
    private const string SpriteRoot = "Assets/Sprite/Enemy";

    [MenuItem("Custom Tools/Manual Enemy Setup (4 Types)", false, 1)]
    public static void SetupEnemies()
    {
        // 1. Candy Tank Slime
        SetupTankSlime();
        // 2. Melting Haribo
        SetupHaribo();
        // 3. Popping Candy Bat
        SetupBat(false);
        // 4. Elite Popping Candy Bat
        SetupBat(true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "Haribo, Bat, Elite Bat, Tank Slime 설정 완료!", "OK");
    }

    private static void SetupTankSlime()
    {
        string name = "CandyTankSlime";
        string spritePath = SpriteRoot + "/candy_tank_slime";
        GameObject go = CreateBaseEnemy(name, spritePath + "/candy tank slime_moving.png");
        
        // Components
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.3f, 0.13f);

        var ranged = go.AddComponent<RangedEnemy>();
        ranged.data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataRoot + "/101_CandyTankSlime.asset");
        ranged.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/CandyTankSlime_Bullet.prefab");
        
        var fp = new GameObject("FirePoint");
        fp.transform.SetParent(go.transform);
        fp.transform.localPosition = new Vector3(-0.135f, 0.02f, 0f);
        ranged.firePoint = fp.transform;

        SetupAnimator(go, spritePath + "/candy_tank_slime.controller");

        SavePrefab(go, name);
    }

    private static void SetupHaribo()
    {
        string name = "MeltingHaribo";
        string spritePath = SpriteRoot + "/melting_haribo";
        GameObject go = CreateBaseEnemy(name, spritePath + "/melting_haribo.png");

        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.12f, 0.12f);

        var haribo = go.AddComponent<MeltingHaribo>();
        // MeltingHaribo initializes itself mostly

        SetupAnimator(go, spritePath + "/melting_haribo.controller");

        SavePrefab(go, name);
    }

    private static void SetupBat(bool isElite)
    {
        string name = isElite ? "ElitePoppingCandyBat" : "PoppingCandyBat";
        string spritePath = SpriteRoot + "/popping_candy_bat";
        string idleSprite = isElite ? "/popping_candy_bat_elite_moving.png" : "/popping_candy_bat_idle.png";
        
        GameObject go = CreateBaseEnemy(name, spritePath + idleSprite);

        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Flying

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.12f, 0.08f);

        var ranged = go.AddComponent<RangedEnemy>();
        string dataAsset = isElite ? "/106_ElitePoppingCandyBat.asset" : "/103_PoppingCandyBat.asset";
        ranged.data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataRoot + dataAsset);
        ranged.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/CandyTankSlime_Bullet.prefab");

        if (isElite)
        {
            go.transform.localScale = new Vector3(15, 15, 15);
        }

        SetupAnimator(go, spritePath + "/popping_candy_bat.controller");

        SavePrefab(go, name);
    }

    private static GameObject CreateBaseEnemy(string name, string spritePath)
    {
        GameObject go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(10, 10, 10);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        
        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<Health>();

        return go;
    }

    private static void SetupAnimator(GameObject go, string controllerPath)
    {
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private static void SavePrefab(GameObject go, string name)
    {
        string path = PrefabRoot + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log($"[ManualFixer] Created/Updated Prefab: {name}");
    }
}
