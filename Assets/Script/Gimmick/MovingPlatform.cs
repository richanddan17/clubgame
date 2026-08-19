using UnityEngine;
using System.Collections;

/// <summary>
/// 이동식 플랫폼. waypoint를 따라 이동하며, delta 방식으로 플레이어를 동기화합니다.
/// PlayerController.ApplyMovement()가 velocity를 직접 세팅하므로,
/// 부모-자식 대신 플랫폼의 위치 변화량(Δpos)을 플레이어에 직접 적용합니다.
/// </summary>
public class MovingPlatform : GimmickBase
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool loop = true;
    [SerializeField] private float waitTime = 0f;

    private int _currentIndex = 0;
    private bool _isWaiting = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (waypoints != null && waypoints.Length > 0)
            transform.position = waypoints[0].position;
    }

    private void Update()
    {
        if (!isActive) return;
        if (waypoints == null || waypoints.Length < 2) return;
        if (_isWaiting) return;

        Transform target = waypoints[_currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            if (loop)
                _currentIndex = (_currentIndex + 1) % waypoints.Length;
            else
                _currentIndex = Mathf.Min(_currentIndex + 1, waypoints.Length - 1);

            if (waitTime > 0f)
                StartCoroutine(WaitAtWaypoint());
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        _isWaiting = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<PlayerController>();
        if (player != null) player.SetOnMovingPlatform(transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<PlayerController>();
        if (player != null) player.ClearMovingPlatform();
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
            int next = loop ? (i + 1) % waypoints.Length : Mathf.Min(i + 1, waypoints.Length - 1);
            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}
