using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : MonoBehaviour, IUnitCommandProvider
{
    public int q;
    public int r;

    public GameObject workerPrefab;
    public float spawnInterval = 10f; // seconds
    public int maxWorkers = 10; // 플레이어 하이브 기본값 ?

    // Deprecated: Use HiveManager.playerStoredResources instead
    // public int storedResources = 0;

    // Relocation state
    public bool isRelocating = false;
    public UnitAgent queenBee; // 여왕벌 참조 (이미 씬에 존재)
    
    private List<UnitAgent> workers = new List<UnitAgent>();
    private Coroutine spawnRoutine;
    private Coroutine relocateRoutine;

    // Commands for hive
    [Header("Hive Commands")]
    public SOCommand[] hiveCommands; // All hive commands (including relocate)
    
    [Header("Debug")]
    public bool showDebugLogs = false; // 디버그 로그 표시
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI relocateTimerText; // 이사 준비 타이머 텍스트?

    void OnEnable()
    {
        if (TileManager.Instance != null)
        {
            var agent = GetComponent<UnitAgent>();
            if (agent != null)
            {
                agent.SetPosition(q, r);
            }
        }

        // Register with HiveManager
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.RegisterHive(this);
        }

        // ? 플레이어 하이브는 WaspWaveManager에 등록하지 않음
        // (EnemyHive만 등록)
        
        // UI 텍스트 초기화
        if (relocateTimerText != null)
        {
            relocateTimerText.gameObject.SetActive(false);
        }
        
        // start spawning
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (relocateRoutine != null) StopCoroutine(relocateRoutine);
        
        // Unregister from HiveManager
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.UnregisterHive(this);
        }

        // ? 플레이어 하이브는 WaspWaveManager에 등록 해제 불필요
    }

    public void Initialize(int q, int r)
    {
        this.q = q; this.r = r;
        
        // Note: queenBee should be assigned externally when constructing the hive
        // The queen already exists in the scene
        
        // ✅ 하이브 건설 시 페르몬 코루틴 취소 (요구사항)
        QueenPheromoneCommandHandler.CancelCurrentPheromoneCommand();
        Debug.Log("[하이브 초기화] 페르몬 명령 취소");
        
        // ✅ 하이브 건설 시 모든 페르몬 효과 제거 (요구사항 1)
        if (PheromoneManager.Instance != null)
        {
            PheromoneManager.Instance.ClearAllPheromones();
            Debug.Log("[하이브 초기화] 모든 페르몬 효과 제거");
        }
        
        // ✅ 여왕벌 비활성화 주석 처리 (요구사항 2)
        /*
        // 여왕벌 비활성화 (하이브 안으로 들어감)
        if (queenBee != null)
        {
            // 여왕벌을 하이브 위치로 이동
            queenBee.SetPosition(q, r);
            Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
            queenBee.transform.position = hivePos;
            
            //// 이동 불가능하게
            //queenBee.canMove = false;
            
            // 여왕벌 무적 상태 활성화
            var queenCombat = queenBee.GetComponent<CombatUnit>();
            if (queenCombat != null)
            {
                queenCombat.SetInvincible(true);
            }
            
            // GameObject 자체를 비활성화
            queenBee.gameObject.SetActive(false);
            
            if (showDebugLogs)
                Debug.Log("[하이브 초기화] 여왕벌 비활성화 (하이브 안으로 들어감) - 무적 상태 활성화");
        }
        */
        
        // Apply upgrades: hive health (먼저 설정)
        var combat = GetComponent<CombatUnit>();
        if (combat != null && HiveManager.Instance != null)
        {
            combat.maxHealth = HiveManager.Instance.GetHiveMaxHealth();
            combat.health = combat.maxHealth; // 최대 체력으로 설정            
            if (showDebugLogs)
                Debug.Log($"[하이브 초기화] 체력 설정: {combat.health}/{combat.maxHealth}");
        }
        
        // Apply upgrades: max workers
        if (HiveManager.Instance != null)
        {
            maxWorkers = HiveManager.Instance.GetMaxWorkers();
        }
        
        // start spawning
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());

        // show boundary using configured hive activity radius from HiveManager
        int radius = 5;
        if (HiveManager.Instance != null) radius = HiveManager.Instance.hiveActivityRadius;
        if (HexBoundaryHighlighter.Instance != null)
        {
            HexBoundaryHighlighter.Instance.ShowBoundary(this, radius);
        }

        // 기존 일꾼 수 카운트 (여왕벌 제외)
        int existingWorkerCount = 0;
        if (TileManager.Instance != null)
        {
            // 하이브 자신의 UnitAgent
            var hiveAgent = GetComponent<UnitAgent>();

            // ✅ GetAllUnits()를 ToList()로 복사하여 컬렉션 수정 문제 해결
            var allUnits = TileManager.Instance.GetAllUnits();

            foreach (var unit in allUnits)
            {
                if (unit != null && unit.faction == Faction.Player && !unit.isQueen)
                {
                    // 하이브 자신은 제외
                    if (hiveAgent != null && unit == hiveAgent)
                    {
                        continue;
                    }

                    // Hive 컴포넌트가 있는 유닛은 제외
                    var unitHive = unit.GetComponent<Hive>();
                    if (unitHive != null)
                    {
                        continue;
                    }

                    existingWorkerCount++;

                    // 기존 일꾼을 workers 리스트에 추가
                    if (!workers.Contains(unit))
                    {
                        workers.Add(unit);
                        unit.homeHive = this;

                        // HiveManager에 등록 ✅
                        if (HiveManager.Instance != null)
                        {
                            HiveManager.Instance.RegisterWorker(unit);
                        }

                        // 수동 명령 플래그 초기화
                        unit.hasManualOrder = false;
                        unit.isFollowingQueen = false;

                        // UnitBehaviorController 초기화
                        var behavior = unit.GetComponent<UnitBehaviorController>();
                        if (behavior != null)
                        {
                            behavior.CancelCurrentTask();
                        }

                        // Ensure RoleAssigner exists and apply default role (Gatherer)
                        var roleAssigner = unit.GetComponent<RoleAssigner>();
                        if (roleAssigner == null)
                        {
                            roleAssigner = unit.gameObject.AddComponent<RoleAssigner>();
                        }
                        // Note: Role will be assigned by HiveManager.RegisterWorker based on squad. Do not forcibly set RoleType here.
                    }
                }
            }
        }

        if (showDebugLogs)
            Debug.Log($"[하이브 초기화] 기존 일꾼 수: {existingWorkerCount}, 최대 일꾼 수: {maxWorkers}");
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            // 일꾼 리스트 정리 (null 제거)
            workers.RemoveAll(w => w == null);
            
            // 실시간으로 HiveManager에서 최대 일꾼 수 가져오기 ?
            if (HiveManager.Instance != null)
            {
                int currentMaxWorkers = HiveManager.Instance.GetMaxWorkers();
                
                // maxWorkers 업데이트 (업그레이드 반영) ?
//                 if (currentMaxWorkers != maxWorkers)
        //                 {
//                     if (showDebugLogs)
//                         Debug.Log($"[하이브] maxWorkers 업데이트: {maxWorkers} → {currentMaxWorkers}");
                    
//                     maxWorkers = currentMaxWorkers;
//                 }
            }
            
            // MaxWorkers 체크
            if (!isRelocating && workers.Count < maxWorkers)
            {
                SpawnWorker();
            }
            else if (workers.Count >= maxWorkers)
            {
                if (showDebugLogs)
                    Debug.Log($"[하이브] 최대 일꾼 수 도달: {workers.Count}/{maxWorkers}");
            }
        }
    }

    void SpawnWorker()
    {
        if (workerPrefab == null) return;

        // 추가 안전장치: 최대 일꾼 수 체크 ?
        if (workers.Count >= maxWorkers)
        {
            Debug.LogWarning($"[하이브] 이미 최대 일꾼 수에 도달했습니다: {workers.Count}/{maxWorkers}");
            return;
        }

        // 타일 내부 랜덤 위치로 생성
        Vector3 pos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);
        
        var go = Instantiate(workerPrefab, pos, Quaternion.identity);
        var agent = go.GetComponent<UnitAgent>();
        if (agent == null) agent = go.AddComponent<UnitAgent>();
        agent.SetPosition(q, r);
        // assign home hive so activity radius checks work
        agent.homeHive = this;
        agent.canMove = true;

        // 하이브의 faction 상속
        var hiveAgent = GetComponent<UnitAgent>();
        if (hiveAgent != null)
        {
            agent.faction = hiveAgent.faction;
        }
        else
        {
            agent.faction = Faction.Player; // 기본값
        }
        
        agent.isQueen = false;
        workers.Add(agent);
        
        Debug.Log($"[하이브 스폰] 일꾼 생성: {workers.Count}/{maxWorkers}");
        
        // HiveManager에 일꾼 등록 ✅
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.RegisterWorker(agent);
        }

        // Ensure RoleAssigner exists and apply default role (Gatherer) for player workers
        if (agent.faction == Faction.Player)
        {
            var roleAssigner = go.GetComponent<RoleAssigner>();
            if (roleAssigner == null)
            {
                roleAssigner = go.AddComponent<RoleAssigner>();
            }
            // Note: role is assigned by HiveManager.RegisterWorker (squad-based). Do not override here.
        }

        // Apply upgrades to newly spawned worker (Player만)
        if (agent.faction == Faction.Player && HiveManager.Instance != null)
        {
            // Ensure RoleAssigner exists and ask it to reapply role+global stats
            var roleAssigner = go.GetComponent<RoleAssigner>();
            if (roleAssigner == null)
            {
                roleAssigner = go.AddComponent<RoleAssigner>();
            }

            // Refresh role so RoleAssigner applies role bonuses combined with current global upgrades
            roleAssigner.RefreshRole();

            // Apply global-only stats that are not role-dependent
            var controller = go.GetComponent<UnitController>();
            if (controller != null)
            {
                // Use role-specific worker speed if available, fallback to Gatherer
                var ra = go.GetComponent<RoleAssigner>();
                if (ra != null && HiveManager.Instance != null)
                {
                    controller.moveSpeed = HiveManager.Instance.GetWorkerSpeed(ra.role);
                }
                else if (HiveManager.Instance != null)
                {
                    controller.moveSpeed = HiveManager.Instance.GetWorkerSpeed(RoleType.Gatherer);
                }
            }
        }
        
        // Enemy 유닛에 AI 추가
        if (agent.faction == Faction.Enemy)
        {
            var enemyAI = go.GetComponent<EnemyAI>();
            if (enemyAI == null)
            {
                enemyAI = go.AddComponent<EnemyAI>();
            }
            
            // AI 설정 (기본 값)
            enemyAI.visionRange = 1;        // 시야 범위: 1칸
            enemyAI.activityRange = 3;       // 활동 범위: 3칸
            enemyAI.attackRange = 0;         // 공격 범위: 0칸 (같은 타일)
            enemyAI.showDebugLogs = false;
            
            // Enemy 유닛 생성 즉시 가시성 체크하여 숨김
            if (EnemyVisibilityController.Instance != null)
            {
                // 다음 프레임에 가시성 업데이트 (유닛 완전 초기화 후)
                StartCoroutine(HideEnemyOnSpawn(agent));
            }
        }
    }

    /// <summary>
    /// Enemy 생성 즉시 가시성 체크하여 플레이어 시야 밖이면 숨김
    /// </summary>
    System.Collections.IEnumerator HideEnemyOnSpawn(UnitAgent agent)
    {
        // 한 프레임 대기 (Agent 완전히 초기화)
        yield return null;
        
        // 즉시 가시성 업데이트 실행
        if (EnemyVisibilityController.Instance != null)
        {
            EnemyVisibilityController.Instance.ForceUpdateVisibility();
        }
    }

    // IUnitCommandProvider implementation
    public IEnumerable<ICommand> GetCommands(UnitAgent agent)
    {
        var commands = new List<ICommand>();
        
        // Add all hive commands
        if (hiveCommands != null)
        {
            foreach (var cmd in hiveCommands)
            {
                if (cmd != null)
                {
                    commands.Add(cmd);
                }
            }
        }
        
        return commands;
    }

    // Check if hive can relocate (not already relocating)
    public bool CanRelocate()
    {
        return !isRelocating;
    }

    /// <summary>
    /// 하이브 이사 시작 (자원 체크는 SOCommand.IsAvailable에서 완료)
    /// </summary>
    public void StartRelocation(int resourceCost)
    {
        if (isRelocating)
        {
            Debug.LogWarning("이미 이사 준비 중입니다.");
            return;
        }

        // 자원 차감
        if (HiveManager.Instance != null)
        {
            if (!HiveManager.Instance.TrySpendResources(resourceCost))
            {
                Debug.LogWarning($"[하이브 이사] 자원 차감 실패: {resourceCost}");
                return;
            }
            Debug.Log($"[하이브 이사] 자원 차감 성공: {resourceCost}");
        }

        isRelocating = true;

        // 일꾼 생성 중지
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        // 알림 토스트 표시 ?
//         if (NotificationToast.Instance != null)
//         {
//             NotificationToast.Instance.ShowMessage("이사 준비. 10초 후 벌집이 파괴됩니다.", 3f);
//         }

        // 10초 카운트다운 시작
        if (relocateRoutine != null)
        {
            StopCoroutine(relocateRoutine);
        }
        relocateRoutine = StartCoroutine(RelocationCountdown());

        Debug.Log("[하이브 이사] 이사 준비 시작! 10초 후 하이브가 파괴됩니다.");
    }

    IEnumerator RelocationCountdown()
    {
        float countdown = 5f;
        var combat = GetComponent<CombatUnit>();
        int initialHealth = combat != null ? combat.health : 0;
        float healthDrainRate = initialHealth / countdown; // 초당 감소량
        
        NotificationToast.Instance.ShowMessage("이사 준비! 5초 후 벌집이 파괴됩니다.");
        Debug.Log($"[하이브 이사] 카운트다운 시작: {countdown}초");

        while (countdown > 0f)
        {
            countdown -= Time.deltaTime;
            
            // 체력 점진 감소
            if (combat != null)
            {
                float drainAmount = healthDrainRate * Time.deltaTime;
                combat.health = Mathf.Max(1, combat.health - Mathf.CeilToInt(drainAmount));
            }
            
            // 1초마다 로그 출력 (디버그용)
            if (showDebugLogs && Mathf.FloorToInt(countdown) != Mathf.FloorToInt(countdown + Time.deltaTime))
            {
                Debug.Log($"[하이브 이사] 남은 시간: {Mathf.FloorToInt(countdown)}초");
            }

            yield return null;
        }

        Debug.Log("[하이브 이사] 카운트다운 완료! 하이브 파괴 시작");
        
        // Countdown finished 후 destroy this hive
        DestroyHive();
    }

    void DestroyHive()
    {
        Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");
        
        // 경계선 제거
        if (HexBoundaryHighlighter.Instance != null)
        {
            HexBoundaryHighlighter.Instance.Clear();
        }
        
        // ✅ 하이브 파괴 시 페르몬 코루틴 취소
        QueenPheromoneCommandHandler.CancelCurrentPheromoneCommand();
        Debug.Log("[하이브 파괴] 페르몬 명령 취소");
        
        // ✅ 모든 페르몬 효과 제거
        if (PheromoneManager.Instance != null)
        {
            PheromoneManager.Instance.ClearAllPheromones();
            Debug.Log("[하이브 파괴] 모든 페르몬 효과 제거");
        }
        
        // ✅ 여왕벌이 선택된 상태면 명령 UI 갱신 (코루틴으로 지연 실행)
        if (queenBee != null && TileClickMover.Instance != null)
        {
            var selectedUnit = TileClickMover.Instance.GetSelectedUnit();
            if (selectedUnit == queenBee)
            {
                // ✅ 다음 프레임에 UI 갱신 (homeHive null 설정 후)
                StartCoroutine(RefreshQueenUIDelayed(queenBee));
            }
        }
        
        // 일꾼들이 여왕벌을 따라다니게 설정
        foreach (var worker in workers)
        {
            if (worker != null)
            {
                worker.isFollowingQueen = true;
                worker.hasManualOrder = false;
                worker.homeHive = null; // 하이브 참조 제거
                
                // WorkerBehaviorController가 있으면 StartFollowingQueen 호출
                var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
                if (workerBehavior != null)
                {
                    workerBehavior.StartFollowingQueen();
                }
                else
                {
                    // 기존 로직 (호환성 유지)
                    // 현재 작업 취소
                    var behavior = worker.GetComponent<UnitBehaviorController>();
                    if (behavior != null)
                    {
                        behavior.CancelCurrentTask();
                    }
                }
                
                if (showDebugLogs)
                    Debug.Log($"[하이브 파괴] 일꾼 {worker.name}이(가) 여왕벌 추적 시작");
            }
        }
        
        // HiveManager에서 등록 해제
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.UnregisterHive(this);
        }
        
        // GameObject 파괴
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 여왕벌 UI 갱신 (지연 실행) ✅
    /// </summary>
    private System.Collections.IEnumerator RefreshQueenUIDelayed(UnitAgent queen)
    {
        // 한 프레임 대기 (homeHive null 설정 완료)
        yield return null;
        
        if (queen == null)
        {
            Debug.LogWarning("[하이브 파괴] 여왕벌이 null입니다!");
            yield break;
        }
        
        // ✅ homeHive가 null로 설정되었는지 확인
        if (queen.homeHive == null)
        {
            Debug.Log("[하이브 파괴] 여왕벌 homeHive 제거 확인");
        }
        
        // ✅ 명령 패널 새로고침 (건설 버튼 활성화)
        if (UnitCommandPanel.Instance != null)
        {
            UnitCommandPanel.Instance.Show(queen);
            Debug.Log("[하이브 파괴] 여왕벌 명령 UI 갱신 완료 (건설 버튼 활성화)");
        }
        else
        {
            Debug.LogWarning("[하이브 파괴] UnitCommandPanel.Instance가 null입니다!");
        }
    }

    // Called when a new hive is constructed to reclaim all homeless workers
    public void ReclaimWorkers()
    {
        // This will be called by the new hive when constructed
        // All workers with homeHive pointing to this old hive should move to new location
        foreach (var worker in workers)
        {
            if (worker == null) continue;
            worker.isFollowingQueen = false;
            worker.hasManualOrder = false;
            
            // ✅ 2. 하이브와 같은 타일에 있지 않으면 이동
            bool isSameTile = (worker.q == q && worker.r == r);
            
            // WorkerBehaviorController가 있으면 OnHiveConstructed 호출 ✅
            var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
            if (workerBehavior != null)
            {
                if (!isSameTile)
                {
                    workerBehavior.OnHiveConstructed(this);
                    Debug.Log($"[하이브 건설] {worker.name} 하이브로 이동 시작: ({worker.q}, {worker.r}) → ({q}, {r})");
                }
                else
                {
                    // 이미 같은 타일에 있으면 homeHive만 설정
                    worker.homeHive = this;
                    Debug.Log($"[하이브 건설] {worker.name} 이미 하이브 타일에 위치: ({q}, {r})");
                }
                continue;
            }
            
            // 기존 로직 (호환성 유지)
            if (!isSameTile)
            {
                // Move worker to new hive location
                var start = TileManager.Instance.GetTile(worker.q, worker.r);
                var dest = TileManager.Instance.GetTile(q, r);
                if (start != null && dest != null)
                {
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null)
                    {
                        var ctrl = worker.GetComponent<UnitController>();
                        if (ctrl == null) ctrl = worker.gameObject.AddComponent<UnitController>();
                        ctrl.agent = worker;
                        ctrl.SetPath(path);
                        Debug.Log($"[하이브 건설] {worker.name} 하이브로 이동 (기존 로직): ({worker.q}, {worker.r}) → ({q}, {r})");
                    }
                }
            }
            else
            {
                Debug.Log($"[하이브 건설] {worker.name} 이미 하이브 타일에 위치 (기존 로직): ({q}, {r})");
            }
        }
    }

    public List<UnitAgent> GetWorkers()
    {
        return new List<UnitAgent>(workers);
    }

    // Commands targeting the hive workers
    public void IssueCommandToWorkers(string commandId, CommandTarget target)
    {
        foreach (var w in workers)
        {
            if (w == null) continue;
            switch (commandId)
            {
                case "hive_explore":
                    var start = TileManager.Instance.GetTile(w.q, w.r);
                    var dest = TileManager.Instance.GetTile(target.q, target.r);
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null)
                    {
                        var ctrl = w.GetComponent<UnitController>();
                        if (ctrl == null) ctrl = w.gameObject.AddComponent<UnitController>();
                        ctrl.agent = w;
                        ctrl.SetPath(path);
                    }
                    break;
                case "hive_gather":
                    // for now treat gather as move
                    goto case "hive_explore";
                case "hive_attack":
                    // move towards target unit or tile
                    goto case "hive_explore";
            }
        }
    }
}
