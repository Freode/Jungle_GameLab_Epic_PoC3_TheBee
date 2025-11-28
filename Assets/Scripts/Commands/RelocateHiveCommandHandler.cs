using UnityEngine;

public static class RelocateHiveCommandHandler
{
    public static void ExecuteRelocate(UnitAgent agent, CommandTarget target)
    {
        Debug.Log("[RelocateHive] ExecuteRelocate 시작");
        
        if (agent == null)
        {
            Debug.LogWarning("[RelocateHive] agent is null");
            return;
        }

        Debug.Log($"[RelocateHive] agent: {agent.name}, q={agent.q}, r={agent.r}");

        // Find the hive at agent's position (agent should be the hive's UnitAgent component)
        Hive hive = agent.GetComponent<Hive>();
        
        if (hive == null)
        {
            Debug.Log("[RelocateHive] agent에 Hive 컴포넌트 없음, 위치로 검색 시도");
            // If agent doesn't have Hive component, try to find hive at agent's location
            hive = FindHiveAtPosition(agent.q, agent.r);
        }
        
        if (hive == null)
        {
            Debug.LogWarning("[RelocateHive] No hive found");
            return;
        }

        Debug.Log($"[RelocateHive] 하이브 발견: {hive.name}");

        // Check if hive can relocate (not already relocating)
        if (!hive.CanRelocate())
        {
            Debug.LogWarning("[RelocateHive] 하이브가 이미 이사 중입니다");
            return;
        }

        // Get resource cost from the relocate_hive command
        int resourceCost = GetRelocateResourceCost(hive);
        Debug.Log($"[RelocateHive] 필요 자원: {resourceCost}");
        
        // Check if HiveManager has enough resources
        if (HiveManager.Instance == null)
        {
            Debug.LogError("[RelocateHive] HiveManager.Instance is null");
            return;
        }
        
        int available = HiveManager.Instance.playerStoredResources;
        Debug.Log($"[RelocateHive] 현재 자원: {available}");
        
        if (!HiveManager.Instance.HasResources(resourceCost))
        {
            Debug.LogWarning($"[RelocateHive] 자원이 부족합니다. 필요: {resourceCost}, 보유: {available}");
            return;
        }

        Debug.Log($"[RelocateHive] 자원 충분. 이사 시작!");
        
        // Start relocation process
        hive.LiftHive();
    }

    private static int GetRelocateResourceCost(Hive hive)
    {
        // Find relocate_hive command from hive's commands
        var commands = hive.GetCommands(null);
        foreach (var cmd in commands)
        {
            if (cmd is SOCommand soCmd && soCmd.id == "relocate_hive")
            {
                Debug.Log($"[GetResourceCost] relocate_hive 명령 찾음, 비용: {soCmd.resourceCost}");
                return soCmd.resourceCost;
            }
        }
        
        // Default to 0 if not found (이사 비용 무료)
        Debug.LogWarning("[GetResourceCost] relocate_hive 명령을 찾을 수 없습니다. 기본 비용 0 사용");
        return 0;
    }

    private static Hive FindHiveAtPosition(int q, int r)
    {
        // Find hive from HiveManager
        if (HiveManager.Instance == null)
        {
            Debug.LogWarning("[FindHiveAtPosition] HiveManager.Instance is null");
            return null;
        }
        
        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive.q == q && hive.r == r)
            {
                Debug.Log($"[FindHiveAtPosition] 하이브 발견: ({q}, {r})");
                return hive;
            }
        }
        
        Debug.LogWarning($"[FindHiveAtPosition] ({q}, {r}) 위치에 하이브 없음");
        return null;
    }
}
