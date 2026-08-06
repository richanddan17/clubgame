using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ClubGame.PlayModeTests
{
    /// <summary>
    /// PlayerController.UseSkill(int) 실행 검증 (프로젝타일 / 근접 / 쿨타임 게이트).
    /// PlayerController는 Assembly-CSharp에 있어 컴파일 타임 참조가 불가능하므로
    /// 리플렉션으로 타입을 찾아 AddComponent / 메서드 호출 / 필드 접근을 수행한다.
    /// </summary>
    public class SkillExecutionTests
    {
        private GameObject _cameraGo;
        private GameObject _groundGo;
        private GameObject _playerGo;
        private Component _playerController;
        private Type _playerType;
        private GameObject _enemyGo;
        private Health _enemyHealth;
        private TestBubbleAffectable _bubbleAffectable;
        private SkillData _projectileSkill;
        private SkillData _meleeSkill;
        private SkillData _fireBallSkill;
        private SkillData _timeStopSkill;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();

            _cameraGo = new GameObject("MainCamera");
            _cameraGo.tag = "MainCamera";
            Camera cam = _cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            _cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            _groundGo = new GameObject("Ground");
            _groundGo.AddComponent<BoxCollider2D>();
            _groundGo.transform.position = new Vector3(0f, -3f, 0f);

            _enemyGo = new GameObject("Enemy");
            _enemyGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            CircleCollider2D enemyCollider = _enemyGo.AddComponent<CircleCollider2D>();
            enemyCollider.isTrigger = true;
            enemyCollider.radius = 1.0f;
            _enemyHealth = _enemyGo.AddComponent<Health>();
            _bubbleAffectable = _enemyGo.AddComponent<TestBubbleAffectable>();

            _playerType = GetPlayerControllerType();
            _playerGo = new GameObject("Player");
            _playerGo.tag = "Player";
            _playerGo.SetActive(false);
            _playerGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            _playerGo.AddComponent<BoxCollider2D>();
            _playerController = _playerGo.AddComponent(_playerType);

            // PlayerController는 [RequireComponent(typeof(Health))]로 Health가 자동 추가된다.
            // 런타임 AddComponent는 UnityEvent 시리얼 필드를 초기화하지 않으므로,
            // Awake에서 PlayerController가 OnDie/OnParry를 구독하기 전에 직접 초기화한다.
            Health playerHealth = _playerGo.GetComponent<Health>();
            playerHealth.OnDie = new UnityEngine.Events.UnityEvent();
            playerHealth.OnParry = new Health.Vector2Event();

            SerializedObject so = new SerializedObject(_playerController);
            SerializedProperty combatSettings = so.FindProperty("combatSettings");
            SerializedProperty moveSettings = so.FindProperty("moveSettings");
            Assert.IsNotNull(combatSettings, "combatSettings 직렬화 필드를 찾을 수 없음");
            Assert.IsNotNull(moveSettings, "moveSettings 직렬화 필드를 찾을 수 없음");

            combatSettings.FindPropertyRelative("FirePoint").objectReferenceValue = _playerGo.transform;
            moveSettings.FindPropertyRelative("WalkSpeed").floatValue = 5f;
            moveSettings.FindPropertyRelative("GroundLayer").intValue = LayerMask.GetMask("Default");

            _projectileSkill = CreateProjectileSkill();
            _meleeSkill = CreateMeleeSkill();

            SerializedProperty equipped = combatSettings.FindPropertyRelative("EquippedSkills");
            equipped.arraySize = 1;
            equipped.GetArrayElementAtIndex(0).objectReferenceValue = _projectileSkill;
            so.ApplyModifiedProperties();

            _playerGo.SetActive(true);
            ResetSkillCooldowns();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(_playerGo);
            UnityEngine.Object.Destroy(_enemyGo);
            UnityEngine.Object.Destroy(_groundGo);
            UnityEngine.Object.Destroy(_cameraGo);
            if (_projectileSkill != null) ScriptableObject.DestroyImmediate(_projectileSkill);
            if (_meleeSkill != null) ScriptableObject.DestroyImmediate(_meleeSkill);
            if (_fireBallSkill != null) ScriptableObject.DestroyImmediate(_fireBallSkill);
            if (_timeStopSkill != null) ScriptableObject.DestroyImmediate(_timeStopSkill);
            yield return null;
        }

        private static SkillData CreateProjectileSkill()
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.ID = 211;
            skill.SkillName = "GumShot";
            skill.Damage = 10f;
            skill.Cooldown = 0.1f;
            skill.SkillType = SkillType.Projectile;
            skill.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BubbleProjectile_blue.prefab");
            skill.ProjectileSpeed = 15f;
            skill.UseBubbleEffect = false;
            return skill;
        }

        private static SkillData CreateFireBallSkill()
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.ID = 221;
            skill.SkillName = "FireBall";
            skill.Damage = 30f;
            skill.Cooldown = 0.1f;
            skill.SkillType = SkillType.Projectile;
            skill.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/FireBallProjectile.prefab");
            skill.ProjectileSpeed = 18f;
            skill.UseBubbleEffect = false;
            return skill;
        }

        private static SkillData CreateTimeStopSkill()
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.ID = 301;
            skill.SkillName = "TimeStop";
            skill.Damage = 0f;
            skill.Cooldown = 0.1f;
            skill.SkillType = SkillType.InstantArea;
            skill.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/TimeStop_Effect.prefab");
            skill.UseBubbleEffect = false;
            return skill;
        }

        private static SkillData CreateMeleeSkill()
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.ID = 201;
            skill.SkillName = "Slash";
            skill.Damage = 10f;
            skill.Cooldown = 0.1f;
            skill.SkillType = SkillType.Melee;
            skill.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/MeleeHitbox.prefab");
            skill.MeleeRange = 1.5f;
            skill.HitboxLifetime = 0.3f;
            skill.UseBubbleEffect = true;
            skill.BubbleEffect = Projectile.BubbleType.Red;
            return skill;
        }

        private static Type GetPlayerControllerType()
        {
            Type t = Type.GetType("PlayerController, Assembly-CSharp");
            if (t == null)
            {
                foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType("PlayerController");
                    if (t != null) break;
                }
            }
            Assert.IsNotNull(t, "PlayerController 타입을 로드된 어셈블리에서 찾을 수 없음");
            return t;
        }

        private void EquipSkill(int slotIndex, SkillData skill)
        {
            SerializedObject so = new SerializedObject(_playerController);
            SerializedProperty equipped = so.FindProperty("combatSettings").FindPropertyRelative("EquippedSkills");
            equipped.arraySize = Mathf.Max(slotIndex + 1, equipped.arraySize);
            equipped.GetArrayElementAtIndex(slotIndex).objectReferenceValue = skill;
            so.ApplyModifiedProperties();
            ResetSkillCooldowns();
        }

        private void InvokeUseSkill(int slotIndex)
        {
            MethodInfo method = _playerType.GetMethod("UseSkill", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "UseSkill 메서드를 찾을 수 없음");
            method.Invoke(_playerController, new object[] { slotIndex });
        }

        private void ResetSkillCooldowns()
        {
            FieldInfo field = _playerType.GetField("_skillLastUsed", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.GetValue(_playerController) is not Array arr) return;
            for (int i = 0; i < arr.Length; i++) arr.SetValue(-100f, i);
        }

        [UnityTest]
        public IEnumerator ProjectileSkill_SpawnsProjectileAndDamagesEnemy()
        {
            EquipSkill(0, _projectileSkill);
            InvokeUseSkill(0);

            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, "프로젝타일 스킬 사용 직후 Projectile이 1개 존재해야 함");
            Assert.AreEqual(100f, _enemyHealth.CurrentHealth, "발사 직후에는 아직 적이 피해를 받지 않아야 함");

            yield return new WaitForSeconds(0.2f);

            Assert.Less(_enemyHealth.CurrentHealth, _enemyHealth.MaxHealth, "프로젝타일에 맞은 적의 체력이 감소해야 함");
        }

        [UnityTest]
        public IEnumerator CooldownGate_BlocksRapidSecondUse()
        {
            EquipSkill(0, _projectileSkill);
            InvokeUseSkill(0);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, "첫 사용 후 Projectile이 1개 존재해야 함");

            InvokeUseSkill(0);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, "쿨타임 게이트가 두 번째 사용을 차단해 Projectile이 늘어나면 안 됨");

            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator MeleeSkill_SpawnsHitboxDamagesEnemyAndAppliesBubble()
        {
            EquipSkill(0, _meleeSkill);
            InvokeUseSkill(0);

            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<MeleeHitbox>(FindObjectsSortMode.None).Length, "근접 스킬 사용 직후 MeleeHitbox가 1개 존재해야 함");

            yield return new WaitForSeconds(0.35f);

            Assert.Less(_enemyHealth.CurrentHealth, _enemyHealth.MaxHealth, "근접 히트박스에 맞은 적의 체력이 감소해야 함");
            Assert.AreEqual(1, _bubbleAffectable.ApplyCount, "거품 효과가 정확히 한 번 적용되어야 함");
            Assert.AreEqual(Projectile.BubbleType.Red, _bubbleAffectable.LastBubbleType, "Red 거품 효과가 적용되어야 함");
        }

        [UnityTest]
        public IEnumerator ProjectileWithVFX_PlaysHitAndDelaysDeactivation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/FireBallProjectile.prefab");
            SpriteVFXAnimator prefabVfx = prefab != null ? prefab.GetComponent<SpriteVFXAnimator>() : null;
            if (prefabVfx == null || prefabVfx.HitDuration <= 0f)
            {
                Assert.Ignore("FireBall prefab VFX not built yet (Todo 3)");
            }
            float hitDuration = prefabVfx.HitDuration;

            _fireBallSkill = CreateFireBallSkill();
            EquipSkill(0, _fireBallSkill);
            InvokeUseSkill(0);

            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, "파이어볼 사용 직후 Projectile이 1개 존재해야 함");

            yield return new WaitForSeconds(0.2f);

            Assert.Less(_enemyHealth.CurrentHealth, _enemyHealth.MaxHealth, "파이어볼에 맞은 적의 체력이 감소해야 함(대미지 정상)");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,
                "임팩트 직후에는 VFX 히트 연출이 재생되는 동안 프로젝타일이 즉시 사라지면 안 됨(지연 Deactivate)");

            yield return new WaitForSeconds(hitDuration + 0.5f);

            Assert.AreEqual(0, UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,
                "HitDuration 경과 후 프로젝타일이 반환/비활성화되어야 함");
        }

        [UnityTest]
        public IEnumerator InstantAreaSkill_SpawnsEffectAndStunsEnemy()
        {
            _timeStopSkill = CreateTimeStopSkill();
            EquipSkill(0, _timeStopSkill);
            InvokeUseSkill(0);

            // Spawn proof: scan all GameObjects for the effect clone.
            // CRITICAL: Object.Instantiate appends "(Clone)" to the clone name, so the real name
            // is "TimeStop_Effect (Clone)" — use StartsWith, NEVER exact equality.
            // (TimeStopEffect is an Assembly-CSharp type, not referenceable from this asmdef —
            //  that is why we scan by GameObject name instead of FindObjectsByType<TimeStopEffect>.)
            int spawned = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name.StartsWith("TimeStop_Effect")) spawned++;
            Assert.AreEqual(1, spawned, "InstantArea 스킬 사용 직후 TimeStop_Effect 효과 오브젝트가 1개 스폰되어야 함");

            Assert.AreEqual(0, _bubbleAffectable.StunCount, "Start() 실행 전에는 아직 스턴이 적용되지 않아야 함");

            yield return new WaitForSeconds(0.2f); // wait for Start() -> ApplyEffect()

            Assert.AreEqual(1, _bubbleAffectable.StunCount,
                "InstantArea 스킬이 범위 내 적에게 스턴을 정확히 한 번 적용해야 함 (OverlapCircleAll at player origin, radius 20)");

            yield return new WaitForSeconds(2.0f); // wait for lifeTime 1.5 self-destroy — avoid polluting next test
        }
    }

    public class TestBubbleAffectable : MonoBehaviour, IBubbleAffectable
    {
        public int ApplyCount;
        public Projectile.BubbleType LastBubbleType;
        public int StunCount;

        public void ApplyStun(float duration) { StunCount++; }

        public void ApplyBubbleEffect(Projectile.BubbleType type)
        {
            ApplyCount++;
            LastBubbleType = type;
        }
    }
}
