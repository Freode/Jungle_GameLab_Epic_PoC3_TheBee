using System.Collections.Generic;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    public int visionRange = 3;

    // track unit positions by id
    private Dictionary<int, Vector2Int> unitPositions = new Dictionary<int, Vector2Int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void LateUpdate()
    {
        RecalculateVisibility();
    }

    // Register/Update/Unregister unit positions
    public void RegisterUnit(int id, int q, int r)
    {
        unitPositions[id] = new Vector2Int(q, r);
    }

    public void UpdateUnitPosition(int id, int q, int r)
    {
        unitPositions[id] = new Vector2Int(q, r);
    }

    public void UnregisterUnit(int id)
    {
        if (unitPositions.ContainsKey(id)) unitPositions.Remove(id);
    }

    // Recalculate which tiles are visible based on current unit positions
    public void RecalculateVisibility()
    {
        var tm = TileManager.Instance;
        if (tm == null) return;

        // compute union of visible tile coordinates
        var visibleCoords = new HashSet<Vector2Int>();
        foreach (var kv in unitPositions)
        {
            var center = kv.Value;
            var tilesInRange = GetTilesCoordsInRange(center.x, center.y, visionRange);
            foreach (var c in tilesInRange) visibleCoords.Add(c);
        }

        // apply fog state changes
        foreach (var tile in tm.GetAllTiles())
        {
            var coord = new Vector2Int(tile.q, tile.r);
            bool isVisible = visibleCoords.Contains(coord);
            if (isVisible)
            {
                tile.SetFogState(HexTile.FogState.Visible);
            }
            else
            {
                if (tile.fogState == HexTile.FogState.Visible)
                {
                    // was visible but no longer: set to Revealed
                    tile.SetFogState(HexTile.FogState.Revealed);
                }
                // if already Revealed or Hidden, keep as is
            }
        }
    }

    // Return list of tile coordinates within axial radius using axial loops
    List<Vector2Int> GetTilesCoordsInRange(int q, int r, int radius)
    {
        var result = new List<Vector2Int>();
        for (int dq = -radius; dq <= radius; dq++)
        {
            int minDr = Mathf.Max(-radius, -dq - radius);
            int maxDr = Mathf.Min(radius, -dq + radius);
            for (int dr = minDr; dr <= maxDr; dr++)
            {
                int qq = q + dq;
                int rr = r + dr;
                result.Add(new Vector2Int(qq, rr));
            }
        }
        return result;
    }

    // Debug: force reveal area around coords (legacy)
    public void RevealArea(int q, int r)
    {
        // legacy single-source reveal: register a temporary source with id -1
        RegisterUnit(-1, q, r);
        RecalculateVisibility();
        UnregisterUnit(-1);
    }

    // Optional: get tiles in range objects
    public List<HexTile> GetTilesInRange(int q, int r, int radius)
    {
        var tm = TileManager.Instance;
        var result = new List<HexTile>();
        if (tm == null) return result;
        var coords = GetTilesCoordsInRange(q, r, radius);
        foreach (var c in coords)
        {
            var tile = tm.GetTile(c.x, c.y);
            if (tile != null) result.Add(tile);
        }
        return result;
    }
}
