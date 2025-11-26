using UnityEngine;

/// <summary>
/// 여왕벌 페르몬 명령 핸들러
/// 특정 부대의 일벌들을 여왕벌의 현재 위치로 이동시킴
/// </summary>
public static class QueenPheromoneCommandHandler
{
    /// <summary>
    /// 페르몬 명령 실행 (마우스 방향 기반 배치) (요구사항 3)
    /// </summary>
    public static void ExecutePheromone(UnitAgent queenAgent, CommandTarget target, WorkerSquad squad)
    {
        if (queenAgent == null)
        {
            Debug.LogWarning("[페르몬] 여왕벌이 없습니다!");
            return;
        }
        
        // ? 마우스 위치 가져오기
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        
        float hexSize = 0.5f;
        if (GameManager.Instance != null) hexSize = GameManager.Instance.hexSize;
        
        // ? 여왕벌 주변 7개 타일 (현재 위치 + 6방향) 검사
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 0),   // 현재 위치
            new Vector2Int(+1, 0),  // 오른쪽
            new Vector2Int(+1, -1), // 오른쪽 위
            new Vector2Int(0, -1),  // 위
            new Vector2Int(-1, 0),  // 왼쪽
            new Vector2Int(-1, +1), // 왼쪽 아래
            new Vector2Int(0, +1),  // 아래
        };
        
        // ? 마우스와 가장 가까운 타일 찾기
        int targetQ = queenAgent.q;
        int targetR = queenAgent.r;
        float minDistance = float.MaxValue;
        
        foreach (var dir in directions)
        {
            int candidateQ = queenAgent.q + dir.x;
            int candidateR = queenAgent.r + dir.y;
            
            // 타일 중심 월드 좌표 계산
            Vector3 tileWorldPos = TileHelper.HexToWorld(candidateQ, candidateR, hexSize);
            
            // 마우스와의 거리 계산
            float distance = Vector3.Distance(mouseWorldPos, tileWorldPos);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                targetQ = candidateQ;
                targetR = candidateR;
            }
        }
        
        Debug.Log($"[페르몬] 여왕벌 위치: ({queenAgent.q}, {queenAgent.r}), 선택된 타일: ({targetQ}, {targetR}), 거리: {minDistance:F2}");
        
        // ? 꿀벌집이 있으면 활동 범위 체크
        if (queenAgent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(
                queenAgent.homeHive.q, queenAgent.homeHive.r,
                targetQ, targetR
            );
            
            int activityRadius = 5; // 기본값
            if (HiveManager.Instance != null)
            {
                activityRadius = HiveManager.Instance.hiveActivityRadius;
            }
            
            // ? 활동 범위 밖이면 여왕벌 현재 위치 사용
            if (distanceToHive > activityRadius)
            {
                Debug.Log($"[페르몬] 활동 범위 밖 ({distanceToHive}/{activityRadius}): 여왕벌 현재 위치 사용");
                targetQ = queenAgent.q;
                targetR = queenAgent.r;
            }
            else
            {
                Debug.Log($"[페르몬] 활동 범위 내: ({targetQ}, {targetR})");
            }
        }
        
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
        
        Debug.Log($"[페르몬] {squad} 부대 {squadWorkers.Count}마리에게 명령: 위치 ({targetQ}, {targetR})로 집결");
        
        // 각 일벌에게 이동 명령
        foreach (var worker in squadWorkers)
        {
            if (worker == null) continue;
            
            // ? 페르몬 명령은 수동 명령으로 설정 (자원 채취 후에도 페르몬 위치로 복귀)
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
