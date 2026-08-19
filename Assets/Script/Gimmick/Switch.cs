using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스위치. E키로 토글하여 연결된 Door를 열거나 닫습니다.
/// 기존 LevelPortal의 E키 인터랙션 패턴을 따릅니다.
/// </summary>
public class Switch : MonoBehaviour
{
    [SerializeField] private Door linkedDoor;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private bool _isPlayerNearby;
    private bool _isActivated;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = false;
        }
    }

    private void Update()
    {
        if (_isPlayerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            _isActivated = !_isActivated;

            if (linkedDoor != null)
                linkedDoor.SetOpen(_isActivated);

            if (_sr != null)
                _sr.sprite = _isActivated ? onSprite : offSprite;
        }
    }
}
