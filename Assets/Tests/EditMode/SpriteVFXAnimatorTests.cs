using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SpriteVFXAnimator 3단계(Start→Loop→Hit) 상태머신 단위 테스트.
/// EditMode 에서는 Time.deltaTime 이 0 이므로, private Update 를 리플렉션으로 직접 호출하고
/// private _timer 를 정확히 1/fps 로 세팅해 프레임당 1회씩 결정적으로 진행시킨다.
/// </summary>
public class SpriteVFXAnimatorTests
{
    private const int StageStart = 0;
    private const int StageLoop = 1;
    private const int StageHit = 2;
    private const int StageDone = 3;

    // ------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------
    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(2, 2);
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private static Sprite[] MakeSprites(int count)
    {
        var arr = new Sprite[count];
        for (int i = 0; i < count; i++) arr[i] = MakeSprite();
        return arr;
    }

    private static void SetField(object target, string name, object value)
    {
        var f = typeof(SpriteVFXAnimator).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"private field '{name}' not found on SpriteVFXAnimator");
        f.SetValue(target, value);
    }

    private static object GetField(object target, string name)
    {
        var f = typeof(SpriteVFXAnimator).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"private field '{name}' not found on SpriteVFXAnimator");
        return f.GetValue(target);
    }

    private static void InvokeLifecycle(object target, string methodName)
    {
        var m = typeof(SpriteVFXAnimator).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"private method '{methodName}' not found on SpriteVFXAnimator");
        m.Invoke(target, null);
    }

    private static int GetStage(object target) => (int)GetField(target, "_stage");

    private static (GameObject go, SpriteVFXAnimator animator) CreateAnimator(bool withRenderer)
    {
        var go = new GameObject("SpriteVFXAnimatorTest");
        if (withRenderer) go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<SpriteVFXAnimator>();
        return (go, animator);
    }

    /// <summary>직렬화 private 필드를 리플렉션으로 세팅한 뒤 OnEnable 을 재호출해 상태를 재계산한다.</summary>
    private static void ConfigureAndEnable(SpriteVFXAnimator animator, Sprite[] start, Sprite[] loop, Sprite[] hit,
        float fps = 12f, bool autoPlay = true, bool destroyOnHitEnd = false)
    {
        SetField(animator, "startFrames", start);
        SetField(animator, "loopFrames", loop);
        SetField(animator, "hitFrames", hit);
        SetField(animator, "fps", fps);
        SetField(animator, "autoPlay", autoPlay);
        SetField(animator, "destroyOnHitEnd", destroyOnHitEnd);
        InvokeLifecycle(animator, "OnEnable");
    }

    /// <summary>_timer 를 정확히 1/fps 로 세팅 후 Update 를 1회 호출해 프레임을 1개씩 결정적으로 진행한다.</summary>
    private static void ForceFrames(SpriteVFXAnimator animator, int count)
    {
        float fps = (float)GetField(animator, "fps");
        float frameTime = 1f / fps;
        for (int i = 0; i < count; i++)
        {
            SetField(animator, "_timer", frameTime);
            InvokeLifecycle(animator, "Update");
        }
    }

    // ------------------------------------------------------------------
    // 1. Start → Loop 자동 전이
    // ------------------------------------------------------------------
    [Test]
    public void StartToLoopAutoTransition()
    {
        var (go, animator) = CreateAnimator(true);
        try
        {
            ConfigureAndEnable(animator, MakeSprites(2), MakeSprites(1), null);
            Assert.AreEqual(StageStart, GetStage(animator), "autoPlay + startFrames -> Start");

            ForceFrames(animator, 2);
            Assert.AreEqual(StageLoop, GetStage(animator), "startFrames 소진 후 Loop 로 전이");

            ForceFrames(animator, 20); // OOB 없이 무한 루프
            Assert.AreEqual(StageLoop, GetStage(animator), "Loop 에서 계속 재생해도 예외/이탈 없음");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 2. startFrames 없으면 Loop 전용
    // ------------------------------------------------------------------
    [Test]
    public void LoopOnlyWhenStartFramesEmpty()
    {
        var (go, animator) = CreateAnimator(true);
        try
        {
            var sprites = MakeSprites(3);
            ConfigureAndEnable(animator, null, sprites, null);
            Assert.AreEqual(StageLoop, GetStage(animator), "startFrames 빈 경우 즉시 Loop");

            var sr = go.GetComponent<SpriteRenderer>();
            ForceFrames(animator, 1);
            Assert.AreSame(sprites[1], sr.sprite, "frame1 -> loopFrames[1]");
            ForceFrames(animator, 1);
            Assert.AreSame(sprites[2], sr.sprite, "frame2 -> loopFrames[2]");
            ForceFrames(animator, 1);
            Assert.AreSame(sprites[0], sr.sprite, "frame3 -> loopFrames[0] (순환)");

            ForceFrames(animator, 30); // 예외 없이 순환
            Assert.AreEqual(StageLoop, GetStage(animator), "30프레임 후에도 Loop 유지");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 3. PlayHit
    // ------------------------------------------------------------------
    [Test]
    public void PlayHitSwitchesToHitAndDone()
    {
        var (go, animator) = CreateAnimator(true);
        try
        {
            ConfigureAndEnable(animator, null, MakeSprites(1), MakeSprites(4));
            Assert.AreEqual(StageLoop, GetStage(animator));

            Assert.AreEqual(4f / 12f, animator.HitDuration, 0.0001f, "HitDuration == hitFrames / fps");

            animator.PlayHit();
            Assert.AreEqual(StageHit, GetStage(animator), "PlayHit -> Hit");

            ForceFrames(animator, 4);
            Assert.AreEqual(StageDone, GetStage(animator), "hitFrames 소진 -> Done");

            animator.PlayHit(); // Done 이후 두 번째 호출은 무시
            Assert.AreEqual(StageDone, GetStage(animator), "Done 이후 PlayHit 무시 (상태 변화 없음)");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 4. PlayHit 가드: hitFrames 빈 경우
    // ------------------------------------------------------------------
    [Test]
    public void PlayHitGuardWhenHitFramesEmpty()
    {
        var (go, animator) = CreateAnimator(true);
        try
        {
            ConfigureAndEnable(animator, MakeSprites(1), null, null);
            Assert.AreEqual(StageStart, GetStage(animator));

            animator.PlayHit();
            Assert.AreEqual(StageDone, GetStage(animator), "hitFrames 없으면 즉시 Done");

            Assert.AreEqual(0f, animator.HitDuration, "hitFrames 없으면 HitDuration 0");

            ForceFrames(animator, 5); // Done 에서 Update 계속 호출해도 예외 없음
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 5. fps 가드
    // ------------------------------------------------------------------
    [Test]
    public void FpsGuardResetsNonPositiveFps()
    {
        var go = new GameObject("SpriteVFXAnimatorTest");
        try
        {
            go.AddComponent<SpriteRenderer>();
            var animator = go.AddComponent<SpriteVFXAnimator>();

            var so = new SerializedObject(animator);
            so.FindProperty("fps").floatValue = -5f;
            so.ApplyModifiedProperties();

            InvokeLifecycle(animator, "OnEnable");
            Assert.AreEqual(12f, (float)GetField(animator, "fps"), "fps <= 0 이면 OnEnable 에서 12 로 리셋");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 6. destroyOnHitEnd
    // ------------------------------------------------------------------
    [Test]
    public void DestroyOnHitEndDestroysGameObject()
    {
        var (go, animator) = CreateAnimator(true);
        try
        {
            ConfigureAndEnable(animator, null, MakeSprites(1), MakeSprites(2), destroyOnHitEnd: true);
            animator.PlayHit();
            Assert.AreEqual(StageHit, GetStage(animator), "PlayHit -> Hit");

            ForceFrames(animator, 2);
            Assert.IsTrue(go == null, "hit 완료 시 destroyOnHitEnd 가 GameObject 파괴");
        }
        finally { if (go != null) Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------------
    // 7. SpriteRenderer 없음 — null-safe
    // ------------------------------------------------------------------
    [Test]
    public void NoSpriteRendererIsNullSafe()
    {
        var (go, animator) = CreateAnimator(false);
        try
        {
            ConfigureAndEnable(animator, MakeSprites(1), MakeSprites(1), null);
            Assert.DoesNotThrow(() => ForceFrames(animator, 10), "SpriteRenderer 없어도 Update 예외 없음");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
