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
    public GameObject bossWaspPrefab; // ✅ 장수말벌 전용 프리팹
    public int bossWaveStartAfterHivesDestroyed = 3; // ✅ 장수말벌 웨이브 시작 조건 (파괴된 하이브 수)
    
    private List<EnemyHive> enemyHives = new List<EnemyHive>();
    private int destroyedHivesCount = 0; // 파괴된 말벌집 수
    private bool bossWaveStarted = false; // ✅ 장수말벌 웨이브 시작 여부
    private bool normalWaveStarted = false; // ✅ 일반 말벌 웨이브 시작 여부
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void OnEnable()
    {
        // ✅ 하이브 파괴 이벤트 구독
        EnemyHive.OnHiveDestroyed += OnEnemyHiveDestroyed;
    }
    
    void OnDisable()
    {
        // ✅ 하이브 파괴 이벤트 구독 해제
        EnemyHive.OnHiveDestroyed -= OnEnemyHiveDestroyed;
    }
    
    /// <summary>
    /// 적 하이브 파괴 이벤트 핸들러 ✅
    /// </summary>
    void OnEnemyHiveDestroyed(EnemyHive hive)
    {
        if (enemyHives.Contains(hive))
        {
            enemyHives.Remove(hive);
        }
        
        destroyedHivesCount++;
        Debug.Log($"[웨이브] 적 하이브 파괴! 총 파괴: {destroyedHivesCount}, 남은 하이브: {enemyHives.Count}");
        
        // ✅ 장수말벌 웨이브 시작 조건 체크
        if (!bossWaveStarted && destroyedHivesCount >= bossWaveStartAfterHivesDestroyed)
        {
            bossWaveStarted = true;
            Debug.Log($"[웨이브] 말벌집 {destroyedHivesCount}개 파괴! 장수말벌 웨이브 시작!");
            StartBossWaveRoutine();
        }
    }

    void Start()
    {
        // ✅ 일반 말벌 웨이브는 첫 하이브 건설 시 시작 (자동 시작 제거)
        // ✅ 장수말벌 웨이브는 말벌집 파괴 시 시작 (자동 시작 제거)
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
    /// 적 하이브에서 가장 가까운 플레이어 하이브 찾기 ?
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
    /// 플레이어 하이브에서 가장 가까운 적 하이브 찾기 (일반 말벌집만)
    /// </summary>
    public EnemyHive FindNearestEnemyHive(Hive playerHive)
    {
        if (playerHive == null || enemyHives.Count == 0) return null;

        // null 제거
        enemyHives.RemoveAll(h => h == null);

        EnemyHive nearest = null;
        int minDist = int.MaxValue;

        foreach (var enemyHive in enemyHives)
        {
            if (enemyHive == null) continue;
            
            // ✅ 장수말벌 하이브는 제외 (장수말벌 웨이브 전용)
            if (enemyHive.isBossHive) continue;

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
    /// 장수말벌 하이브 찾기 (isBossHive 플래그 사용)
    /// </summary>
    EnemyHive FindBossHive()
    {
        foreach (var hive in enemyHives)
        {
            if (hive == null) continue;
            
            // ✅ isBossHive 플래그로 직접 확인
            if (hive.isBossHive)
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
        int waspCount = baseWaspCount + destroyedHivesCount * 3 / 2;
        
        Debug.Log($"[웨이브] 일반 말벌 {waspCount}마리 공격 시작!");
        
        // 말벌 생성 및 공격 명령
        for (int i = 0; i < waspCount; i++)
        {
            SpawnAndAttackWasp(nearestEnemyHive, playerHive, false);
        }
    }

    /// <summary>
    /// 장수말벌 웨이브 발동
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
        int bossCount = bossWaveNumber; // 2, 4, 6, 8...

        Debug.Log($"[웨이브] 장수말벌 웨이브 {bossWaveNumber}: {bossCount}마리 공격!");

        for (int i = 0; i < bossCount; i++)
        {
            SpawnAndAttackWasp(bossHive, playerHive, true);
        }
    }

    /// <summary>
    /// 말벌 생성 및 공격 명령
    /// </summary>
    void SpawnAndAttackWasp(EnemyHive fromHive, Hive targetHive, bool isBoss)
    {
        if (fromHive == null || targetHive == null) return;

        // ✅ 장수말벌이면 bossWaspPrefab, 일반 말벌이면 fromHive.waspPrefab 사용
        GameObject waspPrefab = isBoss ? bossWaspPrefab : fromHive.waspPrefab;
        
        if (waspPrefab == null)
        {
            Debug.LogWarning($"[웨이브] 말벌 프리팹이 없습니다. (isBoss: {isBoss})");
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
            agent.homeHive = null;

            // ✅ 장수말벌은 프리팹에 이미 설정된 스탯 사용 (추가 강화 불필요)
            // 일반 말벌도 프리팹 기본 스탯 사용

            // ✅ 기존 EnemyAI 제거
            var oldAI = waspObj.GetComponent<EnemyAI>();
            if (oldAI != null)
            {
                Destroy(oldAI);
            }

            // ✅ WaveWaspAI 추가
            var waveAI = waspObj.AddComponent<WaveWaspAI>();
            waveAI.visionRange = 25;
            waveAI.scanInterval = 0.5f;
            waveAI.showDebugLogs = false; // ✅ 디버그 로그 비활성화
            
            Debug.Log($"[웨이브] {(isBoss ? "장수말벌" : "일반 말벌")} 생성 완료. WaveWaspAI 적용.");
        }
    }

    /// <summary>
    /// 장수말벌 웨이브 시작 (말벌집 파괴 시 호출)
    /// </summary>
    public void StartBossWaveRoutine()
    {
        StartCoroutine(BossWaveRoutine());
    }
    
    /// <summary>
    /// 일반 말벌 웨이브 시작 (첫 하이브 건설 시 호출)
    /// </summary>
    public void StartNormalWaveRoutine()
    {
        if (normalWaveStarted)
        {
            Debug.Log("[웨이브] 일반 말벌 웨이브가 이미 시작되었습니다.");
            return;
        }
        
        normalWaveStarted = true;
        Debug.Log("[웨이브] 첫 하이브 건설! 일반 말벌 웨이브 시작!");
        StartCoroutine(WaveAttackRoutine());
    }

    /// <summary>
    /// 장수말벌 웨이브 루틴
    /// </summary>
    IEnumerator BossWaveRoutine()
    {
        yield return new WaitForSeconds(30f);

        while (true)
        {
            LaunchBossWaspWave();
            yield return new WaitForSeconds(bossWaveInterval);
        }
    }
}
