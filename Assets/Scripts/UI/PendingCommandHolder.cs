using UnityEngine;

public class PendingCommandHolder : MonoBehaviour
{
    public static PendingCommandHolder Instance { get; private set; }

    private ICommand pendingCommand;
    private UnitAgent pendingAgent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Ensure there is an instance in the scene; create one if missing
    public static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("PendingCommandHolder");
        Instance = go.AddComponent<PendingCommandHolder>();
    }

    public void SetPendingCommand(ICommand cmd, UnitAgent agent)
    {
        pendingCommand = cmd;
        pendingAgent = agent;
    }

    public void Clear()
    {
        pendingCommand = null;
        pendingAgent = null;
    }

    public bool HasPending => pendingCommand != null && pendingAgent != null;

    public void ExecutePending(CommandTarget target)
    {
        if (pendingCommand != null && pendingAgent != null)
        {
            // handle common command ids
            switch (pendingCommand.Id)
            {
                case "move":
                    MoveCommandHandler.ExecuteMove(pendingAgent, target);
                    break;
                case "construct_hive":
                    Debug.Log("Hive2");
                    ConstructHiveHandler.ExecuteConstruct(pendingAgent, target);
                    break;
                case "hive_explore":
                case "hive_gather":
                case "hive_attack":
                    HiveCommandHandler.ExecuteHiveCommand(pendingAgent, pendingCommand.Id, target);
                    break;
                default:
                    // fallback to SOCommand behavior
                    pendingCommand.Execute(pendingAgent, target);
                    break;
            }
        }
        Clear();
    }
}
