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
    [SerializeField] public static float gatheringDuration = 3.0f;
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
                break;
            case WorkerState.Moving:
                currentStateCoroutine = StartCoroutine(MovingStateCoroutine());
                break;
            case WorkerState.Gathering:
                currentStateCoroutine = StartCoroutine(GatheringStateCoroutine());
                break;
            case WorkerState.Attacking:
                currentStateCoroutine = StartCoroutine(AttackingStateCoroutine());
                break;
            case WorkerState.FollowingQueen:
                currentStateCoroutine = StartCoroutine(FollowingQueenStateCoroutine());
                break;
            case WorkerState.Scouting:
                currentStateCoroutine = StartCoroutine(ScoutingStateCoroutine());
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
                Vector3 randomPos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.3f);
                var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));
                activeCoroutines.Add(moveCoroutine);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MovingStateCoroutine()
    {
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

            // ✅ 4. 이전 채취 타일이 있고 활동 범위 내면 복귀
            if (lastGatherTile != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(
                    agent.homeHive.q, agent.homeHive.r,
                    lastGatherTile.q, lastGatherTile.r
                );

                if (distanceToHive <= activityRadius && lastGatherTile.resourceAmount > 0)
                {
                    Debug.Log($"[Worker State] 이전 채취 타일로 복귀: 하이브 ({agent.homeHive.q}, {agent.homeHive.r}) → 자원 ({lastGatherTile.q}, {lastGatherTile.r})");
                    
                    // ✅ 순간 이동 방지: agent.SetPosition() 호출하지 않음
                    // UnitController가 이미 agent 위치를 업데이트했으므로
                    // 단순히 targetTile만 설정하고 경로 재계산
                    
                    targetTile = lastGatherTile;
                    TransitionToState(WorkerState.Moving);
                    yield break;
                }
                // ✅ 이전 채취 타일이 활동 범위 내에 있으며, 자동 탐색 모드인 경우에 유지
                else if (isAutoSearchNearResource && distanceToHive <= activityRadius)
                {
                    // ✅ 주변 6방향 타일 탐색 (거리 1)
                    HexTile nearbyResourceTile = FindNearbyResourceTile(lastGatherTile.q, lastGatherTile.r);
                    
                    if (nearbyResourceTile != null)
                    {
                        Debug.Log($"[Worker State] 주변 자원 타일 발견: ({nearbyResourceTile.q}, {nearbyResourceTile.r}), 자원량: {nearbyResourceTile.resourceAmount}");
                        lastGatherTile = nearbyResourceTile;
                        targetTile = nearbyResourceTile;
                        TransitionToState(WorkerState.Moving);
                        yield break;
                    }
                    else
                    {
                        Debug.Log($"[Worker State] 주변에 자원 타일 없음, 이전 채취 타일 초기화");
                        lastGatherTile = null;
                    }
                }
                else
                {
                    Debug.Log($"[Worker State] 이전 채취 타일이 활동 범위 밖: {distanceToHive}/{activityRadius}");
                    lastGatherTile = null;
                }
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
                Vector3 randomPos = TileHelper.GetRandomPositionInTile(targetTile.q, targetTile.r, agent.hexSize, 0.2f);
                var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.3f));
                activeCoroutines.Add(moveCoroutine);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ✅ 5. 자원 채취 시도
        int gathered = targetTile.TakeResource(gatherAmount);
        if (gathered > 0)
        {
            isCarryingResource = true;
            Debug.Log($"[Worker State] 자원 채취 완료: {gathered}");
        }
        else
        {
            // ✅ 5. 자원 채취 실패 (자원량 0) - 하이브로 복귀
            Debug.Log($"[Worker State] 자원 채취 실패 (자원 없음), 하이브로 복귀");
            lastGatherTile = null; // 타일 정보 초기화
            
            if (agent.homeHive != null)
            {
                targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
                TransitionToState(WorkerState.Moving);
            }
            else
            {
                TransitionToState(WorkerState.Idle);
            }
            yield break;
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

                if (currentState == WorkerState.Attacking && targetUnit != null)
                {
                    continue;
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
                Vector3 evadePos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.4f);
                var moveCoroutine = StartCoroutine(MoveToPositionSmooth(evadePos, 0.2f));
                activeCoroutines.Add(moveCoroutine);
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
        var moveCoroutine = StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));
        activeCoroutines.Add(moveCoroutine);

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
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
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
        TransitionToState(WorkerState.FollowingQueen);
    }

    public override void CancelCurrentTask()
    {
        TransitionToState(WorkerState.Idle);
        targetTile = null;
        targetUnit = null;
        isCarryingResource = false;
        lastGatherTile = null;

        if (mover != null)
        {
            mover.ClearPath();
        }
    }

    #endregion
}
