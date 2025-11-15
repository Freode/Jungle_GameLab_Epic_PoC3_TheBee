using UnityEngine;
using System.Collections.Generic;

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

        // Ensure Hive component exists
        Hive hive = go.GetComponent<Hive>();
        if (hive == null) hive = go.AddComponent<Hive>();
        
        // Add UnitAgent to hive so it can be selected and receive commands
        var hiveAgent = go.GetComponent<UnitAgent>();
        if (hiveAgent == null) hiveAgent = go.AddComponent<UnitAgent>();
        
        hiveAgent.q = q;
        hiveAgent.r = r;
        hiveAgent.canMove = false; // Hive cannot move
        hiveAgent.faction = Faction.Player;
        hiveAgent.SetPosition(q, r);
        
        // If constructing agent is a queen with a previous hive, reclaim workers
        if (agent.isQueen && agent.homeHive != null)
        {
            var oldHive = agent.homeHive;
            
            // Transfer workers from old hive to new hive
            List<UnitAgent> workers = oldHive.GetWorkers();
            foreach (var worker in workers)
            {
                if (worker == null) continue;
                
                // Update worker's home hive reference
                worker.homeHive = hive;
                worker.isFollowingQueen = false;
                worker.hasManualOrder = false;
                
                // Move worker to new hive location
                var start = TileManager.Instance.GetTile(worker.q, worker.r);
                var dest = TileManager.Instance.GetTile(q, r);
                if (start != null && dest != null)
                {
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null)
                    {
                        var ctrl = worker.GetComponent<UnitController>();
                        if (ctrl == null) ctrl = worker.gameObject.AddComponent<UnitController>();
                        ctrl.agent = worker;
                        ctrl.SetPath(path);
                    }
                }
            }
        }
        
        // Update queen's home hive and hive's queen reference
        if (agent.isQueen)
        {
            agent.homeHive = hive;
            hive.queenBee = agent;
        }
        
        // assign worker prefab from GameManager
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
