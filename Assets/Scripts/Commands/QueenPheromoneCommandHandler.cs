using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 여왕벌 페르몬 명령 핸들러
/// 특정 부대의 일벌들을 여왕벌의 현재 위치로 이동시킴
/// </summary>
public static class QueenPheromoneCommandHandler
{
    // ? 현재 실행 중인 페르몬 코루틴 추적
    private static Coroutine currentPheromoneCoroutine = null;
    private static MonoBehaviour currentCoroutineHost = null;
    
    /// <summary>
    /// 페르몬 명령 실행 (마우스 방향 기반 배치)
    /// </summary>
    public static void ExecutePheromone(UnitAgent queenAgent, CommandTarget target, WorkerSquad squad)
    {
        if (queenAgent == null)
        {
            Debug.LogWarning("[페르몬] 여왕벌이 없습니다!");
            return;
        }
        
        // ? 이전 페르몬 명령 코루틴 중단
        CancelCurrentPheromoneCommand();
        
        // ? 마우스 위치 가져오기
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        
        float hexSize = 0.5f;
        if (GameManager.Instance != null) hexSize = GameManager.Instance.hexSize;
        
        // ? 마우스 위치의 타일 찾기 (Raycast 우선, 실패 시 거리 계산)
        HexTile mouseTile = null;
        
        // 방법 1: Raycast로 타일 찾기
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
        foreach (var hit in hits)
        {
            var tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                mouseTile = tile;
                Debug.Log($"[페르몬] Raycast로 타일 발견: ({tile.q}, {tile.r})");
                break;
            }
        }
        
        // 방법 2: Raycast 실패 시 마우스와 가장 가까운 타일 찾기
        if (mouseTile == null && TileManager.Instance != null)
        {
            float minDistance = float.MaxValue;
            
            foreach (var candidateTile in TileManager.Instance.GetAllTiles())
            {
                if (candidateTile == null) continue;
                
                Vector3 tileWorldPos = TileHelper.HexToWorld(candidateTile.q, candidateTile.r, hexSize);
                float distance = Vector3.Distance(mouseWorldPos, tileWorldPos);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    mouseTile = candidateTile;
                }
            }
            
            if (mouseTile != null)
            {
                Debug.Log($"[페르몬] 거리 계산으로 타일 발견: ({mouseTile.q}, {mouseTile.r}), 거리: {minDistance:F2}");
            }
        }
        
        if (mouseTile == null)
        {
            Debug.LogWarning("[페르몬] 마우스 위치의 타일을 찾을 수 없습니다!");
            return;
        }
        
        int pheromoneQ = mouseTile.q;
        int pheromoneR = mouseTile.r;
        
        Debug.Log($"[페르몬] 여왕벌 위치: ({queenAgent.q}, {queenAgent.r}), 페르몬 분사 타일: ({pheromoneQ}, {pheromoneR})");
        
        // ? 활동 범위 체크
        int activityRadius = 5; // 기본값
        if (queenAgent.homeHive != null && HiveManager.Instance != null)
        {
            activityRadius = HiveManager.Instance.hiveActivityRadius;
        }
        
        // ? 페르몬 분사 타일이 활동 범위 밖이면 조정
        bool isPheromoneOutOfRange = false;
        
        if (queenAgent.homeHive != null)
        {
            int pheromoneDistanceToHive = Pathfinder.AxialDistance(
                queenAgent.homeHive.q, queenAgent.homeHive.r,
                pheromoneQ, pheromoneR
            );
            
            if (pheromoneDistanceToHive > activityRadius)
            {
                isPheromoneOutOfRange = true;
                Debug.Log($"[페르몬] 페르몬 타일이 활동 범위 밖 ({pheromoneDistanceToHive}/{activityRadius})");
            }
        }

        // ? 여왕벌 현재 타일 또는 인접 타일이면 이동 없이 즉시 분사
        int distanceToQueen = Pathfinder.AxialDistance(queenAgent.q, queenAgent.r, pheromoneQ, pheromoneR);
        Debug.Log($"[페르몬] 타겟 타일: ({pheromoneQ}, {pheromoneR}), 여왕: ({queenAgent.q}, {queenAgent.r}), 거리: {distanceToQueen}");
        if (distanceToQueen <= 1)
        {
            int finalQ = pheromoneQ;
            int finalR = pheromoneR;
            if (isPheromoneOutOfRange && queenAgent.homeHive != null)
            {
                // 활동 범위 밖이면 여왕 위치에 분사
                finalQ = queenAgent.q;
                finalR = queenAgent.r;
            }

            Debug.Log($"[페르몬] 현재/인접 타일 분사: ({finalQ}, {finalR})");
            SprayPheromone(queenAgent, finalQ, finalR, squad);
            return;
        }
        
        // ? 여왕벌이 현재 위치에 페르몬 분사하는 경우
        if (pheromoneQ == queenAgent.q && pheromoneR == queenAgent.r)
        {
            Debug.Log($"[페르몬] 현재 위치에 페르몬 분사");
            SprayPheromone(queenAgent, pheromoneQ, pheromoneR, squad);
            return;
        }
        
        // ? 여왕벌이 이동해야 하는 경우
        // 1. 페르몬 분사 타일까지의 경로 계산
        var currentTile = TileManager.Instance?.GetTile(queenAgent.q, queenAgent.r);
        var pheromoneTargetTile = mouseTile;
        
        if (currentTile == null || pheromoneTargetTile == null)
        {
            Debug.LogWarning($"[페르몬] 타일을 찾을 수 없습니다");
            return;
        }
        
        var pathToPheromone = Pathfinder.FindPath(currentTile, pheromoneTargetTile);
        
        if (pathToPheromone == null || pathToPheromone.Count == 0)
        {
            Debug.LogWarning($"[페르몬] 경로를 찾을 수 없습니다");
            return;
        }
        
        Debug.Log($"[페르몬] 페르몬 타일까지 경로 길이: {pathToPheromone.Count}");
        
        // ? 2. 여왕벌 이동 위치 결정 (페르몬 타일 바로 앞까지)
        int queenMoveQ = queenAgent.q;
        int queenMoveR = queenAgent.r;
        int finalPheromoneQ = pheromoneQ;
        int finalPheromoneR = pheromoneR;
        
        if (pathToPheromone.Count >= 3)
        {
            // ? 경로가 2개 이상이면 마지막 직전 타일까지 이동
            var moveToTile = pathToPheromone[pathToPheromone.Count - 3]; // 페르몬 타일 바로 앞
            queenMoveQ = moveToTile.q;
            queenMoveR = moveToTile.r;
            
            Debug.Log($"[페르몬] 여왕벌 이동 목표: ({queenMoveQ}, {queenMoveR}) (페르몬 타일 바로 앞, 경로 {pathToPheromone.Count - 2}번째)");
        }
        else if (pathToPheromone.Count == 1 || pathToPheromone.Count == 2)
        {
            // ? 경로가 1개 (인접 타일)면 현재 위치에서 페르몬 분사
            queenMoveQ = queenAgent.q;
            queenMoveR = queenAgent.r;
            
            Debug.Log($"[페르몬] 페르몬 타일이 인접: 현재 위치에서 분사");
        }
        
        // ? 3. 활동 범위 체크 및 조정
        if (queenAgent.homeHive != null)
        {
            // ? 3-1. 여왕벌 이동 위치가 활동 범위 밖이면 경계까지만 이동
            int queenMoveDistanceToHive = Pathfinder.AxialDistance(
                queenAgent.homeHive.q, queenAgent.homeHive.r,
                queenMoveQ, queenMoveR
            );
            
            if (queenMoveDistanceToHive > activityRadius)
            {
                Debug.Log($"[페르몬] 여왕벌 이동 위치가 활동 범위 밖 ({queenMoveDistanceToHive}/{activityRadius}), 경계까지만 이동");
                
                // ? 현재 위치 → 이동 목표 경로에서 활동 범위 경계 찾기
                var pathToMove = Pathfinder.FindPath(currentTile, TileManager.Instance.GetTile(queenMoveQ, queenMoveR));
                
                if (pathToMove != null && pathToMove.Count > 0)
                {
                    HexTile lastValidTile = currentTile;
                    
                    foreach (var tile in pathToMove)
                    {
                        int tileDistanceToHive = Pathfinder.AxialDistance(
                            queenAgent.homeHive.q, queenAgent.homeHive.r,
                            tile.q, tile.r
                        );
                        
                        if (tileDistanceToHive <= activityRadius)
                        {
                            lastValidTile = tile;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    queenMoveQ = lastValidTile.q;
                    queenMoveR = lastValidTile.r;
                    
                    Debug.Log($"[페르몬] 여왕벌 활동 범위 경계: ({queenMoveQ}, {queenMoveR})");
                }
            }
            
            // ? 3-2. 페르몬 분사 위치가 활동 범위 밖이면 여왕벌 이동 위치에 페르몬 분사
            if (isPheromoneOutOfRange)
            {
                finalPheromoneQ = queenMoveQ;
                finalPheromoneR = queenMoveR;
                
                Debug.Log($"[페르몬] 페르몬 분사 위치 조정: ({finalPheromoneQ}, {finalPheromoneR}) (활동 범위 경계)");
            }
        }
        
        // ? 4. 여왕벌이 현재 위치와 같으면 즉시 페르몬 분사
        if (queenMoveQ == queenAgent.q && queenMoveR == queenAgent.r)
        {
            Debug.Log($"[페르몬] 이동 불필요, 즉시 페르몬 분사");
            SprayPheromone(queenAgent, finalPheromoneQ, finalPheromoneR, squad);
            return;
        }
        
        // ? 5. 여왕벌 이동 후 페르몬 분사 (새 코루틴 시작)
        var queenBehavior = queenAgent.GetComponent<MonoBehaviour>();
        if (queenBehavior != null)
        {
            currentCoroutineHost = queenBehavior;
            currentPheromoneCoroutine = queenBehavior.StartCoroutine(MoveQueenAndSprayPheromone(queenAgent, queenMoveQ, queenMoveR, finalPheromoneQ, finalPheromoneR, squad));
            Debug.Log($"[페르몬] 새 페르몬 코루틴 시작");
        }
        else
        {
            Debug.LogWarning("[페르몬] 여왕벌 MonoBehaviour를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 현재 실행 중인 페르몬 명령 취소
    /// </summary>
    public static void CancelCurrentPheromoneCommand()
    {
        if (currentPheromoneCoroutine != null && currentCoroutineHost != null)
        {
            currentCoroutineHost.StopCoroutine(currentPheromoneCoroutine);
            currentPheromoneCoroutine = null;
            currentCoroutineHost = null;
            Debug.Log($"[페르몬] 이전 페르몬 명령 취소");
        }
    }
    
    /// <summary>
    /// 여왕벌을 이동시킨 후 페르몬 분사
    /// </summary>
    private static IEnumerator MoveQueenAndSprayPheromone(UnitAgent queenAgent, int moveQ, int moveR, int pheromoneQ, int pheromoneR, WorkerSquad squad)
    {
        // ? 1. 여왕벌 이동 명령
        var currentTile = TileManager.Instance?.GetTile(queenAgent.q, queenAgent.r);
        var moveTile = TileManager.Instance?.GetTile(moveQ, moveR);
        
        if (currentTile == null || moveTile == null)
        {
            Debug.LogWarning($"[페르몬] 타일을 찾을 수 없습니다: 현재({queenAgent.q}, {queenAgent.r}) → 이동({moveQ}, {moveR})");
            currentPheromoneCoroutine = null;
            currentCoroutineHost = null;
            yield break;
        }
        
        // ? 경로 계산
        var path = Pathfinder.FindPath(currentTile, moveTile);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[페르몬] 경로를 찾을 수 없습니다");
            currentPheromoneCoroutine = null;
            currentCoroutineHost = null;
            yield break;
        }
        
        Debug.Log($"[페르몬] 여왕벌 이동 시작: ({queenAgent.q}, {queenAgent.r}) → ({moveQ}, {moveR}), 경로 길이: {path.Count}");
        
        // ? 2. 여왕벌 이동 (QueenBehaviorController 사용)
        var queenBehavior = queenAgent.GetComponent<QueenBehaviorController>();
        if (queenBehavior != null)
        {
            queenBehavior.IssueCommandToTile(moveTile);
        }
        else
        {
            // QueenBehaviorController가 없으면 UnitController 직접 사용
            var mover = queenAgent.GetComponent<UnitController>();
            if (mover != null)
            {
                mover.SetPath(path);
            }
            else
            {
                Debug.LogWarning("[페르몬] 여왕벌 이동 컨트롤러를 찾을 수 없습니다!");
                currentPheromoneCoroutine = null;
                currentCoroutineHost = null;
                yield break;
            }
        }
        
        // ? 3. 여왕벌이 목표 위치에 도착할 때까지 대기
        float timeout = 30f; // 최대 30초 대기
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // 목표 타일에 도착했는지 체크
            if (queenAgent.q == moveQ && queenAgent.r == moveR)
            {
                Debug.Log($"[페르몬] 여왕벌 도착: ({moveQ}, {moveR})");
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogWarning($"[페르몬] 여왕벌 이동 타임아웃: 현재 ({queenAgent.q}, {queenAgent.r}), 목표 ({moveQ}, {moveR})");
            // 타임아웃이어도 현재 위치에 페르몬 분사
            SprayPheromone(queenAgent, queenAgent.q, queenAgent.r, squad);
            currentPheromoneCoroutine = null;
            currentCoroutineHost = null;
            yield break;
        }
        
        // ? 4. 도착 후 페르몬 분사 (페르몬 타일에)
        yield return new WaitForSeconds(0.1f); // 약간의 딜레이
        
        Debug.Log($"[페르몬] 페르몬 분사: ({pheromoneQ}, {pheromoneR}) (여왕벌 위치: ({queenAgent.q}, {queenAgent.r}))");
        SprayPheromone(queenAgent, pheromoneQ, pheromoneR, squad);
        
        // ? 코루틴 완료
        currentPheromoneCoroutine = null;
        currentCoroutineHost = null;
    }
    
    /// <summary>
    /// 페르몬 분사 (실제 페르몬 효과 적용 및 일벌 명령)
    /// </summary>
    private static void SprayPheromone(UnitAgent queenAgent, int targetQ, int targetR, WorkerSquad squad)
    {
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

        if (PheromoneManager.Instance != null)
        {
            var existing = PheromoneManager.Instance.GetPheromonePositionsOrdered(squad);
            Debug.Log($"[페르몬] 분사 전 리스트 ({squad}): {string.Join(" | ", existing)}");
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

        // 현재 부대 페르몬 목록(추가 순서) 확보
        List<Vector2Int> pheromonePositions = new List<Vector2Int>();
        if (PheromoneManager.Instance != null)
        {
            pheromonePositions = PheromoneManager.Instance.GetPheromonePositionsOrdered(squad);
        }

        // 페르몬이 없으면 집으로 복귀
        if (pheromonePositions.Count == 0)
        {
            Debug.Log($"[페르몬] {squad} 페르몬 없음 → 하이브 복귀 명령");
            SendSquadWorkersHome(squadWorkers);
            return;
        }

        Debug.Log($"[페르몬] 페르몬 분사 완료: ({targetQ}, {targetR})");
        Debug.Log($"[페르몬] {squad} 부대 {squadWorkers.Count}마리 분배: 페르몬 {pheromonePositions.Count}개");

        DistributeWorkersToPheromones(squadWorkers, pheromonePositions);
    }

    /// <summary>
    /// 부대 일벌을 페르몬 위치에 3등분 후 잔여는 첫 페르몬부터 추가 배치
    /// </summary>
    private static void DistributeWorkersToPheromones(System.Collections.Generic.List<UnitAgent> squadWorkers, System.Collections.Generic.List<Vector2Int> pheromonePositions)
    {
        if (pheromonePositions.Count == 0) return;

        int workerIndex = 0;
        int baseShare = squadWorkers.Count / pheromonePositions.Count;
        int remainder = squadWorkers.Count % pheromonePositions.Count;

        for (int i = 0; i < pheromonePositions.Count; i++)
        {
            int assignCount = baseShare + (i < remainder ? 1 : 0);
            Vector2Int coord = pheromonePositions[i];
            var targetTile = TileManager.Instance?.GetTile(coord.x, coord.y);
            if (targetTile == null) continue;

            for (int j = 0; j < assignCount && workerIndex < squadWorkers.Count; j++)
            {
                var worker = squadWorkers[workerIndex++];
                if (worker == null) continue;

                worker.hasManualOrder = true;
                worker.hasManualTarget = true;
                worker.manualTargetCoord = coord;

                var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
                if (workerBehavior != null)
                {
                    workerBehavior.IssueCommandToTile(targetTile);
                    Debug.Log($"[페르몬] {worker.name} → ({coord.x}, {coord.y}) 배정");
                }
            }
        }
    }

    /// <summary>
    /// 페르몬이 없을 때 해당 부대 일벌을 하이브로 복귀시킴
    /// </summary>
    public static void SendSquadWorkersHome(System.Collections.Generic.List<UnitAgent> squadWorkers)
    {
        foreach (var worker in squadWorkers)
        {
            if (worker == null) continue;

            worker.hasManualOrder = false;
            worker.hasManualTarget = false;

            if (worker.homeHive == null) continue;

            var hiveTile = TileManager.Instance?.GetTile(worker.homeHive.q, worker.homeHive.r);
            if (hiveTile == null) continue;

            var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
            if (workerBehavior != null)
            {
                workerBehavior.IssueCommandToTile(hiveTile);
                Debug.Log($"[페르몬] {worker.name} 하이브 복귀 명령");
            }
        }
    }
}
