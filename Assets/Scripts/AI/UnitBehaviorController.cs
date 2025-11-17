using System.Collections.Generic;
using UnityEngine;

public enum UnitTaskType { Idle, Move, Attack, Gather, ReturnToHive, FollowQueen }

public class UnitBehaviorController : MonoBehaviour
{
    public UnitAgent agent;
    public CombatUnit combat;
    public UnitController mover;
    public RoleAssigner role;

    // gather settings
    public int gatherAmount = 1; // amount taken per gather action
    public float gatherCooldown = 1.0f; // seconds to wait before next gather cycle

    // activity radius from home hive
    public int activityRadius = 5;

    // Current assigned task
    public UnitTaskType currentTask = UnitTaskType.Idle;
    public HexTile targetTile;
    public UnitAgent targetUnit;

    // Follow queen settings
    private float followQueenInterval = 1.0f; // Check queen position every second
    private float lastFollowCheck = 0f;
    
    // Idle behavior settings
    private float idleMovementInterval = 5f; // Idle 상태에서 이동 주기
    private float lastIdleMovement = 0f;
    
    // Enemy detection settings
    private float enemyDetectionInterval = 0.5f; // 적 감지 주기
    private float lastEnemyDetection = 0f;

    void Start()
    {
        if (agent == null) agent = GetComponent<UnitAgent>();
        if (combat == null) combat = GetComponent<CombatUnit>();
        if (mover == null) mover = GetComponent<UnitController>();
        if (role == null) role = GetComponent<RoleAssigner>();

        // Read default activity radius from HiveManager if available
        if (HiveManager.Instance != null)
        {
            activityRadius = HiveManager.Instance.hiveActivityRadius;
        }
    }

    void Update()
    {
        // 여왕벌은 FollowQueen 로직 실행하지 않음 ?
        if (agent != null && agent.isQueen)
        {
            // 여왕벌은 Idle 이동과 적 감지만 수행
            if (currentTask == UnitTaskType.Idle && Time.time - lastIdleMovement > idleMovementInterval)
            {
                lastIdleMovement = Time.time;
                IdleMovementWithinTile();
            }
            
            if (currentTask == UnitTaskType.Idle && Time.time - lastEnemyDetection > enemyDetectionInterval)
            {
                lastEnemyDetection = Time.time;
                DetectNearbyEnemies();
            }
            
            return; // 여왕벌은 여기서 종료 ?
        }
        
        // 일꾼만 FollowQueen 로직 실행 ?
        if (agent.isFollowingQueen && !agent.hasManualOrder)
        {
            if (Time.time - lastFollowCheck > followQueenInterval)
            {
                lastFollowCheck = Time.time;
                FollowQueen();
            }
        }
        
        // Idle 상태에서 주기적으로 타일 내부 이동
        if (currentTask == UnitTaskType.Idle && Time.time - lastIdleMovement > idleMovementInterval)
        {
            lastIdleMovement = Time.time;
            IdleMovementWithinTile();
        }
        
        // Idle 상태에서 주기적으로 적 감지
        if (currentTask == UnitTaskType.Idle && Time.time - lastEnemyDetection > enemyDetectionInterval)
        {
            lastEnemyDetection = Time.time;
            DetectNearbyEnemies();
        }
    }

    /// <summary>
    /// Idle 상태에서 타일 내부 랜덤 이동
    /// </summary>
    void IdleMovementWithinTile()
    {
        if (mover == null || agent == null) return;
        
        // 이미 이동 중이면 패스
        if (mover.IsMoving()) return;
        
        // 타일 내부 랜덤 위치로 이동
        mover.MoveWithinCurrentTile();
    }

    /// <summary>
    /// Surrounding enemy detection and automatic combat
    /// </summary>
    void DetectNearbyEnemies()
    {
        if (agent == null || combat == null) return;
        
        // 현재 타일의 적 확인
        var currentTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        if (currentTile != null)
        {
            var enemy = FindEnemyOnTile(currentTile);
            if (enemy != null)
            {
                // 활동 범위 체크 ?
                if (agent.homeHive != null)
                {
                    int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, currentTile.q, currentTile.r);
                    if (distanceToHive > activityRadius)
                    {
                        // 적이 활동 범위 밖이면 무시
                        return;
                    }
                }
                
                // 같은 타일에 적 발견 → 즉시 교전
                currentTask = UnitTaskType.Attack;
                targetUnit = enemy;
                StartCoroutine(WaitAndCombatRoutine(currentTile, enemy));
                return;
            }
        }
        
        // 주변 1칸 내 적 탐색 (FindNearbyEnemy는 이미 활동 범위 체크함)
        var nearbyEnemy = FindNearbyEnemy(1);
        if (nearbyEnemy != null)
        {
            // 주변에 적 발견 → 이동 후 교전
            currentTask = UnitTaskType.Attack;
            targetUnit = nearbyEnemy;
            MoveAndAttack(nearbyEnemy);
        }
    }

    /// <summary>
    /// 주변 범위 내 적 유닛 찾기
    /// </summary>
    UnitAgent FindNearbyEnemy(int range)
    {
        if (TileManager.Instance == null) return null;
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit == agent) continue;
            
            // 적 유닛만
            if (unit.faction == agent.faction) continue;
            if (unit.faction == Faction.Neutral) continue;
            
            // 무적 유닛은 제외 ?
            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null && combat.isInvincible)
            {
                continue;
            }
            
            // 거리 체크
            int distance = Pathfinder.AxialDistance(agent.q, agent.r, unit.q, unit.r);
            if (distance <= range)
            {
                // 하이브가 있는 경우 활동 범위 체크
                if (agent.homeHive != null)
                {
                    int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, unit.q, unit.r);
                    if (distanceToHive > activityRadius)
                    {
                        // 적이 활동 범위 밖
                        continue;
                    }
                }
                // 하이브가 없는 경우 (여왕벌 모드)
                else if (agent.isQueen)
                {
                    // 여왕벌 기준 1칸 이내만 공격
                    if (distance > 1)
                    {
                        continue;
                    }
                }
                // 하이브도 없고 여왕벌도 아닌 경우 (일반 일꾼)
                else
                {
                    // 여왕벌 위치 기준 1칸 이내 체크
                    UnitAgent queen = FindQueenInScene();
                    if (queen != null)
                    {
                        int distanceToQueen = Pathfinder.AxialDistance(queen.q, queen.r, unit.q, unit.r);
                        if (distanceToQueen > 1)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // 여왕벌도 없으면 공격 안 함
                        continue;
                    }
                }
                
                return unit;
            }
        }
        
        return null;
    }

    void FollowQueen()
    {
        UnitAgent queen = null;
        
        // If homeHive exists and has queen reference, use it
        if (agent.homeHive != null && agent.homeHive.queenBee != null)
        {
            queen = agent.homeHive.queenBee;
        }
        else
        {
            // Hive is destroyed or doesn't exist, find queen in scene
            queen = FindQueenInScene();
        }
        
        if (queen == null)
        {
            // No queen to follow
            return;
        }
        
        // If already at queen's position, don't move
        if (agent.q == queen.q && agent.r == queen.r)
        {
            return;
        }

        // Move towards queen
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = TileManager.Instance.GetTile(queen.q, queen.r);
        
        if (start != null && dest != null)
        {
            var path = Pathfinder.FindPath(start, dest);
            if (path != null)
            {
                currentTask = UnitTaskType.FollowQueen;
                mover.SetPath(path);
            }
        }
    }

    UnitAgent FindQueenInScene()
    {
        // Find queen from all units in TileManager
        var allUnits = TileManager.Instance.GetAllUnits();
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.isQueen && unit.faction == agent.faction)
            {
                return unit;
            }
        }
        return null;
    }

    /// <summary>
    /// 적 유닛 공격 명령 (수동 명령) ?
    /// </summary>
    public void IssueAttackCommand(UnitAgent enemy)
    {
        if (enemy == null) return;
        
        // Mark that this worker received a manual order
        if (agent.isFollowingQueen)
        {
            agent.hasManualOrder = true;
            agent.isFollowingQueen = false;
        }
        
        // 활동 범위 체크 (호출자에서 이미 체크했지만 안전을 위해)
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r,
                enemy.q, enemy.r
            );
            
            if (distanceToHive > activityRadius)
            {
                Debug.Log($"[전투] {agent.name}: 적이 활동 범위 밖에 있어 공격할 수 없습니다.");
                return;
            }
        }
        
        // 공격 명령 실행
        currentTask = UnitTaskType.Attack;
        targetUnit = enemy;
        MoveAndAttack(enemy);
        
        Debug.Log($"[전투] {agent.name}: 적 유닛 공격 명령 받음 → {enemy.name} at ({enemy.q}, {enemy.r})");
    }

    public void IssueCommandToTile(HexTile tile)
    {
        // Mark that this worker received a manual order
        if (agent.isFollowingQueen)
        {
            agent.hasManualOrder = true;
            agent.isFollowingQueen = false;
        }

        // Check if worker can move to this tile based on hive presence
        if (!agent.CanMoveToTile(tile.q, tile.r))
        {
            Debug.Log($"Worker cannot move to ({tile.q},{tile.r}): outside activity radius");
            return;
        }

        // Priority logic on click: 1) if resource available -> gather, 2) if enemy present -> attack, 3) else idle
        if (tile == null) return;

        // 1순위: 자원 채취 (하이브가 있고 이사중이 아닐 때) ?
        bool canGather = agent.homeHive != null && !agent.homeHive.isRelocating;
        
        if (canGather && tile.resourceAmount > 0)
        {
            // gather
            currentTask = UnitTaskType.Gather;
            targetTile = tile;
            MoveAndGather(tile);
            return;
        }

        // 2순위: 적 공격 ?
        var enemy = FindEnemyOnTile(tile);
        if (enemy != null)
        {
            // 활동 범위 체크 - 적이 활동 범위 밖이면 공격 불가
            if (agent.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, tile.q, tile.r);
                if (distanceToHive > activityRadius)
                {
                    Debug.Log($"[전투] {agent.name}: 적이 활동 범위 밖에 있어 공격할 수 없습니다.");
                    return;
                }
            }
            
            // attack
            currentTask = UnitTaskType.Attack;
            targetUnit = enemy;
            MoveAndAttack(enemy);
            return;
        }

        // 3순위: 이동만 ?
        currentTask = UnitTaskType.Move;
        targetTile = tile;
        MoveToTile(tile);
    }

    void MoveToTile(HexTile tile)
    {
        if (tile == null) return;
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var path = Pathfinder.FindPath(start, tile);
        if (path != null)
        {
            mover.SetPath(path);
        }
    }

    UnitAgent FindEnemyOnTile(HexTile tile)
    {
        // search for any UnitAgent of enemy faction on the tile using TileManager
        foreach (var u in TileManager.Instance.GetAllUnits())
        {
            if (u == agent) continue;
            if (u.faction == agent.faction) continue;
            if (u.q == tile.q && u.r == tile.r)
            {
                // 무적 유닛은 제외 ?
                var combat = u.GetComponent<CombatUnit>();
                if (combat != null && combat.isInvincible)
                {
                    continue;
                }
                
                return u;
            }
        }
        return null;
    }

    void MoveAndAttack(UnitAgent enemy)
    {
        if (enemy == null) return;
        
        // 활동 범위 체크 - 적이 범위 밖이면 공격하지 않음 ?
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, enemy.q, enemy.r);
            if (distanceToHive > activityRadius)
            {
                Debug.Log($"[전투] {agent.name}: 적이 활동 범위 밖에 있어 공격하지 않습니다. (거리: {distanceToHive}/{activityRadius})");
                currentTask = UnitTaskType.Idle;
                return;
            }
        }
        
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = TileManager.Instance.GetTile(enemy.q, enemy.r);
        var path = Pathfinder.FindPath(start, dest);
        if (path == null) return;
        // go to enemy tile
        mover.SetPath(path);
        // on arrival, attack or evade based on cooldown
        StartCoroutine(WaitAndCombatRoutine(dest, enemy));
    }

    System.Collections.IEnumerator WaitAndCombatRoutine(HexTile dest, UnitAgent enemy)
    {
        // 도착할 때까지 대기
        float waitTimeout = 10f; // 최대 10초 대기
        float waitElapsed = 0f;
        
        while (Vector3.Distance(transform.position, TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize)) > 0.1f)
        {
            waitElapsed += Time.deltaTime;
            
            // 타임아웃
            if (waitElapsed > waitTimeout)
            {
                Debug.LogWarning($"[전투] {agent.name}: 이동 타임아웃, 전투 취소");
                currentTask = UnitTaskType.Idle;
                targetUnit = null;
                yield break;
            }
            
            // 적이 사라짐
            if (enemy == null || enemy.gameObject == null)
            {
                Debug.Log($"[전투] {agent.name}: 적이 사라짐, 전투 취소");
                currentTask = UnitTaskType.Idle;
                targetUnit = null;
                yield break;
            }
            
            yield return null;
        }

        var myCombat = agent.GetComponent<CombatUnit>();
        if (myCombat == null)
        {
            Debug.LogWarning($"[전투] {agent.name}: CombatUnit 컴포넌트 없음");
            currentTask = UnitTaskType.Idle;
            targetUnit = null;
            yield break;
        }
        
        if (enemy == null || enemy.gameObject == null)
        {
            Debug.Log($"[전투] {agent.name}: 적이 없음, 전투 취소");
            currentTask = UnitTaskType.Idle;
            targetUnit = null;
            yield break;
        }

        var enemyCombat = enemy.GetComponent<CombatUnit>();
        if (enemyCombat == null)
        {
            Debug.LogWarning($"[전투] {agent.name}: 적에게 CombatUnit 없음");
            currentTask = UnitTaskType.Idle;
            targetUnit = null;
            yield break;
        }

        Debug.Log($"[전투] {agent.name}: 전투 시작 vs {enemy.name}");

        // 전투 루프
        while (true)
        {
            // 적이 사라졌는지 체크 ?
            if (enemy == null || enemy.gameObject == null)
            {
                Debug.Log($"[전투] {agent.name}: 적이 사라짐, 전투 종료");
                break;
            }
            
            // enemyCombat이 파괴되었는지 체크 ?
            if (enemyCombat == null)
            {
                Debug.Log($"[전투] {agent.name}: 적 CombatUnit 파괴됨, 전투 종료");
                break;
            }
            
            // 적 체력 확인 ?
            if (enemyCombat.health <= 0)
            {
                Debug.Log($"[전투] {agent.name}: 적 처치! 전투 종료");
                break;
            }
            
            // 내 체력 확인 ?
            if (myCombat.health <= 0)
            {
                Debug.Log($"[전투] {agent.name}: 사망, 전투 종료");
                break;
            }

            // 활동 범위 체크 - 전투 중 범위를 벗어나면 중단
            if (agent.homeHive != null)
            {
                // 현재 위치가 활동 범위를 벗어났는지 체크
                int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, agent.q, agent.r);
                if (distanceToHive > activityRadius)
                {
                    Debug.Log($"[전투] {agent.name}: 활동 범위를 벗어나 전투 중단 (거리: {distanceToHive}/{activityRadius})");
                    currentTask = UnitTaskType.Idle;
                    targetUnit = null;
                    
                    // 하이브로 복귀
                    ReturnToHive();
                    yield break;
                }
                
                // 적의 위치도 체크
                int enemyDistanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, enemy.q, enemy.r);
                if (enemyDistanceToHive > activityRadius)
                {
                    Debug.Log($"[전투] {agent.name}: 적이 활동 범위를 벗어나 전투 중단");
                    break;
                }
            }
            
            // 공격 가능하면 공격
            if (myCombat.CanAttack())
            {
                bool attacked = myCombat.TryAttack(enemyCombat);
                if (attacked)
                {
                    Debug.Log($"[전투] {agent.name} 이 {enemy.name} 공격! 데미지: {myCombat.attack} (적 HP: {enemyCombat.health}/{enemyCombat.maxHealth})");
                }
            }
            else
            {
                // 쿨타임이면 회피 행동 (현재 타일 내 랜덤 이동)
                IdleMovementWithinTile();
            }

            // 공격 간격
            yield return new WaitForSeconds(0.1f);
        }

        // 전투 종료 ?
        Debug.Log($"[전투] {agent.name}: 전투 루틴 종료, Idle로 전환");
        currentTask = UnitTaskType.Idle;
        targetUnit = null;
        
        // 주변에 다른 적이 있는지 확인 (자동 재교전) ?
        yield return new WaitForSeconds(0.5f); // 0.5초 대기 후
        
        if (currentTask == UnitTaskType.Idle) // 여전히 Idle이면
        {
            // 주변 적 재탐지
            var nearbyEnemy = FindNearbyEnemy(1);
            if (nearbyEnemy != null)
            {
                Debug.Log($"[전투] {agent.name}: 주변에 적 발견, 재교전 시작");
                currentTask = UnitTaskType.Attack;
                targetUnit = nearbyEnemy;
                MoveAndAttack(nearbyEnemy);
            }
        }
    }

    /// <summary>
    /// 하이브로 복귀
    /// </summary>
    void ReturnToHive()
    {
        if (agent.homeHive == null || mover == null) return;

        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);

        if (start != null && dest != null)
        {
            var path = Pathfinder.FindPath(start, dest);
            if (path != null && path.Count > 0)
            {
                mover.SetPath(path);
                currentTask = UnitTaskType.ReturnToHive;
                Debug.Log($"[전투] {agent.name} 하이브로 복귀");
            }
        }
    }

    void MoveAndGather(HexTile tile)
    {
        if (tile == null) return;
        
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = tile;
        
        // 경로 찾기
        var path = Pathfinder.FindPath(start, dest);
        if (path == null || path.Count == 0)
        {
            Debug.Log($"[자원 채취] 경로를 찾을 수 없습니다: ({start.q}, {start.r}) → ({dest.q}, {dest.r})");
            currentTask = UnitTaskType.Idle;
            return;
        }
        
        // 경로 설정
        mover.SetPath(path);
        
        // targetTile 저장 (반복 채취용)
        targetTile = dest;
        
        // 도착 후 채취 시작
        StartCoroutine(WaitAndGatherRoutine(dest));
    }

    System.Collections.IEnumerator WaitAndGatherRoutine(HexTile dest)
    {
        // 1단계: 타일 중심까지 이동 대기
        Vector3 tileCenter = TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize);
        
        while (Vector3.Distance(transform.position, tileCenter) > 0.15f)
        {
            // 이동 취소 감지
            if (currentTask != UnitTaskType.Gather)
            {
                Debug.Log($"[자원 채취] 채취 취소됨");
                yield break;
            }
            yield return null;
        }
        
        // 2단계: 타일 도착 - 타일 내부 랜덤 위치로 이동 (작업 위치)
        Vector3 workPosition = TileHelper.GetRandomPositionInTile(dest.q, dest.r, agent.hexSize, 0.25f);
        
        // 작업 위치로 부드럽게 이동
        float moveTime = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveTime);
            transform.position = Vector3.Lerp(startPos, workPosition, t);
            yield return null;
        }
        
        transform.position = workPosition;
        
        // 3단계: 자원 채취
        int taken = dest.TakeResource(gatherAmount);
        
        Debug.Log($"[자원 채취] {agent.name}이(가) ({dest.q}, {dest.r})에서 {taken} 자원 채취");
        
        // 4단계: 하이브로 복귀
        var hive = HiveManager.Instance.FindNearestHive(dest.q, dest.r);
        if (hive != null)
        {
            // 현재 위치에서 하이브까지 경로 찾기 (매번 새로 계산)
            var currentTile = TileManager.Instance.GetTile(dest.q, dest.r);
            var hiveTile = TileManager.Instance.GetTile(hive.q, hive.r);
            var pathBack = Pathfinder.FindPath(currentTile, hiveTile);
            
            if (pathBack != null && pathBack.Count > 0)
            {
                // 경로 설정 (정상 이동)
                mover.SetPath(pathBack);
                
                // 자원 전달 루틴 시작
                StartCoroutine(DeliverResourcesRoutine(hive, taken, dest));
            }
            else
            {
                // 경로 없으면 자원 그냥 추가하고 Idle
                if (taken > 0 && HiveManager.Instance != null)
                {
                    HiveManager.Instance.AddResources(taken);
                }
                currentTask = UnitTaskType.Idle;
            }
        }
        else
        {
            // 하이브 없으면 Idle
            currentTask = UnitTaskType.Idle;
        }
    }

    System.Collections.IEnumerator DeliverResourcesRoutine(Hive hive, int amount, HexTile sourceTile)
    {
        // 하이브 중심 위치
        Vector3 hiveCenter = TileHelper.HexToWorld(hive.q, hive.r, agent.hexSize);
        
        float timeoutCounter = 0f;
        float maxWaitTime = 15f; // 최대 15초 대기
        
        // 하이브 타일에 도착할 때까지 대기
        while (Vector3.Distance(transform.position, hiveCenter) > 0.2f && timeoutCounter < maxWaitTime)
        {
            // 이동이 중단되었는지 체크
            if (mover != null && !mover.IsMoving() && currentTask != UnitTaskType.Gather)
            {
                Debug.Log($"[자원 전달] 이동 중단됨. 자원 버림: {amount}");
                currentTask = UnitTaskType.Idle;
                targetTile = null; // ? targetTile 초기화
                yield break;
            }
            
            timeoutCounter += Time.deltaTime;
            yield return null;
        }
        
        // 타임아웃 체크
        if (timeoutCounter >= maxWaitTime)
        {
            Debug.LogWarning($"[자원 전달] 타임아웃! 자원 강제 전달: {amount}");
        }
        
        // 하이브 도착 - 타일 내부 랜덤 위치로 이동
        Vector3 deliverPosition = TileHelper.GetRandomPositionInTile(hive.q, hive.r, agent.hexSize, 0.25f);
        
        float moveTime = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveTime);
            transform.position = Vector3.Lerp(startPos, deliverPosition, t);
            yield return null;
        }
        
        transform.position = deliverPosition;
        
        // homeHive 업데이트 (새 하이브에 할당)
        if (agent.homeHive == null || agent.homeHive != hive)
        {
            agent.homeHive = hive;
            Debug.Log($"[자원 전달] {agent.name}이(가) 새 하이브에 할당되었습니다.");
        }
        
        // 자원 전달
        if (HiveManager.Instance != null && amount > 0)
        {
            HiveManager.Instance.AddResources(amount);
            Debug.Log($"[자원 전달] {agent.name}이(가) {amount} 자원 전달 완료");
        }

        // 원래 타겟 타일에 자원이 남아있으면 다시 채취 ?
        if (sourceTile != null && sourceTile.resourceAmount > 0)
        {
            currentTask = UnitTaskType.Gather;
            targetTile = sourceTile; // ? targetTile 유지
            yield return new WaitForSeconds(gatherCooldown);
            
            // 다시 채취 시작 (매번 새로운 경로)
            MoveAndGather(sourceTile);
        }
        else
        {
            // 자원 없으면 Idle
            currentTask = UnitTaskType.Idle;
            targetTile = null; // ? targetTile 초기화
            
            Debug.Log($"[자원 채취] {agent.name}: 자원 고갈, Idle 상태로 전환");
        }
    }

    Hive FindNearestHive(int q, int r)
    {
        return HiveManager.Instance.FindNearestHive(q, r);
    }

    /// <summary>
    /// 현재 작업 취소
    /// </summary>
    public void CancelCurrentTask()
    {
        currentTask = UnitTaskType.Idle;
        targetTile = null;
        
        // 이동 중지
        if (mover != null)
        {
            mover.ClearPath();
        }
        
        Debug.Log($"[행동] {agent.name}의 작업이 취소되었습니다.");
    }
}
