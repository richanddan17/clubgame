using UnityEngine;

/// <summary>
/// 체크포인트. 플레이어가 닿으면 리스폰 위치를 갱신합니다.
/// static _respawnPoint를 사용하므로, 씬 전환 전까지 유효합니다.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private Color activatedColor = Color.green;

    private bool _isActivated;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isActivated) return;
        if (!other.CompareTag("Player")) return;

        _isActivated = true;

        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.SetRespawnPoint(transform.position);

        if (_sr != null)
        {
            if (activatedSprite != null)
                _sr.sprite = activatedSprite;
            _sr.color = activatedColor;
        }
    }
}
