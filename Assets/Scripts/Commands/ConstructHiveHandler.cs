using UnityEngine;
using System.Collections.Generic;

public class ConstructHiveHandler : MonoBehaviour
{
    // Construct a hive at the agent's current tile (e.g., queen builds hive at her position)
    public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
    {
        if (agent == null) return;

        var tm = TileManager.Instance;
        if (tm == null) return;

        // Use agent's current axial coords rather than the passed target
        int q = agent.q;
        int r = agent.r;

        var tile = tm.GetTile(q, r);
        if (tile == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;
        var hivePrefab = gm.hivePrefab;
        if (hivePrefab == null) return;

        Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
        var go = GameObject.Instantiate(hivePrefab, pos, Quaternion.identity);

        // Ensure Hive component exists
        Hive hive = go.GetComponent<Hive>();
        if (hive == null) hive = go.AddComponent<Hive>();
        
        // Add UnitAgent to hive so it can be selected and receive commands
        var hiveAgent = go.GetComponent<UnitAgent>();
        if (hiveAgent == null) hiveAgent = go.AddComponent<UnitAgent>();
        
        hiveAgent.q = q;
        hiveAgent.r = r;
        hiveAgent.canMove = false; // Hive cannot move
        hiveAgent.faction = Faction.Player;
        hiveAgent.SetPosition(q, r);
        
        // Update queen's home hive and hive's queen reference (Initialize 전에!) ?
        if (agent.isQueen)
        {
            agent.homeHive = hive;
            hive.queenBee = agent;
            
            Debug.Log($"[하이브 건설] 여왕벌 참조 설정 완료");
        }
        
        // Initialize hive (여왕벌 비활성화 포함) ?
        hive.Initialize(q, r);
        
        // Transfer workers from old hive or find homeless workers
        if (agent.isQueen)
        {
            // Note: agent.homeHive는 이미 위에서 새 hive로 설정됨
            // 이전 하이브에서 일꾼 이전은 필요 없음 (새 하이브이므로)
            
            // homeHive가 없는 일꾼들을 찾아서 할당
            AssignHomelessWorkersToHive(hive, q, r);
            
            // ✅ 0.5초 후 LineRenderer 생성 확인 및 재시도
            //MonoBehaviour mono = hive;
            //if (mono != null)
            //{
            //    mono.StartCoroutine(VerifyHiveSetup(hive, q, r));
            //}
        }
        
        Debug.Log($"[하이브 건설] 하이브 건설 완료: ({q}, {r})");
    }
    
    /// <summary>
    /// 하이브 설정 검증 (0.5초 후 LineRenderer 및 일벌 상태 확인)
    /// </summary>
    private static System.Collections.IEnumerator VerifyHiveSetup(Hive hive, int q, int r)
    {
        // 0.5초 대기
        yield return new UnityEngine.WaitForSeconds(0.5f);
        
        if (hive == null)
        {
            Debug.LogWarning("[하이브 검증] 하이브가 null입니다!");
            yield break;
        }
        
        Debug.Log($"[하이브 검증] 하이브 설정 검증 시작: ({q}, {r})");
        
        // ✅ 1. LineRenderer 생성 확인
        bool lineRendererExists = false;
        var highlighter = HexBoundaryHighlighter.Instance;
        
        if (highlighter != null)
        {
            // HexBoundaryHighlighter의 LineRenderer 확인
            var lineRenderers = highlighter.GetComponentsInChildren<LineRenderer>(true);
            foreach (var lr in lineRenderers)
            {
                if (lr != null && lr.gameObject.activeSelf && lr.positionCount > 0)
                {
                    lineRendererExists = true;
                    Debug.Log($"[하이브 검증] LineRenderer 발견: {lr.name}, positionCount={lr.positionCount}");
                    break;
                }
            }
        }
        
        // ✅ 2. LineRenderer가 없으면 재생성
        if (!lineRendererExists)
        {
            Debug.LogWarning($"[하이브 검증] LineRenderer가 생성되지 않음! 재시도 중...");
            
            if (highlighter != null)
            {
                // HexBoundaryHighlighter 강제 활성화
                if (!highlighter.enabledForHives)
                {
                    highlighter.SetEnabledForHives(true);
                    Debug.Log("[하이브 검증] HexBoundaryHighlighter 강제 활성화");
                }
                
                // ShowBoundary 재호출
                int radius = 5;
                if (HiveManager.Instance != null)
                {
                    radius = HiveManager.Instance.hiveActivityRadius;
                }
                
                highlighter.ShowBoundary(hive, radius);
                Debug.Log($"[하이브 검증] ShowBoundary 재호출 완료: radius={radius}");
            }
            else
            {
                Debug.LogError("[하이브 검증] HexBoundaryHighlighter.Instance가 null입니다!");
            }
            
            // ✅ 3. 일벌 상태 재초기화
            Debug.Log("[하이브 검증] 일벌 상태 재초기화 시작");
            AssignHomelessWorkersToHive(hive, q, r);
        }
        else
        {
            Debug.Log($"[하이브 검증] LineRenderer 정상 생성 확인됨: ({q}, {r})");
        }
    }

    /// <summary>
    /// homeHive가 없는 일꾼들을 새 하이브에 할당
    /// </summary>
    private static void AssignHomelessWorkersToHive(Hive hive, int q, int r)
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            if (unit.faction != Faction.Player) continue;
            if (unit.isQueen) continue;
            
            // ✅ 모든 일꾼을 새 하이브로 이동 (homeHive 유무 상관없이)
            
            // ✅ 1. WorkerBehaviorController 상태 초기화 (채취/공격 상태 해제)
            var workerBehavior = unit.GetComponent<WorkerBehaviorController>();
            if (workerBehavior != null)
            {
                Debug.Log($"[하이브 건설] {unit.name} WorkerBehaviorController 상태 초기화");
                workerBehavior.CancelCurrentTask(); // 모든 코루틴 중단 및 상태 초기화
            }
            
            // ✅ 2. UnitController 경로 초기화
            var ctrl = unit.GetComponent<UnitController>();
            if (ctrl != null)
            {
                ctrl.ClearPath(); // 기존 이동 경로 제거
            }
            
            // ✅ 3. 새 하이브를 homeHive로 설정
            unit.homeHive = hive;
            unit.isFollowingQueen = false;
            unit.hasManualOrder = false;
            
            // ✅ 4. WorkerBehaviorController를 통해 하이브로 이동 명령
            if (workerBehavior != null)
            {
                workerBehavior.OnHiveConstructed(hive);
                Debug.Log($"[하이브 건설] {unit.name} 하이브로 이동 명령 (WorkerBehaviorController)");
            }
            else
            {
                // ✅ 5. WorkerBehaviorController가 없으면 UnitController로 직접 이동
                var start = TileManager.Instance.GetTile(unit.q, unit.r);
                var dest = TileManager.Instance.GetTile(q, r);
                if (start != null && dest != null)
                {
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null && path.Count > 0)
                    {
                        if (ctrl == null) ctrl = unit.gameObject.AddComponent<UnitController>();
                        ctrl.agent = unit;
                        ctrl.SetPath(path);
                        Debug.Log($"[하이브 건설] {unit.name} 하이브로 이동 명령 (UnitController)");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 여왕벌을 하이브 타일 위치로 이동
    /// </summary>
    private static void MoveQueenToHive(UnitAgent queen, int q, int r)
    {
        if (queen == null) return;
        
        // 타일 좌표 업데이트
        queen.SetPosition(q, r);
        
        // 월드 위치 업데이트 (하이브 중심)
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queen.hexSize);
        queen.transform.position = hivePos;
        
        Debug.Log($"[하이브 건설] 여왕벌이 하이브 위치로 이동: ({q}, {r})");
    }
}
