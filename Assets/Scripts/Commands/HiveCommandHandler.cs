using UnityEngine;

public class HiveCommandHandler : MonoBehaviour
{
    public static void ExecuteHiveCommand(UnitAgent agent, string commandId, CommandTarget target)
    {
        if (agent == null) return;
        // use HiveManager registry instead of FindObjectsOfType
        var hm = HiveManager.Instance;
        if (hm == null) return;

        Hive found = null;
        foreach (var h in hm.GetAllHives())
        {
            if (h.q == agent.q && h.r == agent.r)
            {
                found = h;
                break;
            }
        }
        if (found == null) return;

        found.IssueCommandToWorkers(commandId, target);
    }
}
