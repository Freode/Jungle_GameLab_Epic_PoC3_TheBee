using System.Collections;
using UnityEngine;

/// <summary>
/// 일꾼 꿀벌 상태
/// </summary>
public enum WorkerState
{
    Idle,           // 대기 상태
    Moving,         // 이동 상태
    Gathering,      // 자원 채취 상태
    Attacking,      // 공격 상태
    FollowingQueen, // 여왕벌 쫓는 상태
    Scouting        // 정찰 상태
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
    [SerializeField] private float gatheringDuration = 2.0f;
    [SerializeField] private float scoutRandomMoveMinDelay = 0.2f;
    [SerializeField] private float scoutRandomMoveMaxDelay = 1.5f;
    [SerializeField] private float workerEnemyDetectionInterval = 0.2f;
    
    private Coroutine currentStateCoroutine = null;
    private bool isCarryingResource = false;
    
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
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        if (currentState != WorkerState.Attacking)
        {
            previousState = currentState;
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
            // 1. 적 감지
            var enemy = FindNearbyEnemy(1);
            if (enemy != null)
            {
                targetUnit = enemy;
                TransitionToState(WorkerState.Attacking);
                yield break;
            }

            // 2. 자원 들고 있으면 하이브로 이동
            if (isCarryingResource && agent.homeHive != null)
            {
                targetTile = TileManager.Instance.GetTile(agent.homeHive.q, agent.homeHive.r);
                TransitionToState(WorkerState.Moving);
                yield break;
            }

            // 3. 랜덤 대기 후 타일 내 랜덤 이동
            float waitTime = Random.Range(idleRandomMoveMinDelay, idleRandomMoveMaxDelay);
            float elapsed = 0f;

            while (elapsed < waitTime)
            {
                // 대기 중 적 감지
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

            // 타일 내 랜덤 위치로 이동
            if (agent != null)
            {
                Vector3 randomPos = TileHelper.GetRandomPositionInTile(agent.q, agent.r, agent.hexSize, 0.3f);
                StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));
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

        // 경로 찾기
        var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
        var path = Pathfinder.FindPath(currentTile, targetTile);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[Worker State] 경로 없음: ({currentTile.q}, {currentTile.r}) → ({targetTile.q}, {targetTile.r})");
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        mover.SetPath(path);

        // 목적지 도달 대기
        Vector3 destination = TileHelper.HexToWorld(targetTile.q, targetTile.r, agent.hexSize);
        float timeout = 15f;
        float elapsed = 0f;

        while (Vector3.Distance(transform.position, destination) > 0.2f && elapsed < timeout)
        {
            //// 이동 중 적 감지
            //var enemy = FindNearbyEnemy(1);
            //if (enemy != null)
            //{
            //    targetUnit = enemy;
            //    TransitionToState(WorkerState.Attacking);
            //    yield break;
            //}

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning($"[Worker State] 이동 타임아웃");
            TransitionToState(WorkerState.Idle);
            yield break;
        }

        // 도착 후 처리
        var finalEnemy = FindNearbyEnemy(1);
        if (finalEnemy != null)
        {
            targetUnit = finalEnemy;
            TransitionToState(WorkerState.Attacking);
            yield break;
        }

        // 자원 들고 있고 하이브 도착
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
                StartCoroutine(MoveToPositionSmooth(randomPos, 0.3f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        int gathered = targetTile.TakeResource(gatherAmount);
        if (gathered > 0)
        {
            isCarryingResource = true;
            Debug.Log($"[Worker State] 자원 채취 완료: {gathered}");
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
            // 활동 범위 체크
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

                yield return StartCoroutine(EvadeCoroutine());

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
                StartCoroutine(MoveToPositionSmooth(evadePos, 0.2f));
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

            // 여왕벌과 같은 타일이면 대기
            if (agent.q == queen.q && agent.r == queen.r)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // 여왕벌 위치로 이동
            var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
            var queenTile = TileManager.Instance.GetTile(queen.q, queen.r);
            var path = Pathfinder.FindPath(currentTile, queenTile);

            if (path != null && path.Count > 0)
            {
                mover.SetPath(path);
            }

            yield return new WaitForSeconds(1f);
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

            Vector3 destination = TileHelper.HexToWorld(targetTile.q, targetTile.r, agent.hexSize);
            float timeout = 15f;
            float elapsed = 0f;

            while (Vector3.Distance(transform.position, destination) > 0.2f && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
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
        StartCoroutine(MoveToPositionSmooth(randomPos, 0.5f));

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

        if (mover != null)
        {
            mover.ClearPath();
        }
    }

    #endregion
}
