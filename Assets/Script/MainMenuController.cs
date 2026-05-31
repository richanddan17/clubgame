using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 메인 메뉴(오프닝 화면) 제어 스크립트
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("씬 설정")]
    public string newGameSceneName = "LobbyScene"; // 새 게임 시작 시 이동할 씬

    /// <summary>
    /// 새 게임 시작
    /// </summary>
    public void NewGame()
    {
        Debug.Log("새 게임을 시작합니다.");
        // 데이터 초기화 로직이 필요하다면 여기에 추가
        SceneManager.LoadScene(newGameSceneName);
    }

    /// <summary>
    /// 게임 불러오기
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("저장된 게임을 불러옵니다.");
        // 세이브 시스템 구현 후 로직 추가
    }

    /// <summary>
    /// 설정 열기
    /// </summary>
    public void OpenSettings()
    {
        Debug.Log("설정 메뉴를 엽니다.");
        // 설정 팝업 UI 활성화 로직 추가
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
