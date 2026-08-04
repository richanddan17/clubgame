using UnityEngine;

/// <summary>
/// 3단계(Start → Loop → Hit) 코드 기반 스프라이트 애니메이션.
/// (STUB — Todo 1 실패-우선 TDD 골격. 동작은 이후 단계에서 구현된다.)
/// </summary>
public class SpriteVFXAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private bool destroyOnHitEnd = false;

    private enum Stage { Start, Loop, Hit, Done }
    private Stage _stage;
    private int _frameIndex;
    private float _timer;
    private SpriteRenderer _sr;
    private bool _hitFired;

    private void Awake() { }
    private void OnEnable() { }
    private void Update() { }

    public void PlayHit() { }
    public float HitDuration => 0f;
}
