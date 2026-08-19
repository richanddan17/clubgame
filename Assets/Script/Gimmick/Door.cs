using UnityEngine;

/// <summary>
/// 문. Switch에서 SetOpen()으로 열기/닫기 제어.
/// openPosition이 없으면 위쪽으로 3유닛 이동합니다.
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] private Transform openPosition;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool startOpen = false;

    private Vector3 _closedPos;
    private Vector3 _targetPos;

    private void Awake()
    {
        _closedPos = transform.position;
        _targetPos = startOpen ? GetOpenTarget() : _closedPos;
        transform.position = _targetPos;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, speed * Time.deltaTime);
    }

    public void SetOpen(bool open)
    {
        _targetPos = open ? GetOpenTarget() : _closedPos;
    }

    private Vector3 GetOpenTarget()
    {
        return openPosition != null ? openPosition.position : _closedPos + Vector3.up * 3f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 closed = Application.isPlaying ? _closedPos : transform.position;
        Vector3 open = openPosition != null ? openPosition.position : closed + Vector3.up * 3f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(closed, transform.localScale);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(open, transform.localScale);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(closed, open);
    }
}
