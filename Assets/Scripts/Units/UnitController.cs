using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitAgent agent;
    public float moveSpeed = 2f; // tiles per second

    [Header("이동 설정")]
    [Tooltip("타일 내 랜덤 이동 범위 (0~1, 0=중앙만, 1=타일 전체)")]
    [Range(0f, 1f)]
    public float tileRandomRadius = 0.3f; // 타일 반지름의 50%까지 랜덤
    
    [Tooltip("타일 경계로부터의 최소 패딩 (타일 반지름 비율)")]
    [Range(0f, 0.5f)]
    public float tilePadding = 0.1f; // 타일 반지름의 10% 패딩

    private Queue<HexTile> pathQueue = new Queue<HexTile>();
    private bool isMoving = false;
    private Coroutine moveCoroutine;
    
    // ✅ 현재 이동 중인 타일 추적
    private HexTile currentMovingToTile = null;

    void Update()
    {
        if (!isMoving && pathQueue.Count > 0)
        {
            var next = pathQueue.Dequeue();
            moveCoroutine = StartCoroutine(MoveToTileCoroutine(next));
        }
    }

    /// <summary>
    /// 경로 설정 (이동 중이면 현재 위치에서 새 목적지로 재계산)
    /// </summary>
    /// <param name="path">이동 경로</param>
    /// <param name="forceCenter">true면 정중앙 이동, null이면 agent.useRandomPosition 사용</param>
    public void SetPath(List<HexTile> path, bool? forceCenter = null)
    {
        if (path == null || path.Count == 0) return;

        // ✅ 1. 이동 중이면 현재 코루틴 중단하고 새 경로로 재계산
        if (isMoving || pathQueue.Count > 0)
        {
            // ✅ 현재 이동 중인 타일 정보 저장 (코루틴 중단 전에!)
            HexTile oldMovingToTile = currentMovingToTile;
            
            // ✅ 현재 이동 중인 코루틴 중단
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
                isMoving = false;
                currentMovingToTile = null; // 이동 중단
            }
            
            // 기존 경로 클리어
            pathQueue.Clear();
            
            // ✅ 새 경로의 시작점 결정
            if (agent != null && TileManager.Instance != null)
            {
                var destTile = path[path.Count - 1];
                var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
                
                // ✅ 목적지가 현재 타일과 같으면 방향에 따라 처리
                if (destTile.q == agent.q && destTile.r == agent.r)
                {
                    // 이동 중이었으면 정지 명령
                    if (oldMovingToTile != null)
                    {
                        Debug.Log($"[UnitController] 현재 타일 클릭: 정지 명령, 현재 타일 ({agent.q}, {agent.r})에서 대기");
                    }
                    else
                    {
                        Debug.Log($"[UnitController] 현재 타일 클릭 (대기 중): ({destTile.q}, {destTile.r})");
                    }
                    pathQueue.Enqueue(currentTile);
                    return;
                }
                
                // ✅ 이동 중이었고, 이동 방향 정보가 있는 경우
                if (oldMovingToTile != null && currentTile != null)
                {
                    // 1. 기존 이동 방향 벡터 계산 (현재 타일 → 이동 중이던 타일)
                    Vector2Int oldDirection = new Vector2Int(
                        oldMovingToTile.q - agent.q,
                        oldMovingToTile.r - agent.r
                    );
                    
                    // 2. 새 경로의 첫 방향 벡터 계산 (현재 타일 기준)
                    var newPath = Pathfinder.FindPath(currentTile, destTile);
                    
                    // ✅ 경로가 유효한지 확인
                    if (newPath != null && newPath.Count > 1)
                    {
                        // 새 경로의 첫 번째 이동 방향
                        Vector2Int newDirection = new Vector2Int(
                            newPath[1].q - newPath[0].q,
                            newPath[1].r - newPath[0].r
                        );
                        
                        // 3. 방향 비교
                        bool isSameDirection = (oldDirection.x == newDirection.x && oldDirection.y == newDirection.y);
                        bool isOppositeDirection = (oldDirection.x == -newDirection.x && oldDirection.y == -newDirection.y);
                        
                        if (isSameDirection)
                        {
                            // ✅ 같은 방향: 바로 새로운 경로 설정 (처음부터)
                            Debug.Log($"[UnitController] 같은 방향 감지: 현재 타일 ({agent.q}, {agent.r})에서 새 경로 시작 → 목적지 ({destTile.q}, {destTile.r})");
                            
                            // 새 경로를 pathQueue에 추가
                            for (int i = 0; i < newPath.Count; i++)
                            {
                                pathQueue.Enqueue(newPath[i]);
                            }
                            
                            Debug.Log($"[UnitController] 새 경로 설정 완료: {newPath.Count}개 타일");
                            return;
                        }
                        else if (isOppositeDirection)
                        {
                            // ✅ 반대 방향: 현재 타일에서 시작 (즉시 유턴)
                            Debug.Log($"[UnitController] 반대 방향 감지: 현재 타일 ({agent.q}, {agent.r})에서 즉시 유턴 → 목적지 ({destTile.q}, {destTile.r})");
                            
                            // 새 경로를 pathQueue에 추가
                            for (int i = 0; i < newPath.Count; i++)
                            {
                                pathQueue.Enqueue(newPath[i]);
                            }
                            
                            Debug.Log($"[UnitController] 새 경로 설정 완료: {newPath.Count}개 타일");
                            return;
                        }
                        else
                        {
                            // ✅ 다른 방향 (대각선 등): 주변에서 가장 가까운 타일부터 시작
                            HexTile startTile = FindClosestTileAroundAgent(destTile);
                            if (startTile != null)
                            {
                                Debug.Log($"[UnitController] 다른 방향 감지: 가장 가까운 타일 ({startTile.q}, {startTile.r})에서 시작 → 목적지 ({destTile.q}, {destTile.r})");
                                
                                var finalPath = Pathfinder.FindPath(startTile, destTile);
                                if (finalPath != null && finalPath.Count > 0)
                                {
                                    for (int i = 0; i < finalPath.Count; i++)
                                    {
                                        pathQueue.Enqueue(finalPath[i]);
                                    }
                                    Debug.Log($"[UnitController] 새 경로 설정 완료: {finalPath.Count}개 타일");
                                }
                            }
                            return;
                        }
                    }
                    else if (newPath != null && newPath.Count == 1)
                    {
                        // ✅ 경로가 1개(목적지만) = 인접 타일
                        Vector2Int newDirection = new Vector2Int(
                            destTile.q - agent.q,
                            destTile.r - agent.r
                        );
                        
                        bool isSameDirection = (oldDirection.x == newDirection.x && oldDirection.y == newDirection.y);
                        bool isOppositeDirection = (oldDirection.x == -newDirection.x && oldDirection.y == -newDirection.y);
                        
                        if (isSameDirection)
                        {
                            // ✅ 같은 방향: 바로 새로운 경로 설정
                            Debug.Log($"[UnitController] 인접 타일 - 같은 방향: 현재 타일 ({agent.q}, {agent.r})에서 새 경로 시작");
                            
                            pathQueue.Enqueue(destTile);
                            Debug.Log($"[UnitController] 새 경로 설정 완료: 1개 타일");
                            return;
                        }
                        else if (isOppositeDirection)
                        {
                            // ✅ 반대 방향: 현재 타일에서 시작
                            Debug.Log($"[UnitController] 인접 타일 - 반대 방향: 현재 타일 ({agent.q}, {agent.r})에서 시작");
                            
                            pathQueue.Enqueue(destTile);
                            Debug.Log($"[UnitController] 새 경로 설정 완료: 1개 타일");
                            return;
                        }
                        else
                        {
                            // ✅ 다른 방향: 가장 가까운 타일
                            HexTile startTile = FindClosestTileAroundAgent(destTile);
                            Debug.Log($"[UnitController] 인접 타일 - 다른 방향: 가장 가까운 타일에서 시작");
                            
                            if (startTile != null)
                            {
                                var finalPath = Pathfinder.FindPath(startTile, destTile);
                                if (finalPath != null && finalPath.Count > 0)
                                {
                                    for (int i = 0; i < finalPath.Count; i++)
                                    {
                                        pathQueue.Enqueue(finalPath[i]);
                                    }
                                    Debug.Log($"[UnitController] 새 경로 설정 완료: {finalPath.Count}개 타일");
                                }
                            }
                            return;
                        }
                    }
                    else
                    {
                        // 경로를 찾을 수 없으면 가장 가까운 타일 사용
                        HexTile startTile = FindClosestTileAroundAgent(destTile);
                        Debug.Log($"[UnitController] 경로 없음: 가장 가까운 타일에서 시작");
                        
                        if (startTile != null)
                        {
                            var finalPath = Pathfinder.FindPath(startTile, destTile);
                            if (finalPath != null && finalPath.Count > 0)
                            {
                                for (int i = 0; i < finalPath.Count; i++)
                                {
                                    pathQueue.Enqueue(finalPath[i]);
                                }
                                Debug.Log($"[UnitController] 새 경로 설정 완료: {finalPath.Count}개 타일");
                            }
                        }
                        return;
                    }
                }
                else
                {
                    // ✅ 이동 중이 아니었거나 방향 정보가 없는 경우: 현재 타일에서 새 경로 시작
                    Debug.Log($"[UnitController] 대기 중이거나 방향 정보 없음: 현재 타일에서 새 경로 시작");
                    
                    var newPath = Pathfinder.FindPath(currentTile, destTile);
                    if (newPath != null && newPath.Count > 0)
                    {
                        // 현재 타일이 첫 타일이면 스킵
                        int startIdx = (newPath[0].q == agent.q && newPath[0].r == agent.r) ? 1 : 0;
                        for (int i = startIdx; i < newPath.Count; i++)
                        {
                            pathQueue.Enqueue(newPath[i]);
                        }
                        Debug.Log($"[UnitController] 새 경로 설정 완료: {newPath.Count - startIdx}개 타일");
                    }
                    return;
                }
            }
            return;
        }

        // 새로운 경로 설정 (이동 중이 아닐 때)
        pathQueue.Clear();
        
        int startIndex = 0;
        if (path.Count > 0 && agent != null && path[0].q == agent.q && path[0].r == agent.r)
        {
            startIndex = 1;
        }
        for (int i = startIndex; i < path.Count; i++)
        {
            pathQueue.Enqueue(path[i]);
        }
    }
    
    /// <summary>
    /// 현재 위치 주변 1칸 타일 중 가장 가까운 타일 찾기 ✅
    /// </summary>
    HexTile FindClosestTileAroundAgent(HexTile destTile)
    {
        if (agent == null || TileManager.Instance == null) return null;
        
        // 1. 현재 위치 및 주변 1칸 타일 수집
        List<HexTile> candidateTiles = new List<HexTile>();
        
        // 현재 타일 추가
        var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
        if (currentTile != null)
        {
            candidateTiles.Add(currentTile);
        }
        
        // 주변 6방향 타일 추가
        foreach (var dir in HexTile.NeighborDirections)
        {
            int neighborQ = agent.q + dir.x;
            int neighborR = agent.r + dir.y;
            var neighborTile = TileManager.Instance.GetTile(neighborQ, neighborR);
            
            if (neighborTile != null)
            {
                // ✅ 이미 경로에 포함된 타일은 제외
                if (pathQueue.Count > 0 && pathQueue.Contains(neighborTile))
                {
                    continue;
                }
                
                candidateTiles.Add(neighborTile);
            }
        }
        
        // 2. 가장 가까운 타일 찾기 (현재 월드 위치 기준)
        HexTile closestTile = null;
        float minDistance = float.MaxValue;
        
        Vector3 currentWorldPos = transform.position;
        
        foreach (var tile in candidateTiles)
        {
            Vector3 tileWorldPos = TileHelper.HexToWorld(tile.q, tile.r, agent.hexSize);
            float distance = Vector3.Distance(currentWorldPos, tileWorldPos);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTile = tile;
            }
        }
        
        return closestTile;
    }

    public void ClearPath()
    {
        pathQueue.Clear();
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        isMoving = false;
        currentMovingToTile = null; // ✅ 이동 정보 초기화
    }

    public bool IsMoving()
    {
        return isMoving || pathQueue.Count > 0;
    }

    public void MoveWithinCurrentTile()
    {
        if (agent == null) return;
        
        Vector3 newPos = TileHelper.GetRandomPositionInCurrentTile(
            transform.position, 
            agent.q, 
            agent.r, 
            agent.hexSize,
            0.2f
        );
        
        if (!TileHelper.IsPositionInTile(newPos, agent.q, agent.r, agent.hexSize))
        {
            newPos = TileHelper.HexToWorld(agent.q, agent.r, agent.hexSize);
        }
        
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveWithinTileCoroutine(newPos));
    }

    System.Collections.IEnumerator MoveWithinTileCoroutine(Vector3 targetPos)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / (moveSpeed * agent.hexSize * 2f);
        
        if (travelTime <= 0f)
        {
            transform.position = targetPos;
            isMoving = false;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector3 lerpedPos = Vector3.Lerp(startPos, targetPos, t);
            
            if (TileHelper.IsPositionInTile(lerpedPos, agent.q, agent.r, agent.hexSize))
            {
                transform.position = lerpedPos;
            }
            else
            {
                break;
            }
            
            yield return null;
        }

        if (TileHelper.IsPositionInTile(targetPos, agent.q, agent.r, agent.hexSize))
        {
            transform.position = targetPos;
        }
        else
        {
            transform.position = TileHelper.HexToWorld(agent.q, agent.r, agent.hexSize);
        }
        
        yield return null;
        isMoving = false;
    }

    /// <summary>
    /// ✅ 타일 내 위치 계산 (랜덤 or 정중앙)
    /// </summary>
    /// <param name="q">타일 q 좌표</param>
    /// <param name="r">타일 r 좌표</param>
    /// <param name="useRandom">true면 랜덤 위치, false면 정중앙</param>
    Vector3 GetPositionInTile(int q, int r, bool useRandom)
    {
        if (agent == null) return TileHelper.HexToWorld(q, r, 0.5f);

        // 타일 중심 위치
        Vector3 center = TileHelper.HexToWorld(q, r, agent.hexSize);
        
        // ✅ 정중앙 이동
        if (!useRandom)
        {
            return center;
        }
        
        // ✅ 랜덤 위치 이동
        // 랜덤 반지름 계산 (패딩 ~ tileRandomRadius 사이)
        float minRadius = agent.hexSize * tilePadding;
        float maxRadius = agent.hexSize * tileRandomRadius;
        float randomRadius = Random.Range(minRadius, maxRadius);
        
        // 랜덤 각도 (0 ~ 360도)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // 랜덤 오프셋 계산
        float offsetX = Mathf.Cos(randomAngle) * randomRadius;
        float offsetY = Mathf.Sin(randomAngle) * randomRadius;
        
        // 최종 위치
        Vector3 randomPos = new Vector3(
            center.x + offsetX,
            center.y + offsetY,
            center.z
        );
        
        return randomPos;
    }

    System.Collections.IEnumerator MoveToTileCoroutine(HexTile dest)
    {
        isMoving = true;
        currentMovingToTile = dest; // ✅ 현재 이동 중인 타일 저장
        
        Vector3 startPos = transform.position;
        
        // ✅ UnitAgent의 useRandomPosition 설정에 따라 위치 결정
        bool useRandom = (agent != null) ? agent.useRandomPosition : true;
        Vector3 endPos = GetPositionInTile(dest.q, dest.r, useRandom);

        float distance = Vector3.Distance(startPos, endPos);
        float travelTime = distance / (moveSpeed * agent.hexSize);
        if (travelTime <= 0f)
        {
            transform.position = endPos;
            agent.SetPosition(dest.q, dest.r);
            isMoving = false;
            currentMovingToTile = null; // ✅ 이동 완료
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        agent.SetPosition(dest.q, dest.r);

        yield return null;
        isMoving = false;
        currentMovingToTile = null; // ✅ 이동 완료
    }
}
