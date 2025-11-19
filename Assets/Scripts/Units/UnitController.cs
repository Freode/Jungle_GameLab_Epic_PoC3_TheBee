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
            // ✅ 현재 이동 중인 코루틴 중단
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
                isMoving = false;
            }
            
            // 기존 경로 클리어
            pathQueue.Clear();
            
            // 현재 위치에서 목적지로 새 경로 찾기
            if (agent != null && TileManager.Instance != null)
            {
                var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
                var destTile = path[path.Count - 1];
                
                var newPath = Pathfinder.FindPath(currentTile, destTile);
                if (newPath != null && newPath.Count > 0)
                {
                    Debug.Log($"[UnitController] 이동 중 새 명령: ({agent.q}, {agent.r}) → ({destTile.q}, {destTile.r})");
                    
                    // 첫 타일이 현재 타일이면 스킵
                    int newStartIndex = (newPath[0].q == agent.q && newPath[0].r == agent.r) ? 1 : 0;
                    for (int i = newStartIndex; i < newPath.Count; i++)
                    {
                        pathQueue.Enqueue(newPath[i]);
                    }
                }
            }
            return;
        }

        // 새로운 경로 설정
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

    public void ClearPath()
    {
        pathQueue.Clear();
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        isMoving = false;
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
    }
}
