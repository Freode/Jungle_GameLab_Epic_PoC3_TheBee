using UnityEngine;
using System.Collections.Generic;

public class ConstructHiveHandler : MonoBehaviour
{
    // Construct a hive at the agent's current tile (e.g., queen builds hive at her position)
    public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
    {
        if (agent == null) return;

        var tm = TileManager.Instance;
        if (tm == null) return;

        // Use agent's current axial coords rather than the passed target
        int q = agent.q;
        int r = agent.r;

        var tile = tm.GetTile(q, r);
        if (tile == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;
        var hivePrefab = gm.hivePrefab;
        if (hivePrefab == null) return;

        Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
        var go = GameObject.Instantiate(hivePrefab, pos, Quaternion.identity);

        // Ensure Hive component exists
        Hive hive = go.GetComponent<Hive>();
        if (hive == null) hive = go.AddComponent<Hive>();
        
        // Add UnitAgent to hive so it can be selected and receive commands
        var hiveAgent = go.GetComponent<UnitAgent>();
        if (hiveAgent == null) hiveAgent = go.AddComponent<UnitAgent>();
        
        hiveAgent.q = q;
        hiveAgent.r = r;
        hiveAgent.canMove = false; // Hive cannot move
        hiveAgent.faction = Faction.Player;
        hiveAgent.SetPosition(q, r);
        
        // Update queen's home hive and hive's queen reference (Initialize 전에!) ?
        if (agent.isQueen)
        {
            agent.homeHive = hive;
            hive.queenBee = agent;
            
            Debug.Log($"[하이브 건설] 여왕벌 참조 설정 완료");
        }
        
        // Initialize hive (여왕벌 비활성화 포함) ?
        hive.Initialize(q, r);
        
        // Transfer workers from old hive or find homeless workers
        if (agent.isQueen && agent.homeHive != null)
        {
            // Note: agent.homeHive는 이미 위에서 새 hive로 설정됨
            // 이전 하이브에서 일꾼 이전은 필요 없음 (새 하이브이므로)
            
            // homeHive가 없는 일꾼들을 찾아서 할당
            AssignHomelessWorkersToHive(hive, q, r);
        }
        else if (agent.isQueen)
        {
            // 여왕벌이지만 이전 하이브가 없으면 주변의 homeHive 없는 일꾼들 찾기
            AssignHomelessWorkersToHive(hive, q, r);
        }
        
        Debug.Log($"[하이브 건설] 하이브 건설 완료: ({q}, {r})");
    }

    /// <summary>
    /// homeHive가 없는 일꾼들을 새 하이브에 할당
    /// </summary>
    private static void AssignHomelessWorkersToHive(Hive hive, int q, int r)
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            if (unit.faction != Faction.Player) continue;
            if (unit.isQueen) continue;
            
            // homeHive가 없는 일꾼만
            if (unit.homeHive == null)
            {
                // 새 하이브를 homeHive로 설정
                unit.homeHive = hive;
                unit.isFollowingQueen = false;
                unit.hasManualOrder = false;
                
                // 하이브로 이동
                var start = TileManager.Instance.GetTile(unit.q, unit.r);
                var dest = TileManager.Instance.GetTile(q, r);
                if (start != null && dest != null)
                {
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null && path.Count > 0)
                    {
                        var ctrl = unit.GetComponent<UnitController>();
                        if (ctrl == null) ctrl = unit.gameObject.AddComponent<UnitController>();
                        ctrl.agent = unit;
                        ctrl.SetPath(path);
                    }
                }
                
                Debug.Log($"[하이브 건설] {unit.name}이(가) 새 하이브에 할당되었습니다.");
            }
        }
    }

    /// <summary>
    /// 여왕벌을 하이브 타일 위치로 이동
    /// </summary>
    private static void MoveQueenToHive(UnitAgent queen, int q, int r)
    {
        if (queen == null) return;
        
        // 타일 좌표 업데이트
        queen.SetPosition(q, r);
        
        // 월드 위치 업데이트 (하이브 중심)
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queen.hexSize);
        queen.transform.position = hivePos;
        
        Debug.Log($"[하이브 건설] 여왕벌이 하이브 위치로 이동: ({q}, {r})");
    }
}
