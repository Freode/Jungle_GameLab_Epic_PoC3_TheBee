using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 말벌집 전용 클래스
/// - 플레이어 하이브와 분리된 독립적인 클래스
/// - 말벌 생성 및 관리
/// - 시야 시스템 최적화
/// - 단순화된 AI 연동
/// </summary>
public class EnemyHive : MonoBehaviour
{
    [Header("위치")]
    public int q;
    public int r;

    [Header("생성 설정")]
    public GameObject waspPrefab; // 말벌 프리팹
    public float spawnInterval = 8f; // 생성 간격 (초)
    public int maxWasps = 15; // 최대 말벌 수

    [Header("전투 설정")]
    public int visionRange = 5; // 말벌집 시야 범위
    public int activityRange = 5; // 말벌 활동 범위

    [Header("디버그")]
    public bool showDebugLogs = false;

    private List<UnitAgent> wasps = new List<UnitAgent>(); // 생성된 말벌 목록
    private Coroutine spawnRoutine;
    private UnitAgent hiveAgent; // 하이브 자신의 UnitAgent
    private CombatUnit combat; // 하이브 전투 유닛

    void Awake()
    {
        hiveAgent = GetComponent<UnitAgent>();
        combat = GetComponent<CombatUnit>();
        
        if (hiveAgent == null)
        {
            hiveAgent = gameObject.AddComponent<UnitAgent>();
        }
        
        if (combat == null)
        {
            combat = gameObject.AddComponent<CombatUnit>();
        }
    }

    void OnEnable()
    {
        // TileManager에 위치 등록
        if (TileManager.Instance != null && hiveAgent != null)
        {
            hiveAgent.SetPosition(q, r);
        }

        // WaspWaveManager에 등록
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.RegisterEnemyHive(this);
        }

        // 생성 루틴 시작
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    void OnDisable()
    {
        // 생성 루틴 중지
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        // WaspWaveManager에서 등록 해제
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.UnregisterEnemyHive(this);
        }
    }

    /// <summary>
    /// 말벌집 초기화
    /// </summary>
    public void Initialize(int q, int r, int maxHealth = 250)
    {
        this.q = q;
        this.r = r;

        // 타일 찾기 ?
        if (TileManager.Instance != null)
        {
            var tile = TileManager.Instance.GetTile(q, r);
            if (tile != null)
            {
                // 타일의 자식으로 설정 ?
                transform.SetParent(tile.transform);
                
                // 타일에 등록 ?
                tile.enemyHive = this;
                
                // 타일 중심 위치 (로컬 좌표) ?
                transform.localPosition = Vector3.zero;
                
                if (showDebugLogs)
                    Debug.Log($"[적 하이브] 타일에 부착: ({q}, {r})");
            }
            else
            {
                Debug.LogError($"[적 하이브] 타일을 찾을 수 없습니다: ({q}, {r})");
                
                // 타일이 없으면 월드 좌표로 설정
                Vector3 worldPos = TileHelper.HexToWorld(q, r, 0.5f);
                transform.position = worldPos;
            }
        }

        // UnitAgent 설정
        if (hiveAgent != null)
        {
            hiveAgent.SetPosition(q, r);
            hiveAgent.faction = Faction.Enemy;
            hiveAgent.visionRange = visionRange;
            hiveAgent.canMove = false;
            hiveAgent.isQueen = false;
        }

        // CombatUnit 설정
        if (combat != null)
        {
            combat.maxHealth = maxHealth;
            combat.health = maxHealth;
            combat.attack = 0; // 하이브는 공격 안 함
        }

        // 초기에는 모든 렌더러 비활성화 (EnemyVisibilityController가 제어)
        SetAllRenderersEnabled(false);

        // EnemyVisibilityController에 즉시 업데이트 요청
        if (EnemyVisibilityController.Instance != null)
        {
            StartCoroutine(InitializeVisibilityCheck());
        }

        if (showDebugLogs)
            Debug.Log($"[적 하이브] 초기화 완료: ({q}, {r}), HP: {combat.health}/{combat.maxHealth}");
    }

    /// <summary>
    /// 초기 가시성 체크 (한 프레임 대기 후) ?
    /// </summary>
    IEnumerator InitializeVisibilityCheck()
    {
        // 완전 초기화 대기
        yield return null;
        
        // 가시성 즉시 업데이트
        if (EnemyVisibilityController.Instance != null)
        {
            EnemyVisibilityController.Instance.ForceUpdateVisibility();
            
            if (showDebugLogs)
                Debug.Log($"[적 하이브] 초기 가시성 체크 완료: ({q}, {r})");
        }
    }

    /// <summary>
    /// 모든 렌더러 활성화/비활성화 ?
    /// </summary>
    void SetAllRenderersEnabled(bool enabled)
    {
        // 자신의 레너더러
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.enabled = enabled;
        
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = enabled;
        
        // 자식 레너더러
        var childRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in childRenderers)
        {
            r.enabled = enabled;
        }
        
        var childSprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in childSprites)
        {
            s.enabled = enabled;
        }
        
        // 부모 레너더러
        if (transform.parent != null)
        {
            var parentRenderer = transform.parent.GetComponent<Renderer>();
            if (parentRenderer != null) parentRenderer.enabled = enabled;
            
            var parentSprite = transform.parent.GetComponent<SpriteRenderer>();
            if (parentSprite != null) parentSprite.enabled = enabled;
        }
        
        if (showDebugLogs)
            Debug.Log($"[적 하이브] 모든 렌더러 {(enabled ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 말벌 생성 루프
    /// </summary>
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // null 제거
            wasps.RemoveAll(w => w == null);

            // 최대 수 체크
            if (wasps.Count < maxWasps)
            {
                SpawnWasp();
            }
            else if (showDebugLogs)
            {
                Debug.Log($"[적 하이브] 최대 말벌 수 도달: {wasps.Count}/{maxWasps}");
            }
        }
    }

    /// <summary>
    /// 말벌 생성
    /// </summary>
    void SpawnWasp()
    {
        if (waspPrefab == null)
        {
            Debug.LogError("[적 하이브] 말벌 프리팹이 없습니다!");
            return;
        }

        // 안전 체크
        if (wasps.Count >= maxWasps)
        {
            Debug.LogWarning($"[적 하이브] 이미 최대 말벌 수: {wasps.Count}/{maxWasps}");
            return;
        }

        // 타일 내부 랜덤 위치
        Vector3 spawnPos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);

        if (showDebugLogs)
            Debug.Log($"[적 하이브] 말벌 생성 위치: ({q}, {r}) → World: {spawnPos}");

        // 말벌 생성
        GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
        var waspAgent = waspObj.GetComponent<UnitAgent>();
        
        if (waspAgent == null)
        {
            waspAgent = waspObj.AddComponent<UnitAgent>();
        }

        // UnitAgent 설정
        waspAgent.hexSize = 0.5f; // hexSize 명시적 설정
        waspAgent.SetPosition(q, r); // 타일 좌표 설정
        waspAgent.faction = Faction.Enemy;
        waspAgent.homeHive = null; // EnemyHive는 Hive가 아니므로 null
        waspAgent.canMove = true;
        waspAgent.isQueen = false;

        // 월드 위치 확인 및 보정
        Vector3 expectedWorldPos = TileHelper.HexToWorld(q, r, 0.5f);
        if (Vector3.Distance(waspObj.transform.position, expectedWorldPos) > 1f)
        {
            Debug.LogWarning($"[적 하이브] 말벌 위치 보정: {waspObj.transform.position} → {expectedWorldPos}");
            waspObj.transform.position = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);
        }

        // 말벌 리스트에 추가
        wasps.Add(waspAgent);

        // EnemyAI 설정
        var enemyAI = waspObj.GetComponent<EnemyAI>();
        if (enemyAI == null)
        {
            enemyAI = waspObj.AddComponent<EnemyAI>();
        }

        // AI 설정 (하이브 정보 전달)
        enemyAI.visionRange = 3; // 말벌 시야 범위
        enemyAI.activityRange = activityRange; // 하이브 활동 범위
        enemyAI.attackRange = 0; // 근접 전투
        enemyAI.showDebugLogs = showDebugLogs; // 디버그 로그 동기화

        // CombatUnit 설정 (WaspWaveManager에서 설정된 값 사용)
        var waspCombat = waspObj.GetComponent<CombatUnit>();
        if (waspCombat != null)
        {
            // WaspWaveManager의 값 사용 (이미 설정되어 있음)
            if (showDebugLogs)
                Debug.Log($"[적 하이브] 말벌 생성: HP={waspCombat.health}, ATK={waspCombat.attack}");
        }

        // 시야 시스템에 즉시 숨김
        if (EnemyVisibilityController.Instance != null)
        {
            StartCoroutine(HideWaspOnSpawn(waspAgent));
        }

        if (showDebugLogs)
            Debug.Log($"[적 하이브] 말벌 생성 완료: {wasps.Count}/{maxWasps} at ({q}, {r})");
    }

    /// <summary>
    /// 말벌 생성 즉시 가시성 체크하여 숨김
    /// </summary>
    IEnumerator HideWaspOnSpawn(UnitAgent wasp)
    {
        // 한 프레임 대기 (완전 초기화)
        yield return null;

        // 즉시 가시성 업데이트
        if (EnemyVisibilityController.Instance != null)
        {
            EnemyVisibilityController.Instance.ForceUpdateVisibility();
        }
    }

    /// <summary>
    /// 말벌집 파괴
    /// </summary>
    public void DestroyHive()
    {
        if (showDebugLogs)
            Debug.Log($"[적 하이브] 파괴: ({q}, {r})");

        // ✅ 3. 타일을 자원 타일로 변환
        if (TileManager.Instance != null)
        {
            var tile = TileManager.Instance.GetTile(q, r);
            if (tile != null)
            {
                // 자원 타일로 설정
                if (GameManager.Instance != null && GameManager.Instance.terrainTypes != null)
                {
                    // 자원이 있는 지형 타입 찾기 (resourceYield > 0인 첫 번째 지형)
                    TerrainType resourceTerrain = null;
                    foreach (var terrain in GameManager.Instance.terrainTypes)
                    {
                        if (terrain != null && terrain.resourceYield > 0)
                        {
                            resourceTerrain = terrain;
                            break;
                        }
                    }

                    // 자원 지형이 있으면 적용
                    if (resourceTerrain != null)
                    {
                        tile.SetTerrain(resourceTerrain);
                        tile.SetResourceAmount(500); // ✅ 자원량 500으로 설정
                        
                        if (showDebugLogs)
                            Debug.Log($"[적 하이브] 타일을 자원 타일로 변환: ({q}, {r}), 자원량: 500");
                    }
                    else
                    {
                        Debug.LogWarning($"[적 하이브] 자원 지형 타입을 찾을 수 없습니다.");
                    }
                }

                // 타일의 enemyHive 참조 제거
                tile.enemyHive = null;
            }
        }

        // 모든 말벌에게 하이브 파괴 알림
        foreach (var wasp in wasps)
        {
            if (wasp != null)
            {
                // 말벌의 homeHive 참조 제거
                // (EnemyAI가 homeHive를 참조하므로)
                var enemyAI = wasp.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    // AI가 하이브 없이 작동하도록 설정
                    // (이미 homeHive == null이면 제한 없이 작동)
                }
            }
        }

        // WaspWaveManager에서 등록 해제
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.UnregisterEnemyHive(this);
        }

        // GameObject 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// 생성된 말벌 리스트 가져오기
    /// </summary>
    public List<UnitAgent> GetWasps()
    {
        // null 제거
        wasps.RemoveAll(w => w == null);
        return new List<UnitAgent>(wasps);
    }

    /// <summary>
    /// 현재 말벌 수 가져오기
    /// </summary>
    public int GetWaspCount()
    {
        wasps.RemoveAll(w => w == null);
        return wasps.Count;
    }

    /// <summary>
    /// 하이브 위치 가져오기 (EnemyAI에서 사용)
    /// </summary>
    public Vector2Int GetPosition()
    {
        return new Vector2Int(q, r);
    }

    // Gizmos로 하이브 정보 표시
    void OnDrawGizmosSelected()
    {
        // 하이브 위치
        Vector3 pos = TileHelper.HexToWorld(q, r, 0.5f);
        
        // 시야 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, visionRange * 0.5f);

        // 활동 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, activityRange * 0.5f);

        // 말벌 수 표시 (에디터에서만) ?
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"Wasps: {GetWaspCount()}/{maxWasps}");
#endif
    }
}
