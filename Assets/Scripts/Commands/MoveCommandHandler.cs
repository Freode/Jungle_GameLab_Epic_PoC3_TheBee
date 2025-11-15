using UnityEngine;
using System.Collections.Generic;

// Lightweight handler that interprets SOCommand with id "move"
public class MoveCommandHandler : MonoBehaviour
{
    public static void ExecuteMove(UnitAgent agent, CommandTarget target)
    {
        if (agent == null) return;
        if (target.type != CommandTargetType.Tile) return;

        var tm = TileManager.Instance;
        if (tm == null) return;

        var start = tm.GetTile(agent.q, agent.r);
        var dest = tm.GetTile(target.q, target.r);
        
        // Check if worker can move to target (based on hive presence)
        if (!agent.CanMoveToTile(target.q, target.r))
        {
            Debug.Log($"Worker cannot move to ({target.q},{target.r}): outside activity radius");
            return;
        }
        
        var path = Pathfinder.FindPath(start, dest);
        if (path == null) return;

        // Mark as manual order if worker is following queen
        if (agent.isFollowingQueen)
        {
            agent.hasManualOrder = true;
            agent.isFollowingQueen = false;
        }

        var ctrl = agent.GetComponent<UnitController>();
        if (ctrl == null) ctrl = agent.gameObject.AddComponent<UnitController>();
        ctrl.agent = agent;
        ctrl.SetPath(path);
    }
}
