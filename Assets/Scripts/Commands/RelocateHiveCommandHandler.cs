using UnityEngine;

public static class RelocateHiveCommandHandler
{
    public static void ExecuteRelocate(UnitAgent agent, CommandTarget target)
    {
        if (agent == null)
        {
            Debug.LogWarning("RelocateHiveCommandHandler: agent is null");
            return;
        }

        // Find the hive at agent's position (agent should be the hive's UnitAgent component)
        Hive hive = agent.GetComponent<Hive>();
        
        if (hive == null)
        {
            // If agent doesn't have Hive component, try to find hive at agent's location
            hive = FindHiveAtPosition(agent.q, agent.r);
        }
        
        if (hive == null)
        {
            Debug.LogWarning("RelocateHiveCommandHandler: No hive found");
            return;
        }

        // Check if hive can relocate (not already relocating)
        if (!hive.CanRelocate())
        {
            Debug.LogWarning("하이브가 이미 이사 중입니다");
            return;
        }

        // Get resource cost from the relocate_hive command
        int resourceCost = GetRelocateResourceCost(hive);
        Debug.Log($"[RelocateHive] SOCommand resourceCost: {resourceCost}"); // 디버그 로그
        
        // Check if HiveManager has enough resources
        if (HiveManager.Instance == null || !HiveManager.Instance.HasResources(resourceCost))
        {
            int available = HiveManager.Instance != null ? HiveManager.Instance.playerStoredResources : 0;
            Debug.LogWarning($"자원이 부족합니다. 필요: {resourceCost}, 보유: {available}");
            return;
        }

        // Start relocation process
        hive.StartRelocation(resourceCost);
    }

    private static int GetRelocateResourceCost(Hive hive)
    {
        // Find relocate_hive command from hive's commands
        var commands = hive.GetCommands(null);
        foreach (var cmd in commands)
        {
            if (cmd is SOCommand soCmd && soCmd.id == "relocate_hive")
            {
                Debug.Log($"[GetResourceCost] Found relocate_hive command with resourceCost: {soCmd.resourceCost}");
                return soCmd.resourceCost;
            }
        }
        
        // Default to 50 if not found (should not happen)
        Debug.LogWarning("relocate_hive command not found in hive.hiveCommands array! Using default cost 50");
        return 50;
    }

    private static Hive FindHiveAtPosition(int q, int r)
    {
        // Find hive from HiveManager
        if (HiveManager.Instance == null) return null;
        
        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive.q == q && hive.r == r)
            {
                return hive;
            }
        }
        
        return null;
    }
}
