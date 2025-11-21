using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HiveUI : MonoBehaviour
{
    public HiveManager hiveManager;
    public TextMeshProUGUI storedText;

    void Start()
    {
        if (hiveManager == null) hiveManager = HiveManager.Instance;

        // 초기 텍스트 설정
        UpdateResourceText();
    }

    void OnEnable()
    {
        // HiveManager의 자원 변경 이벤트 구독
        if (hiveManager != null)
        {
            hiveManager.OnResourcesChanged += UpdateResourceText;
        }
        else if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += UpdateResourceText;
        }
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        if (hiveManager != null)
        {
            hiveManager.OnResourcesChanged -= UpdateResourceText;
        }
        else if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged -= UpdateResourceText;
        }
    }

    /// <summary>
    /// 자원 텍스트 업데이트 (이벤트 핸들러)
    /// </summary>
    void UpdateResourceText()
    {
        if (hiveManager == null) hiveManager = HiveManager.Instance;
        if (hiveManager == null || storedText == null) return;

        // Display player's total stored resources
        storedText.text = $"<color=#FFFF00>꿀</color> : <color=#00FF00>{hiveManager.playerStoredResources}</color>";
    }
}
