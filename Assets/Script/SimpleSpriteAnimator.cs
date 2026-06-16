using UnityEngine;

/// <summary>
/// 스프라이트 시트 애니메이션을 코드로 간단히 재생함 (VFX용)
/// </summary>
public class SimpleSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool loop = false;

    private SpriteRenderer _sr;
    private float _timer;
    private int _frameIndex;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _frameIndex = 0;
        _timer = 0;
        if (frames != null && frames.Length > 0 && _sr != null) _sr.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f / fps)
        {
            _timer = 0;
            _frameIndex++;

            if (_frameIndex >= frames.Length)
            {
                if (loop) _frameIndex = 0;
                else _frameIndex = frames.Length - 1;
            }

            _sr.sprite = frames[_frameIndex];
        }
    }

    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        _frameIndex = 0;
        if (frames != null && frames.Length > 0) _sr.sprite = frames[0];
    }
}
