using System.Collections.Generic;
using UnityEngine;

// Simple A* pathfinder on axial hex grid using TileManager
public static class Pathfinder
{
    public static List<HexTile> FindPath(HexTile start, HexTile goal)
    {
        if (start == null || goal == null) return null;
        if (start == goal) return new List<HexTile> { start };

        var tm = TileManager.Instance;
        if (tm == null) return null;

        var closedSet = new HashSet<Vector2Int>();
        var openSet = new List<Vector2Int> { new Vector2Int(start.q, start.r) };

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        var gScore = new Dictionary<Vector2Int, int>();
        var fScore = new Dictionary<Vector2Int, int>();

        Vector2Int startCoord = new Vector2Int(start.q, start.r);
        Vector2Int goalCoord = new Vector2Int(goal.q, goal.r);

        gScore[startCoord] = 0;
        fScore[startCoord] = Heuristic(startCoord, goalCoord);

        while (openSet.Count > 0)
        {
            // get node in openSet with lowest fScore
            Vector2Int current = openSet[0];
            int bestF = fScore.ContainsKey(current) ? fScore[current] : int.MaxValue;
            foreach (var node in openSet)
            {
                int f = fScore.ContainsKey(node) ? fScore[node] : int.MaxValue;
                if (f < bestF)
                {
                    bestF = f;
                    current = node;
                }
            }

            if (current == goalCoord)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            var neighbors = tm.GetNeighbors(current.x, current.y);
            foreach (var n in neighbors)
            {
                var ncoord = new Vector2Int(n.q, n.r);
                if (closedSet.Contains(ncoord)) continue;

                int tentativeG = (gScore.ContainsKey(current) ? gScore[current] : int.MaxValue) + 1; // uniform cost

                if (!openSet.Contains(ncoord)) openSet.Add(ncoord);
                else if (tentativeG >= (gScore.ContainsKey(ncoord) ? gScore[ncoord] : int.MaxValue)) continue;

                cameFrom[ncoord] = current;
                gScore[ncoord] = tentativeG;
                fScore[ncoord] = tentativeG + Heuristic(ncoord, goalCoord);
            }
        }

        return null; // no path
    }

    static List<HexTile> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var tm = TileManager.Instance;
        var totalPath = new List<HexTile>();
        totalPath.Add(tm.GetTile(current.x, current.y));
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(tm.GetTile(current.x, current.y));
        }
        totalPath.Reverse();
        return totalPath;
    }

    static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return AxialDistance(a.x, a.y, b.x, b.y);
    }

    // axial distance
    public static int AxialDistance(int q1, int r1, int q2, int r2)
    {
        int x1 = q1;
        int z1 = r1;
        int y1 = -x1 - z1;

        int x2 = q2;
        int z2 = r2;
        int y2 = -x2 - z2;

        return (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) + Mathf.Abs(z1 - z2)) / 2;
    }
}
