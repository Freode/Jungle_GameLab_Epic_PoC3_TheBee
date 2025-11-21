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

    [Header("Resource Settings")] // ? 자원 설정 추가
    [Tooltip("자원 클러스터 개수")]
    public int resourceClusterCount = 10;
    [Tooltip("한 클러스터당 최소 타일 수")]
    public int minTilesPerCluster = 3;
    [Tooltip("한 클러스터당 최대 타일 수")]
    public int maxTilesPerCluster = 6;
    [Tooltip("클러스터 간 최소 거리 (타일 수)")]
    public int minClusterDistance = 3; // ? 최소 거리 추가

    [Header("Spawn")]
    public GameObject queenPrefab; // Initial queen spawned at game start
    public GameObject hivePrefab; // prefab used when constructing a hive
    public GameObject normalBeePrefab; // worker bee prefab for hive spawning

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

        if (normalTerrain == null)
        {
            Debug.LogError("GameManager: normalTerrain is not assigned.");
            return;
        }

        System.Random rnd = new System.Random(noiseSeed);

        // 1단계: 모든 타일을 normal 지형으로 생성 ?
        if (hexMap)
        {
            int radius = Mathf.Max(0, mapRadius);
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    CreateTileAt(q, r, normalTerrain); // ? normal 지형으로 생성
                }
            }
        }
        else
        {
            int halfW = mapWidth / 2;
            int halfH = mapHeight / 2;
            for (int r = -halfH; r < mapHeight - halfH; r++)
            {
                for (int q = -halfW; q < mapWidth - halfW; q++)
                {
                    CreateTileAt(q, r, normalTerrain); // ? normal 지형으로 생성
                }
            }
        }
        
        // 2단계: 자원 클러스터 생성 ?
        GenerateResourceClusters(rnd);
    }

    void CreateTileAt(int q, int r, TerrainType terrain) // ? terrain 파라미터 추가
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

        tile.SetTerrain(terrain); // ? 지정된 지형 설정

        // Initially mark all as Hidden
        tile.SetFogState(HexTile.FogState.Hidden);
    }
    
    /// <summary>
    /// 자원 클러스터 생성 ?
    /// </summary>
    void GenerateResourceClusters(System.Random rnd)
    {
        if (TileManager.Instance == null) return;
        
        // 자원 지형 찾기 (resourceYield > 0인 지형)
        TerrainType resourceTerrain = null;
        foreach (var terrain in terrainTypes)
        {
            if (terrain != null && terrain.resourceYield > 0)
            {
                resourceTerrain = terrain;
                break;
            }
        }
        
        if (resourceTerrain == null)
        {
            Debug.LogWarning("GameManager: 자원 지형이 없습니다. (resourceYield > 0)");
            return;
        }
        
        HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>(); // 클러스터 내 타일
        Dictionary<Vector2Int, int> tileClusterID = new Dictionary<Vector2Int, int>(); // ? 타일별 클러스터 ID
        List<Vector2Int> clusterCenters = new List<Vector2Int>(); // 클러스터 중심 좌표 목록
        
        for (int i = 0; i < resourceClusterCount; i++)
        {
            int currentClusterID = i + 1; // ? 현재 클러스터 ID (1부터 시작)
            
            // 랜덤 중심 좌표 선택 (다른 클러스터와 1칸 이상 분리) ?
            int attempts = 0;
            int centerQ = 0, centerR = 0;
            bool validCenter = false;
            
            while (attempts < 200)
            {
                if (hexMap)
                {
                    int radius = mapRadius;
                    centerQ = rnd.Next(-radius, radius + 1);
                    int r1 = Mathf.Max(-radius, -centerQ - radius);
                    int r2 = Mathf.Min(radius, -centerQ + radius);
                    centerR = rnd.Next(r1, r2 + 1);
                }
                else
                {
                    int halfW = mapWidth / 2;
                    int halfH = mapHeight / 2;
                    centerQ = rnd.Next(-halfW, mapWidth - halfW);
                    centerR = rnd.Next(-halfH, mapHeight - halfH);
                }
                
                var coord = new Vector2Int(centerQ, centerR);
                
                // ? 1. 이미 사용된 타일인지 체크
                if (usedPositions.Contains(coord))
                {
                    attempts++;
                    continue;
                }
                
                // ? 2. 주변 1칸 이내에 다른 클러스터의 자원 타일이 있는지 체크 (핵심!)
                bool hasOtherClusterNearby = false;
                foreach (var dir in HexTile.NeighborDirections)
                {
                    int nq = centerQ + dir.x;
                    int nr = centerR + dir.y;
                    var ncoord = new Vector2Int(nq, nr);
                    
                    // ? 다른 클러스터의 타일인지 확인
                    if (tileClusterID.ContainsKey(ncoord))
                    {
                        // 같은 클러스터면 OK, 다른 클러스터면 NG
                        if (tileClusterID[ncoord] != currentClusterID)
                        {
                            hasOtherClusterNearby = true;
                            break;
                        }
                    }
                }
                
                if (hasOtherClusterNearby)
                {
                    attempts++;
                    continue;
                }
                
                // ? 3. 다른 클러스터 중심과의 최소 거리 체크 (추가 안전장치)
                bool tooClose = false;
                foreach (var existingCenter in clusterCenters)
                {
                    int distance = Pathfinder.AxialDistance(centerQ, centerR, existingCenter.x, existingCenter.y);
                    if (distance < minClusterDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose)
                {
                    validCenter = true;
                    break;
                }
                
                attempts++;
            }
            
            if (!validCenter) 
            {
                Debug.LogWarning($"[GameManager] 클러스터 {currentClusterID} 생성 실패: 적절한 위치를 찾을 수 없습니다.");
                continue;
            }
            
            // ? 클러스터 중심 기록
            clusterCenters.Add(new Vector2Int(centerQ, centerR));
            
            // 클러스터 크기 결정
            int clusterSize = rnd.Next(minTilesPerCluster, maxTilesPerCluster + 1);
            
            // 중심 타일 자원 설정
            usedPositions.Add(new Vector2Int(centerQ, centerR));
            tileClusterID[new Vector2Int(centerQ, centerR)] = currentClusterID; // ? 클러스터 ID 기록
            CreateResourceTile(centerQ, centerR, resourceTerrain, centerQ, centerR);
            
            // 주변 타일 자원 설정 (BFS 방식)
            List<Vector2Int> tilesToProcess = new List<Vector2Int>();
            tilesToProcess.Add(new Vector2Int(centerQ, centerR));
            int tilesCreated = 1;
            
            while (tilesToProcess.Count > 0 && tilesCreated < clusterSize)
            {
                int idx = rnd.Next(0, tilesToProcess.Count);
                var current = tilesToProcess[idx];
                tilesToProcess.RemoveAt(idx);
                
                // 6방향 이웃 탐색
                foreach (var dir in HexTile.NeighborDirections)
                {
                    if (tilesCreated >= clusterSize) break;
                    
                    int nq = current.x + dir.x;
                    int nr = current.y + dir.y;
                    var ncoord = new Vector2Int(nq, nr);
                    
                    if (usedPositions.Contains(ncoord)) continue;
                    
                    // ? 주변에 다른 클러스터가 있는지 확인
                    bool hasOtherClusterAdjacent = false;
                    foreach (var checkDir in HexTile.NeighborDirections)
                    {
                        int checkQ = nq + checkDir.x;
                        int checkR = nr + checkDir.y;
                        var checkCoord = new Vector2Int(checkQ, checkR);
                        
                        if (tileClusterID.ContainsKey(checkCoord) && tileClusterID[checkCoord] != currentClusterID)
                        {
                            hasOtherClusterAdjacent = true;
                            break;
                        }
                    }
                    
                    if (hasOtherClusterAdjacent) continue; // ? 다른 클러스터와 인접하면 건너뜀
                    
                    var neighborTile = TileManager.Instance.GetTile(nq, nr);
                    if (neighborTile != null && rnd.NextDouble() < 0.6) // 60% 확률
                    {
                        usedPositions.Add(ncoord);
                        tileClusterID[ncoord] = currentClusterID; // ? 클러스터 ID 기록
                        CreateResourceTile(nq, nr, resourceTerrain, centerQ, centerR);
                        tilesToProcess.Add(ncoord);
                        tilesCreated++;
                    }
                }
            }
            
            Debug.Log($"[GameManager] 클러스터 {currentClusterID}/{resourceClusterCount} 생성 완료: 중심 ({centerQ}, {centerR}), 타일 수: {tilesCreated}");
        }
        
        Debug.Log($"[GameManager] 총 {clusterCenters.Count}개의 자원 클러스터 생성 완료 (모든 클러스터는 1칸 이상 분리됨)");
    }
    
    /// <summary>
    /// 자원 타일 생성 (거리에 따라 자원량 조정) ?
    /// </summary>
    void CreateResourceTile(int q, int r, TerrainType resourceTerrain, int centerQ, int centerR)
    {
        var tile = TileManager.Instance.GetTile(q, r);
        if (tile == null) return;
        
        // 지형 변경
        tile.SetTerrain(resourceTerrain);
        
        // (0,0)으로부터의 거리 계산
        int distanceFromOrigin = Pathfinder.AxialDistance(0, 0, q, r);
        
        // 거리에 따라 자원량 결정 ?
        int resourceAmount = 0;
        System.Random rnd = new System.Random(q * 1000 + r);
        
        if (distanceFromOrigin <= 3)
        {
            // 3칸 이내: 400~450
            resourceAmount = rnd.Next(400, 451);
        }
        else if (distanceFromOrigin <= 5)
        {
            // 7칸 이내: 275~325
            resourceAmount = rnd.Next(275, 326);
        }
        else if (distanceFromOrigin <= 7)
        {
            // 10칸 이내: 175~225
            resourceAmount = rnd.Next(175, 226);
        }
        else if (distanceFromOrigin <= 10)
        {
            // 10칸 이내: 90~130
            resourceAmount = rnd.Next(90, 131);
        }
        else
        {
            // 그 외: 35~75
            resourceAmount = rnd.Next(35, 76);
        }
        
        // 중심으로부터의 거리에 따라 추가 감소 (클러스터 중심이 더 많음)
        int distanceFromCenter = Pathfinder.AxialDistance(centerQ, centerR, q, r);
        float reductionFactor = 1f - (distanceFromCenter * 0.1f); // 10%씩 감소
        resourceAmount = Mathf.RoundToInt(resourceAmount * Mathf.Max(0.5f, reductionFactor));
        
        tile.SetResourceAmount(resourceAmount);
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
            agent.isQueen = true; // Mark as queen bee
            agent.canMove = true;
            agent.faction = Faction.Player;
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
