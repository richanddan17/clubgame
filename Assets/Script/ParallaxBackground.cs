using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("설정 (1: 멀리 있음, 0.1: 가까이 있음)")]
    [Range(0, 1)]
    public float parallaxEffect; 
    
    private Transform cam;
    private float length, startpos;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        
        if (cam == null) return;

        startpos = transform.position.x;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 실제 월드 크기 너비 계산
            length = sr.bounds.size.x;

            // [개선] 양옆으로 배경을 복제하여 무한 루프 시 빈 공간 방지
            CreateDuplicate(sr, -1, "Left");
            CreateDuplicate(sr, 1, "Right");
        }
    }

    private void CreateDuplicate(SpriteRenderer originalSr, int side, string suffix)
    {
        GameObject duplicate = new GameObject(gameObject.name + "_" + suffix);
        duplicate.transform.SetParent(this.transform);
        duplicate.transform.localScale = Vector3.one;
        // 부모의 스케일을 고려하여 로컬 좌표로 정확히 너비만큼 이동 (side: -1 또는 1)
        duplicate.transform.localPosition = new Vector3((length * side) / transform.localScale.x, 0, 0);

        var sr2 = duplicate.AddComponent<SpriteRenderer>();
        sr2.sprite = originalSr.sprite;
        sr2.sortingLayerID = originalSr.sortingLayerID;
        sr2.sortingOrder = originalSr.sortingOrder;
        sr2.color = originalSr.color;
    }

    void LateUpdate()
    {
        if (cam == null || length <= 0) return;

        // 카메라 이동에 따른 배경 위치 계산
        float dist = (cam.position.x * parallaxEffect);
        // 루핑 체크를 위한 상대 거리 계산
        float temp = (cam.position.x * (1 - parallaxEffect));

        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // 무한 루프: 화면을 완전히 벗어나면 시작 지점 갱신 (오른쪽/왼쪽 대응)
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}
