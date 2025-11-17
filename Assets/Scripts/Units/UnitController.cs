using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitAgent agent;
    public float moveSpeed = 2f; // tiles per second

    private Queue<HexTile> pathQueue = new Queue<HexTile>();
    private bool isMoving = false;
    private Coroutine moveCoroutine;
    
    // 경로 큐잉을 위한 목적지 저장 ?
    private HexTile queuedDestination = null;

    void Update()
    {
        if (!isMoving && pathQueue.Count > 0)
        {
            var next = pathQueue.Dequeue();
            moveCoroutine = StartCoroutine(MoveToTileCoroutine(next));
        }
        else if (!isMoving && queuedDestination != null)
        {
            // 이동이 끝나고 큐잉된 목적지가 있으면 새로운 경로 탐색 ?
            ProcessQueuedDestination();
        }
    }

    /// <summary>
    /// 경로 설정 (연속 클릭 시 큐잉) ?
    /// </summary>
    public void SetPath(List<HexTile> path)
    {
        if (path == null || path.Count == 0) return;
        
        // 이미 이동 중이거나 대기 중인 경로가 있으면 큐잉 ?
        if (isMoving || pathQueue.Count > 0)
        {
            // 마지막 목적지 저장
            queuedDestination = path[path.Count - 1];
            Debug.Log($"[UnitController] 경로 큐잉: 현재 이동 완료 후 ({queuedDestination.q}, {queuedDestination.r})로 이동 예정");
            return;
        }
        
        // 새로운 경로 설정
        pathQueue.Clear();
        queuedDestination = null;
        
        // remove first tile if it's the current tile
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
    /// 큐잉된 목적지 처리 ?
    /// </summary>
    void ProcessQueuedDestination()
    {
        if (queuedDestination == null || agent == null) return;
        
        // 현재 위치에서 목적지로 경로 탐색 ?
        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        if (startTile == null)
        {
            Debug.LogWarning($"[UnitController] 현재 타일을 찾을 수 없습니다: ({agent.q}, {agent.r})");
            queuedDestination = null;
            return;
        }
        
        var path = Pathfinder.FindPath(startTile, queuedDestination);
        if (path != null && path.Count > 0)
        {
            Debug.Log($"[UnitController] 큐잉된 경로 실행: ({agent.q}, {agent.r}) → ({queuedDestination.q}, {queuedDestination.r})");
            
            // 경로 설정
            pathQueue.Clear();
            int startIndex = (path[0].q == agent.q && path[0].r == agent.r) ? 1 : 0;
            for (int i = startIndex; i < path.Count; i++)
            {
                pathQueue.Enqueue(path[i]);
            }
        }
        else
        {
            Debug.LogWarning($"[UnitController] 큐잉된 경로를 찾을 수 없습니다: ({agent.q}, {agent.r}) → ({queuedDestination.q}, {queuedDestination.r})");
        }
        
        queuedDestination = null;
    }

    public void ClearPath()
    {
        pathQueue.Clear();
        queuedDestination = null; // 큐잉된 목적지도 초기화 ?
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        isMoving = false;
    }

    /// <summary>
    /// 현재 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        return isMoving || pathQueue.Count > 0 || queuedDestination != null; // 큐잉된 목적지도 체크 ?
    }

    /// <summary>
    /// 현재 타일 내부로 랜덤 위치로 이동 (회피 행동)
    /// </summary>
    public void MoveWithinCurrentTile()
    {
        if (agent == null) return;
        
        // 타일의 범위, World 좌표로 변환
        Vector3 newPos = TileHelper.GetRandomPositionInCurrentTile(
            transform.position, 
            agent.q, 
            agent.r, 
            agent.hexSize,
            0.2f // 반경 20% (작게 움직이도록)
        );
        
        // 타일 범위 내에 있는지 확인
        if (!TileHelper.IsPositionInTile(newPos, agent.q, agent.r, agent.hexSize))
        {
            // 타일 밖이면 중심으로 이동
            newPos = TileHelper.HexToWorld(agent.q, agent.r, agent.hexSize);
        }
        
        // 코루틴으로 부드럽게 이동
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveWithinTileCoroutine(newPos));
    }

    /// <summary>
    /// 타일 내부 이동 코루틴
    /// </summary>
    System.Collections.IEnumerator MoveWithinTileCoroutine(Vector3 targetPos)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / (moveSpeed * agent.hexSize * 2f); // 빠르게 이동
        
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
            
            // 이동 중에도 타일 범위 체크
            if (TileHelper.IsPositionInTile(lerpedPos, agent.q, agent.r, agent.hexSize))
            {
                transform.position = lerpedPos;
            }
            else
            {
                // 범위 넘어가면 중단
                break;
            }
            
            yield return null;
        }

        // 최종 위치 확인
        if (TileHelper.IsPositionInTile(targetPos, agent.q, agent.r, agent.hexSize))
        {
            transform.position = targetPos;
        }
        else
        {
            // 안전한 위치로 이동
            transform.position = TileHelper.HexToWorld(agent.q, agent.r, agent.hexSize);
        }
        
        yield return null;
        isMoving = false;
    }

    System.Collections.IEnumerator MoveToTileCoroutine(HexTile dest)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize);

        float distance = Vector3.Distance(startPos, endPos);
        float travelTime = distance / (moveSpeed * agent.hexSize); // normalize by hexSize
        if (travelTime <= 0f)
        {
            // instantly snap
            transform.position = endPos;
            agent.SetPosition(dest.q, dest.r);
            isMoving = false;
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

        // ensure final position exact
        transform.position = endPos;
        agent.SetPosition(dest.q, dest.r);

        // small yield to ensure other systems update
        yield return null;
        isMoving = false;
    }
}
