using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Map Shape")]
    public bool hexMap = true; // if true, generate a hex-shaped map around (0,0)
    public int mapRadius = 5; // radius for hex-shaped map (number of rings around center)

    [Header("Map Size (rect)")]
    public int mapWidth = 20;
    public int mapHeight = 12;

    [Header("Hex Settings")]
    public float hexSize = 0.5f;
    public GameObject hexPrefab;

    [Header("Terrain")]
    public TerrainType[] terrainTypes;
    public TerrainType normalTerrain; // new: color to use when resources depleted
    public float noiseScale = 0.15f;
    public int noiseSeed = 0;

    [Header("Spawn")]
    public GameObject queenPrefab;
    public GameObject hivePrefab; // prefab used when constructing a hive
    public GameObject normalBeePrefab; // renamed from workerPrefab to normalBeePrefab

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // optional: persist across scenes
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GenerateMap();
        SpawnQueenNearEdge();
    }

    void Update()
    {
    }

    void GenerateMap()
    {
        if (hexPrefab == null)
        {
            Debug.LogError("GameManager: hexPrefab is not assigned.");
            return;
        }

        if (terrainTypes == null || terrainTypes.Length == 0)
        {
            Debug.LogError("GameManager: terrainTypes is empty.");
            return;
        }

        System.Random rnd = new System.Random(noiseSeed);
        float xOffset = rnd.Next(-10000, 10000);
        float yOffset = rnd.Next(-10000, 10000);

        if (hexMap)
        {
            int radius = Mathf.Max(0, mapRadius);
            // axial coordinates: for each q from -radius..+radius, r from max(-radius, -q-radius)..min(radius, -q+radius)
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    CreateTileAt(q, r, xOffset, yOffset);
                }
            }
        }
        else
        {
            // rectangular/parallelogram generation centered around origin as a fallback
            int halfW = mapWidth / 2;
            int halfH = mapHeight / 2;
            for (int r = -halfH; r < mapHeight - halfH; r++)
            {
                for (int q = -halfW; q < mapWidth - halfW; q++)
                {
                    CreateTileAt(q, r, xOffset, yOffset);
                }
            }
        }
    }

    void CreateTileAt(int q, int r, float xOffset, float yOffset)
    {
        Vector3 pos = TileHelper.HexToWorld(q, r, hexSize);
        GameObject go = Instantiate(hexPrefab, pos, Quaternion.identity, transform);
        go.name = $"Hex_{q}_{r}";

        HexTile tile = go.GetComponent<HexTile>();
        if (tile == null)
        {
            tile = go.AddComponent<HexTile>();
        }

        tile.SetCoords(q, r);

        // Register tile with TileManager
        var tm = TileManager.Instance;
        if (tm != null) tm.RegisterTile(tile);

        float nx = (q + xOffset) * noiseScale;
        float ny = (r + yOffset) * noiseScale;
        float sample = Mathf.PerlinNoise(nx, ny);
        int terrainIndex = Mathf.FloorToInt(sample * terrainTypes.Length);
        terrainIndex = Mathf.Clamp(terrainIndex, 0, terrainTypes.Length - 1);

        tile.SetTerrain(terrainTypes[terrainIndex]);

        // Initially mark all as Hidden
        tile.SetFogState(HexTile.FogState.Hidden);
    }

    void SpawnQueenNearEdge()
    {
        if (queenPrefab == null) return;
        int radius = mapRadius;
        if (!hexMap)
        {
            // for non hex map, choose near outer edge of rectangle
            int q = mapWidth/2 - 2;
            int r = 0;
            SpawnQueenAt(q, r);
            return;
        }

        // pick a random edge direction and position two tiles inwards from the outer ring
        System.Random rnd = new System.Random();
        int dir = rnd.Next(0, 6);
        // direction vectors for axial
        Vector2Int[] dirs = new Vector2Int[] {
            new Vector2Int(1,0), new Vector2Int(1,-1), new Vector2Int(0,-1),
            new Vector2Int(-1,0), new Vector2Int(-1,1), new Vector2Int(0,1)
        };
        Vector2Int chosenDir = dirs[dir];
        // position at radius-2 along that direction
        int qPos = chosenDir.x * (radius - 2);
        int rPos = chosenDir.y * (radius - 2);
        // slight random offset along adjacent edge positions
        int offset = rnd.Next(-1, 2);
        qPos += offset * dirs[(dir + 2) % 6].x;
        rPos += offset * dirs[(dir + 2) % 6].y;

        SpawnQueenAt(qPos, rPos);
    }

    void SpawnQueenAt(int q, int r)
    {
        var tile = TileManager.Instance.GetTile(q, r);
        if (tile == null) return;
        Vector3 pos = TileHelper.HexToWorld(q, r, hexSize);
        var go = Instantiate(queenPrefab, pos, Quaternion.identity);
        var agent = go.GetComponent<UnitAgent>();
        if (agent != null)
        {
            agent.SetPosition(q, r);
        }

        // Move camera to queen position (preserve camera z)
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            cam.transform.position = new Vector3(pos.x, pos.y, camPos.z);
        }

        // Update Fog based on queen presence
        FogOfWarManager.Instance?.RecalculateVisibility();
    }
}
