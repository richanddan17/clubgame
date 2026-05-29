using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
    [Header("이동 설정")]
    public string sceneToLoad; // 이동할 씬 이름 (예: mainscene)
    public int levelNumber;    // 레벨 번호 (1, 2, ...)

    [Header("시각적 피드백 (선택사항)")]
    public GameObject instructionUI; // "E를 누르세요" 같은 UI 오브젝트

    private bool isPlayerNearby = false;

    private void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (instructionUI != null) instructionUI.SetActive(true);
            Debug.Log("레벨 " + levelNumber + " 입장 가능! (E키를 누르세요)");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (instructionUI != null) instructionUI.SetActive(false);
        }
    }

    private void Update()
    {
        // 플레이어가 근처에 있고 E 키를 누르면 씬 전환
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.Log("레벨 " + levelNumber + "으로 이동 중...");
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError("Scene To Load 이름이 설정되지 않았습니다!");
            }
        }
    }
}
