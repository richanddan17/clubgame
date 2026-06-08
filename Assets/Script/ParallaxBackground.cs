using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("설정 (1: 멀리 있음, 0.1: 가까이 있음)")]
    [Range(0, 1)]
    public float parallaxEffect; 
    public int backgroundLayerIndex; // BiomeData의 backgroundLayers 중 몇 번째인지
    
    private Transform cam;
    private float length, startpos;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (Camera.main != null) cam = Camera.main.transform;
        
        if (cam == null) return;

        UpdateStartSettings();

        // 바이옴 변경 이벤트 구독
        if (BiomeManager.Instance != null)
        {
            BiomeManager.Instance.OnBiomeChanged += HandleBiomeChanged;
        }
    }

    private void OnDestroy()
    {
        if (BiomeManager.Instance != null)
        {
            BiomeManager.Instance.OnBiomeChanged -= HandleBiomeChanged;
        }
    }

    private void HandleBiomeChanged(BiomeData newData)
    {
        if (newData.backgroundLayers != null && backgroundLayerIndex < newData.backgroundLayers.Length)
        {
            Sprite newSprite = newData.backgroundLayers[backgroundLayerIndex];
            if (newSprite != null)
            {
                sr.sprite = newSprite;
                UpdateStartSettings(); // 스프라이트가 바뀌었으므로 길이 등 재계산

                // 자식(복제본)들도 업데이트
                foreach (Transform child in transform)
                {
                    var childSr = child.GetComponent<SpriteRenderer>();
                    if (childSr != null) childSr.sprite = newSprite;
                }
            }
        }
    }

    private void UpdateStartSettings()
    {
        startpos = transform.position.x;
        if (sr != null && sr.sprite != null)
        {
            length = sr.bounds.size.x;
            
            // 기존 복제본 삭제 (재설정 시)
            foreach (Transform child in transform)
            {
                // 간단히 하기 위해 파괴 (성능 최적화 필요 시 재사용 로직 고려)
                if (child.name.Contains("Left") || child.name.Contains("Right"))
                    Destroy(child.gameObject);
            }

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
