using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 진영 하이브 생성 시스템
/// - 장수말벌집: 맵 중앙 (0,0)에 1개만 생성
/// - 일반 말벌집: 장수말벌집으로부터 거리 4~9 사이, 최소 간격 3 이상으로 랜덤 생성
/// </summary>
public class EnemyHiveSpawner : MonoBehaviour
{
    public static EnemyHiveSpawner Instance { get; private set; }

    [Header("프리팹")]
    public GameObject eliteWaspHivePrefab;  // 장수말벌집 프리팹
    public GameObject normalWaspHivePrefab; // 일반 말벌집 프리팹

    [Header("생성 설정")]
    [Tooltip("게임 시작 시 자동 생성")]
    public bool autoSpawnOnStart = true;
    
    [Tooltip("생성 지연 시간 (초) - 맵 로드 대기")]
    public float spawnDelay = 1f;
    
    [Tooltip("일반 말벌집 생성 개수")]
    public int normalHiveCount = 3;
    
    [Tooltip("일반 말벌집 최소 거리 (장수말벌집 기준)")]
    public int minDistanceFromElite = 4;
    
    [Tooltip("일반 말벌집 최대 거리 (장수말벌집 기준)")]
    public int maxDistanceFromElite = 9;
    
    [Tooltip("일반 말벌집 간 최소 간격")]
    public int minDistanceBetweenHives = 3;

    private GameObject eliteHive;
    private List<GameObject> normalHives = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 게임 시작 시 자동 생성
        if (autoSpawnOnStart)
        {
            if (spawnDelay > 0)
            {
                Invoke(nameof(SpawnAllEnemyHives), spawnDelay);
            }
            else
            {
                SpawnAllEnemyHives();
            }
        }
    }

    /// <summary>
    /// 모든 적 하이브 생성
    /// </summary>
    public void SpawnAllEnemyHives()
    {
        // 프리팹 체크
        if (!ValidatePrefabs())
        {
            Debug.LogError("[적 하이브 생성] 필수 프리팹이 설정되지 않았습니다!");
            return;
        }

        // TileManager 체크
        if (TileManager.Instance == null)
        {
            Debug.LogError("[적 하이브 생성] TileManager가 없습니다!");
            return;
        }

        // 중복 생성 방지
        if (eliteHive != null || normalHives.Count > 0)
        {
            Debug.LogWarning("[적 하이브 생성] 이미 생성되었습니다.");
            return;
        }

        // 1. 장수말벌집 생성 (0, 0)
        SpawnEliteWaspHive();

        // 2. 일반 말벌집 생성
        SpawnNormalWaspHives(normalHiveCount);

        Debug.Log($"[적 하이브 생성] 장수말벌집 1개, 일반 말벌집 {normalHives.Count}개 생성 완료");
    }

    /// <summary>
    /// 프리팹 유효성 검사
    /// </summary>
    bool ValidatePrefabs()
    {
        bool isValid = true;

        if (eliteWaspHivePrefab == null)
        {
            Debug.LogError("[적 하이브 생성] Elite Wasp Hive Prefab이 없습니다!");
            isValid = false;
        }

        if (normalWaspHivePrefab == null)
        {
            Debug.LogError("[적 하이브 생성] Normal Wasp Hive Prefab이 없습니다!");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 엘리트말벌집 생성 (0, 0 위치)
    /// </summary>
    void SpawnEliteWaspHive()
    {
        int q = 0, r = 0;
        
        // 타일 존재 확인
        var tile = TileManager.Instance?.GetTile(q, r);
        if (tile == null)
        {
            Debug.LogError($"[적 하이브 생성] 타일 ({q}, {r})이 존재하지 않습니다!");
            return;
        }

        // ✅ 자원 타일이면 일반 타일로 변경
        if (tile.resourceAmount > 0 && GameManager.Instance != null && GameManager.Instance.normalTerrain != null)
        {
            Debug.Log($"[적 하이브 생성] 자원 타일 ({q}, {r})을 일반 타일로 변경");
            tile.SetTerrain(GameManager.Instance.normalTerrain);
            tile.SetResourceAmount(0);
        }

        // 프리팹 생성 (위치는 Initialize에서 설정됨)
        eliteHive = Instantiate(eliteWaspHivePrefab, Vector3.zero, Quaternion.identity);
        eliteHive.name = "EliteWaspHive";

        // EnemyHive 컴포넌트 가져오기
        var enemyHive = eliteHive.GetComponent<EnemyHive>();
        if (enemyHive != null)
        {
            // EnemyHive.Initialize() 호출 (q, r 설정 및 타일 부착)
            enemyHive.Initialize(q, r, 6250); // 장수말벌집 체력 ? 원하는 값으로 변경
            Debug.Log($"[적 하이브 생성] 엘리트말벌집 생성: ({q}, {r})");
        }
        else
        {
            Debug.LogError($"[적 하이브 생성] EnemyHive 컴포넌트를 찾을 수 없습니다!");
            Destroy(eliteHive);
            eliteHive = null;
        }
    }

    /// <summary>
    /// 일반 말벌집 생성 및 배치
    /// </summary>
    void SpawnNormalWaspHives(int count)
    {
        List<Vector2Int> spawnedPositions = new List<Vector2Int>();
        int attempts = 0;
        int maxAttempts = 100;

        for (int i = 0; i < count && attempts < maxAttempts; attempts++)
        {
            Vector2Int pos = FindValidHivePosition(spawnedPositions);
            
            if (pos.x == int.MinValue) continue;

            // ✅ 자원 타일이면 일반 타일로 변경
            var tile = TileManager.Instance?.GetTile(pos.x, pos.y);
            if (tile != null && tile.resourceAmount > 0 && GameManager.Instance != null && GameManager.Instance.normalTerrain != null)
            {
                Debug.Log($"[적 하이브 생성] 자원 타일 ({pos.x}, {pos.y})을 일반 타일로 변경");
                tile.SetTerrain(GameManager.Instance.normalTerrain);
                tile.SetResourceAmount(0);
            }

            // 프리팹 생성 (위치는 Initialize에서 설정됨)
            GameObject hive = Instantiate(normalWaspHivePrefab, Vector3.zero, Quaternion.identity);
            hive.name = $"NormalWaspHive_{i + 1}";

            // EnemyHive 컴포넌트 가져오기
            var enemyHive = hive.GetComponent<EnemyHive>();
            if (enemyHive != null)
            {
                // EnemyHive.Initialize() 호출 (q, r 설정 및 타일 부착)
                enemyHive.Initialize(pos.x, pos.y, 750); // 일반 말벌집 체력 ? 원하는 값으로 변경
                Debug.Log($"[적 하이브 생성] 일반 말벌집 생성: ({pos.x}, {pos.y})");
            }
            else
            {
                Debug.LogError($"[적 하이브 생성] EnemyHive 컴포넌트를 찾을 수 없습니다!");
                Destroy(hive);
                continue;
            }

            normalHives.Add(hive);
            spawnedPositions.Add(pos);
            i++;
        }

        if (normalHives.Count < count)
        {
            Debug.LogWarning($"[적 하이브 생성] 요청: {count}개, 실제 생성: {normalHives.Count}개");
        }
    }

    /// <summary>
    /// 유효한 하이브 생성 위치 찾기
    /// </summary>
    Vector2Int FindValidHivePosition(List<Vector2Int> existingPositions)
    {
        int attempts = 0;
        int maxAttempts = 50;

        while (attempts < maxAttempts)
        {
            attempts++;

            // 랜덤 거리와 방향 생성
            int distance = Random.Range(minDistanceFromElite, maxDistanceFromElite + 1);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Hex 좌표로 변환 (근사값)
            int q = Mathf.RoundToInt(distance * Mathf.Cos(angle));
            int r = Mathf.RoundToInt(distance * Mathf.Sin(angle));

            // 실제 거리 확인
            int actualDistance = Pathfinder.AxialDistance(0, 0, q, r);
            if (actualDistance < minDistanceFromElite || actualDistance > maxDistanceFromElite)
                continue;

            // 기존 하이브들과의 거리 확인
            bool tooClose = false;
            foreach (var pos in existingPositions)
            {
                int dist = Pathfinder.AxialDistance(q, r, pos.x, pos.y);
                if (dist < minDistanceBetweenHives)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            // 타일이 존재하는지 확인
            var tile = TileManager.Instance?.GetTile(q, r);
            if (tile == null) continue;

            // 유효한 위치 찾음
            return new Vector2Int(q, r);
        }

        // 유효한 위치를 못 찾음
        return new Vector2Int(int.MinValue, int.MinValue);
    }

    /// <summary>
    /// 모든 적 하이브 제거
    /// </summary>
    public void ClearAllEnemyHives()
    {
        if (eliteHive != null)
        {
            Destroy(eliteHive);
            eliteHive = null;
        }

        foreach (var hive in normalHives)
        {
            if (hive != null)
                Destroy(hive);
        }
        normalHives.Clear();

        Debug.Log("[적 하이브 생성] 모든 적 하이브 제거 완료");
    }

    /// <summary>
    /// 장수말벌집 가져오기
    /// </summary>
    public GameObject GetEliteHive()
    {
        return eliteHive;
    }

    /// <summary>
    /// 일반 말벌집 목록 가져오기
    /// </summary>
    public List<GameObject> GetNormalHives()
    {
        return new List<GameObject>(normalHives);
    }

    /// <summary>
    /// 특정 하이브 제거 (파괴됐을 때)
    /// </summary>
    public void UnregisterHive(GameObject hive)
    {
        if (hive == eliteHive)
        {
            eliteHive = null;
            Debug.Log("[적 하이브 생성] 장수말벌집 파괴됨!");
        }
        else if (normalHives.Contains(hive))
        {
            normalHives.Remove(hive);
            Debug.Log($"[적 하이브 생성] 일반 말벌집 파괴됨! (남은 개수: {normalHives.Count})");
        }
    }
}
