using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HiveUI : MonoBehaviour
{
    public HiveManager hiveManager;
    public TextMeshProUGUI storedText;
    public int selectedHiveIndex = 0;

    void Start()
    {
        if (hiveManager == null) hiveManager = HiveManager.Instance;
    }

    void Update()
    {
        if (hiveManager == null || storedText == null) return;
        var hives = hiveManager.GetAllHives();
        int i = 0;
        foreach (var h in hives)
        {
            if (i == selectedHiveIndex)
            {
                storedText.text = $"Stored: {h.storedResources}";
                return;
            }
            i++;
        }
        storedText.text = "No hive";
    }
}
