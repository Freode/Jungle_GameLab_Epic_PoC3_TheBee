using UnityEngine;

// Helper to wire panel prefab fields in editor without manual script hookup
public class UnitCommandPanelUI : MonoBehaviour
{
    public UnitCommandPanel panel;

    void Start()
    {
        if (panel == null) panel = GetComponent<UnitCommandPanel>();
    }
}
