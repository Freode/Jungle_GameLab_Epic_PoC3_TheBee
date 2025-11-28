using UnityEngine;
using TMPro;

/// <summary>
/// Worker count UI showing total and per-role counts.
/// </summary>
public class WorkerCountUI : MonoBehaviour
{
    public static WorkerCountUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI workerCountText;
    
    [Header("Settings")]
    [Tooltip("Show debug logs")]
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
        // initial text
        if (workerCountText != null)
        {
            if (HiveManager.Instance != null)
                ForceUpdate();
            else
                workerCountText.text = $"ÀÏ²Û ¼ö: <color=#00FF00>0</color>[0/0/0]  //  ÃÖ´ë: <color=#00FF00>0</color>[0/0/0]";
        }

        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnWorkerCountChanged += OnWorkerCountChanged;
            if (showDebugLogs) Debug.Log("[WorkerCountUI] Subscribed to HiveManager.OnWorkerCountChanged");
        }
    }

    void OnDestroy()
    {
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnWorkerCountChanged -= OnWorkerCountChanged;
            if (showDebugLogs) Debug.Log("[WorkerCountUI] Unsubscribed from HiveManager.OnWorkerCountChanged");
        }
    }

    void OnWorkerCountChanged(int currentWorkers, int maxWorkers)
    {
        if (workerCountText == null) return;
        if (HiveManager.Instance == null)
        {
            workerCountText.text = $"ÀÏ²Û ¼ö: <color=#00FF00>{currentWorkers}</color>[0/0/0]  //  ÃÖ´ë: <color=#00FF00>{maxWorkers}</color>[0/0/0]";
            return;
        }

        int totalCurrent = HiveManager.Instance.GetCurrentWorkers();
        int totalMax = HiveManager.Instance.GetMaxWorkers();

        int gatherCur = HiveManager.Instance.GetSquadCount(WorkerSquad.Squad1);
        int attackerCur = HiveManager.Instance.GetSquadCount(WorkerSquad.Squad2);
        int tankCur = HiveManager.Instance.GetSquadCount(WorkerSquad.Squad3);

        int gatherMax = HiveManager.Instance.GetMaxWorkersForRole(RoleType.Gatherer);
        int attackerMax = HiveManager.Instance.GetMaxWorkersForRole(RoleType.Attacker);
        int tankMax = HiveManager.Instance.GetMaxWorkersForRole(RoleType.Tank);

        string gatherColor = ColorUtility.ToHtmlStringRGB(HiveManager.Instance.squad1Color);
        string attackerColor = ColorUtility.ToHtmlStringRGB(HiveManager.Instance.squad2Color);
        string tankColor = ColorUtility.ToHtmlStringRGB(HiveManager.Instance.squad3Color);

        workerCountText.text =
            $"ÀÏ¹ú ¼ö: <color=#00FF00>{totalCurrent}</color>[<color=#{gatherColor}>{gatherCur}</color>/<color=#{attackerColor}>{attackerCur}</color>/<color=#{tankColor}>{tankCur}</color>]\n//  " +
            $"ÃÖ´ë: <color=#00FF00>{totalMax}</color>[<color=#{gatherColor}>{gatherMax}</color>/<color=#{attackerColor}>{attackerMax}</color>/<color=#{tankColor}>{tankMax}</color>]";

        if (showDebugLogs) Debug.Log($"[WorkerCountUI] Updated: {totalCurrent}/{totalMax} [{gatherCur}/{gatherMax} {attackerCur}/{attackerMax} {tankCur}/{tankMax}]");
    }

    public void ForceUpdate()
    {
        if (HiveManager.Instance != null)
        {
            OnWorkerCountChanged(HiveManager.Instance.GetCurrentWorkers(), HiveManager.Instance.GetMaxWorkers());
        }
    }
}
