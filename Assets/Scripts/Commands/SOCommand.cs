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
            return $"꿀: <color=#00FF00>{resourceCost}</color>";
        }
    }

    public string LevelCostText(int level = 1)
    {
        if (resourceCost <= 0) return "";
        return $"꿀 : <color=#00FF00>{resourceCost * level}</color>";
    }
    
    // ✅ 현재 레벨 가져오기 (기본 구현: 0 반환, SOUpgradeCommand에서 오버라이드)
    public virtual int GetCurrentLevel()
    {
        return 0;
    }

    // Default availability checks - can be overridden by subclassing or by external validator
    public virtual bool IsAvailable(UnitAgent agent)
    {
        if (agent == null) return false;

        // ✅ 꿀벌집 건설 명령: 꿀벌집이 이미 존재하면 비활성화 (요구사항 1)
        if (id == "construct_hive")
        {
            if (HiveManager.Instance != null)
            {
                // 플레이어 하이브가 1개 이상 존재하면 건설 불가
                int playerHiveCount = 0;
                foreach (var hive in HiveManager.Instance.GetAllHives())
                {
                    if (hive != null)
                    {
                        var hiveAgent = hive.GetComponent<UnitAgent>();
                        if (hiveAgent != null && hiveAgent.faction == Faction.Player)
                        {
                            playerHiveCount++;
                        }
                    }
                }

                if (playerHiveCount > 0)
                {
                    return false; // 꿀벌집이 이미 존재하므로 건설 불가
                }

                // If a player-initiated construction is already in progress, disable construct command immediately
                if (HiveManager.Instance.isConstructingHive)
                {
                    return false;
                }
            }
        }
        //착륙(Land) 버튼 활성화 조건
        if (id == "land_hive")
        {
            // 여왕벌이 하이브를 들고 있고(carriedHive != null), 현재 떠있는 상태일 때만 버튼 표시
            return agent.carriedHive != null && agent.carriedHive.isFloating;
        }

        //이사(Relocate) 버튼 활성화 조건 (하이브용)
        if (id == "relocate_hive")
        {
            Hive hive = agent.GetComponent<Hive>();
            // 이미 떠있으면 이사 버튼 숨김 (이미 이사 중이니까)
            if (hive != null && hive.isFloating) return false;
        }

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
                
            //착륙 명령 추가
            case "land_hive":
                LandHiveCommandHandler.ExecuteLand(agent);
                break;
            case "hive_explore":
            case "hive_gather":
            case "hive_attack":
                HiveCommandHandler.ExecuteHiveCommand(agent, id, target);
                break;
            
            //페르몬 명령 (요구사항 1)
            case "pheromone_squad1":
                QueenPheromoneCommandHandler.ExecutePheromone(agent, target, WorkerSquad.Squad1);
                break;
            case "pheromone_squad2":
                QueenPheromoneCommandHandler.ExecutePheromone(agent, target, WorkerSquad.Squad2);
                break;
            case "pheromone_squad3":
                QueenPheromoneCommandHandler.ExecutePheromone(agent, target, WorkerSquad.Squad3);
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
