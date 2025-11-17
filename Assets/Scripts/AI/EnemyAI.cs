using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 유닛(말벌)의 AI 행동 제어
/// - 시야 범위 내 플레이어 유닛 자동 추적 및 공격
/// - 하이브 활동 범위 제한
/// - 타겟 없을 시 하이브 1칸 내에서 순찰
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("AI 설정")]
    [Tooltip("시야 범위 (타일 수)")]
    public int visionRange = 3;
    
    [Tooltip("하이브 활동 범위 (타일 수)")]
    public int activityRange = 5;
    
    [Tooltip("타겟 탐색 주기 (초)")]
    public float scanInterval = 1f;
    
    [Tooltip("공격 범위 (타일 수) - 0이면 같은 타일에서만 공격")]
    public int attackRange = 0; // 근접 전투
    
    [Tooltip("순찰 주기 (초) - 하이브 근처 이동")]
    public float patrolInterval = 3f;

    [Header("디버그")]
    public bool showDebugLogs = false;

    private UnitAgent agent;
    private UnitController controller;
    private CombatUnit combat;
    private EnemyHive homeHive; // EnemyHive 타입으로 변경 ?
    
    private UnitAgent currentTarget;
    private float lastScanTime;
    private float lastPatrolTime;
    private bool isChasing = false;
    private bool isPatrolling = false;

    void Awake()
    {
        agent = GetComponent<UnitAgent>();
        controller = GetComponent<UnitController>();
        combat = GetComponent<CombatUnit>();
    }

    void Start()
    {
        // 홈 하이브 찾기 (가장 가까운 EnemyHive) ?
        if (agent != null && agent.faction == Faction.Enemy)
        {
            homeHive = FindNearestEnemyHive();
            
            if (showDebugLogs)
            {
                if (homeHive != null)
                    Debug.Log($"[Enemy AI] {agent.name} 홈 하이브 설정: ({homeHive.q}, {homeHive.r})");
                else
                    Debug.LogWarning($"[Enemy AI] {agent.name} 홈 하이브를 찾을 수 없습니다!");
            }
        }

        lastScanTime = Time.time;
        lastPatrolTime = Time.time;
    }

    /// <summary>
    /// 가장 가까운 EnemyHive 찾기 ?
    /// </summary>
    EnemyHive FindNearestEnemyHive()
    {
        var allHives = FindObjectsOfType<EnemyHive>();
        EnemyHive nearest = null;
        int minDistance = int.MaxValue; // 타일 거리로 변경 ?

        // 현재 말벌의 타일 좌표 사용 ?
        int myQ = agent.q;
        int myR = agent.r;

        if (showDebugLogs)
            Debug.Log($"[Enemy AI] {agent.name} 홈 하이브 검색 시작: 현재 위치 ({myQ}, {myR})");

        foreach (var hive in allHives)
        {
            if (hive == null) continue;

            // 타일 좌표 기반 거리 계산 ?
            int distance = Pathfinder.AxialDistance(myQ, myR, hive.q, hive.r);

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 하이브 거리 체크: ({hive.q}, {hive.r}) → 거리: {distance}");

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = hive;
            }
        }

        if (showDebugLogs && nearest != null)
            Debug.Log($"[Enemy AI] 가장 가까운 하이브: ({nearest.q}, {nearest.r}), 거리: {minDistance}");

        return nearest;
    }

    void Update()
    {
        // Enemy가 아니면 작동하지 않음
        if (agent == null || agent.faction != Faction.Enemy)
            return;

        // 주기적으로 타겟 스캔
        if (Time.time - lastScanTime >= scanInterval)
        {
            lastScanTime = Time.time;
            UpdateBehavior();
        }

        // 타겟 없을 때 주기적으로 순찰
        if (currentTarget == null && !isChasing && Time.time - lastPatrolTime >= patrolInterval)
        {
            lastPatrolTime = Time.time;
            PatrolAroundHive();
        }
    }

    void UpdateBehavior()
    {
        // 현재 타겟 유효성 검사
        if (currentTarget != null)
        {
            // 타겟이 죽었거나 제거됨
            if (currentTarget == null || currentTarget.gameObject == null)
            {
                StopChasing();
                return;
            }

            // 타겟이 시야 밖으로 벗어남
            int distanceToTarget = GetDistance(agent.q, agent.r, currentTarget.q, currentTarget.r);
            if (distanceToTarget > visionRange * 2) // 시야의 2배까지는 추격 유지
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 타겟이 너무 멀어짐. 추격 중단.");
                StopChasing();
                return;
            }

            // 하이브 범위를 벗어나면 추격 중단
            if (!IsWithinHiveRange(currentTarget.q, currentTarget.r))
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 타겟이 하이브 범위 밖. 추격 중단.");
                StopChasing();
                ReturnToHive();
                return;
            }

            // 공격 범위 내이면 공격
            bool canAttack = combat != null && combat.CanAttack();

            if (distanceToTarget <= attackRange)
            {
                if (canAttack)
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
            if (canAttack)
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
            // 타겟 없음 - 새로운 타겟 탐색
            currentTarget = FindNearestPlayerUnit();
            
            if (currentTarget != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 새 타겟 발견: {currentTarget.name}");
                isChasing = true;
                isPatrolling = false;
            }
            else
            {
                // 타겟 없음
                if (isChasing)
                {
                    // 추격 중이었으면 하이브로 복귀
                    ReturnToHive();
                }
            }
        }
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

        // 하이브 1칸 이내의 랜덤 타일 선택
        var patrolTile = GetRandomTileAroundHive();
        if (patrolTile != null)
        {
            var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
            var path = Pathfinder.FindPath(startTile, patrolTile);
            
            if (path != null && path.Count > 0)
            {
                controller.agent = agent;
                controller.SetPath(path);
                isPatrolling = true;

                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 하이브 근처 순찰: ({patrolTile.q}, {patrolTile.r})");
            }
        }
    }

    /// <summary>
    /// 하이브 1칸 이내의 랜덤 타일 가져오기
    /// </summary>
    HexTile GetRandomTileAroundHive()
    {
        if (homeHive == null || TileManager.Instance == null) return null;

        var possibleTiles = new List<HexTile>();

        // 하이브를 중심으로 1칸 이내의 모든 타일 수집
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
    /// 시야 범위 내 가장 가까운 플레이어 유닛 찾기
    /// </summary>
    UnitAgent FindNearestPlayerUnit()
    {
        if (TileManager.Instance == null) return null;

        UnitAgent nearestWorker = null; // 가장 가까운 일반 유닛 (꿀벌)
        UnitAgent nearestHive = null; // 가장 가까운 플레이어 하이브
        int minWorkerDistance = int.MaxValue;
        int minHiveDistance = int.MaxValue;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit == agent) continue;
            
            // 플레이어 유닛만
            if (unit.faction != Faction.Player) continue;

            // 무적 유닛은 제외 ?
            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null && combat.isInvincible)
            {
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] {unit.name}은(는) 무적 상태라 타겟에서 제외");
                continue;
            }

            int distance = GetDistance(agent.q, agent.r, unit.q, unit.r);

            // 시야 범위 내
            if (distance <= visionRange)
            {
                // 하이브 활동 범위 내의 타겟만
                if (IsWithinHiveRange(unit.q, unit.r))
                {
                    // 플레이어 하이브 체크
                    var hive = unit.GetComponent<Hive>();
                    if (hive != null)
                    {
                        // 하이브는 후순위
                        if (distance < minHiveDistance)
                        {
                            minHiveDistance = distance;
                            nearestHive = unit;
                        }
                    }
                    else
                    {
                        // 일반 유닛 (꿀벌) - 최우선 타겟
                        if (distance < minWorkerDistance)
                        {
                            minWorkerDistance = distance;
                            nearestWorker = unit;
                        }
                    }
                }
            }
        }

        // 일반 유닛(꿀벌)이 있으면 우선 공격
        if (nearestWorker != null)
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 일반 유닛 발견! 우선 공격 대상: {nearestWorker.name}");
            return nearestWorker;
        }

        // 일반 유닛이 없으면 하이브 공격
        if (nearestHive != null)
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 플레이어 하이브 공격: {nearestHive.name}");
        }

        return nearestHive;
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
            controller.SetPath(path);
            isPatrolling = false;

            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 타겟 추격 중: {target.name}");
        }
    }

    /// <summary>
    /// 타겟 공격
    /// </summary>
    void Attack(UnitAgent target)
    {
        if (target == null || combat == null) return;

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
                if (showDebugLogs)
                    Debug.Log($"[Enemy AI] 타겟 처치!");
                StopChasing();
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[Enemy AI] 공격 쿨타임 중...");
        }
    }

    /// <summary>
    /// 추격 중단
    /// </summary>
    void StopChasing()
    {
        currentTarget = null;
        isChasing = false;

        // 이동 중단
        if (controller != null)
        {
            controller.ClearPath();
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
            controller.SetPath(path);

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
}
