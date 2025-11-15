using UnityEngine;

[CreateAssetMenu(menuName = "Commands/SOCommand", fileName = "SOCommand")]
public class SOCommand : ScriptableObject, ICommand
{
    public string id;
    public string displayName;
    public Sprite icon;
    public bool requiresTarget = true;
    public bool hidePanelOnClick = false;

    [Header("Resource Requirements")]
    public int resourceCost = 0;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool RequiresTarget => requiresTarget;
    public bool HidePanelOnClick => hidePanelOnClick;

    // Default availability checks ? can be overridden by subclassing or by external validator
    public virtual bool IsAvailable(UnitAgent agent)
    {
        if (agent == null) return false;

        // Special check for relocate_hive command
        if (id == "relocate_hive")
        {
            Hive hive = agent.GetComponent<Hive>();
            if (hive == null) return false;
            
            // Check if already relocating
            if (hive.isRelocating) return false;
            
            // Check resource cost from HiveManager
            if (resourceCost > 0 && HiveManager.Instance != null)
            {
                if (!HiveManager.Instance.HasResources(resourceCost))
                {
                    return false;
                }
            }
            
            return true;
        }

        // Check resource cost if this command requires resources
        if (resourceCost > 0 && HiveManager.Instance != null)
        {
            if (!HiveManager.Instance.HasResources(resourceCost))
            {
                return false;
            }
        }

        return true;
    }

    // Execute is abstract-ish: by default we just log; real behavior should be implemented in CommandHandlers
    public virtual void Execute(UnitAgent agent, CommandTarget target)
    {
        Debug.Log($"Executing SOCommand {displayName} for agent {agent?.name} target {target.type}");
        // route common commands
        switch (id)
        {
            case "move":
                MoveCommandHandler.ExecuteMove(agent, target);
                break;
            case "construct_hive":
                ConstructHiveHandler.ExecuteConstruct(agent, target);
                break;
            case "relocate_hive":
                RelocateHiveCommandHandler.ExecuteRelocate(agent, target);
                break;
            case "hive_explore":
            case "hive_gather":
            case "hive_attack":
                HiveCommandHandler.ExecuteHiveCommand(agent, id, target);
                break;
        }
    }
}
