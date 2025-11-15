using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    private Dictionary<Vector2Int, HexTile> tiles = new Dictionary<Vector2Int, HexTile>();
    private List<UnitAgent> units = new List<UnitAgent>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterTile(HexTile tile)
    {
        tiles[new Vector2Int(tile.q, tile.r)] = tile;
    }

    public HexTile GetTile(int q, int r)
    {
        tiles.TryGetValue(new Vector2Int(q, r), out HexTile tile);
        return tile;
    }

    public List<HexTile> GetNeighbors(int q, int r)
    {
        var result = new List<HexTile>(6);
        foreach (var dir in HexTile.NeighborDirections)
        {
            var nc = new Vector2Int(q + dir.x, r + dir.y);
            if (tiles.TryGetValue(nc, out HexTile t)) result.Add(t);
        }
        return result;
    }

    public IEnumerable<HexTile> GetAllTiles() => tiles.Values;

    // unit registration to avoid FindObjectsOfType
    public void RegisterUnit(UnitAgent unit)
    {
        if (!units.Contains(unit)) units.Add(unit);
    }

    public void UnregisterUnit(UnitAgent unit)
    {
        if (units.Contains(unit)) units.Remove(unit);
    }

    public IEnumerable<UnitAgent> GetAllUnits() => units;
}
