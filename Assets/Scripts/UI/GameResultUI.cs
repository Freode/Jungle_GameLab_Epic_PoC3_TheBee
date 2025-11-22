using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 게임 승리/패배 결과 UI
/// </summary>
public class GameResultUI : MonoBehaviour
{
    public static GameResultUI Instance { get; private set; }
    
    [Header("UI References")]
    public GameObject resultPanel; // 결과 패널
    public TextMeshProUGUI titleText; // "승리!" 또는 "패배"
    public TextMeshProUGUI messageText; // 상세 메시지
    public Button restartButton; // 재시작 버튼
    public Button quitButton; // 종료 버튼
    
    [Header("Colors")]
    public Color victoryColor = new Color(1f, 0.84f, 0f); // 금색
    public Color defeatColor = new Color(0.8f, 0.2f, 0.2f); // 빨강
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 초기 숨김
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 버튼 이벤트 연결
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    /// <summary>
    /// 승리 UI 표시
    /// </summary>
    public void ShowVictory()
    {
        if (resultPanel == null) return;
        
        resultPanel.SetActive(true);
        
        if (titleText != null)
        {
            titleText.text = "승리!";
            titleText.color = victoryColor;
        }
        
        if (messageText != null)
        {
            messageText.text = "장수말벌집을 파괴했습니다!\n숲의 평화를 되찾았습니다.";
        }
        
        Debug.Log("[GameResultUI] 승리 화면 표시");
    }
    
    /// <summary>
    /// 패배 UI 표시
    /// </summary>
    public void ShowDefeat()
    {
        if (resultPanel == null) return;
        
        resultPanel.SetActive(true);
        
        if (titleText != null)
        {
            titleText.text = "패배";
            titleText.color = defeatColor;
        }
        
        if (messageText != null)
        {
            messageText.text = "여왕벌이 사망했습니다...\n벌집의 미래가 암담합니다.";
        }
        
        Debug.Log("[GameResultUI] 패배 화면 표시");
    }
    
    /// <summary>
    /// 재시작 버튼 클릭
    /// </summary>
    void OnRestartClicked()
    {
        Debug.Log("[GameResultUI] 재시작 버튼 클릭");
        
        // 시간 정상화
        Time.timeScale = 1f;
        
        // 현재 씬 재로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>
    /// 종료 버튼 클릭
    /// </summary>
    void OnQuitClicked()
    {
        Debug.Log("[GameResultUI] 종료 버튼 클릭");
        
        // 에디터에서는 플레이 모드 중단
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 빌드에서는 앱 종료
        Application.Quit();
        #endif
    }
}
