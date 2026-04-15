using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    /// <summary>
    /// 카메라가 플레이어를 따라다니는 스크립트
    /// </summary>

    [Header("추적 설정")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(-7, 9.5f, 0);

    private Vector3 desiredPosition;

    void Awake()
    {
        Debug.Log("CameraFollow script initialized on: " + gameObject.name);
    }

    void Start()
    {
        Debug.Log("CameraFollow Start called. Target: " + (target != null ? target.name : "null"));
    }
    void LateUpdate()
    {
        if (target == null) 
        {
            Debug.Log("Target is null!");
            return;
        }

        float direction = target.localScale.x > 0 ? 1 : -1;
        desiredPosition.x = target.position.x + offset.x * direction;
        desiredPosition.y = target.position.y + offset.y;
        desiredPosition.z = offset.z; // z는 항상 고정

        Debug.Log("Camera position updated to: " + desiredPosition);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}