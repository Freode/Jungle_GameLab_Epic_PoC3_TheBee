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
    }

    void Update()
    {
        if (hiveManager == null || storedText == null) return;
        
        // Display player's total stored resources
        storedText.text = $"Stored: {hiveManager.playerStoredResources}";
    }
}
