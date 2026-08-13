using UnityEngine;

/// <summary>
/// 3단계(Start → Loop → Hit) 코드 기반 스프라이트 애니메이션.
/// OnEnable 시점에 _sr 을 캐시하고, Update 의 _timer 누적으로 프레임을 진행시킨다.
/// Hit 종료 시 _stage = Done 이 되며, destroyOnHitEnd 이면 GameObject 를 파괴한다.
/// (EditMode 에서는 Application.isPlaying 이 false 이므로 DestroyImmediate 를 사용.)
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

    private void OnEnable()
    {
        _frameIndex = 0;
        _timer = 0f;
        _hitFired = false;

        if (fps <= 0f) fps = 12f;

        _stage = autoPlay && startFrames != null && startFrames.Length > 0
            ? Stage.Start
            : (loopFrames != null && loopFrames.Length > 0 ? Stage.Loop : Stage.Done);

        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_stage == Stage.Done) return;

        _timer += Time.deltaTime;
        float frameTime = 1f / fps;
        while (_timer >= frameTime)
        {
            _timer -= frameTime;
            _frameIndex++;
            ApplyCurrentFrame();
        }
    }

    private void ApplyCurrentFrame()
    {
        if (_sr == null) return;

        switch (_stage)
        {
            case Stage.Start:
                _sr.sprite = startFrames[_frameIndex % startFrames.Length];
                if (_frameIndex >= startFrames.Length) _stage = Stage.Loop;
                break;

            case Stage.Loop:
                _sr.sprite = loopFrames[_frameIndex % loopFrames.Length];
                break;

            case Stage.Hit:
                _sr.sprite = hitFrames[Mathf.Min(_frameIndex, hitFrames.Length - 1)];
                if (_frameIndex >= hitFrames.Length)
                {
                    _stage = Stage.Done;
                    if (!_hitFired) _hitFired = true;
                    if (destroyOnHitEnd)
                    {
                        if (Application.isPlaying) Destroy(gameObject);
                        else DestroyImmediate(gameObject);
                    }
                }
                break;
        }
    }

    public void PlayHit()
    {
        if (_stage == Stage.Hit || _stage == Stage.Done) return;

        _stage = (hitFrames != null && hitFrames.Length > 0) ? Stage.Hit : Stage.Done;
        _frameIndex = 0;
        _timer = 0f;
    }

    public float HitDuration => (hitFrames != null && hitFrames.Length > 0) ? hitFrames.Length / fps : 0f;
}
