using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 유닛(말벌) AI 총괄
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("AI 설정")]
    [Tooltip("시야 범위 (타일 수)")]
    public int visionRange = 3;
    [Tooltip("하이브 활동 범위 (타일 수)")]
    public int activityRange = 5;
    [Tooltip("스캔 간격 (초)")]
    public float scanInterval = 1f;
    [Tooltip("공격 범위 (타일 거리) - 0이면 같은 타일에서만 공격")]
    public int attackRange = 0;
    [Tooltip("순찰 간격 (초)")]
    public float patrolInterval = 3f;

    [Header("디버그")]
    public bool showDebugLogs = false;

    [HideInInspector]
    public bool isWaveWasp = false;
    [HideInInspector]
    public Hive targetHive = null;

    private UnitAgent agent;
    private UnitController controller;
    private CombatUnit combat;
    private EnemyHive homeHive;

    private UnitAgent currentTarget;
    private float lastScanTime;
    private float lastPatrolTime;
    private bool isChasing = false;
    private bool isPatrolling = false;
    private bool isMovingToTargetHive = false;

    void Awake()
    {
        agent = GetComponent<UnitAgent>();
        controller = GetComponent<UnitController>();
        combat = GetComponent<CombatUnit>();
    }

    void Start()
    {
        if (agent != null && agent.faction == Faction.Enemy)
        {
            homeHive = FindNearestEnemyHive();
        }
        lastScanTime = Time.time;
        lastPatrolTime = Time.time;
    }

    void Update()
    {
        if (agent == null || agent.faction != Faction.Enemy) return;

        if (Time.time - lastScanTime >= scanInterval)
        {
            lastScanTime = Time.time;
            UpdateBehavior();
        }

        if (!isWaveWasp && currentTarget == null && !isChasing && Time.time - lastPatrolTime >= patrolInterval)
        {
            lastPatrolTime = Time.time;
            PatrolAroundHive();
        }
    }

    void UpdateBehavior()
    {
        // ? 1. 타겟 하이브 유효성 체크 (웨이브 말벌만)
        if (isWaveWasp && targetHive != null)
        {
            // 타겟 하이브가 파괴되었는지 확인
            if (targetHive == null || targetHive.gameObject == null)
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 타겟 하이브 파괴됨. 여왕벌 찾기.");
                
                targetHive = null;
                isMovingToTargetHive = false;
                // 여왕벌 찾기로 전환
                MoveToQueenBee();
                return;
            }
        }
        
        // ? 2. 적 타겟이 있는 경우 - 우선 처리
        if (currentTarget != null)
        {
            // 타겟이 죽었거나 제거됨
            if (currentTarget == null || currentTarget.gameObject == null)
            {
                StopChasing();
                // ? 다음 UpdateBehavior()에서 자동으로 적 탐색
                return;
            }

            // 타겟이 시야 밖으로 벗어남
            int distanceToTarget = GetDistance(agent.q, agent.r, currentTarget.q, currentTarget.r);

            // ? 웨이브 말벌이 아닐 때만 활동 범위 체크
            if (!isWaveWasp)
            {
                // ? 시야의 2배를 벗어나면 추격 중단
                if (distanceToTarget > visionRange * 2)
                {
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 타겟이 너무 멀어짐. 추격 중단.");
                    StopChasing();
                
                    // ? 타겟 하이브로 복귀
                    if (isWaveWasp && targetHive != null)
                    {
                        MoveToTargetHive();
                    }
                    return;
                }

                // ? 타겟이 활동 범위를 벗어나면 추격 중단
                if (!IsWithinHiveRange(currentTarget.q, currentTarget.r))
                {
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 타겟이 하이브 범위 밖. 추격 중단.");
                    StopChasing();
                    ReturnToHive();
                    return;
                }
            }

            // ? 타겟이 하이브인지 확인
            var targetHiveComponent = currentTarget.GetComponent<Hive>();
            bool isTargetHive = (targetHiveComponent != null);

            // ? 하이브는 건물이라 같은 타일 진입 불가 → 인접 타일(distance = 1)에서 공격
            if (isTargetHive && distanceToTarget == 1)
            {
                // ? 인접 타일에서 이동 중단
                if (controller != null && controller.IsMoving())
                {
                    controller.ClearPath();
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 하이브 인접 도착. 이동 중단.");
                }
                
                // ? 공격 가능하면 공격
                bool canAttack = combat != null && combat.CanAttack();
                if (canAttack)
                {
                    // Re-evaluate priority: prefer tank-role workers on the tile before attacking
                    var prioritized = FindTankOnTile(currentTarget.q, currentTarget.r);
                    if (prioritized != null && prioritized != currentTarget)
                    {
                        if (showDebugLogs) Debug.Log($"[Enemy AI] 탱크 유닛 발견. 공격 대상 변경: {prioritized.name}");
                        currentTarget = prioritized;
                        isChasing = true;
                    }

                    Attack(currentTarget);
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 하이브 공격! 거리: {distanceToTarget}");
                }
                else
                {
                    // 공격 쿨타임 중에는 회피
                    EvadeInCurrentTile();
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 공격 쿨타임. 회피 행동.");
                }
                return;
            }

            // ? 공격 범위 체크 (attackRange = 0, 같은 타일에서만 공격)
            bool canAttack2 = combat != null && combat.CanAttack();

            if (distanceToTarget <= attackRange)
            {
                // Re-evaluate priority on the tile before attacking
                var prioritized = FindTankOnTile(currentTarget.q, currentTarget.r);
                if (prioritized != null && prioritized != currentTarget)
                {
                    if (showDebugLogs) Debug.Log($"[Enemy AI] 탱크 유닛 발견. 공격 대상 변경: {prioritized.name}");
                    currentTarget = prioritized;
                }

                if (canAttack2)
                {
                    Attack(currentTarget);
                }
                else
                {
                    EvadeInCurrentTile();
                    
                    if (showDebugLogs)
                        Debug.Log($"[Enemy AI] 공격 쿨타임. 회피 행동.");
                }
                return;
            }

            // 추격 중
            if (canAttack2)
            {
                ChaseTarget(currentTarget);
            }
            else
            {
                EvadeInCurrentTile();
                
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 공격 쿨타임. 회피 행동.");
            }
        }
        else
        {
            // ? 3. 적 타겟 없음 - 웨이브 말벌은 타겟 하이브 도착 체크
            if (isWaveWasp && targetHive != null)
            {
                var targetAgent = targetHive.GetComponent<UnitAgent>();
                if (targetAgent != null)
                {
                    int distanceToTarget = GetDistance(agent.q, agent.r, targetAgent.q, targetAgent.r);
                    
                    // ? 타겟 하이브 인접 도착 → currentTarget 설정하고 다음 프레임에 공격
                    if (distanceToTarget <= 1)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[Enemy AI] 타겟 하이브 인접 도착! 공격 대상 설정. 거리: {distanceToTarget}");
                        
                        // 타겟 하이브를 currentTarget으로 설정
                        currentTarget = targetAgent;
                        isMovingToTargetHive = false;
                        isChasing = true;
                        
                        // ? 경로 중단 (인접 타일에서 멈춤)
                        if (controller != null)
                        {
                            controller.ClearPath();
                        }
                        
                        // ? 즉시 공격 시도
                        bool canAttack = combat != null && combat.CanAttack();
                        
                        // prefer tank on tile if present
                        var prioritized = FindTankOnTile(currentTarget.q, currentTarget.r);
                        if (prioritized != null && prioritized != currentTarget)
                        {
                            if (showDebugLogs) Debug.Log($"[Enemy AI] 탱크 유닛 발견. 공격 대상 변경: {prioritized.name}");
                            currentTarget = prioritized;
                        }

                        if (canAttack) Attack(currentTarget);
                        
                        // ? return 제거 - 다음 프레임에 "적 타겟이 있는 경우" 로직으로 계속 공격
                        return;
                    }
                }
            }
            
            // ? 4. 적 타겟 탐색
            currentTarget = FindNearestPlayerUnit();
            
            if (currentTarget != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 새 타겟 발견: {currentTarget.name}");
                isChasing = true;
                isPatrolling = false;
                isMovingToTargetHive = false; // ? 타겟 하이브 이동 중단
            }
            else
            {
                // ? 5. 적도 없음 - 타겟 하이브 또는 순찰
                if (isWaveWasp && targetHive != null)
                {
                    // ? 이동 중이 아니고, 이미 이동 중이지 않을 때만 이동 시작
                    if (!isMovingToTargetHive && (controller == null || !controller.IsMoving()))
                    {
                        MoveToTargetHive();
                    }
                }
                else
                {
                    // 일반 말벌: 순찰
                    if (isChasing)
                    {
                        // 추격 중이었으면 하이브로 복귀
                        ReturnToHive();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 현재 타일에 탱커형 일꾼이 있는지 검사하고 반환
    /// </summary>
    UnitAgent FindTankOnTile(int q, int r)
    {
        if (TileManager.Instance == null) return null;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            if (unit.faction != Faction.Player) continue;
            if (unit.q != q || unit.r != r) continue;

            var roleAssigner = unit.GetComponent<RoleAssigner>();
            if (roleAssigner != null && roleAssigner.role == RoleType.Tank)
            {
                var combatUnit = unit.GetComponent<CombatUnit>();
                if (combatUnit != null && !combatUnit.isInvincible && combatUnit.health > 0)
                {
                    return unit;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 하이브 1칸 이내에서 랜덤 순찰
    /// </summary>
    void PatrolAroundHive()
    {
        if (homeHive == null || controller == null) return;

        // 이미 이동 중이면 순찰하지 않음
        if (controller.IsMoving()) return;

        // 현재 위치가 하이브 1칸 이내인지 확인
        int currentDistance = GetDistance(agent.q, agent.r, homeHive.q, homeHive.r);
        if (currentDistance > 1)
        {
            // 하이브 밖이면 복귀
            ReturnToHive();
            return;
        }

        // 하이브 1칸 이내의 랜덤 타일 선택 (적 없을 때 기본 순찰)
        var patrolTile = GetRandomTileAroundHive();
        if (patrolTile != null)
        {
            var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
            var path = Pathfinder.FindPath(startTile, patrolTile);
            
            if (path != null && path.Count > 0)
            {
                controller.agent = agent;
                // ? SetPathSimple 사용
                controller.SetPathSimple(path);
                isPatrolling = true;

                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 하이브 1칸 내 순찰: ({patrolTile.q}, {patrolTile.r})");
            }
        }
    }

    /// <summary>
    /// 하이브 1칸 이내의 랜덤 타일 가져오기 ?
    /// </summary>
    HexTile GetRandomTileAroundHive()
    {
        if (homeHive == null || TileManager.Instance == null) return null;

        var possibleTiles = new List<HexTile>();

        // ? 하이브 1칸 이내의 모든 타일 수집
        for (int dq = -1; dq <= 1; dq++)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                // 큐브 좌표 제약: dq + dr + ds = 0
                int ds = -dq - dr;
                if (ds < -1 || ds > 1) continue;

                int tq = homeHive.q + dq;
                int tr = homeHive.r + dr;

                // 현재 위치는 제외
                if (tq == agent.q && tr == agent.r) continue;

                var tile = TileManager.Instance.GetTile(tq, tr);
                if (tile != null)
                {
                    possibleTiles.Add(tile);
                }
            }
        }

        // 랜덤 타일 선택
        if (possibleTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleTiles.Count);
            return possibleTiles[randomIndex];
        }

        return null;
    }

    /// <summary>
    /// 시야 범위 내 가장 가까운 플레이어 유닛 찾기 (개선: 일꾼 우선 → 건물 공격)
    /// </summary>
    UnitAgent FindNearestPlayerUnit()
    {
        if (TileManager.Instance == null) return null;

        List<UnitAgent> workers = new List<UnitAgent>();
        List<UnitAgent> hives = new List<UnitAgent>();

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit == agent) continue;
            
            // 플레이어 유닛만
            if (unit.faction != Faction.Player) continue;

            // 무적 유닛은 제외
            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null && combat.isInvincible)
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] {unit.name}은(는) 무적 상태라 타겟에서 제외");
                continue;
            }

            int distance = GetDistance(agent.q, agent.r, unit.q, unit.r);

            // ? 시야 범위 내만 체크 (활동 범위 체크 제거)
            if (distance <= visionRange)
            {
                // ? 타입별 분류
                var hive = unit.GetComponent<Hive>();
                if (hive != null)
                {
                    // 하이브는 2순위
                    hives.Add(unit);
                }
                else
                {
                    // 일반 유닛 (꿀벌) - 1순위
                    workers.Add(unit);
                }
            }
        }

        // ? 1순위: 가장 가까운 일반 유닛(꿀벌)
        UnitAgent target = GetClosestPlayerUnit(workers, "일반 유닛");
        
        // ? 2순위: 가장 가까운 하이브
        if (target == null)
        {
            target = GetClosestPlayerUnit(hives, "플레이어 하이브");
        }

        return target;
    }
    
    /// <summary>
    /// 리스트에서 가장 가까운 플레이어 유닛 찾기 ?
    /// </summary>
    UnitAgent GetClosestPlayerUnit(List<UnitAgent> units, string typeName)
    {
        if (units == null || units.Count == 0) return null;
        
        UnitAgent closest = null;
        int minDistance = int.MaxValue;
        
        foreach (var unit in units)
        {
            int distance = GetDistance(agent.q, agent.r, unit.q, unit.r);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = unit;
            }
        }
        
        if (closest != null && showDebugLogs)
        {
            Debug.Log($"[Enemy AI] {typeName} 발견! 우선 공격 대상: {closest.name}");
        }
        
        return closest;
    }

    /// <summary>
    /// 타겟 추격
    /// </summary>
    void ChaseTarget(UnitAgent target)
    {
        if (target == null || controller == null) return;

        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        var targetTile = TileManager.Instance?.GetTile(target.q, target.r);

        if (startTile == null || targetTile == null) return;

        var path = Pathfinder.FindPath(startTile, targetTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            // ? SetPathSimple 사용 (방향 체크 없이 단순 설정)
            controller.SetPathSimple(path);
            isPatrolling = false;

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 타겟 추격 중: {target.name}");
        }
    }

    /// <summary>
    /// 타겟 하이브로 이동 ?
    /// </summary>
    void MoveToTargetHive()
    {
        if (targetHive == null || controller == null) return;

        var targetTile = TileManager.Instance?.GetTile(targetHive.q, targetHive.r);
        if (targetTile == null) return;

        // ? 이미 타겟 하이브로 이동 중이면 스킵
        if (isMovingToTargetHive)
        {
            // ? 정말로 이동 중인지 확인 (pathQueue 또는 isMoving)
            if (controller.IsMoving())
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 이미 타겟 하이브로 이동 중. 경로 유지.");
                return;
            }
            else
            {
                // ? 플래그는 true인데 실제로는 멈춰있음 → 재설정 필요
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 이동 플래그 true이지만 실제로는 멈춤. 경로 재설정.");
                isMovingToTargetHive = false;
            }
        }

        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        if (startTile == null) return;

        // ? 이미 타겟 하이브 인접 타일이면 이동하지 않음
        int distanceToTarget = GetDistance(agent.q, agent.r, targetHive.q, targetHive.r);
        if (distanceToTarget <= 1)
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 이미 타겟 하이브 인접. 이동 불필요.");
            return;
        }

        var path = Pathfinder.FindPath(startTile, targetTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            // ? SetPathSimple 사용 (방향 체크 없이 단순 설정)
            controller.SetPathSimple(path);
            isMovingToTargetHive = true;

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 타겟 하이브로 이동 시작: ({targetHive.q}, {targetHive.r}), 경로: {path.Count}타일");
        }
    }

    /// <summary>
    /// 여왕벌 찾아서 이동 ?
    /// </summary>
    void MoveToQueenBee()
    {
        if (controller == null) return;

        // 플레이어 여왕벌 찾기
        UnitAgent queenBee = FindPlayerQueenBee();
        
        if (queenBee == null)
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 여왕벌을 찾을 수 없습니다. 순찰 모드.");
            
            // 여왕벌이 없으면 순찰
            if (homeHive != null)
            {
                ReturnToHive();
            }
            return;
        }

        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        var queenTile = TileManager.Instance?.GetTile(queenBee.q, queenBee.r);

        if (startTile == null || queenTile == null) return;

        var path = Pathfinder.FindPath(startTile, queenTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            // ? SetPathSimple 사용
            controller.SetPathSimple(path);
            isMovingToTargetHive = false;

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 여왕벌로 이동 시작: ({queenBee.q}, {queenBee.r})");
        }
    }

    /// <summary>
    /// 플레이어 여왕벌 찾기 ?
    /// </summary>
    UnitAgent FindPlayerQueenBee()
    {
        if (TileManager.Instance == null) return null;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            
            // 플레이어 진영의 여왕벌
            if (unit.faction == Faction.Player && unit.isQueen)
            {
                return unit;
            }
        }

        return null;
    }

    /// <summary>
    /// 타겟 공격
    /// </summary>
    void Attack(UnitAgent target)
    {
        if (target == null || combat == null) return;

        // Before each attack attempt, re-evaluate the best target on the same tile
        var prioritized = FindTankOnTile(target.q, target.r);
        if (prioritized != null && prioritized != target)
        {
            if (showDebugLogs) Debug.Log($"[Enemy AI] 우선순위 재탐색: 탱커 발견, 공격 대상 변경 -> {prioritized.name}");
            currentTarget = prioritized;
            target = prioritized;
        }

        // 여왕이 하이브 위에 있다면 하이브로 공격 전환
        target = RedirectToHiveIfQueenOnHive(target);

        var targetCombat = target.GetComponent<CombatUnit>();
        if (targetCombat == null) return;

        // 공격 시도 (쿨타임 체크 포함)
        bool attacked = combat.TryAttack(targetCombat);

        if (attacked)
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] {agent.name}이(가) {target.name}을(를) 공격! 데미지: {combat.attack}");

            // 타겟이 죽었으면 추격 중단
            if (targetCombat.health <= 0)
            {
                if (showDebugLogs) Debug.Log($"[Enemy AI] 타겟 처치!");
                StopChasing();
            }
        }
        else
        {
            if (showDebugLogs) Debug.Log($"[Enemy AI] 공격 쿨타임 중...");
        }
    }

    /// <summary>
    /// 여왕과 하이브가 같은 타일일 때 공격 대상을 하이브로 전환
    /// </summary>
    UnitAgent RedirectToHiveIfQueenOnHive(UnitAgent target)
    {
        if (target == null || !target.isQueen) return target;

        // 여왕이 속한 하이브가 없으면 그대로
        if (target.homeHive == null) return target;

        // 좌표 비교
        if (target.q == target.homeHive.q && target.r == target.homeHive.r)
        {
            var hiveAgent = target.homeHive.GetComponent<UnitAgent>();
            if (hiveAgent != null)
            {
                if (showDebugLogs) Debug.Log("[Enemy AI] 여왕이 하이브 위 → 공격 대상 하이브로 전환");
                return hiveAgent;
            }
        }

        return target;
    }

    /// <summary>
    /// 추격 중단
    /// </summary>
    void StopChasing()
    {
        currentTarget = null;
        isChasing = false;

        // ? 웨이브 말벌: 타겟만 초기화 (다음 UpdateBehavior에서 자동으로 탐색)
        // ? 일반 말벌: 이동 중단
        if (!isWaveWasp && !isMovingToTargetHive)
        {
            if (controller != null)
            {
                controller.ClearPath();
            }
        }

        if (showDebugLogs)
            Debug.Log($"[Enemy AI] 추격 중단");
    }

    /// <summary>
    /// 현재 타일 내에서 랜덤 위치로 회피 (쿨타임 동안)
    /// </summary>
    void EvadeInCurrentTile()
    {
        if (controller == null) return;

        // 이미 이동 중이면 회피하지 않음
        if (controller.IsMoving()) return;

        // 현재 타일 내부에서만 이동
        controller.MoveWithinCurrentTile();

        if (showDebugLogs)
            Debug.Log($"[Enemy AI] 타일 내부 회피 이동");
    }

    /// <summary>
    /// 하이브로 복귀
    /// </summary>
    void ReturnToHive()
    {
        if (homeHive == null || controller == null) return;

        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        var hiveTile = TileManager.Instance?.GetTile(homeHive.q, homeHive.r);

        if (startTile == null || hiveTile == null) return;

        // 하이브 1칸 이내에 있으면 복귀하지 않음
        int distanceToHive = GetDistance(agent.q, agent.r, homeHive.q, homeHive.r);
        if (distanceToHive <= 1)
        {
            isChasing = false;
            return;
        }

        var path = Pathfinder.FindPath(startTile, hiveTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            // ? SetPathSimple 사용
            controller.SetPathSimple(path);

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 하이브로 복귀");
        }

        isChasing = false;
    }

    /// <summary>
    /// 하이브 활동 범위 내에 있는지 확인
    /// </summary>
    bool IsWithinHiveRange(int q, int r)
    {
        if (homeHive == null) return true; // 하이브 없으면 제한 없음

        int distance = GetDistance(homeHive.q, homeHive.r, q, r);
        
        // 하이브 1칸 이내면 항상 허용
        if (distance <= 1)
            return true;

        // 활동 범위 내인지 확인
        return distance <= activityRange;
    }

    /// <summary>
    /// 두 지점 사이의 거리 계산 (Axial Distance)
    /// </summary>
    int GetDistance(int q1, int r1, int q2, int r2)
    {
        return Pathfinder.AxialDistance(q1, r1, q2, r2);
    }

    /// <summary>
    /// 현재 타겟 가져오기 (디버그용)
    /// </summary>
    public UnitAgent GetCurrentTarget()
    {
        return currentTarget;
    }

    /// <summary>
    /// 추격 중인지 확인 (디버그용)
    /// </summary>
    public bool IsChasing()
    {
        return isChasing;
    }

    /// <summary>
    /// 순찰 중인지 확인 (디버그용)
    /// </summary>
    public bool IsPatrolling()
    {
        return isPatrolling;
    }

    // Gizmos로 시야 및 활동 범위 표시
    void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        // 시야 범위 (노란색)
        Gizmos.color = Color.yellow;
        Vector3 pos = TileHelper.HexToWorld(agent.q, agent.r, 0.5f);
        Gizmos.DrawWireSphere(pos, visionRange * 0.5f);

        // 활동 범위 (빨간색)
        if (homeHive != null)
        {
            Gizmos.color = Color.red;
            Vector3 hivePos = TileHelper.HexToWorld(homeHive.q, homeHive.r, 0.5f);
            Gizmos.DrawWireSphere(hivePos, activityRange * 0.5f);
        }

        // 현재 타겟 (초록색 선)
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Vector3 targetPos = TileHelper.HexToWorld(currentTarget.q, currentTarget.r, 0.5f);
            Gizmos.DrawLine(pos, targetPos);
        }
    }

    /// <summary>
    /// 가장 가까운 EnemyHive 찾기
    /// </summary>
    EnemyHive FindNearestEnemyHive()
    {
        var allHives = FindObjectsOfType<EnemyHive>();
        EnemyHive nearest = null;
        int minDistance = int.MaxValue;

        if (TileManager.Instance == null || agent == null) return null;

        int myQ = agent.q;
        int myR = agent.r;

        foreach (var hive in allHives)
        {
            if (hive == null) continue;
            int d = Pathfinder.AxialDistance(myQ, myR, hive.q, hive.r);
            if (d < minDistance)
            {
                minDistance = d;
                nearest = hive;
            }
        }

        return nearest;
    }
}
