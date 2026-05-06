using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    /// <summary>
    /// 카메라가 플레이어를 따라다니는 스크립트
    /// </summary>

    [Header("추적 설정")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -10);

    private Vector3 desiredPosition;

    /// <summary>
    /// 물리 연산 후 카메라 위치 업데이트
    /// LateUpdate 사용시 플레이어 이동 후 카메라가 따라와서 더 부드러움
    /// </summary>
    void LateUpdate()
    {
        if (target == null) return;

        desiredPosition.x = target.position.x + offset.x;
        desiredPosition.y = target.position.y + offset.y;
        desiredPosition.z = offset.z; // z는 항상 고정

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}