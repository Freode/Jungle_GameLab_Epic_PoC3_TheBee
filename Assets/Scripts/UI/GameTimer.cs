using UnityEngine;
using TMPro;

/// <summary>
/// 전역 게임 시간 타이머
/// - Time.timeScale 영향을 받아 일시정지 시에는 멈춤
/// - mm:ss 포맷으로 업데이트
/// - UI Text (TextMeshProUGUI) 에 연결하여 표시
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    public TextMeshProUGUI timerText;

    private float elapsedTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateTimerText();
    }

    void Update()
    {
        // elapsedTime respects Time.timeScale because we use scaled deltaTime
        if (Time.timeScale > 0f)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText();
        }
    }

    void UpdateTimerText()
    {
        if (timerText == null) return;
        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = string.Format("시간 : <color=#00FF00>{0:00}:{1:00}</color>", minutes, seconds);
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    public float GetElapsedTime() => elapsedTime;
}
