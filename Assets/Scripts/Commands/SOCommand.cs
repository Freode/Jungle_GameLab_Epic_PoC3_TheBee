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
    
    // ICommand 비용 프로퍼티 구현
    public int ResourceCost => resourceCost;
    public string CostText
    {
        get
        {
            if (resourceCost <= 0) return "";
            return $"비용: {resourceCost}";
        }
    }

    // Default availability checks - can be overridden by subclassing or by external validator
    public virtual bool IsAvailable(UnitAgent agent)
    {
        if (agent == null) return false;

        // Check if command requires a hive
        if (RequiresHive())
        {
            Hive hive = agent.GetComponent<Hive>();
            if (hive == null) return false;
            
            // Special check for relocate_hive command
            if (id == "relocate_hive" && hive.isRelocating)
                return false;
        }

        // Check resource cost
        if (resourceCost > 0 && HiveManager.Instance != null)
        {
            if (!HiveManager.Instance.HasResources(resourceCost))
            {
                return false;
            }
        }

        return true;
    }

    // Execute is virtual: default implementation routes to handlers
    public virtual void Execute(UnitAgent agent, CommandTarget target)
    {
        Debug.Log($"Executing SOCommand {displayName} for agent {agent?.name} target {target.type}");
        
        // Route commands to appropriate handlers
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
            
            // Upgrade commands
            case "upgrade_hive_range":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.HiveRange, resourceCost);
                break;
            case "upgrade_worker_attack":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.WorkerAttack, resourceCost);
                break;
            case "upgrade_worker_health":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.WorkerHealth, resourceCost);
                break;
            case "upgrade_worker_speed":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.WorkerSpeed, resourceCost);
                break;
            case "upgrade_hive_health":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.HiveHealth, resourceCost);
                break;
            case "upgrade_max_workers":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.MaxWorkers, resourceCost);
                break;
            case "upgrade_gather_amount":
                UpgradeCommandHandler.ExecuteUpgrade(UpgradeType.GatherAmount, resourceCost);
                break;
        }
    }

    // Helper to check if command requires a hive
    private bool RequiresHive()
    {
        return id == "relocate_hive" || 
               id == "hive_explore" || 
               id == "hive_gather" || 
               id == "hive_attack" ||
               id.StartsWith("upgrade_");
    }
}
