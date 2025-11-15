using System.Collections.Generic;
using UnityEngine;

// Draws boundary lines for the outer edges of a ring of hex tiles around a hive
[RequireComponent(typeof(Transform))]
public class HexBoundaryHighlighter : MonoBehaviour
{
    public static HexBoundaryHighlighter Instance { get; private set; }

    public Color lineColor = Color.green;
    public float lineWidth = 0.05f;
    public Material lineMaterial;

    // Controls whether the highlighter should react to ShowBoundary calls (enabled when hives exist)
    public bool enabledForHives { get; private set; } = false;
    // Enable verbose debug logging
    public bool debugLogs = false;

    private List<LineRenderer> lines = new List<LineRenderer>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Instance = this;

        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Called by higher-level manager when hive presence changes
    public void SetEnabledForHives(bool enabled)
    {
        enabledForHives = enabled;
        if (debugLogs) Debug.Log($"HexBoundaryHighlighter.SetEnabledForHives: {enabled}");
        if (!enabled)
        {
            Clear();
        }
    }

    public void ShowBoundary(Hive hive, int radius)
    {
        if (!enabledForHives)
        {
            if (debugLogs) Debug.Log("HexBoundaryHighlighter.ShowBoundary ignored: not enabledForHives");
            return;
        }

        if (hive == null)
        {
            Clear();
            return;
        }
        ShowBoundaryAt(hive.q, hive.r, radius);
    }

    // Show boundary centered at axial coords (q, r) with given radius
    public void ShowBoundaryAt(int q0, int r0, int radius)
    {
        if (!enabledForHives)
        {
            // if (debugLogs) Debug.Log("HexBoundaryHighlighter.ShowBoundaryAt ignored: not enabledForHives");
            return;
        }

        Clear();
        if (radius < 0) return;

        var tm = TileManager.Instance;
        if (tm == null) return;

        float size = 0.5f;
        if (GameManager.Instance != null) size = GameManager.Instance.hexSize;

        // if (debugLogs) Debug.Log($"ShowBoundaryAt start center=({q0},{r0}) radius={radius} hexSize={size}");

        // start tile check
        var startTile = tm.GetTile(q0, r0);
        if (startTile == null)
        {
            // if (debugLogs) Debug.Log($"HexBoundaryHighlighter: hive tile missing at {q0},{r0}");
            return;
        }

        // Step 1: BFS to collect all tiles within radius (Group A)
        var groupA = new HashSet<Vector2Int>();
        var queue = new Queue<(int q, int r, int d)>();
        
        var startCoord = new Vector2Int(q0, r0);
        groupA.Add(startCoord);
        queue.Enqueue((q0, r0, 0));

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            int cq = cur.q, cr = cur.r, cd = cur.d;
            
            // if (debugLogs) Debug.Log($"BFS collecting tile ({cq},{cr}) d={cd}");

            // Explore neighbors
            for (int side = 0; side < 6; side++)
            {
                var nd = HexTile.NeighborDirections[side];
                int nq = cq + nd.x;
                int nr = cr + nd.y;
                int ndist = cd + 1;

                // Skip if beyond radius
                if (ndist > radius)
                {
                    continue;
                }

                var neighborTile = tm.GetTile(nq, nr);
                
                // Skip if no tile exists
                if (neighborTile == null)
                {
                    continue;
                }

                var nCoord = new Vector2Int(nq, nr);

                // Skip if already in Group A
                if (groupA.Contains(nCoord))
                {
                    continue;
                }

                // Add to Group A and enqueue
                groupA.Add(nCoord);
                queue.Enqueue((nq, nr, ndist));
                // if (debugLogs) Debug.Log($"  Added tile ({nq},{nr}) to Group A, ndist={ndist}");
            }
        }

        // if (debugLogs) Debug.Log($"BFS complete: Group A has {groupA.Count} tiles");

        // Step 2: For each tile in Group A, check each direction
        // If neighbor is NOT in Group A, that edge is an outer boundary
        var outerEdges = new Dictionary<string, (int cq, int cr, int side)>();

        foreach (var tileCoord in groupA)
        {
            int cq = tileCoord.x;
            int cr = tileCoord.y;

            // Check all 6 directions
            for (int side = 0; side < 6; side++)
            {
                var nd = HexTile.NeighborDirections[side];
                int nq = cq + nd.x;
                int nr = cr + nd.y;
                var nCoord = new Vector2Int(nq, nr);

                // If neighbor is NOT in Group A, this edge is an outer boundary
                if (!groupA.Contains(nCoord))
                {
                    string key = cq + "_" + cr + "_" + side;
                    if (!outerEdges.ContainsKey(key))
                    {
                        outerEdges[key] = (cq, cr, side);
                        // if (debugLogs) Debug.Log($"Outer edge: tile ({cq},{cr}) side={side} -> neighbor ({nq},{nr}) NOT in Group A");
                    }
                }
            }
        }

        // if (debugLogs) Debug.Log($"Outer edges found: {outerEdges.Count}");

        if (outerEdges.Count == 0) return;

        // Build adjacency graph from collected edges (merge vertices within epsilon)
        var adj = new Dictionary<string, List<string>>();
        var posMap = new Dictionary<string, Vector3>();

        float eps = 0.001f;
        System.Func<Vector3, string> getVertexKey = (v) =>
        {
            foreach (var kv in posMap)
            {
                float dist = (kv.Value - v).magnitude;
                if (dist <= eps)
                {
                    // if (debugLogs) Debug.Log($"  Merged vertex at {v} to existing {kv.Value} (dist={dist})");
                    return kv.Key;
                }
            }
            int x = Mathf.RoundToInt(v.x * 10000f);
            int y = Mathf.RoundToInt(v.y * 10000f);
            string newKey = x + "_" + y;
            posMap[newKey] = v;
            // if (debugLogs) Debug.Log($"  Created new vertex key {newKey} at {v}");
            return newKey;
        };

        foreach (var kv in outerEdges)
        {
            var v = kv.Value;
            int cq2 = v.cq, cr2 = v.cr, cside2 = v.side;
            
            // Get the two corners that form this edge
            // Mapping:
            // side 0: 30¡Æ ¡æ 330¡Æ   (corner 0 ¡æ corner 5)
            // side 1: 330¡Æ ¡æ 270¡Æ  (corner 5 ¡æ corner 4)
            // side 2: 270¡Æ ¡æ 210¡Æ  (corner 4 ¡æ corner 3)
            // side 3: 210¡Æ ¡æ 150¡Æ  (corner 3 ¡æ corner 2)
            // side 4: 150¡Æ ¡æ 90¡Æ   (corner 2 ¡æ corner 1)
            // side 5: 90¡Æ ¡æ 30¡Æ    (corner 1 ¡æ corner 0)
            // Pattern: side i goes from corner (6-i)%6 to corner (5-i+6)%6
            int corner1 = (6 - cside2) % 6;
            int corner2 = (5 - cside2 + 6) % 6;
            
            Vector3 pa = TileHelper.HexToWorld(cq2, cr2, size) + GetHexCorner(size, corner1);
            Vector3 pb = TileHelper.HexToWorld(cq2, cr2, size) + GetHexCorner(size, corner2);
            string aKey = getVertexKey(pa);
            string bKey = getVertexKey(pb);
            // if (debugLogs) Debug.Log($"Edge {kv.Key}: ({cq2},{cr2}) side={cside2} -> corners {corner1},{corner2} -> vertices {aKey} to {bKey}");
            if (!adj.ContainsKey(aKey)) adj[aKey] = new List<string>();
            if (!adj.ContainsKey(bKey)) adj[bKey] = new List<string>();
            if (!adj[aKey].Contains(bKey)) adj[aKey].Add(bKey);
            if (!adj[bKey].Contains(aKey)) adj[bKey].Add(aKey);
        }

        // if (debugLogs)
        // {
        //     foreach (var kv2 in adj)
        //     {
        //         Debug.Log($"adj node {kv2.Key} degree={kv2.Value.Count}");
        //     }
        // }

        // Trace paths from adjacency graph
        var used = new HashSet<string>();
        int pathsCreated = 0;

        foreach (var kv in adj)
        {
            var startKey = kv.Key;
            if (used.Contains(startKey)) continue;

            // prefer endpoint (degree 1) to start
            string endpoint = null;
            foreach (var k in adj.Keys)
            {
                if (adj[k].Count == 1 && !used.Contains(k)) { endpoint = k; break; }
            }
            string current = endpoint ?? startKey;

            var pathKeys = new List<string>();
            string prev = null;
            while (current != null && !used.Contains(current))
            {
                used.Add(current);
                pathKeys.Add(current);
                string next = null;
                foreach (var cand in adj[current])
                {
                    if (cand == prev) continue;
                    if (used.Contains(cand)) continue;
                    next = cand;
                    break;
                }
                prev = current;
                current = next;
            }

            if (pathKeys.Count < 2) continue;

            var positions = new Vector3[pathKeys.Count];
            for (int i = 0; i < pathKeys.Count; i++) positions[i] = posMap[pathKeys[i]];

            string firstKey = pathKeys[0];
            string lastKey = pathKeys[pathKeys.Count - 1];
            bool isLoop = adj.ContainsKey(lastKey) && adj[lastKey].Contains(firstKey);

            if (isLoop)
            {
                Vector3 centroid = Vector3.zero;
                foreach (var p in positions) centroid += p;
                centroid /= positions.Length;
                System.Array.Sort(positions, (p1, p2) =>
                {
                    float a1 = Mathf.Atan2(p1.y - centroid.y, p1.x - centroid.x);
                    float a2 = Mathf.Atan2(p2.y - centroid.y, p2.x - centroid.x);
                    return a1.CompareTo(a2);
                });
            }

            var lr = GetLineRenderer();
            lr.loop = isLoop;
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.widthMultiplier = 1f;
            lr.material = lineMaterial;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.useWorldSpace = true;
            lr.gameObject.SetActive(true);
            pathsCreated++;
        }

        // if (debugLogs)
        // {
        //     Debug.Log($"HexBoundaryHighlighter: pathsCreated={pathsCreated}");
        // }
    }

    Vector3 GetHexCorner(float size, int i)
    {
        // use counter-clockwise ordering: angle = 60 * i + 30 (30¡Æ, 90¡Æ, 150¡Æ, 210¡Æ, 270¡Æ, 330¡Æ)
        // Pointy-top hexagon
        float angleDeg = 60f * i + 30f;
        float angleRad = Mathf.Deg2Rad * angleDeg;
        return new Vector3(size * Mathf.Cos(angleRad), size * Mathf.Sin(angleRad), 0f);
    }

    LineRenderer GetLineRenderer()
    {
        // reuse disabled renderer or create new
        foreach (var lr in lines)
        {
            if (!lr.gameObject.activeSelf) return lr;
        }
        var go = new GameObject("HexBoundaryLine");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.loop = false;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.material = lineMaterial;
        // default multiplier; per-line widths set when used
        line.widthMultiplier = 1f;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;
        // use world space positions
        line.useWorldSpace = true;
        // set alignment
        line.alignment = LineAlignment.TransformZ;
        lines.Add(line);
        return line;
    }

    public void Clear()
    {
        foreach (var lr in lines)
        {
            if (lr != null) lr.gameObject.SetActive(false);
        }
    }
}
