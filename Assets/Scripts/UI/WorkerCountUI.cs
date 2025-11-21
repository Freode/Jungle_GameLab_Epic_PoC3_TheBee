using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 하이브의 현재/최대 일꾼 수를 화면에 표시
/// 이벤트 기반으로 업데이트 (interval 사용 안 함) ?
/// </summary>
public class WorkerCountUI : MonoBehaviour
{
    public static WorkerCountUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI workerCountText;
    
    [Header("Settings")]
    [Tooltip("디버그 로그 표시")]
    [SerializeField] private bool showDebugLogs = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 초기 텍스트 설정
        if (workerCountText != null)
        {
            workerCountText.text = $"일꾼 : <color=#00FF00>0</color>/{HiveManager.Instance.GetMaxWorkers()}";
        }

        // HiveManager의 일꾼 수 변경 이벤트 구독 ?
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnWorkerCountChanged += OnWorkerCountChanged;

            if (showDebugLogs)
                Debug.Log("[WorkerCountUI] 일꾼 수 변경 이벤트 구독");

            // 초기 업데이트
            ForceUpdate();
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 ?
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnWorkerCountChanged -= OnWorkerCountChanged;
            
            if (showDebugLogs)
                Debug.Log("[WorkerCountUI] 일꾼 수 변경 이벤트 구독 해제");
        }
    }

    /// <summary>
    /// 일꾼 수 변경 이벤트 핸들러 ?
    /// </summary>
    void OnWorkerCountChanged(int currentWorkers, int maxWorkers)
    {
        if (workerCountText == null) return;
        
        //// 텍스트 업데이트 (색상 포함)
        //if (currentWorkers >= maxWorkers)
        //{
        //    // 최대치 도달 - 노란색
        //    workerCountText.text = $"일꾼: <color=#00FF00>{currentWorkers}</color>/{maxWorkers}";
        //}
        //else if (currentWorkers >= maxWorkers * 0.7f)
        //{
        //    // 70% 이상 - 초록색
        //    workerCountText.text = $"일꾼: <color=#00FF00>{currentWorkers}</color>/{maxWorkers}";
        //}
        //else if (currentWorkers >= maxWorkers * 0.4f)
        //{
        //    // 40% 이상 - 흰색
        //    workerCountText.text = $"일꾼: {currentWorkers}/{maxWorkers}";
        //}
        //else
        //{
        //    // 40% 미만 - 빨간색
        //    workerCountText.text = $"일꾼: <color=#FF0000>{currentWorkers}/{maxWorkers}</color>";
        //}

        workerCountText.text = $"일꾼: <color=#00FF00>{currentWorkers}</color>/{maxWorkers}";

        if (showDebugLogs)
            Debug.Log($"[WorkerCountUI] 업데이트: {currentWorkers}/{maxWorkers}");
    }

    /// <summary>
    /// 즉시 업데이트 (외부 호출용)
    /// HiveManager에 수동으로 이벤트 발생 요청
    /// </summary>
    public void ForceUpdate()
    {
        if (HiveManager.Instance != null)
        {
            // HiveManager의 현재 값으로 직접 업데이트 ?
            OnWorkerCountChanged(HiveManager.Instance.currentWorkers, HiveManager.Instance.maxWorkers);
        }
    }
}
