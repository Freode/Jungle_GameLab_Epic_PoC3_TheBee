using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일꾼 꿀벌 상태
/// </summary>
public enum WorkerState
{
    Idle,
    Moving,
    Gathering,
    Attacking,
    FollowingQueen,
    Scouting
}

/// <summary>
/// 일꾼 꿀벌 전용 행동 컨트롤러 (State Machine)
/// </summary>
public class WorkerBehaviorController : UnitBehaviorController
{
    [Header("Worker State Machine")]
    public WorkerState currentState = WorkerState.Idle;
    private WorkerState previousState = WorkerState.Idle;

    [Header("Worker Settings")]
    [SerializeField] private float idleRandomMoveMinDelay = 0.2f;
    [SerializeField] private float idleRandomMoveMaxDelay = 1.5f;
    [SerializeField] private float gatheringDuration = 3.0f;
    [SerializeField] private float scoutRandomMoveMinDelay = 0.2f;
    [SerializeField] private float scoutRandomMoveMaxDelay = 1.5f;
    [SerializeField] private float workerEnemyDetectionInterval = 0.2f;
    
    private Coroutine currentStateCoroutine = null;
    private List<Coroutine> activeCoroutines = new List<Coroutine>(); // ✅ 실행 중인 모든 코루틴 추적
    private bool isCarryingResource = false;
    private HexTile lastGatherTile = null; // ✅ 마지막 자원 채취 타일
    public static bool isAutoSearchNearResource = false;      // 자동 자원 탐색 모드

    protected override void Start()
    {
        base.Start();
        
        // ✅ gatherColor를 UnitAgent에 전달
        if (agent != null)
        {
            agent.gatherColor = gatherColor;
        }
        
        TransitionToState(WorkerState.Idle);
    }

    /// <summary>
    /// 상태 전환
    /// </summary>
    void TransitionToState(WorkerState newState)
    {
        // ✅ 1. 이전 상태의 모든 코루틴 중지
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }
        
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();

        if (currentState != WorkerState.Attacking)
        {
            previousState = currentState;
        }

        // ✅ 3. 여왕벌 쫓기/정찰 상태로 전환 시 자원 채취 타일 초기화
        if (newState == WorkerState.FollowingQueen || newState == WorkerState.Scouting)
        {
            lastGatherTile = null;
            Debug.Log($"[Worker State] 자원 채취 타일 초기화 (상태: {newState})");
        }

        currentState = newState;

        Debug.Log($"[Worker State] {agent.name}: {previousState} → {currentState}");

        switch (newState)
        {
            case WorkerState.Idle:
                currentStateCoroutine = StartCoroutine(IdleStateCoroutine());
                currentTaskString = "대기";
                break;
            case WorkerState.Moving:
                currentStateCoroutine = StartCoroutine(MovingStateCoroutine());
                currentTaskString = "이동";
                break;
            case WorkerState.Gathering:
                currentStateCoroutine = StartCoroutine(GatheringStateCoroutine());
                currentTaskString = "꿀 채취";
                break;
            case WorkerState.Attacking:
                currentStateCoroutine = StartCoroutine(AttackingStateCoroutine());
                currentTaskString = "공격";
                break;
            case WorkerState.FollowingQueen:
                currentStateCoroutine = StartCoroutine(FollowingQueenStateCoroutine());
                currentTaskString = "여왕벌 호위";
                break;
            case WorkerState.Scouting:
                currentStateCoroutine = StartCoroutine(ScoutingStateCoroutine());
                currentTaskString = "정찰";
                break;
        }
    }

    #region State Coroutines

    IEnumerator IdleStateCoroutine()
    {
        while (currentState == WorkerState.Idle)
        {
            var enemy = FindNearbyEnemy(1);
            if (enemy != null)
            {
                targetUnit = enemy;
                TransitionToState(WorkerState.Attacking);
                yield break;
            }

            if (isCarryingResource && agent.homeHive != null)
            {
                targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
                TransitionToState(WorkerState.Moving);
                yield break;
            }

            float waitTime = Random.Range(idleRandomMoveMinDelay, idleRandomMoveMaxDelay);
            float elapsed = 0f;

            while (elapsed < waitTime)
            {
                enemy = FindNearbyEnemy(1);
                if (enemy != null)
                {
                    targetUnit = enemy;
                    TransitionToState(WorkerState.Attacking);
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (agent != null)
            {
                // Use UnitController's intra-tile movement to keep movement speed consistent with role-based moveSpeed
                if (mover != null)
                {
                    var c = StartCoroutine(PerformMoverWithinCurrentTile());
                    activeCoroutines.Add(c);
                }
                else
                {
                    Vector3 randomPos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.3f);
                    var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));
                    activeCoroutines.Add(moveCoroutine);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MovingStateCoroutine()
    {
        // 꿀 보유 중(1번 부대)이라면 항상 하이브로 우선 복귀
        if (isCarryingResource && agent.homeHive != null && ShouldForceReturnToHive())
        {
            targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
            Debug.Log($"[Worker State] 자원 보유 → 하이브 우선 복귀: ({targetTile.q}, {targetTile.r})");
        }

        if (targetTile == null)
        {
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
        var path = Pathfinder.FindPath(currentTile, targetTile);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[Worker State] 경로 없음: ({currentTile.q}, {currentTile.r}) → ({targetTile.q}, {targetTile.r})");
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        mover.SetPath(path);

        // ✅ 타일 좌표 기반으로 도착 체크 (랜덤 위치 때문에 World 좌표는 부정확)
        float timeout = 15f;
        float elapsed = 0f;
        float checkInterval = 0.1f; // 0.1초마다 체크
        float lastCheckTime = 0f;

        while (elapsed < timeout)
        {
            // ✅ 타일 좌표가 목표 타일과 같으면 도착
            if (agent.q == targetTile.q && agent.r == targetTile.r)
            {
                Debug.Log($"[Worker State] 목표 타일 도착: ({targetTile.q}, {targetTile.r})");
                break;
            }

            // ✅ 이동이 완전히 멈췄는지 체크 (경로 큐가 비고 이동 중이 아님)
            if (!mover.IsMoving())
            {
                // 타일 좌표는 목표와 다른데 이동이 멈췄으면 타임아웃
                if (agent.q != targetTile.q || agent.r != targetTile.r)
                {
                    Debug.LogWarning($"[Worker State] 이동 중단됨: 현재 ({agent.q}, {agent.r}), 목표 ({targetTile.q}, {targetTile.r})");
                    break;
                }
            }

            elapsed += Time.deltaTime;
            lastCheckTime += Time.deltaTime;
            
            // 체크 간격마다 로그
            if (lastCheckTime >= checkInterval)
            {
                lastCheckTime = 0f;
                // Debug.Log($"[Worker State] 이동 중: ({agent.q}, {agent.r}) → ({targetTile.q}, {targetTile.r}), IsMoving: {mover.IsMoving()}");
            }
            
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning($"[Worker State] 이동 타임아웃: 현재 ({agent.q}, {agent.r}), 목표 ({targetTile.q}, {targetTile.r})");
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        var finalEnemy = FindNearbyEnemy(1);
        if (finalEnemy != null)
        {
            targetUnit = finalEnemy;
            TransitionToState(WorkerState.Attacking);
            yield break;
        }

        // ✅ 4. 자원 들고 하이브 도착 시 처리
        if (isCarryingResource && agent.homeHive != null &&
            targetTile.q == agent.homeHive.q && targetTile.r == agent.homeHive.r)
        {
            // 자원 전달
            if (HiveManager.Instance != null)
            {
                HiveManager.Instance.AddResources(gatherAmount);
                Debug.Log($"[Worker State] 자원 전달: {gatherAmount}");
            }
            isCarryingResource = false;
            
            // ✅ UnitAgent에 자원 보유 상태 해제 (색상 복원)
            if (agent != null)
            {
                agent.SetCarryingResource(false);
            }

            // ✅ 수동 명령(페르몬)이 있으면 지정된 수동 목표로 복귀
            if (agent.hasManualOrder)
            {
                if (agent.hasManualTarget)
                {
                    int manualQ = agent.manualTargetCoord.x;
                    int manualR = agent.manualTargetCoord.y;

                    int distanceToHive = Pathfinder.AxialDistance(
                        agent.homeHive.q, agent.homeHive.r,
                        manualQ, manualR
                    );

                    if (distanceToHive <= activityRadius)
                    {
                        var manualTile = TileManager.Instance.GetTile(manualQ, manualR);
                        if (manualTile != null)
                        {
                            Debug.Log($"[Worker State] 수동 명령: 지정 위치로 복귀 ({manualQ}, {manualR})");
                            targetTile = manualTile;
                            TransitionToState(WorkerState.Moving);
                            yield break;
                        }
                    }
                    else
                    {
                        Debug.Log($"[Worker State] 수동 목표가 활동 범위 밖: {distanceToHive}/{activityRadius}, 수동 명령 해제");
                    }
                }

                // 기존 로직과의 호환을 위해 페르몬 위치 조회 (수동 목표가 없거나 실패 시)
                if (PheromoneManager.Instance != null)
                {
                    // ✅ 부대 정보 가져오기
                    WorkerSquad workerSquad = WorkerSquad.None;

                    if (HiveManager.Instance != null)
                    {
                        foreach (WorkerSquad squad in System.Enum.GetValues(typeof(WorkerSquad)))
                        {
                            if (squad == WorkerSquad.None) continue;

                            var squadWorkers = HiveManager.Instance.GetSquadWorkers(squad);
                            if (squadWorkers.Contains(agent))
                            {
                                workerSquad = squad;
                                break;
                            }
                        }
                    }

                    var pheromonePos = PheromoneManager.Instance.GetCurrentPheromonePosition(workerSquad);

                    if (pheromonePos.HasValue)
                    {
                        int pheroQ = pheromonePos.Value.x;
                        int pheroR = pheromonePos.Value.y;

                        int distanceToHive = Pathfinder.AxialDistance(
                            agent.homeHive.q, agent.homeHive.r,
                            pheroQ, pheroR
                        );

                        if (distanceToHive <= activityRadius)
                        {
                            Debug.Log($"[Worker State] 페르몬 명령: 페르몬 위치로 복귀 ({pheroQ}, {pheroR})");

                            targetTile = TileManager.Instance.GetTile(pheroQ, pheroR);
                            if (targetTile != null)
                            {
                                TransitionToState(WorkerState.Moving);
                                yield break;
                            }
                        }
                        else
                        {
                            Debug.Log($"[Worker State] 페르몬 위치가 활동 범위 밖: {distanceToHive}/{activityRadius}, 수동 명령 해제");
                        }
                    }
                    else
                    {
                        Debug.Log($"[Worker State] 페르몬 위치 없음: 수동 명령 해제");
                    }
                }

                // 유효한 수동 목적지가 없으면 수동 명령 해제
                agent.hasManualOrder = false;
                agent.hasManualTarget = false;
            }
            // ✅ 수동 명령이 없으면 Idle
            else if (!agent.hasManualOrder)
            {
                Debug.Log($"[Worker State] 자동 모드: Idle 상태");
                lastGatherTile = null;
                TransitionToState(WorkerState.Idle);
                yield break;
            }
            // ✅ 수동 명령이지만 페르몬 위치가 없으면 Idle
            else
            {
                Debug.Log($"[Worker State] 페르몬 명령이지만 페르몬 위치 없음: Idle 상태");
                TransitionToState(WorkerState.Idle);
                yield break;
            }
        }

        // 자원 있는 타일 + 하이브 존재
        if (targetTile.resourceAmount > 0 && agent.homeHive != null && !agent.homeHive.isRelocating)
        {
            TransitionToState(WorkerState.Gathering);
            yield break;
        }

        TransitionToState(WorkerState.Idle);
    }

    // 1번 부대(채집)이고 자원 보유 중이면 하이브를 우선하게 만들기
    private bool ShouldForceReturnToHive()
    {
        if (!isCarryingResource) return false;
        if (HiveManager.Instance == null) return false;

        var squadWorkers = HiveManager.Instance.GetSquadWorkers(WorkerSquad.Squad1);
        return squadWorkers.Contains(agent);
    }

    IEnumerator GatheringStateCoroutine()
    {
        if (targetTile == null || targetTile.resourceAmount <= 0)
        {
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        // ✅ 2. 현재 자원 채취 타일 기억
        lastGatherTile = targetTile;
        Debug.Log($"[Worker State] 자원 채취 타일 저장: ({lastGatherTile.q}, {lastGatherTile.r})");

        Debug.Log($"[Worker State] 자원 채취 시작: ({targetTile.q}, {targetTile.r})");

        // Ensure UnitController finished arriving to avoid visual teleport
        // Wait one frame then wait until mover reports not moving
        yield return null;
        if (mover != null)
        {
            int safety = 0;
            while (mover.IsMoving() && safety < 60)
            {
                // wait up to ~1s (60 frames)
                yield return null;
                safety++;
            }

            // stop any small local coroutines we started to avoid conflicting transforms
            foreach (var c in activeCoroutines)
            {
                if (c != null) StopCoroutine(c);
            }
            activeCoroutines.Clear();

            // clear mover path so controller won't start new tile moves
            mover.ClearPath();

            // Ensure logical tile coords are set (some systems rely on agent.q/r)
            agent.SetPosition(targetTile.q, targetTile.r);

            // short settle pause to let other systems finish without moving the transform
            yield return new WaitForSeconds(0.05f);
        }

        float elapsed = 0f;
        while (elapsed < gatheringDuration)
        {
            var enemy = FindNearbyEnemy(1);
            if (enemy != null)
            {
                targetUnit = enemy;
                TransitionToState(WorkerState.Attacking);
                yield break;
            }

            if (elapsed % 0.5f < 0.1f)
            {
                if (mover != null)
                {
                    var c = StartCoroutine(PerformMoverWithinCurrentTile());
                    activeCoroutines.Add(c);
                }
                else
                {
                    Vector3 randomPos = TileHelper.GetRandomPositionInTile(targetTile.q, targetTile.r, agent.hexSize, 0.2f);
                    var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.3f));
                    activeCoroutines.Add(moveCoroutine);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ✅ 5. 자원 채취 시도
        int gathered = targetTile.TakeResource(gatherAmount);
        if (gathered > 0)
        {
            isCarryingResource = true;

            // ✅ UnitAgent에 자원 보유 상태 전달 (색상 변경)
            if (agent != null)
            {
                agent.SetCarryingResource(true);
            }

            Debug.Log($"[Worker State] 자원 채취 완료: {gathered}");
        }
        else
        {
            // ✅ 5. 자원 채취 실패 (자원량 0) - 우선 페르몬 위치로 이동, 없으면 하이브로 복귀
            Debug.Log($"[Worker State] 자원 채취 실패 (자원 없음), 페르몬 또는 하이브로 복귀 시도");
            lastGatherTile = null; // 타일 정보 초기화

            bool movedToPheromone = false;
            if (!agent.hasManualOrder && PheromoneManager.Instance != null)
            {
                WorkerSquad workerSquad = WorkerSquad.None;
                if (HiveManager.Instance != null)
                {
                    foreach (WorkerSquad squad in System.Enum.GetValues(typeof(WorkerSquad)))
                    {
                        if (squad == WorkerSquad.None) continue;
                        var squadWorkers = HiveManager.Instance.GetSquadWorkers(squad);
                        if (squadWorkers.Contains(agent))
                        {
                            workerSquad = squad;
                            break;
                        }
                    }
                }

                var pheromonePos = PheromoneManager.Instance.GetCurrentPheromonePosition(workerSquad);
                if (pheromonePos.HasValue)
                {
                    int pheroQ = pheromonePos.Value.x;
                    int pheroR = pheromonePos.Value.y;

                    if (agent.homeHive != null)
                    {
                        int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, pheroQ, pheroR);
                        if (distanceToHive <= activityRadius)
                        {
                            Debug.Log($"[Worker State] 채취 실패 후 페르몬 위치로 이동 ({pheroQ}, {pheroR})");
                            targetTile = TileManager.Instance.GetTile(pheroQ, pheroR);
                            TransitionToState(WorkerState.Moving);
                            movedToPheromone = true;
                        }
                    }
                    else
                    {
                        // no home hive, allow moving to pheromone regardless
                        Debug.Log($"[Worker State] 채취 실패 후 페르몬 위치로 이동 (무주택): ({pheroQ}, {pheroR})");
                        targetTile = TileManager.Instance.GetTile(pheroQ, pheroR);
                        TransitionToState(WorkerState.Moving);
                        movedToPheromone = true;
                    }
                }
            }

            if (!movedToPheromone)
            {
                if (agent.homeHive != null)
                {
                    targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
                    TransitionToState(WorkerState.Moving);
                }
                else
                {
                    TransitionToState(WorkerState.Idle);
                }
            }
            yield break;
        }

        // After gathering, if the gathered tile is now empty, prefer assigned pheromone location (if any and in range)
        if (targetTile != null && targetTile.resourceAmount <= 0 && !agent.hasManualOrder && PheromoneManager.Instance != null)
        {
            WorkerSquad workerSquad = WorkerSquad.None;
            if (HiveManager.Instance != null)
            {
                foreach (WorkerSquad squad in System.Enum.GetValues(typeof(WorkerSquad)))
                {
                    if (squad == WorkerSquad.None) continue;
                    var squadWorkers = HiveManager.Instance.GetSquadWorkers(squad);
                    if (squadWorkers.Contains(agent))
                    {
                        workerSquad = squad;
                        break;
                    }
                }
            }

            var pheromonePos = PheromoneManager.Instance.GetCurrentPheromonePosition(workerSquad);
            if (pheromonePos.HasValue)
            {
                int pheroQ = pheromonePos.Value.x;
                int pheroR = pheromonePos.Value.y;

                if (agent.homeHive != null)
                {
                    int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, pheroQ, pheroR);
                    if (distanceToHive <= activityRadius)
                    {
                        Debug.Log($"[Worker State] 자원 고갈: 페르몬 위치로 이동 ({pheroQ}, {pheroR})");
                        targetTile = TileManager.Instance.GetTile(pheroQ, pheroR);
                        TransitionToState(WorkerState.Moving);
                        yield break;
                    }
                }
                else
                {
                    targetTile = TileManager.Instance.GetTile(pheroQ, pheroR);
                    TransitionToState(WorkerState.Moving);
                    yield break;
                }
            }
        }

        if (agent.homeHive != null)
        {
            targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
            TransitionToState(WorkerState.Moving);
        }
        else
        {
            TransitionToState(WorkerState.Idle);
        }
    }

    IEnumerator AttackingStateCoroutine()
    {
        if (targetUnit == null)
        {
            TransitionToState(previousState);
            yield break;
        }

        var myCombat = agent.GetComponent<CombatUnit>();
        if (myCombat == null)
        {
            TransitionToState(previousState);
            yield break;
        }

        var enemyCombat = targetUnit.GetComponent<CombatUnit>();
        if (enemyCombat == null)
        {
            TransitionToState(previousState);
            yield break;
        }

        Debug.Log($"[Worker State] 공격 시작: {targetUnit.name}");

        while (targetUnit != null && enemyCombat != null && enemyCombat.health > 0)
        {
            if (agent.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, agent.q, agent.r);
                if (distanceToHive > activityRadius)
                {
                    Debug.Log($"[Worker State] 활동 범위 벗어남");
                    targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
                    TransitionToState(WorkerState.Moving);
                    yield break;
                }
            }

            if (myCombat.CanAttack())
            {
                bool attacked = myCombat.TryAttack(enemyCombat);
                if (attacked)
                {
                    Debug.Log($"[Worker State] 공격 성공: {myCombat.attack} 데미지");
                }

                var evadeCoroutine = StartCoroutine(EvadeCoroutine());
                activeCoroutines.Add(evadeCoroutine);
                yield return evadeCoroutine;

                // After attack cooldown, re-evaluate nearby enemies and prefer unit targets (e.g., wasps) over structures
                if (currentState == WorkerState.Attacking)
                {
                    var higherPriority = FindNearbyEnemy(1);
                    if (higherPriority != null && higherPriority != targetUnit)
                    {
                        var higherCombat = higherPriority.GetComponent<CombatUnit>();
                        if (higherCombat != null && higherCombat.health > 0)
                        {
                            Debug.Log($"[Worker State] 더 우선순위 적 발견: {higherPriority.name} (교체)");
                            targetUnit = higherPriority;
                            enemyCombat = higherCombat; // update reference to new target's CombatUnit
                            // continue to next loop iteration and attack new target
                            continue;
                        }
                    }

                    // If no higher priority found, continue attacking current target if still valid
                    if (targetUnit != null)
                    {
                        // refresh enemyCombat in case it changed externally
                        enemyCombat = targetUnit.GetComponent<CombatUnit>();
                        if (enemyCombat == null || enemyCombat.health <= 0)
                        {
                            break;
                        }

                        continue;
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return null;
        }

        Debug.Log($"[Worker State] 공격 종료, {previousState}로 복귀");
        TransitionToState(previousState);
    }

    IEnumerator EvadeCoroutine()
    {
        if (combat == null) yield break;

        float evadeTime = combat.attackCooldown;
        float elapsed = 0f;

        while (elapsed < evadeTime)
        {
            if (elapsed % 0.3f < 0.1f)
            {
                if (mover != null)
                {
                    var c = StartCoroutine(PerformMoverWithinCurrentTile());
                    activeCoroutines.Add(c);
                }
                else
                {
                    Vector3 evadePos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.4f);
                    var moveCoroutine = StartCoroutine(MoveToPositionSmooth(evadePos, 0.2f));
                    activeCoroutines.Add(moveCoroutine);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FollowingQueenStateCoroutine()
    {
        UnitAgent queen = FindQueenInScene();
        if (queen == null)
        {
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        Debug.Log($"[Worker State] 여왕벌 추적 시작");

        // ✅ 3. 여왕벌의 이전 위치 저장
        int lastQueenQ = queen.q;
        int lastQueenR = queen.r;

        while (currentState == WorkerState.FollowingQueen)
        {
            // 적 감지
            var enemy = FindNearbyEnemy(1);
            if (enemy != null)
            {
                targetUnit = enemy;
                TransitionToState(WorkerState.Attacking);
                yield break;
            }

            // 여왕벌 위치 확인
            queen = FindQueenInScene();
            if (queen == null)
            {
                TransitionToState(WorkerState.Idle);
                yield break;
            }

            // ✅ 3. 여왕벌 위치가 변경되었는지 체크
            bool queenMoved = (queen.q != lastQueenQ || queen.r != lastQueenR);
            
            if (queenMoved)
            {
                Debug.Log($"[Worker State] 여왕벌 위치 변경 감지: ({lastQueenQ}, {lastQueenR}) → ({queen.q}, {queen.r})");
                lastQueenQ = queen.q;
                lastQueenR = queen.r;
            }

            // 여왕벌과 같은 타일이면 대기
            if (agent.q == queen.q && agent.r == queen.r)
            {
                yield return new WaitForSeconds(0.5f); // ✅ 대기 시간 단축 (1s → 0.5s)
                continue;
            }

            // ✅ 3. 여왕벌 위치가 변경되었거나 아직 도착하지 않았으면 경로 재계산
            if (queenMoved || !mover.IsMoving())
            {
                var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
                var queenTile = TileManager.Instance.GetTile(queen.q, queen.r);
                var path = Pathfinder.FindPath(currentTile, queenTile);

                if (path != null && path.Count > 0)
                {
                    mover.SetPath(path);
                    Debug.Log($"[Worker State] 여왕벌 추적 경로 갱신: ({agent.q}, {agent.r}) → ({queen.q}, {queen.r})");
                }
            }

            yield return new WaitForSeconds(0.3f); // ✅ 체크 간격 단축 (1s → 0.3s)
        }
    }

    IEnumerator ScoutingStateCoroutine()
    {
        if (targetTile == null)
        {
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        Debug.Log($"[Worker State] 정찰 시작: ({targetTile.q}, {targetTile.r})");

        var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
        var path = Pathfinder.FindPath(currentTile, targetTile);

        if (path != null && path.Count > 0)
        {
            mover.SetPath(path);

            // ✅ 타일 좌표 기반으로 도착 체크
            float timeout = 15f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // ✅ 타일 좌표가 목표와 같으면 도착
                if (agent.q == targetTile.q && agent.r == targetTile.r)
                {
                    Debug.Log($"[Worker State] 정찰 목표 도착: ({targetTile.q}, {targetTile.r})");
                    break;
                }

                // ✅ 이동이 멈췄는지 체크
                if (!mover.IsMoving())
                {
                    if (agent.q != targetTile.q || agent.r != targetTile.r)
                    {
                        Debug.LogWarning($"[Worker State] 정찰 이동 중단: 현재 ({agent.q}, {agent.r}), 목표 ({targetTile.q}, {targetTile.r})");
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
            {
                Debug.LogWarning($"[Worker State] 정찰 타임아웃");
            }
        }

        var enemy = FindNearbyEnemy(1);
        if (enemy != null)
        {
            targetUnit = enemy;
            TransitionToState(WorkerState.Attacking);
            yield break;
        }

        float waitTime = Random.Range(scoutRandomMoveMinDelay, scoutRandomMoveMaxDelay);
        float waitElapsed = 0f;

        while (waitElapsed < waitTime)
        {
            enemy = FindNearbyEnemy(1);
            if (enemy != null)
            {
                targetUnit = enemy;
                TransitionToState(WorkerState.Attacking);
                yield break;
            }

            waitElapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 randomPos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.3f);
        if (mover != null)
        {
            var c = StartCoroutine(PerformMoverWithinCurrentTile());
            activeCoroutines.Add(c);
        }
        else
        {
            var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));
            activeCoroutines.Add(moveCoroutine);
        }

        yield return new WaitForSeconds(0.5f);

        if (currentState == WorkerState.Scouting)
        {
            currentStateCoroutine = StartCoroutine(ScoutingStateCoroutine());
        }
    }

    #endregion

    #region Helper Methods

    IEnumerator MoveToPositionSmooth(Vector3 targetPos, float duration)
    {
        // simple fixed-duration smoothing (do not sync to mover.moveSpeed) to preserve previous visual behavior
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float travelTime = Mathf.Max(0.0001f, duration);

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }

    /// <summary>
    /// 주변 6방향 타일 중 자원이 있고 활동 범위 내에 있는 타일 찾기
    /// </summary>
    /// <param name="centerQ">중심 타일의 q 좌표</param>
    /// <param name="centerR">중심 타일의 r 좌표</param>
    /// <returns>자원이 있는 타일 (없으면 null)</returns>
    HexTile FindNearbyResourceTile(int centerQ, int centerR)
    {
        if (agent.homeHive == null)
        {
            return null;
        }

        // 6방향 이웃 타일 탐색
        foreach (var direction in HexTile.NeighborDirections)
        {
            int neighborQ = centerQ + direction.x;
            int neighborR = centerR + direction.y;

            // 타일 가져오기
            HexTile neighborTile = TileManager.Instance.GetTile(neighborQ, neighborR);
            if (neighborTile == null)
            {
                continue;
            }

            // 자원이 있는지 확인
            if (neighborTile.resourceAmount <= 0)
            {
                continue;
            }

            // 활동 범위 내에 있는지 확인
            int distanceToHive = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r,
                neighborQ, neighborR
            );

            if (distanceToHive <= activityRadius)
            {
                return neighborTile;
            }
        }

        return null;
    }

    #endregion

    #region Public Methods

    public override void IssueCommandToTile(HexTile tile)
    {
        if (tile == null) return;

        // ✅ 1. 모든 코루틴 중단
        StopAllCoroutines();
        
        // ✅ 2. 코루틴 추적 리스트 초기화
        activeCoroutines.Clear();
        currentStateCoroutine = null;
        
        // ✅ 3. UnitController 경로 초기화
        if (mover != null)
        {
            mover.ClearPath();
        }

        if (agent.isFollowingQueen)
        {
            agent.hasManualOrder = true;
            agent.isFollowingQueen = false;
        }

        if (!agent.CanMoveToTile(tile.q, tile.r))
        {
            Debug.Log($"[Worker State] 활동 범위 밖: ({tile.q}, {tile.r})");
            return;
        }

        targetTile = tile;

        // ✅ 4. 새로운 상태로 전환 (내부에서 새 코루틴 시작)
        if (agent.homeHive != null)
        {
            TransitionToState(WorkerState.Moving);
        }
        else
        {
            TransitionToState(WorkerState.Scouting);
        }
    }

    public override void IssueAttackCommand(UnitAgent enemy)
    {
        if (enemy == null) return;

        targetUnit = enemy;
        TransitionToState(WorkerState.Attacking);
    }

    public void OnHiveConstructed(Hive newHive)
    {
        if (newHive == null) return;

        agent.homeHive = newHive;
        targetTile = TileManager.Instance.GetTile(newHive.q, newHive.r);
        TransitionToState(WorkerState.Moving);

        Debug.Log($"[Worker State] 새 하이브 건설 완료, 이동 시작");
    }

    public void StartFollowingQueen()
    {
        agent.isFollowingQueen = true;
        agent.hasManualOrder = false;
        agent.hasManualTarget = false;
        TransitionToState(WorkerState.FollowingQueen);
    }

    public override void CancelCurrentTask()
    {
        TransitionToState(WorkerState.Idle);
        targetTile = null;
        targetUnit = null;
        isCarryingResource = false;
        lastGatherTile = null;
        
        // ✅ UnitAgent에 자원 보유 상태 해제
        if (agent != null)
        {
            agent.SetCarryingResource(false);
        }

        if (mover != null)
        {
            mover.ClearPath();
        }
    }

    #endregion

    // helper that asks UnitController to perform an intra-tile move and waits until it's done
    IEnumerator PerformMoverWithinCurrentTile()
    {
        if (mover == null) yield break;

        mover.MoveWithinCurrentTile();
        int safety = 0;
        while (mover.IsMoving() && safety < 120)
        {
            yield return null;
            safety++;
        }
    }
}
