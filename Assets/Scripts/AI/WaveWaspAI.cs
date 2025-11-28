using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 웨이브 말벌 전용 AI (일반 EnemyAI와 완전 분리)
/// - 여왕벌 위치로 이동
/// - 이동 중 적 발견 시 공격
/// - 공격 → 회피 → 적 탐색 반복
/// - 일벌 우선, 하이브는 2순위
/// </summary>
public class WaveWaspAI : MonoBehaviour
{
    [Header("AI 설정")]
    public int visionRange = 25; // 시야 범위
    public float scanInterval = 0.5f; // 적 탐색 주기
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    private UnitAgent agent;
    private UnitController controller;
    private CombatUnit combat;
    
    private UnitAgent currentTarget; // 현재 공격 대상
    private UnitAgent queenBee; // 여왕벌 (목표)
    private float lastScanTime;
    private Coroutine rescanAfterCooldownRoutine = null;
    
    void Awake()
    {
        agent = GetComponent<UnitAgent>();
        controller = GetComponent<UnitController>();
        combat = GetComponent<CombatUnit>();
    }
    
    void Start()
    {
        lastScanTime = Time.time;
        StartCoroutine(WaveWaspBehavior());
    }
    
    /// <summary>
    /// 웨이브 말벌 메인 루프
    /// </summary>
    IEnumerator WaveWaspBehavior()
    {
        // 초기 대기 (생성 직후 안정화)
        yield return new WaitForSeconds(0.5f);
        
        if (showDebugLogs)
            Debug.Log($"[Wave Wasp] AI 시작: {agent.name}");
        
        while (true)
        {
            yield return new WaitForSeconds(scanInterval);
            
            // 1. 여왕벌 찾기 (없으면 플레이어 하이브 찾기)
            if (queenBee == null || queenBee.gameObject == null)
            {
                queenBee = FindQueenBee();
                
                if (queenBee == null)
                {
                    // 여왕벌 없으면 플레이어 하이브 찾기
                    UnitAgent playerHive = FindPlayerHive();
                    
                    if (playerHive != null)
                    {
                        queenBee = playerHive; // 임시로 하이브를 목표로 설정
                        if (showDebugLogs)
                            Debug.Log($"[Wave Wasp] 여왕벌 없음. 플레이어 하이브 발견: ({playerHive.q}, {playerHive.r})");
                    }
                    else
                    {
                        if (showDebugLogs)
                            Debug.Log($"[Wave Wasp] 여왕벌/하이브 없음. 계속 탐색...");
                        continue; // 다음 프레임에 다시 시도
                    }
                }
                else
                {
                    if (showDebugLogs)
                        Debug.Log($"[Wave Wasp] 여왕벌 발견! 위치: ({queenBee.q}, {queenBee.r})");
                }
            }
            
            // 2. 적 탐색 먼저
            UnitAgent nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy != null)
            {
                // 적 발견 → 타겟 설정
                if (currentTarget != nearestEnemy)
                {
                    currentTarget = nearestEnemy;
                    if (showDebugLogs)
                        Debug.Log($"[Wave Wasp] 새 타겟 설정: {currentTarget.name}");
                }
            }
            else
            {
                // 적 없음 → 타겟 해제
                if (currentTarget != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[Wave Wasp] 적 없음. 타겟 해제.");
                    currentTarget = null;
                }
            }
            
            // 3. 행동 결정
            if (currentTarget != null && currentTarget.gameObject != null)
            {
                // 타겟 공격
                AttackTarget();
            }
            else
            {
                // 여왕벌/하이브로 이동
                MoveToQueenBee();
            }
        }
    }
    
    /// <summary>
    /// 타겟 공격
    /// </summary>
    void AttackTarget()
    {
        if (currentTarget == null || currentTarget.gameObject == null)
        {
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 타겟 사라짐. 재탐색.");
            currentTarget = null;
            return;
        }

        // 여왕이 하이브 위에 있으면 하이브로 공격 전환
        var redirected = RedirectTargetIfQueenOnHive(currentTarget);
        if (redirected != currentTarget)
        {
            currentTarget = redirected;
        }

        int distance = GetDistance(agent.q, agent.r, currentTarget.q, currentTarget.r);
        
        // 하이브는 인접 타일에서 공격
        var targetHive = currentTarget.GetComponent<Hive>();
        int attackDistance = (targetHive != null) ? 1 : 0;
        
        // 공격 범위 내
        if (distance <= attackDistance)
        {
            // 공격 가능하면 공격
            if (combat != null && combat.CanAttack())
            {
                var targetCombat = currentTarget.GetComponent<CombatUnit>();
                if (targetCombat != null)
                {
                    bool attacked = combat.TryAttack(targetCombat);
                    
                    if (attacked)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[Wave Wasp] {currentTarget.name} 공격! HP: {targetCombat.health}");
                        
                        // 타겟 죽음 체크
                        if (targetCombat.health <= 0)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[Wave Wasp] {currentTarget.name} 처치!");
                            currentTarget = null;
                        }
                    }
                }
            }
            else
            {
                // 쿨타임 → 회피
                Evade();
                // 예약된 재탐색 시작
                StartRescanAfterCooldown();
            }
        }
        else
        {
            // 추격
            ChaseTarget();
        }
    }
    
    /// <summary>
    /// 타겟 추격
    /// </summary>
    void ChaseTarget()
    {
        if (currentTarget == null || controller == null) return;
        
        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        var targetTile = TileManager.Instance?.GetTile(currentTarget.q, currentTarget.r);
        
        if (startTile == null || targetTile == null) return;
        
        var path = Pathfinder.FindPath(startTile, targetTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            controller.SetPathSimple(path);
            
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] {currentTarget.name} 추격 중. 거리: {GetDistance(agent.q, agent.r, currentTarget.q, currentTarget.r)}");
        }
    }
    
    /// <summary>
    /// 회피 기동
    /// </summary>
    void Evade()
    {
        if (controller == null) return;
        
        // 이미 이동 중이면 스킵
        if (controller.IsMoving()) return;
        
        controller.MoveWithinCurrentTile();
        
        if (showDebugLogs)
            Debug.Log($"[Wave Wasp] 회피 기동!");
    }
    
    /// <summary>
    /// 여왕벌로 이동
    /// </summary>
    void MoveToQueenBee()
    {
        if (queenBee == null || controller == null || agent == null)
        {
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 여왕벌 또는 컴포넌트 없음.");
            return;
        }

        // 여왕이 하이브 위에 있으면 하이브를 목표로 전환
        var redirected = RedirectTargetIfQueenOnHive(queenBee);
        if (redirected != queenBee)
        {
            currentTarget = redirected;
        }

        // 여왕벌과의 거리
        int distance = GetDistance(agent.q, agent.r, queenBee.q, queenBee.r);
        
        if (showDebugLogs)
            Debug.Log($"[Wave Wasp] 여왕벌까지 거리: {distance}");
        
        // 이미 인접하면 대기
        if (distance <= 1)
        {
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 여왕벌 인접. 대기 중...");
            return;
        }
        
        // 경로 계산
        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        var queenTile = TileManager.Instance?.GetTile(queenBee.q, queenBee.r);
        
        if (startTile == null || queenTile == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[Wave Wasp] 타일 없음! Start: {startTile}, Queen: {queenTile}");
            return;
        }
        
        var path = Pathfinder.FindPath(startTile, queenTile);
        
        if (path != null && path.Count > 0)
        {
            controller.agent = agent;
            controller.SetPathSimple(path);
            
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 여왕벌로 이동 시작. 경로: {path.Count}타일");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"[Wave Wasp] 경로 없음! ({agent.q}, {agent.r}) → ({queenBee.q}, {queenBee.r})");
        }
    }

    UnitAgent RedirectTargetIfQueenOnHive(UnitAgent queen)
    {
        if (queen == null || queen.homeHive == null) return queen;
        if (queen.q == queen.homeHive.q && queen.r == queen.homeHive.r)
        {
            var hiveAgent = queen.homeHive.GetComponent<UnitAgent>();
            if (hiveAgent != null)
            {
                if (showDebugLogs) Debug.Log("[Wave Wasp] 여왕이 하이브 위 → 하이브로 목표 전환");
                return hiveAgent;
            }
        }
        return queen;
    }
    
    /// <summary>
    /// 가장 가까운 적 찾기 (일벌 우선, 하이브 2순위)
    /// </summary>
    UnitAgent FindNearestEnemy()
    {
        if (TileManager.Instance == null) return null;
        
        UnitAgent closestWorker = null;
        UnitAgent closestHive = null;
        int minWorkerDist = int.MaxValue;
        int minHiveDist = int.MaxValue;
        List<UnitAgent> tankCandidates = new List<UnitAgent>();
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit == agent) continue;
            
            // 플레이어만
            if (unit.faction != Faction.Player) continue;
            
            // 무적 제외
            var combatUnit = unit.GetComponent<CombatUnit>();
            if (combatUnit != null && combatUnit.isInvincible) continue;
            
            int distance = GetDistance(agent.q, agent.r, unit.q, unit.r);
            
            // 시야 범위 내
            if (distance <= visionRange)
            {
                var hive = unit.GetComponent<Hive>();
                
                if (hive != null)
                {
                    // 하이브
                    if (distance < minHiveDist)
                    {
                        minHiveDist = distance;
                        closestHive = unit;
                    }
                }
                else
                {
                    // 일벌
                    // 우선 탱커 후보 수집
                    var ra = unit.GetComponent<RoleAssigner>();
                    if (ra != null && ra.role == RoleType.Tank)
                    {
                        tankCandidates.Add(unit);
                    }

                    if (distance < minWorkerDist)
                    {
                        minWorkerDist = distance;
                        closestWorker = unit;
                    }
                }
            }
        }

        // 탱커가 보이면 탱커 우선 (가장 가까운 탱커)
        if (tankCandidates.Count > 0)
        {
            UnitAgent closestTank = null;
            int minTankDist = int.MaxValue;
            foreach (var t in tankCandidates)
            {
                int d = GetDistance(agent.q, agent.r, t.q, t.r);
                if (d < minTankDist)
                {
                    minTankDist = d;
                    closestTank = t;
                }
            }
            if (closestTank != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[Wave Wasp] 탱커형 일벌 발견: {closestTank.name}, 거리: {minTankDist}");
                return closestTank;
            }
        }
        
        // 일벌 우선
        if (closestWorker != null)
        {
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 일벌 발견: {closestWorker.name}, 거리: {minWorkerDist}");
            return closestWorker;
        }
        
        // 하이브 2순위
        if (closestHive != null)
        {
            if (showDebugLogs)
                Debug.Log($"[Wave Wasp] 하이브 발견: {closestHive.name}, 거리: {minHiveDist}");
            return closestHive;
        }
        
        return null;
    }
    
    /// <summary>
    /// 여왕벌 찾기
    /// </summary>
    UnitAgent FindQueenBee()
    {
        if (TileManager.Instance == null) return null;
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            
            if (unit.faction == Faction.Player && unit.isQueen)
            {
                if (showDebugLogs)
                    Debug.Log($"[Wave Wasp] 여왕벌 발견: ({unit.q}, {unit.r})");
                return unit;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 플레이어 하이브 찾기
    /// </summary>
    UnitAgent FindPlayerHive()
    {
        if (TileManager.Instance == null) return null;
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            
            // 플레이어 하이브 찾기
            if (unit.faction == Faction.Player)
            {
                var hive = unit.GetComponent<Hive>();
                if (hive != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[Wave Wasp] 플레이어 하이브 발견: ({unit.q}, {unit.r})");
                    return unit;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 거리 계산
    /// </summary>
    int GetDistance(int q1, int r1, int q2, int r2)
    {
        return Pathfinder.AxialDistance(q1, r1, q2, r2);
    }

    // Schedule a rescan when attack cooldown ends so we can re-evaluate tanks
    void StartRescanAfterCooldown()
    {
        if (combat == null) return;
        float remaining = combat.GetAttackCooldownRemaining();
        if (remaining <= 0f) return;

        if (rescanAfterCooldownRoutine != null)
        {
            StopCoroutine(rescanAfterCooldownRoutine);
            rescanAfterCooldownRoutine = null;
        }
        rescanAfterCooldownRoutine = StartCoroutine(RescanAfterCooldownRoutine(remaining));
    }

    System.Collections.IEnumerator RescanAfterCooldownRoutine(float wait)
    {
        yield return new WaitForSeconds(wait + 0.01f);
        rescanAfterCooldownRoutine = null;

        // immediate re-evaluation: find nearest enemy and set as currentTarget
        var found = FindNearestEnemy();
        if (found != null)
        {
            currentTarget = found;
            if (showDebugLogs) Debug.Log($"[Wave Wasp] 쿨다운 후 재탐색: 새로운 타겟 {currentTarget.name}");
            // if in range and can attack, attempt attack immediately
            AttackTarget();
        }
    }
}
