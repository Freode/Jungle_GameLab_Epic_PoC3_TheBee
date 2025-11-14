using System.Collections.Generic;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    // Default vision range if unit doesn't provide one
    public int defaultVisionRange = 3;

    // track unit positions and vision by id
    private class UnitSight
    {
        public Vector2Int pos;
        public int vision;
    }

    private Dictionary<int, UnitSight> unitSight = new Dictionary<int, UnitSight>();

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

    // Register/Update/Unregister unit positions with per-unit vision
    public void RegisterUnit(int id, int q, int r, int vision)
    {
        unitSight[id] = new UnitSight { pos = new Vector2Int(q, r), vision = vision > 0 ? vision : defaultVisionRange };
    }

    public void UpdateUnitPosition(int id, int q, int r, int vision)
    {
        if (unitSight.ContainsKey(id))
        {
            unitSight[id].pos = new Vector2Int(q, r);
            unitSight[id].vision = vision > 0 ? vision : defaultVisionRange;
        }
        else
        {
            RegisterUnit(id, q, r, vision);
        }
    }

    public void UnregisterUnit(int id)
    {
        if (unitSight.ContainsKey(id)) unitSight.Remove(id);
    }

    // Recalculate which tiles are visible based on current unit positions and their individual vision
    public void RecalculateVisibility()
    {
        var tm = TileManager.Instance;
        if (tm == null) return;

        // compute union of visible tile coordinates
        var visibleCoords = new HashSet<Vector2Int>();
        foreach (var kv in unitSight)
        {
            var center = kv.Value.pos;
            int vision = kv.Value.vision;
            var tilesInRange = GetTilesCoordsInRange(center.x, center.y, vision);
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
        RegisterUnit(-1, q, r, defaultVisionRange);
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
