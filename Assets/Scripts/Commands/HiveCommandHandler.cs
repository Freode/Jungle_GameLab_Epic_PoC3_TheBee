using UnityEngine;

public class HiveCommandHandler : MonoBehaviour
{
    public static void ExecuteHiveCommand(UnitAgent agent, string commandId, CommandTarget target)
    {
        // agent is the unit that issued the command (queen)
        // find Hive component at same coordinates
        var hives = GameObject.FindObjectsOfType<Hive>();
        Hive found = null;
        foreach (var h in hives)
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
