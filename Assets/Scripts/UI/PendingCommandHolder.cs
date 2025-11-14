using UnityEngine;

public class PendingCommandHolder : MonoBehaviour
{
    public static PendingCommandHolder Instance { get; private set; }

    private ICommand pendingCommand;
    private UnitAgent pendingAgent;

    void Awake() { Instance = this; }

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
            // special-case move command id
            if (pendingCommand.Id == "move")
            {
                MoveCommandHandler.ExecuteMove(pendingAgent, target);
            }
            else
            {
                pendingCommand.Execute(pendingAgent, target);
            }
        }
        Clear();
    }
}
