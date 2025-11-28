using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConstructHiveHandler : MonoBehaviour
{
    // Construct a hive at the agent's current tile
    public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
    {
        // 1. 기본 유효성 검사
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

        // ✅ [에러 해결 2] HiveManager에 함수가 없어도 여기서 직접 확인하도록 수정
        if (CheckIfPlayerHiveExists())
        {
            if (NotificationToast.Instance != null) 
                NotificationToast.Instance.ShowMessage("이미 하이브가 존재합니다!", 2f);
            return;
        }

        // mark constructing immediately so construct command is disabled while building
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.SetConstructingState(true);
        }

        agent.StartCoroutine(BuildProcessRoutine(agent, q, r, gm));
    }

    // ✅ [추가] 3초 대기 및 건설 코루틴
    private static IEnumerator BuildProcessRoutine(UnitAgent agent, int q, int r, GameManager gm)
    {
        // Ensure flag is cleared regardless of how coroutine exits
        try
        {
            float duration = 3.0f;
            float timer = duration;
            Vector3 startPos = agent != null ? agent.transform.position : Vector3.zero;

            // 시작 알림
            if (NotificationToast.Instance != null)
                NotificationToast.Instance.ShowMessage($"3초 뒤 하이브를 건설합니다. 움직이면 취소됩니다.", 2f);
        
            Debug.Log($"[Construct] 건설 준비... ({duration}초)");

            // 3초 카운트다운
            while (timer > 0)
            {
                if (agent == null) yield break; // 죽었으면 중단

                // 움직임 체크 (0.1f 이상 움직이면 취소)
                if (Vector3.Distance(agent.transform.position, startPos) > 0.1f)
                {
                    if (NotificationToast.Instance != null)
                        NotificationToast.Instance.ShowMessage("이동하여 건설이 취소되었습니다.", 1.5f);
                    Debug.Log("[Construct] 움직임 감지되어 취소됨");
                    yield break;
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            // === 건설 시작 ===
        
            if (NotificationToast.Instance != null)
                NotificationToast.Instance.ShowMessage("건설 완료!", 1.5f);

            Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
            var go = GameObject.Instantiate(gm.hivePrefab, pos, Quaternion.identity);

            // Ensure Hive component exists
            Hive hive = go.GetComponent<Hive>();
            if (hive == null) hive = go.AddComponent<Hive>();
        
            // Add UnitAgent to hive
            var hiveAgent = go.GetComponent<UnitAgent>();
            if (hiveAgent == null) hiveAgent = go.AddComponent<UnitAgent>();
        
            hiveAgent.q = q;
            hiveAgent.r = r;
            hiveAgent.canMove = false;
            hiveAgent.faction = Faction.Player;
            hiveAgent.SetPosition(q, r);
        
            // Update queen's home hive
            if (agent != null && agent.isQueen)
            {
                agent.homeHive = hive;
                hive.queenBee = agent;
            
                Debug.Log($"[하이브 건설] 여왕벌 참조 설정 완료");
            }
        
            // Initialize hive
            hive.Initialize(q, r);
        
            // Transfer workers
            if (agent != null && agent.isQueen)
            {
                // homeHive가 없는 일꾼들을 찾아서 할당
                AssignHomelessWorkersToHive(hive, q, r);
            }
        
            Debug.Log($"[하이브 건설] 하이브 건설 완료: ({q}, {r})");

            // 검증 코루틴 실행
            if (agent != null)
                agent.StartCoroutine(VerifyHiveSetup(hive, q, r));
        }
        finally
        {
            // Always clear constructing flag when coroutine exits (cancel or complete)
            if (HiveManager.Instance != null)
            {
                HiveManager.Instance.SetConstructingState(false);
            }
        }
    }

    // ✅ [헬퍼] 플레이어 하이브 존재 여부 확인 (내부 로직)
    private static bool CheckIfPlayerHiveExists()
    {
        if (HiveManager.Instance == null) return false;
        
        // HiveManager의 GetAllHives()는 있다고 가정 (보통 기본 제공)
        foreach (var h in HiveManager.Instance.GetAllHives())
        {
            var ag = h.GetComponent<UnitAgent>();
            if (ag != null && ag.faction == Faction.Player) return true;
        }
        return false;
    }
    
    private static IEnumerator VerifyHiveSetup(Hive hive, int q, int r)
    {
        yield return new WaitForSeconds(0.5f);
        if (hive == null) yield break;

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
                if (!highlighter.enabledForHives) highlighter.SetEnabledForHives(true);
                int radius = (HiveManager.Instance != null) ? HiveManager.Instance.hiveActivityRadius : 5;
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

    private static void AssignHomelessWorkersToHive(Hive hive, int q, int r)
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;
            if (unit.faction != Faction.Player) continue;
            if (unit.isQueen) continue;
            
            var workerBehavior = unit.GetComponent<WorkerBehaviorController>();
            if (workerBehavior != null) workerBehavior.CancelCurrentTask();
            
            var ctrl = unit.GetComponent<UnitController>();
            if (ctrl != null) ctrl.ClearPath();
            
            unit.homeHive = hive;
            unit.isFollowingQueen = false;
            unit.hasManualOrder = false;
            
            if (workerBehavior != null)
            {
                workerBehavior.OnHiveConstructed(hive);
            }
            else
            {
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
                    }
                }
            }
        }
    }
}