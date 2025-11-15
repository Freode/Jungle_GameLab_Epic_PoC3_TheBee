using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitAgent agent;
    public float moveSpeed = 2f; // tiles per second

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

    public void SetPath(List<HexTile> path)
    {
        pathQueue.Clear();
        if (path == null) return;
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
