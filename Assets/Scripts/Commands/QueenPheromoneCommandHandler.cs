using UnityEngine;

/// <summary>
/// 여왕벌 페르몬 명령 핸들러
/// 특정 부대의 일벌들을 여왕벌의 현재 위치로 이동시킴
/// </summary>
public static class QueenPheromoneCommandHandler
{
    public static void ExecutePheromone(UnitAgent queenAgent, CommandTarget target, WorkerSquad squad)
    {
        if (queenAgent == null)
        {
            Debug.LogWarning("[페르몬] 여왕벌이 없습니다!");
            return;
        }
        
        // ? 여왕벌의 현재 위치를 목표로 설정 (요구사항 2)
        int targetQ = queenAgent.q;
        int targetR = queenAgent.r;
        
        var targetTile = TileManager.Instance?.GetTile(targetQ, targetR);
        if (targetTile == null)
        {
            Debug.LogWarning($"[페르몬] 타일을 찾을 수 없습니다: ({targetQ}, {targetR})");
            return;
        }
        
        if (HiveManager.Instance == null)
        {
            Debug.LogWarning("[페르몬] HiveManager를 찾을 수 없습니다!");
            return;
        }
        
        // ? 페르몬 효과 추가 (타일 강조 표시)
        if (PheromoneManager.Instance != null)
        {
            PheromoneManager.Instance.AddPheromone(targetQ, targetR, squad);
        }
        
        // 해당 부대의 일벌 목록 가져오기
        var squadWorkers = HiveManager.Instance.GetSquadWorkers(squad);
        
        if (squadWorkers.Count == 0)
        {
            Debug.Log($"[페르몬] {squad} 부대에 일벌이 없습니다!");
            return;
        }
        
        Debug.Log($"[페르몬] {squad} 부대 {squadWorkers.Count}마리에게 명령: 여왕벌 위치 ({targetQ}, {targetR})로 집결");
        
        // 각 일벌에게 이동 명령
        foreach (var worker in squadWorkers)
        {
            if (worker == null) continue;
            
            // ? 페르몬 명령은 수동 명령으로 설정 (자원 채취 후에도 페르몬 위치로 복귀) (요구사항 1)
            worker.hasManualOrder = true;
            
            // WorkerBehaviorController를 통해 이동 명령
            var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
            if (workerBehavior != null)
            {
                workerBehavior.IssueCommandToTile(targetTile);
                Debug.Log($"[페르몬] {worker.name} ({squad}) → ({targetQ}, {targetR}) 이동 명령 (hasManualOrder=true)");
            }
        }
    }
}
