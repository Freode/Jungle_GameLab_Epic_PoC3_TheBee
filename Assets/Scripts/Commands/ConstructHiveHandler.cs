using UnityEngine;

public class ConstructHiveHandler : MonoBehaviour
{
    // Construct a hive at the agent's current tile (e.g., queen builds hive at her position)
    public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
    {
        if (agent == null) return;

        var tm = TileManager.Instance;
        if (tm == null) return;

        // Use agent's current axial coords rather than the passed target
        int q = agent.q;
        int r = agent.r;

        var tile = tm.GetTile(q, r);
        if (tile == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;
        var hivePrefab = gm.hivePrefab;
        if (hivePrefab == null) return;

        Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
        var go = GameObject.Instantiate(hivePrefab, pos, Quaternion.identity);

        // If the hive prefab mistakenly contains a UnitAgent, ensure it's positioned correctly and not treated as a free unit
        var possibleAgent = go.GetComponent<UnitAgent>();
        if (possibleAgent != null)
        {
            possibleAgent.q = q;
            possibleAgent.r = r;
            possibleAgent.canMove = false;
            possibleAgent.SetPosition(q, r);
        }

        // Ensure Hive component exists and initialize
        Hive hive = go.GetComponent<Hive>();
        if (hive == null) hive = go.AddComponent<Hive>();
        // assign worker prefab from GameManager (renamed to normalBeePrefab)
        hive.workerPrefab = gm.normalBeePrefab;
        hive.Initialize(q, r);

        // Enable HexBoundaryHighlighter singleton (if present) so boundaries are available immediately
        if (HexBoundaryHighlighter.Instance != null)
        {
            var bh = HexBoundaryHighlighter.Instance;
            if (bh.gameObject != null && !bh.gameObject.activeInHierarchy)
            {
                bh.gameObject.SetActive(true);
            }
            bh.enabled = true;
        }

        // Update fog visibility after hive placed (in case position matters)
        FogOfWarManager.Instance?.RecalculateVisibility();
    }
}
