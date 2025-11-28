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

    private float savedQueenSpeed = 0f;
    [Header("Relocation Settings")]
    public float carrySpeedMultiplier = 0.5f; // 이사 중 이동 속도 배율 (0.5 = 50% 속도)

    // Commands for hive
    [Header("Hive Commands")]
    public SOCommand[] hiveCommands; // All hive commands (including relocate)
    
    [Header("Debug")]
    public bool showDebugLogs = false; // 디버그 로그 표시
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI relocateTimerText; // 이사 준비 타이머 텍스트?

    public bool isFloating = false; // 이사 중(떠있는 상태) 여부

    [Header("Casting Settings")]
    public float castTime = 3.0f; // 3초 카운트다운
    private Coroutine castingRoutine; // 현재 진행 중인 카운트다운 코루틴

    [Header("Visual Settings")]
    public SpriteRenderer hiveVisual; // ✅ 에디터에서 하이브의 SpriteRenderer를 여기다 드래그해서 넣으세요
    public Color carriedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 들어올렸을 때 색상 (회색)
    private Color savedColor = Color.white;

    public bool IsCasting => castingRoutine != null;

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
                        unit.hasManualTarget = false;
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
    void Update()
    {
        // 떠있는 상태라면, 내 논리적 좌표(q,r)를 여왕벌과 똑같이 맞춤
        if (isFloating && queenBee != null)
        {
            // 여왕벌이 이동해서 좌표가 바뀌었다면
            if (q != queenBee.q || r != queenBee.r)
            {
                // 하이브의 좌표도 갱신 (그래야 적들이 여기로 쫓아옴)
                var agent = GetComponent<UnitAgent>();
                if (agent != null)
                {
                    agent.SetPosition(queenBee.q, queenBee.r);
                }
                q = queenBee.q;
                r = queenBee.r;
            }
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            // ✅ 이사 중(Floating)이면 스폰하지 않음
            if (isFloating) continue;

            // 일꾼 리스트 정리 (null 제거)
            workers.RemoveAll(w => w == null);
            
            // 실시간 MaxWorkers 업데이트
            if (HiveManager.Instance != null)
            {
                maxWorkers = HiveManager.Instance.GetMaxWorkers();
            }
            
            // MaxWorkers 체크
            if (workers.Count < maxWorkers)
            {
                SpawnWorker();
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
    public new bool CanRelocate()
    {
        return !isFloating; // 떠있지 않을 때만 이사 가능
    }

    /// <summary>
    /// 하이브 이사 시작 (자원 체크는 SOCommand.IsAvailable에서 완료)
    /// </summary>
    private void LiftHive()
    {
        if (isFloating) return;
        if (queenBee == null) return;

        Debug.Log("[Hive] 카운트다운 완료! 하이브 이륙.");

        // Clear all placed pheromones when lifting
        if (PheromoneManager.Instance != null)
        {
            PheromoneManager.Instance.ClearAllPheromones();
            Debug.Log("[Hive] 이륙: 모든 페르몬 제거");
        }
        
        // Also cancel any current pheromone command
        QueenPheromoneCommandHandler.CancelCurrentPheromoneCommand();

        // 1. 상태 변경
        isFloating = true;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        
        // 2. 타일에서 제거
        //var agent = GetComponent<UnitAgent>();
        //if (agent != null) { TileManager.Instance?.UnregisterUnit(agent); agent.Unregister(); }

        // 3. 시각적 처리 (여왕벌 위로)
        transform.SetParent(queenBee.transform);
        //transform.localPosition = new Vector3(0, 1.5f, 0); 

        if (hiveVisual != null) 
        {
            savedColor = hiveVisual.color; // 현재 색 기억!
            hiveVisual.color = carriedColor;
        }
        if (HexBoundaryHighlighter.Instance != null)
        {
            HexBoundaryHighlighter.Instance.Clear();
        }
        queenBee.homeHive = null;

        var queenCombat = queenBee.GetComponent<CombatUnit>();
        if (queenCombat != null)
        {
            queenCombat.SetInvincible(true); 
            Debug.Log("[Hive] 여왕벌 무적 모드 ON");
        }
        // 4. 속도 감소
        var queenController = queenBee.GetComponent<UnitController>();
        if (queenController != null)
        {
            savedQueenSpeed = queenController.moveSpeed;
            queenController.moveSpeed *= carrySpeedMultiplier;
        }

        // 5. 데이터 연결
        queenBee.carriedHive = this;

        // 6. 일꾼 처리
        foreach (var worker in workers)
        {
            if (worker != null)
            {
                worker.isFollowingQueen = true;
                worker.homeHive = null; 
                var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
                if (workerBehavior != null) workerBehavior.StartFollowingQueen();
            }
        }

        // 7. 콜라이더 끄기
        //var myCollider = GetComponent<Collider>();
        //if (myCollider != null) myCollider.enabled = false;
        
        // 8. UI 갱신 (Land 버튼 활성화)
        StartCoroutine(RefreshQueenUI());
    }

    private void LandHive(int newQ, int newR)
    {
        if (!isFloating) return;

        Debug.Log($"[Hive] 카운트다운 완료! 하이브 착륙: ({newQ}, {newR})");

        // Clear any pheromones when landing as well
        if (PheromoneManager.Instance != null)
        {
            PheromoneManager.Instance.ClearAllPheromones();
            Debug.Log("[Hive] 착륙: 모든 페르몬 제거");
        }
        
        // 1. 상태 변경
        isFloating = false;

        // 2. 시각적 처리 (분리 및 배치)
        transform.SetParent(null);
        transform.localScale = Vector3.one; 

        if (hiveVisual != null) 
        {
            hiveVisual.color = savedColor;
        }
        
        // 3. 좌표 이동
        q = newQ; r = newR;
        var agent = GetComponent<UnitAgent>();
        if (agent != null)
        {
            agent.q = newQ; agent.r = newR;
            transform.position = TileHelper.HexToWorld(newQ, newR, agent.hexSize);
            TileManager.Instance?.RegisterUnit(agent);
            agent.RegisterWithFog();
        }

        if (HexBoundaryHighlighter.Instance != null)
        {
            int radius = 5;
            if (HiveManager.Instance != null) radius = HiveManager.Instance.hiveActivityRadius;
            
            // HexBoundaryHighlighter가 꺼져있을 수 있으므로 켜줌
            if (!HexBoundaryHighlighter.Instance.enabledForHives) 
                HexBoundaryHighlighter.Instance.SetEnabledForHives(true);

            HexBoundaryHighlighter.Instance.ShowBoundary(this, radius);
        }
        // 여왕벌 귀속 및 무적 해제
        if (queenBee != null)
        {
            queenBee.homeHive = this;
            queenBee.carriedHive = null;
            var queenCombat = queenBee.GetComponent<CombatUnit>();
            if (queenCombat != null)
            {
                queenCombat.SetInvincible(false);
                Debug.Log("[Hive] 여왕벌 무적 모드 OFF");
            }
        }

        // 4. 속도 복구
        var queenController = queenBee.GetComponent<UnitController>();
        if (queenController != null && savedQueenSpeed > 0)
        {
            queenController.moveSpeed = savedQueenSpeed;
        }


        // 6. 데이터 해제
        if (queenBee != null) queenBee.carriedHive = null;

        // 7. 일꾼 복귀
        foreach (var worker in workers)
        {
            if (worker != null)
            {
                worker.isFollowingQueen = false;
                worker.homeHive = this;
                var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
                if (workerBehavior != null) workerBehavior.OnHiveConstructed(this);
            }
        }

        // 8. 스폰 재개
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
        
        // 9. UI 갱신 (Land 버튼 비활성화)
        StartCoroutine(RefreshQueenUI());
    }

public void StartLiftSequence()
    {
        // 이미 떠있거나, 캐스팅 중이면 무시
        if (isFloating || castingRoutine != null) return;
        if (queenBee == null) return;

        // 위치 검사: 여왕벌이 하이브 위에 있어야 함
        if (queenBee.q != this.q || queenBee.r != this.r)
        {
            if (NotificationToast.Instance != null) NotificationToast.Instance.ShowMessage("여왕벌이 하이브 위에 있어야 합니다!", 2f);
            return;
        }

        // 이륙 카운트다운 시작
        castingRoutine = StartCoroutine(CastingRoutine(true, 0, 0));
        StartCoroutine(RefreshQueenUI());


        // Immediately refresh command UI so Land command becomes available when float begins
        StartCoroutine(DelayedRefreshCommandsAfterCastStart());
    }

    private IEnumerator DelayedRefreshCommandsAfterCastStart()
    {
        // small delay to allow internal flags (isFloating/isRelocating) to update if set elsewhere
        yield return null;
        if (UnitCommandPanel.Instance != null) UnitCommandPanel.Instance.RefreshAllCommands();
    }
public void StartLandSequence(int targetQ, int targetR)
    {
        // 떠있지 않거나, 캐스팅 중이면 무시
        if (!isFloating || castingRoutine != null) return;

        // 착륙 카운트다운 시작
        castingRoutine = StartCoroutine(CastingRoutine(false, targetQ, targetR));
        StartCoroutine(RefreshQueenUI());


        // Force UI refresh so relocating/land-mode buttons update immediately
        StartCoroutine(DelayedRefreshCommandsAfterCastStart());
    }
private IEnumerator CastingRoutine(bool isLifting, int targetQ, int targetR)
    {
        float timer = castTime;
        Vector3 startPos = queenBee.transform.position; // 시작 위치 저장
        string actionName = isLifting ? "이륙" : "착륙";

        if (NotificationToast.Instance != null)
        {
            NotificationToast.Instance.ShowMessage($"{castTime}초 뒤 {actionName}을 시작합니다. 움직이면 취소됩니다.", 2f);
        }
        // UI 표시
        if (relocateTimerText != null)
        {
            relocateTimerText.gameObject.SetActive(true);
            relocateTimerText.color = Color.white;
        }

        Debug.Log($"[Hive] {actionName} 준비... 움직이면 취소됩니다.");

        while (timer > 0)
        {
            // 1. 여왕벌 사망 체크
            if (queenBee == null)
            {
                CancelCasting();
                yield break;
            }

            // 2. 움직임 체크 (0.1f 이상 이동 시 취소)
            if (Vector3.Distance(queenBee.transform.position, startPos) > 0.1f)
            {
                Debug.Log($"[Hive] 여왕벌이 움직여서 {actionName}이(가) 취소되었습니다.");
                if (NotificationToast.Instance != null) 
                    NotificationToast.Instance.ShowMessage("이동하여 작업이 취소되었습니다.", 1.5f);
                
                // UI에 취소 표시
                if (relocateTimerText != null)
                {
                    relocateTimerText.color = Color.red;
                    relocateTimerText.text = "취소됨!";
                }
                yield return new WaitForSeconds(1f); // 1초 뒤 UI 끄기
                
                CancelCasting();
                yield break;
            }

            // 3. 타이머 갱신
            timer -= Time.deltaTime;
            if (relocateTimerText != null)
            {
                relocateTimerText.text = $"{actionName} 중...\n{timer:F1}초";
            }

            yield return null;
        }

        // 카운트다운 완료! 실제 동작 실행
        if (relocateTimerText != null) relocateTimerText.gameObject.SetActive(false);
        castingRoutine = null;

        if (NotificationToast.Instance != null)
        {
            NotificationToast.Instance.ShowMessage($"{actionName} 완료!", 1.5f);
        }

        if (isLifting) LiftHive();         // 실제 이륙
        else LandHive(targetQ, targetR);   // 실제 착륙

        // After lift/land, force UI refresh on next frame
        yield return null;
        if (UnitCommandPanel.Instance != null) UnitCommandPanel.Instance.RefreshAllCommands();
    }

    // 캐스팅 강제 취소 (내부용)
    private void CancelCasting()
    {
        if (relocateTimerText != null) relocateTimerText.gameObject.SetActive(false);
        castingRoutine = null;
    }



    void DestroyHive()
    {
        Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");

        if (isFloating && queenBee != null)
        {
            Debug.LogWarning("[하이브 파괴] 운반 중 파괴됨! 여왕벌 상태를 복구합니다.");

            // 1. 여왕벌 속도 원상복구
            var queenController = queenBee.GetComponent<UnitController>();
            // 현재 속도가 저장된 속도보다 느리다면(패널티 상태라면) 복구
            if (queenController != null && savedQueenSpeed > 0)
            {
                queenController.moveSpeed = savedQueenSpeed;
                Debug.Log($"[하이브 파괴] 여왕벌 속도 복구: {savedQueenSpeed}");
            }

            // 2. 여왕벌이 "나 하이브 들고 있어"라고 생각하는 거 지우기
            if (queenBee.carriedHive == this)
            {
                queenBee.carriedHive = null;
            }
            var queenCombat = queenBee.GetComponent<CombatUnit>();
            if (queenCombat != null)
            {
                queenCombat.SetInvincible(false);
            }

            // 3. 여왕벌의 homeHive는 이미 null(LiftHive에서 해제됨)이므로 
            //    건드릴 필요 없음 (집이 터졌으니 홈이 없는 게 맞음)

            // 4. UI 강제 갱신 (Land 버튼 없애기 위해)
            StartCoroutine(RefreshQueenUI());
        }
        
        if (HiveManager.Instance != null)
        {
            int totalResources = HiveManager.Instance.playerStoredResources;

            // 자원이 있을 때만 분산 로직 수행
            if (totalResources > 0)
            {
                // 1. 플레이어 창고 즉시 비우기
                HiveManager.Instance.TrySpendResources(totalResources);

                // 2. 50% 분할 계산
                int stealAmount = Mathf.FloorToInt(totalResources * 0.5f);
                int dropAmount = totalResources - stealAmount;

                // 3. 적 하이브 탐색
                EnemyHive nearestEnemy = null;
                if (WaspWaveManager.Instance != null)
                {
                    nearestEnemy = WaspWaveManager.Instance.FindNearestEnemyHive(this);
                }

                // 4. 강탈 처리
                if (nearestEnemy != null)
                {
                    nearestEnemy.stolenResources += stealAmount;
                    Debug.Log($"[하이브 파괴] 꿀 {stealAmount} (50%)가 적 하이브({nearestEnemy.name})로 넘어갔습니다.");
                    // 내 위치(World 좌표)에서 적 위치(World 좌표)로 꿀 발사
                    Vector3 myPos = transform.position;
                    // 적 위치 계산 (Hex 좌표 -> World 좌표)
                    Vector3 enemyPos = TileHelper.HexToWorld(nearestEnemy.q, nearestEnemy.r, 0.5f); // 0.5f는 hexSize(GameManager확인필요)

                    // HiveManager에게 연출 위임
                    HiveManager.Instance.PlayResourceStealEffect(myPos, enemyPos, stealAmount);
                }
                else
                {
                    // 적이 없으면 강탈분을 드랍분에 합쳐서 전량 바닥에 뿌림
                    dropAmount += stealAmount;
                    Debug.Log("[하이브 파괴] 적 하이브가 없습니다. 모든 꿀을 바닥에 뿌립니다.");
                }

                // 5. 바닥 뿌리기 함수 호출
                ScatterResources(dropAmount);
            }
        }
        
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
        

        if (queenBee != null && TileClickMover.Instance != null)
        {
            var selectedUnit = TileClickMover.Instance.GetSelectedUnit();
            if (selectedUnit == queenBee)
            {
                // ✅ 수정됨: 인자 없이 호출
                StartCoroutine(RefreshQueenUI()); 
            }
        }
        
        // 일꾼들이 여왕벌을 따라다니게 설정
        foreach (var worker in workers)
        {
            if (worker != null)
            {
                worker.isFollowingQueen = true;
                worker.hasManualOrder = false;
                worker.hasManualTarget = false;
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
private IEnumerator RefreshQueenUI()
    {
        yield return null; // 한 프레임 대기 (데이터 업데이트 후 UI 갱신)
        
        if (queenBee != null && UnitCommandPanel.Instance != null)
        {
            // 현재 선택된 유닛이 여왕벌일 때만 UI 갱신
            if (TileClickMover.Instance.GetSelectedUnit() == queenBee)
            {
                UnitCommandPanel.Instance.Show(queenBee);
            }
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
            worker.hasManualTarget = false;
            
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
    /// <summary>
    /// 자원 흩뿌리기 (기존 자원 보존 + 지형 변경 시 뻥튀기 방지)
    /// </summary>
    private void ScatterResources(int amount)
    {
        if (amount <= 0 || TileManager.Instance == null) return;

        // 1. 타일 수집 (본진 + 주변 6방향)
        List<HexTile> targetTiles = new List<HexTile>();
        var centerTile = TileManager.Instance.GetTile(q, r);
        if (centerTile != null) targetTiles.Add(centerTile);
        
        var neighbors = TileManager.Instance.GetNeighbors(q, r);
        if (neighbors != null) targetTiles.AddRange(neighbors);

        if (targetTiles.Count == 0) return;

        // 2. 자원 지형 정보 가져오기
        TerrainType resourceTerrain = null;
        if (GameManager.Instance != null && GameManager.Instance.terrainTypes != null)
        {
            foreach (var terrain in GameManager.Instance.terrainTypes)
            {
                if (terrain != null && terrain.resourceYield > 0)
                {
                    resourceTerrain = terrain;
                    break;
                }
            }
        }

        // 3. 랜덤 가중치 부여
        float totalWeight = 0f;
        float[] weights = new float[targetTiles.Count];
        for (int i = 0; i < targetTiles.Count; i++)
        {
            weights[i] = UnityEngine.Random.Range(0.1f, 1.0f);
            totalWeight += weights[i];
        }

        // 4. 자원 분배 및 타일 적용
        int distributedTotal = 0;
        for (int i = 0; i < targetTiles.Count; i++)
        {
            float ratio = weights[i] / totalWeight;
            int tileAddAmount = Mathf.FloorToInt(amount * ratio);
            
            // 마지막 타일 잔여량 보정
            if (i == targetTiles.Count - 1)
            {
                tileAddAmount = amount - distributedTotal;
            }
            distributedTotal += tileAddAmount;

            HexTile tile = targetTiles[i];
            
            // 1) 현재 타일에 있는 자원량을 미리 백업 (없으면 0)
            int existingAmount = tile.resourceAmount;

            // 2) 지형 변경이 필요한 경우 (풀 -> 꽃)
            bool needTerrainChange = (resourceTerrain != null && tile.terrain != resourceTerrain);

            if (needTerrainChange)
            {
                // 지형 변경 (이 순간 HexTile 내부 로직으로 자원이 500 등으로 초기화됨)
                tile.SetTerrain(resourceTerrain);

                // 3) 백업해둔 기존 자원량으로 강제 복구 (뻥튀기된 500을 지움)
                // 만약 빈 땅이었다면 existingAmount는 0이므로, 0으로 깔끔하게 초기화됨.
                tile.SetResourceAmount(existingAmount);
            }

            // 4) 최종적으로 (기존 자원 + 이번에 뿌릴 자원)을 더함
            tile.SetResourceAmount(tile.resourceAmount + tileAddAmount);
            
            // 디버그 로그 (확인용)
            // Debug.Log($"[타일 처리] ({tile.q},{tile.r}) 기존:{existingAmount} + 추가:{tileAddAmount} = 최종:{tile.resourceAmount}");
            // ========================================================
        }
        
        Debug.Log($"[하이브 파괴] 자원 {amount}개를 {targetTiles.Count}개 타일에 안전하게 흩뿌렸습니다.");
    }
    // ✅ [추가] 안전장치: 스크립트가 파괴될 때 마지막 점검
    void OnDestroy()
    {
        // 1. 타일 매니저 등록 해제
        if (TileManager.Instance != null)
        {
            var agent = GetComponent<UnitAgent>();
            if(agent != null) TileManager.Instance.UnregisterUnit(agent);
        }

        // 2. 운반 중 파괴되었을 때 여왕벌 상태 복구 (DestroyHive가 호출 안 됐을 경우 대비)
        if (isFloating && queenBee != null)
        {
            var queenController = queenBee.GetComponent<UnitController>();
            // 여왕벌이 아직 살아있고, 속도가 느려진 상태라면
            if (queenController != null && savedQueenSpeed > 0)
            {
                // 현재 속도가 저장된(원래) 속도보다 작다면 복구
                if (queenController.moveSpeed < savedQueenSpeed)
                {
                    queenController.moveSpeed = savedQueenSpeed;
                }
            }

            // 참조 끊기
            if (queenBee.carriedHive == this)
            {
                queenBee.carriedHive = null;
            }
        }
    }
}
