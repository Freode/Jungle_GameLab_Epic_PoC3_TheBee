using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy 유닛의 가시성을 현재 플레이어 시야에 따라 제어
/// - 플레이어 시야 내에 있을 때만 보임
/// - 시야에서 벗어나면 숨김 (전장의 안개가 걷혀있어도)
/// - 말벌집은 한 번 발견하면 계속 보임
/// </summary>
public class EnemyVisibilityController : MonoBehaviour
{
    public static EnemyVisibilityController Instance { get; private set; }

    [Header("설정")]
    [Tooltip("가시성 업데이트 주기 (초)")]
    public float updateInterval = 0.2f;
    
    [Tooltip("디버그 로그 표시")]
    public bool showDebugLogs = false;

    private float lastUpdateTime;
    private HashSet<Vector2Int> currentVisibleTiles = new HashSet<Vector2Int>();
    
    // 한 번 발견된 하이브 목록
    private HashSet<GameObject> discoveredHives = new HashSet<GameObject>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;
            UpdateEnemyVisibility();
        }
    }

    /// <summary>
    /// 모든 Enemy 유닛의 시각화 업데이트
    /// </summary>
    void UpdateEnemyVisibility()
    {
        // 현재 플레이어 시야 계산
        CalculatePlayerVision();

        if (showDebugLogs)
            Debug.Log($"[시야 디버그] 현재 플레이어 시야 타일 수: {currentVisibleTiles.Count}");

        // 모든 유닛 확인
        if (TileManager.Instance == null) return;

        int processedEnemies = 0;
        int visibleEnemies = 0;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;

            // Enemy 유닛만 처리
            if (unit.faction != Faction.Enemy) continue;

            processedEnemies++;

            // 하이브 체크 (플레이어 Hive와 EnemyHive 모두 체크)
            var playerHive = unit.GetComponent<Hive>();
            var enemyHive = unit.GetComponent<EnemyHive>();
            bool isHive = (playerHive != null || enemyHive != null);

            // 유닛 위치
            Vector2Int unitPos = new Vector2Int(unit.q, unit.r);

            // 플레이어 시야 안에 있는지 확인
            bool isCurrentlyVisible = currentVisibleTiles.Contains(unitPos);

            if (showDebugLogs && isHive)
                Debug.Log($"[시야 디버그] 하이브 체크: {unit.name} at ({unit.q}, {unit.r}), 시야 내: {isCurrentlyVisible}");

            bool shouldBeVisible = false;

            if (isHive)
            {
                // 하이브: 한 번 발견하면 항상 보임
                if (isCurrentlyVisible)
                {
                    // 처음 발견
                    if (!discoveredHives.Contains(unit.gameObject))
                    {
                        discoveredHives.Add(unit.gameObject);
                        
                        string hiveType = enemyHive != null ? "적 말벌집" : "하이브";
                        Debug.Log($"[시야] {hiveType} 발견: {unit.name} at ({unit.q}, {unit.r})");
                        
                        if (showDebugLogs)
                            Debug.Log($"[시야 디버그] 발견된 하이브 수: {discoveredHives.Count}");
                    }
                }

                // 발견된 하이브는 항상 보임
                shouldBeVisible = discoveredHives.Contains(unit.gameObject);
                
                if (showDebugLogs)
                    Debug.Log($"[시야 디버그] 하이브 {unit.name} shouldBeVisible: {shouldBeVisible}");
            }
            else
            {
                // 일반 유닛: 현재 시야 범위만 보임
                shouldBeVisible = isCurrentlyVisible;
            }

            if (shouldBeVisible) visibleEnemies++;

            // 시각화 설정
            SetUnitVisibility(unit, shouldBeVisible);
        }
        
        // EnemyHive GameObject들도 직접 체크 ?
        var allEnemyHives = FindObjectsOfType<EnemyHive>();
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] FindObjectsOfType<EnemyHive> 결과: {allEnemyHives.Length}개");

        foreach (var enemyHive in allEnemyHives)
        {
            if (enemyHive == null) continue;
            
            // EnemyHive의 UnitAgent 가져오기
            var hiveAgent = enemyHive.GetComponent<UnitAgent>();
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] EnemyHive: {enemyHive.name}, UnitAgent: {(hiveAgent != null ? "있음" : "없음")}");
            
            if (hiveAgent == null)
            {
                Debug.LogWarning($"[시야 경고] EnemyHive {enemyHive.name}에 UnitAgent가 없습니다!");
                continue;
            }
            
            // 이미 처리되었으면 스킵
            if (hiveAgent.faction != Faction.Enemy)
            {
                if (showDebugLogs)
                    Debug.Log($"[시야 디버그] EnemyHive {enemyHive.name} faction이 Enemy가 아님: {hiveAgent.faction}");
                continue;
            }
            
            // 위치 확인
            Vector2Int hivePos = new Vector2Int(enemyHive.q, enemyHive.r);
            bool isVisible = currentVisibleTiles.Contains(hivePos);
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] EnemyHive {enemyHive.name} at ({enemyHive.q}, {enemyHive.r}), 시야 내: {isVisible}");
            
            if (isVisible)
            {
                // 처음 발견
                if (!discoveredHives.Contains(enemyHive.gameObject))
                {
                    discoveredHives.Add(enemyHive.gameObject);
                    Debug.Log($"[시야] 적 말벌집 발견: {enemyHive.name} at ({enemyHive.q}, {enemyHive.r})");
                }
            }
            
            // 발견된 하이브는 항상 보임
            bool shouldBeVisible = discoveredHives.Contains(enemyHive.gameObject);
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] EnemyHive {enemyHive.name} shouldBeVisible: {shouldBeVisible}");
            
            // EnemyHive GameObject의 렌더러 활성화 ?
            SetEnemyHiveVisibility(enemyHive, shouldBeVisible);
        }

        if (showDebugLogs)
            Debug.Log($"[시야 디버그] 처리된 적: {processedEnemies}개, 표시할 적: {visibleEnemies}개");
    }
    
    /// <summary>
    /// EnemyHive의 시각화 설정 ?
    /// </summary>
    void SetEnemyHiveVisibility(EnemyHive hive, bool visible)
    {
        if (hive == null) return;
        
        GameObject targetObject = hive.gameObject;
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] EnemyHive 렌더러 설정: {targetObject.name}, visible={visible}");

        int rendererCount = 0;

        // 1. 자신의 SpriteRenderer
        var sprite = targetObject.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.enabled = visible;
            rendererCount++;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] EnemyHive SpriteRenderer: {sprite.name}, enabled={visible}");
        }

        // 2. 자신의 Renderer
        var renderer = targetObject.GetComponent<Renderer>();
        if (renderer != null && renderer != sprite)
        {
            renderer.enabled = visible;
            rendererCount++;
        }

        // 3. 자식 GameObject들의 모든 Renderer
        var childRenderers = targetObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in childRenderers)
        {
            r.enabled = visible;
            rendererCount++;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] EnemyHive 자식 Renderer: {r.name}, enabled={visible}");
        }

        // 4. 자식 GameObject들의 모든 SpriteRenderer
        var childSprites = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in childSprites)
        {
            // 이미 처리된 렌더러는 건너뛰기
            bool alreadyProcessed = false;
            foreach (var r in childRenderers)
            {
                if (r == s)
                {
                    alreadyProcessed = true;
                    break;
                }
            }
            
            if (!alreadyProcessed)
            {
                s.enabled = visible;
                rendererCount++;
            }
        }
        
        // 5. 부모 GameObject 체크
        if (targetObject.transform.parent != null)
        {
            var parentRenderer = targetObject.transform.parent.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                parentRenderer.enabled = visible;
                rendererCount++;
            }
            
            var parentSprite = targetObject.transform.parent.GetComponent<SpriteRenderer>();
            if (parentSprite != null && parentSprite != parentRenderer)
            {
                parentSprite.enabled = visible;
                rendererCount++;
            }
        }
        
        // 렌더러가 하나도 없으면 경고
        if (rendererCount == 0)
        {
            Debug.LogWarning($"[시야 경고] EnemyHive {hive.name}에 렌더러를 찾을 수 없습니다!");
        }
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] EnemyHive 총 {rendererCount}개 렌더러 처리 완료");
    }

    /// <summary>
    /// 현재 플레이어 시야 범위 계산
    /// </summary>
    void CalculatePlayerVision()
    {
        currentVisibleTiles.Clear();

        if (TileManager.Instance == null) return;

        // 모든 플레이어 유닛의 시야 합산
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;

            // 플레이어 유닛만
            if (unit.faction != Faction.Player) continue;

            // 유닛의 시야 범위 타일 추가
            var visibleTiles = GetTilesInRange(unit.q, unit.r, unit.visionRange);
            foreach (var tile in visibleTiles)
            {
                currentVisibleTiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// 범위 내 타일 좌표 가져오기
    /// </summary>
    List<Vector2Int> GetTilesInRange(int q, int r, int radius)
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

    /// <summary>
    /// 유닛 시각화 설정
    /// </summary>
    void SetUnitVisibility(UnitAgent unit, bool visible)
    {
        if (unit == null) return;

        // 하이브인 경우 Hive 컴포넌트의 GameObject에서 렌더러 찾기
        var hive = unit.GetComponent<Hive>();
        GameObject targetObject = unit.gameObject;
        
        if (hive != null)
        {
            targetObject = hive.gameObject;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] 하이브 렌더러 찾기: {targetObject.name}, visible={visible}");
        }

        int rendererCount = 0;

        // 1. 자신의 SpriteRenderer
        var sprite = targetObject.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.enabled = visible;
            rendererCount++;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] SpriteRenderer 설정: {sprite.name}, enabled={visible}");
        }

        // 2. 자신의 Renderer
        var renderer = targetObject.GetComponent<Renderer>();
        if (renderer != null && renderer != sprite) // SpriteRenderer와 중복 방지
        {
            renderer.enabled = visible;
            rendererCount++;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] Renderer 설정: {renderer.name}, enabled={visible}");
        }

        // 3. 자식 GameObject들의 모든 Renderer (비활성화된 GameObject 포함)
        var childRenderers = targetObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in childRenderers)
        {
            r.enabled = visible;
            rendererCount++;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] 자식 Renderer 설정: {r.name}, enabled={visible}");
        }

        // 4. 자식 GameObject들의 모든 SpriteRenderer (비활성화된 GameObject 포함)
        var childSprites = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in childSprites)
        {
            // 이미 처리된 렌더러는 건너뛰기
            bool alreadyProcessed = false;
            foreach (var r in childRenderers)
            {
                if (r == s)
                {
                    alreadyProcessed = true;
                    break;
                }
            }
            
            if (!alreadyProcessed)
            {
                s.enabled = visible;
                rendererCount++;
                
                if (showDebugLogs)
                    Debug.Log($"[시야 디버그] 자식 SpriteRenderer 설정: {s.name}, enabled={visible}");
            }
        }
        
        // 5. 부모 GameObject 체크 (하이브가 자식일 경우)
        if (hive != null && targetObject.transform.parent != null)
        {
            var parentRenderer = targetObject.transform.parent.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                parentRenderer.enabled = visible;
                rendererCount++;
                
                if (showDebugLogs)
                    Debug.Log($"[시야 디버그] 부모 Renderer 설정: {parentRenderer.name}, enabled={visible}");
            }
            
            var parentSprite = targetObject.transform.parent.GetComponent<SpriteRenderer>();
            if (parentSprite != null && parentSprite != parentRenderer)
            {
                parentSprite.enabled = visible;
                rendererCount++;
                
                if (showDebugLogs)
                    Debug.Log($"[시야 디버그] 부모 SpriteRenderer 설정: {parentSprite.name}, enabled={visible}");
            }
        }
        
        // 6. 컬라이더 활성화/비활성화 (클릭 가능/불가능) ?
        var collider2D = targetObject.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.enabled = visible;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] Collider2D 설정: {collider2D.name}, enabled={visible}");
        }
        
        var collider3D = targetObject.GetComponent<Collider>();
        if (collider3D != null)
        {
            collider3D.enabled = visible;
            
            if (showDebugLogs)
                Debug.Log($"[시야 디버그] Collider3D 설정: {collider3D.name}, enabled={visible}");
        }
        
        // 렌더러가 하나도 없으면 경고
        if (rendererCount == 0 && hive != null)
        {
            Debug.LogWarning($"[시야 경고] 하이브 {unit.name}에 렌더러를 찾을 수 없습니다!");
        }
        
        if (showDebugLogs && hive != null)
            Debug.Log($"[시야 디버그] 총 {rendererCount}개 렌더러 처리 완료");
    }

    /// <summary>
    /// 특정 위치가 플레이어 시야 내인지 확인 (외부 호출용)
    /// </summary>
    public bool IsPositionVisible(int q, int r)
    {
        return currentVisibleTiles.Contains(new Vector2Int(q, r));
    }

    /// <summary>
    /// 현재 시야 타일 개수 (디버그용)
    /// </summary>
    public int GetVisibleTileCount()
    {
        return currentVisibleTiles.Count;
    }

    /// <summary>
    /// 발견된 하이브 개수 (디버그용)
    /// </summary>
    public int GetDiscoveredHiveCount()
    {
        return discoveredHives.Count;
    }

    /// <summary>
    /// 하이브 발견 기록 초기화 (게임 재시작용)
    /// </summary>
    public void ResetDiscoveredHives()
    {
        discoveredHives.Clear();
    }

    /// <summary>
    /// 즉시 가시성 업데이트 (Enemy 생성 시 등)
    /// </summary>
    public void ForceUpdateVisibility()
    {
        UpdateEnemyVisibility();
    }
}
