using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 말벌집에서 꿀벌집을 향한 주기적 웨이브 공격 관리
/// - EnemyHive를 사용하여 플레이어 Hive와 완전 분리
/// - 가장 가까운 말벌집에서 80초마다 1마리 공격
/// - 말벌집 파괴 시 다음 가까운 곳에서 누적 공격 (1마리 추가)
/// </summary>
public class WaspWaveManager : MonoBehaviour
{
    public static WaspWaveManager Instance { get; private set; }

    [Header("일반 말벌 웨이브 설정")]
    public float normalWaveInterval = 80f; // 80초마다
    public int baseWaspCount = 1; // 기본 1마리
    
    [Header("장수말벌 웨이브 설정")]
    public float bossWaveInterval = 180f; // 180초마다
    public int bossWaveNumber = 0; // 현재 웨이브 번호
    
    private List<EnemyHive> enemyHives = new List<EnemyHive>(); // Hive → EnemyHive ?
    private int destroyedHivesCount = 0; // 파괴된 말벌집 수
    
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
        StartCoroutine(WaveAttackRoutine());
    }

    /// <summary>
    /// 적 하이브 등록 (EnemyHive 타입) ?
    /// </summary>
    public void RegisterEnemyHive(EnemyHive hive)
    {
        if (hive == null || enemyHives.Contains(hive)) return;
        
        enemyHives.Add(hive);
        Debug.Log($"[웨이브] 적 하이브 등록: {hive.name} at ({hive.q}, {hive.r})");
    }

    /// <summary>
    /// 적 하이브 제거 (파괴 시) ?
    /// </summary>
    public void UnregisterEnemyHive(EnemyHive hive)
    {
        if (enemyHives.Contains(hive))
        {
            enemyHives.Remove(hive);
            destroyedHivesCount++;
            Debug.Log($"[웨이브] 적 하이브 파괴! 총 파괴: {destroyedHivesCount}");
        }
    }

    /// <summary>
    /// 웨이브 공격 루틴
    /// </summary>
    IEnumerator WaveAttackRoutine()
    {
        // 게임 시작 후 첫 웨이브까지 대기
        yield return new WaitForSeconds(normalWaveInterval);

        while (true)
        {
            // 일반 말벌 웨이브
            LaunchNormalWaspWave();
            
            yield return new WaitForSeconds(normalWaveInterval);
        }
    }

    /// <summary>
    /// 일반 말벌 웨이브 발동
    /// </summary>
    void LaunchNormalWaspWave()
    {
        // 플레이어 하이브 찾기
        Hive playerHive = FindNearestPlayerHive();
        if (playerHive == null)
        {
            Debug.Log("[웨이브] 플레이어 하이브가 없습니다.");
            return;
        }

        // 가장 가까운 적 하이브 찾기
        EnemyHive nearestEnemyHive = FindNearestEnemyHive(playerHive);
        if (nearestEnemyHive == null)
        {
            Debug.Log("[웨이브] 적 하이브가 없습니다.");
            return;
        }

        // 공격할 말벌 수 = 기본 1마리 + 파괴된 하이브 수
        int waspCount = baseWaspCount + destroyedHivesCount;
        
        Debug.Log($"[웨이브] 일반 말벌 {waspCount}마리 공격 시작!");
        
        // 말벌 생성 및 공격 명령
        for (int i = 0; i < waspCount; i++)
        {
            SpawnAndAttackWasp(nearestEnemyHive, playerHive, false);
        }
    }

    /// <summary>
    /// 장수말벌 웨이브 발동 (외부에서 호출)
    /// </summary>
    public void LaunchBossWaspWave()
    {
        Hive playerHive = FindNearestPlayerHive();
        if (playerHive == null) return;

        // 장수말벌 하이브 찾기 (특정 태그나 속성으로 구분)
        EnemyHive bossHive = FindBossHive();
        if (bossHive == null)
        {
            Debug.Log("[웨이브] 장수말벌 하이브가 없습니다.");
            return;
        }

        bossWaveNumber++;
        int bossCount = bossWaveNumber * 2; // 2, 4, 6, 8...

        Debug.Log($"[웨이브] 장수말벌 웨이브 {bossWaveNumber}: {bossCount}마리 공격!");

        for (int i = 0; i < bossCount; i++)
        {
            SpawnAndAttackWasp(bossHive, playerHive, true);
        }
    }

    /// <summary>
    /// 말벌 생성 및 공격 명령 ?
    /// </summary>
    void SpawnAndAttackWasp(EnemyHive fromHive, Hive targetHive, bool isBoss)
    {
        if (fromHive == null || targetHive == null) return;

        // 말벌 생성
        GameObject waspPrefab = fromHive.waspPrefab; // workerPrefab → waspPrefab ?
        if (waspPrefab == null)
        {
            Debug.LogWarning("[웨이브] 말벌 프리팹이 없습니다.");
            return;
        }

        // 하이브 위치에서 랜덤 생성
        Vector3 spawnPos = TileHelper.GetRandomPositionInTile(fromHive.q, fromHive.r, 0.5f, 0.15f);
        GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
        
        var agent = waspObj.GetComponent<UnitAgent>();
        if (agent != null)
        {
            agent.SetPosition(fromHive.q, fromHive.r);
            agent.faction = Faction.Enemy;
            agent.canMove = true;
            agent.homeHive = null; // EnemyHive는 Hive가 아니므로 null ?

            // 장수말벌 강화 (보스)
            if (isBoss)
            {
                var combat = waspObj.GetComponent<CombatUnit>();
                if (combat != null)
                {
                    combat.maxHealth *= 2; // 체력 2배
                    combat.health = combat.maxHealth;
                    combat.attack = Mathf.RoundToInt(combat.attack * 1.5f); // 공격력 1.5배
                }
                
                // 크기 증가 (시각적 구분)
                waspObj.transform.localScale *= 1.3f;
            }

            // AI 설정
            var enemyAI = waspObj.GetComponent<EnemyAI>();
            if (enemyAI == null)
            {
                enemyAI = waspObj.AddComponent<EnemyAI>();
            }

            enemyAI.visionRange = isBoss ? 5 : 3; // 시야 범위
            enemyAI.activityRange = isBoss ? 10 : fromHive.activityRange; // 활동 범위 ?
            enemyAI.attackRange = 0; // 근접 전투
            enemyAI.showDebugLogs = false;

            // 타겟 하이브로 이동 명령
            StartCoroutine(MoveToTargetHive(agent, targetHive));
        }

        // 말벌 생성 즉시 숨김
        if (EnemyVisibilityController.Instance != null)
        {
            StartCoroutine(HideWaspOnSpawn(agent));
        }
    }

    /// <summary>
    /// 말벌을 타겟 하이브로 이동
    /// </summary>
    IEnumerator MoveToTargetHive(UnitAgent wasp, Hive target)
    {
        yield return null; // 초기화 대기

        if (wasp == null || target == null) yield break;

        var controller = wasp.GetComponent<UnitController>();
        if (controller == null)
        {
            controller = wasp.gameObject.AddComponent<UnitController>();
        }

        var startTile = TileManager.Instance?.GetTile(wasp.q, wasp.r);
        var targetTile = TileManager.Instance?.GetTile(target.q, target.r);

        if (startTile != null && targetTile != null)
        {
            var path = Pathfinder.FindPath(startTile, targetTile);
            if (path != null && path.Count > 0)
            {
                controller.agent = wasp;
                controller.SetPath(path);
                Debug.Log($"[웨이브] 말벌이 플레이어 하이브로 이동 시작!");
            }
        }
    }

    /// <summary>
    /// 말벌 생성 즉시 숨김
    /// </summary>
    IEnumerator HideWaspOnSpawn(UnitAgent agent)
    {
        yield return null;
        
        if (EnemyVisibilityController.Instance != null)
        {
            EnemyVisibilityController.Instance.ForceUpdateVisibility();
        }
    }

    /// <summary>
    /// 장수말벌 웨이브 시작 (외부 호출용)
    /// </summary>
    public void StartBossWaveRoutine()
    {
        StartCoroutine(BossWaveRoutine());
    }

    /// <summary>
    /// 장수말벌 웨이브 루틴
    /// </summary>
    IEnumerator BossWaveRoutine()
    {
        yield return new WaitForSeconds(bossWaveInterval);

        while (true)
        {
            LaunchBossWaspWave();
            yield return new WaitForSeconds(bossWaveInterval);
        }
    }

    /// <summary>
    /// 가장 가까운 플레이어 하이브 찾기
    /// </summary>
    Hive FindNearestPlayerHive()
    {
        if (HiveManager.Instance == null) return null;

        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive == null) continue;
            
            var agent = hive.GetComponent<UnitAgent>();
            if (agent != null && agent.faction == Faction.Player)
            {
                // 첫 번째 플레이어 하이브 반환
                return hive;
            }
        }

        return null;
    }

    /// <summary>
    /// 플레이어 하이브에서 가장 가까운 적 하이브 찾기 ?
    /// </summary>
    EnemyHive FindNearestEnemyHive(Hive playerHive)
    {
        if (playerHive == null || enemyHives.Count == 0) return null;

        // null 제거
        enemyHives.RemoveAll(h => h == null);

        EnemyHive nearest = null;
        int minDist = int.MaxValue;

        foreach (var enemyHive in enemyHives)
        {
            if (enemyHive == null) continue;

            int dist = Pathfinder.AxialDistance(playerHive.q, playerHive.r, enemyHive.q, enemyHive.r);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemyHive;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 장수말벌 하이브 찾기 (태그나 이름으로 구분) ?
    /// </summary>
    EnemyHive FindBossHive()
    {
        foreach (var hive in enemyHives)
        {
            if (hive == null) continue;
            
            // 이름에 "Boss" 포함 또는 특정 태그
            if (hive.name.Contains("Boss") || hive.name.Contains("장수"))
            {
                return hive;
            }
        }

        return null;
    }

    /// <summary>
    /// 현재 등록된 적 하이브 수 (디버그용)
    /// </summary>
    public int GetEnemyHiveCount()
    {
        enemyHives.RemoveAll(h => h == null);
        return enemyHives.Count;
    }
}
