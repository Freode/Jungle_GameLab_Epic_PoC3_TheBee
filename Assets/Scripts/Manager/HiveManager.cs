using System.Collections.Generic;
using UnityEngine;

public class HiveManager : MonoBehaviour
{
    public static HiveManager Instance { get; private set; }

    private List<Hive> hives = new List<Hive>();

    [Header("Hive Settings")]
    public int hiveActivityRadius = 5;

    [Header("Player Resources")]
    public int playerStoredResources = 0; // 플레이어의 전체 저장 자원

    // 자원 변경 이벤트
    public event System.Action OnResourcesChanged;
    
    // 일꾼 수 변경 이벤트 ✅
    public event System.Action<int, int> OnWorkerCountChanged; // (현재 일꾼 수, 최대 일꾼 수)
    
    // ✅ 하이브 건설/파괴 이벤트 (요구사항 2)
    public event System.Action OnPlayerHiveConstructed; // 플레이어 하이브 건설
    public event System.Action OnPlayerHiveDestroyed; // 플레이어 하이브 파괴

    [Header("Worker Management")]
    public int currentWorkers = 0; // 현재 일꾼 수 ✅
    public int maxWorkers = 10; // 최대 일꾼 수 (기본값) ✅
    private List<UnitAgent> allWorkers = new List<UnitAgent>(); // 모든 일꾼 목록 ✅
    
    [Header("Worker Squad System")]
    [Tooltip("1번 부대 색상 (빨강)")]
    public Color squad1Color = Color.red;
    [Tooltip("2번 부대 색상 (초록)")]
    public Color squad2Color = Color.green;
    [Tooltip("3번 부대 색상 (파랑)")]
    public Color squad3Color = Color.blue;
    [Header("Visual Effects")]
    public GameObject honeyProjectilePrefab; // 꿀 투사체 프리팹
    
    private Dictionary<WorkerSquad, List<UnitAgent>> squadWorkers = new Dictionary<WorkerSquad, List<UnitAgent>>()
    {
        { WorkerSquad.Squad1, new List<UnitAgent>() },
        { WorkerSquad.Squad2, new List<UnitAgent>() },
        { WorkerSquad.Squad3, new List<UnitAgent>() }
    };

    [Header("Upgrade Levels")]
    public int hiveRangeLevel = 0;          // 하이브 활동 범위 레벨
    public int workerAttackLevel = 0;       // 일꾼 공격력 레벨
    public int workerHealthLevel = 0;       // 일꾼 체력 레벨
    public int workerSpeedLevel = 0;        // 일꾼 이동 속도 레벨
    public int hiveHealthLevel = 0;         // 하이브 체력 레벨
    public int maxWorkersLevel = 0;         // 최대 일꾼 수 레벨
    public int gatherAmountLevel = 0;       // 자원 채취량 레벨

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterHive(Hive hive)
    {
        if (hive == null) return;
        if (!hives.Contains(hive)) hives.Add(hive);

        // ✅ 플레이어 하이브 건설 이벤트 발생 (요구사항 2)
        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null && hiveAgent.faction == Faction.Player)
        {
            OnPlayerHiveConstructed?.Invoke();
            Debug.Log("[HiveManager] 플레이어 하이브 건설 이벤트 발생");
        }

        // ✅ 첫 번째 하이브 등록 시 처리
        if (hives.Count == 1)
        {
            // ✅ 1. HexBoundaryHighlighter 활성화 (먼저 실행)
            var bh = HexBoundaryHighlighter.Instance;
            if (bh != null)
            {
                bh.SetEnabledForHives(true);
                bh.debugLogs = false; // disable debug logs
                Debug.Log("[HiveManager] HexBoundaryHighlighter 활성화 완료");
            }
            else
            {
                Debug.LogWarning("[HiveManager] HexBoundaryHighlighter.Instance가 null입니다!");
            }
            
            // ✅ 2. 일반 말벌 웨이브 시작 (나중에 실행)
            if (WaspWaveManager.Instance != null)
            {
                WaspWaveManager.Instance.StartNormalWaveRoutine();
                Debug.Log("[HiveManager] 일반 말벌 웨이브 시작 요청 완료");
            }
            else
            {
                Debug.LogWarning("[HiveManager] WaspWaveManager.Instance가 null입니다! 웨이브가 시작되지 않습니다.");
            }
        }
    }

    public void UnregisterHive(Hive hive)
    {
        if (hive == null) return;
        if (hives.Contains(hive)) hives.Remove(hive);

        // ✅ 플레이어 하이브 파괴 이벤트 발생 (요구사항 2)
        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null && hiveAgent.faction == Faction.Player)
        {
            OnPlayerHiveDestroyed?.Invoke();
            Debug.Log("[HiveManager] 플레이어 하이브 파괴 이벤트 발생");
        }

        if (hives.Count == 0)
        {
            // disable boundary highlighter when no hives remain
            var bh = HexBoundaryHighlighter.Instance;
            if (bh != null)
            {
                bh.SetEnabledForHives(false);
                bh.debugLogs = false;
            }
        }
    }

    public IEnumerable<Hive> GetAllHives() => hives;

    public Hive FindNearestHive(int q, int r)
    {
        Hive found = null;
        int best = int.MaxValue;
        foreach (var h in hives)
        {
            int d = Pathfinder.AxialDistance(q, r, h.q, h.r);
            if (d < best)
            {
                best = d; found = h;
            }
        }
        return found;
    }

    /// <summary>
    /// 일꾼 등록 (생성 시 호출) ✅
    /// </summary>
    public void RegisterWorker(UnitAgent worker)
    {
        if (worker == null || allWorkers.Contains(worker)) return;
        
        allWorkers.Add(worker);
        
        // ✅ 3부대 자동 배치: 가장 작은 인원을 가진 부대에 배치
        WorkerSquad assignedSquad = GetSmallestSquad();
        AssignWorkerToSquad(worker, assignedSquad);
        
        currentWorkers = allWorkers.Count;
        
        Debug.Log($"[HiveManager] 일꾼 등록: {currentWorkers}/{maxWorkers}, 부대: {assignedSquad}");
        
        // 일꾼 수 변경 이벤트 발생
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }
    
    /// <summary>
    /// 가장 작은 인원을 가진 부대 찾기 ✅
    /// </summary>
    private WorkerSquad GetSmallestSquad()
    {
        WorkerSquad smallest = WorkerSquad.Squad1;
        int minCount = int.MaxValue;
        
        foreach (var kvp in squadWorkers)
        {
            // null 제거
            kvp.Value.RemoveAll(w => w == null);
            
            int count = kvp.Value.Count;
            if (count < minCount)
            {
                minCount = count;
                smallest = kvp.Key;
            }
        }
        
        return smallest;
    }
    
    /// <summary>
    /// 일꾼을 특정 부대에 배치 ✅
    /// </summary>
    private void AssignWorkerToSquad(UnitAgent worker, WorkerSquad squad)
    {
        if (worker == null) return;
        
        // UnitAgent에 부대 정보 저장 (확장 필요)
        var workerAgent = worker.GetComponent<UnitAgent>();
        if (workerAgent != null)
        {
            // ✅ 부대 색상 적용 (요구사항 2)
            Color squadColor = GetSquadColor(squad);
            ApplySquadColor(worker, squadColor);
            
            // ✅ 부대 데이터 저장 (UnitAgent에 squad 필드 추가 필요)
            // workerAgent.squad = squad; // 나중에 추가

            // ✅ 부대에 따른 역할 자동 할당: 1번 부대 = 채취형, 2번 = 공격형, 3번 = 탱커형
            var roleAssigner = worker.GetComponent<RoleAssigner>();
            if (roleAssigner == null)
            {
                roleAssigner = worker.gameObject.AddComponent<RoleAssigner>();
            }

            switch (squad)
            {
                case WorkerSquad.Squad1:
                    roleAssigner.SetRole(RoleType.Gatherer);
                    break;
                case WorkerSquad.Squad2:
                    roleAssigner.SetRole(RoleType.Attacker);
                    break;
                case WorkerSquad.Squad3:
                    roleAssigner.SetRole(RoleType.Tank);
                    break;
                default:
                    roleAssigner.SetRole(RoleType.Gatherer);
                    break;
            }
        }
        
        // 부대 리스트에 추가
        if (!squadWorkers[squad].Contains(worker))
        {
            squadWorkers[squad].Add(worker);
        }
        
        Debug.Log($"[부대] {worker.name} → {squad} 배치 (인원: {squadWorkers[squad].Count}), 색상: {GetSquadColor(squad)}");
        
        // ✅ 해당 부대의 기존 페르몬 명령이 있으면 자동으로 이동 명령 (코루틴으로 지연 실행)
        if (PheromoneManager.Instance != null)
        {
            var pheromonePos = PheromoneManager.Instance.GetCurrentPheromonePosition(squad);
            if (pheromonePos.HasValue)
            {
                StartCoroutine(ApplyPheromoneToWorkerDelayed(worker, squad, pheromonePos.Value));
            }
        }

        // If this is a tank squad, scale up the worker prefab slightly; otherwise ensure default scale
        float targetScale = 1f;
        if (squad == WorkerSquad.Squad3)
        {
            targetScale = 1.35f; // 탱커형 일벌은 35% 크게
        }
        worker.transform.localScale = Vector3.one * targetScale;
        Debug.Log($"[부대] {worker.name} 스케일 설정 시도: {targetScale}");

        // Ensure scale persists (in case other initialization overwrites scale) - reapply next frame
        StartCoroutine(EnsureScaleApplied(worker, targetScale));
    }
    
    /// <summary>
    /// 신규 일꾼에게 페르몬 명령 지연 적용 (다음 프레임에 실행)
    /// </summary>
    private System.Collections.IEnumerator ApplyPheromoneToWorkerDelayed(UnitAgent worker, WorkerSquad squad, Vector2Int pheromonePos)
    {
        // 한 프레임 대기 (일꾼 완전 초기화)
        yield return null;
        
        if (worker == null)
        {
            Debug.LogWarning($"[부대] 일꾼이 null입니다. 페르몬 명령 적용 실패.");
            yield break;
        }
        
        var targetTile = TileManager.Instance?.GetTile(pheromonePos.x, pheromonePos.y);
        if (targetTile == null)
        {
            Debug.LogWarning($"[부대] 타일을 찾을 수 없습니다: ({pheromonePos.x}, {pheromonePos.y})");
            yield break;
        }
        
        var workerBehavior = worker.GetComponent<WorkerBehaviorController>();
        if (workerBehavior != null)
        {
            // ✅ 기존 작업 취소 후 새 명령 실행
            workerBehavior.CancelCurrentTask();
            
            // ✅ 수동 명령 플래그 설정 (자동 자원 채취 방지)
            worker.hasManualOrder = true;
            
            // ✅ 페르몬 위치로 이동
            workerBehavior.IssueCommandToTile(targetTile);
            
            Debug.Log($"[부대] {worker.name} → {squad} 페르몬 명령 자동 적용: ({pheromonePos.x}, {pheromonePos.y})");
        }
        else
        {
            Debug.LogWarning($"[부대] {worker.name}에 WorkerBehaviorController가 없습니다.");
        }
    }
    
    /// <summary>
    /// 부대 색상 가져오기 ✅
    /// </summary>
    private Color GetSquadColor(WorkerSquad squad)
    {
        switch (squad)
        {
            case WorkerSquad.Squad1: return squad1Color;
            case WorkerSquad.Squad2: return squad2Color;
            case WorkerSquad.Squad3: return squad3Color;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 일벌에게 부대 색상 적용 ✅
    /// </summary>
    private void ApplySquadColor(UnitAgent worker, Color color)
    {
        if (worker == null) return;
        
        // ✅ 1. UnitAgent의 originalColor 업데이트 (기본 색상 변경)
        worker.SetOriginalColor(color);
        Debug.Log($"[부대 색상] {worker.name} 기본 색상 업데이트: {color}");
        
        // ✅ 2. SpriteRenderer 색상 적용
        var sprite = worker.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = color;
        }
        
        // ✅ 3. Renderer 색상 적용 (MaterialPropertyBlock 사용)
        var renderer = worker.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            mpb.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// 일꾼 해제 (죽음 시 호출) ✅
    /// </summary>
    public void UnregisterWorker(UnitAgent worker)
    {
        if (worker == null || !allWorkers.Contains(worker)) return;
        
        allWorkers.Remove(worker);
        
        // ✅ 모든 부대에서 제거
        foreach (var squad in squadWorkers.Values)
        {
            squad.Remove(worker);
        }
        
        currentWorkers = allWorkers.Count;
        
        Debug.Log($"[HiveManager] 일꾼 해제: {currentWorkers}/{maxWorkers}");
        
        // 일꾼 수 변경 이벤트 발생
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    /// <summary>
    /// 모든 일꾼 목록 가져오기 ✅
    /// </summary>
    public List<UnitAgent> GetAllWorkers()
    {
        // null 제거
        allWorkers.RemoveAll(w => w == null);
        currentWorkers = allWorkers.Count;
        return new List<UnitAgent>(allWorkers);
    }

    /// <summary>
    /// 현재 일꾼 수 가져오기 ✅
    /// </summary>
    public int GetCurrentWorkers()
    {
        // null 제거
        allWorkers.RemoveAll(w => w == null);
        currentWorkers = allWorkers.Count;
        return currentWorkers;
    }
    
    /// <summary>
    /// 특정 부대의 일꾼 목록 가져오기 ✅
    /// </summary>
    public List<UnitAgent> GetSquadWorkers(WorkerSquad squad)
    {
        if (!squadWorkers.ContainsKey(squad))
        {
            return new List<UnitAgent>();
        }
        
        // null 제거
        squadWorkers[squad].RemoveAll(w => w == null);
        return new List<UnitAgent>(squadWorkers[squad]);
    }
    
    /// <summary>
    /// 부대별 인원 수 가져오기 ✅
    /// </summary>
    public int GetSquadCount(WorkerSquad squad)
    {
        if (!squadWorkers.ContainsKey(squad))
        {
            return 0;
        }
        
        squadWorkers[squad].RemoveAll(w => w == null);
        return squadWorkers[squad].Count;
    }

    // Add resources to player's storage
    public void AddResources(int amount)
    {
        playerStoredResources += amount;
        Debug.Log($"자원 추가: +{amount}, 총 자원: {playerStoredResources}");
        
        // 이벤트 발생
        OnResourcesChanged?.Invoke();
    }

    // Try to spend resources, returns true if successful
    public bool TrySpendResources(int amount)
    {
        if (playerStoredResources >= amount)
        {
            playerStoredResources -= amount;
            Debug.Log($"자원 소모: -{amount}, 남은 자원: {playerStoredResources}");
            
            // 이벤트 발생
            OnResourcesChanged?.Invoke();
            
            return true;
        }
        return false;
    }

    // Check if player has enough resources
    public bool HasResources(int amount)
    {
        return playerStoredResources >= amount;
    }

    // ========== 업그레이드 시스템 ==========

    // 1. 하이브 활동 범위 업그레이드
    public bool UpgradeHiveRange(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        hiveRangeLevel++;
        hiveActivityRadius = 4 + hiveRangeLevel;

        // 모든 하이브의 경계선 업데이트
        foreach (var hive in hives)
        {
            if (hive != null && HexBoundaryHighlighter.Instance != null)
            {
                HexBoundaryHighlighter.Instance.ShowBoundary(hive, hiveActivityRadius);
            }
        }

        Debug.Log($"[업그레이드] 하이브 활동 범위 +1! 현재 범위: {hiveActivityRadius}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "활동 범위 확장",
                "하이브 활동 범위 +1",
                $"{hiveActivityRadius}칸"
            );
        }
        
        return true;
    }

    // 2. 일꾼 공격력 업그레이드
    public bool UpgradeWorkerAttack(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        workerAttackLevel++;
        UpdateAllWorkerCombat();

        Debug.Log($"[업그레이드] 일꾼 공격력 +1! 현재: {GetWorkerAttack()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "날카로운 침",
                "일꾼 공격력 +1",
                $"{GetWorkerAttack()}"
            );
        }
        
        return true;
    }

    // 3. 일꾼 체력 업그레이드
    public bool UpgradeWorkerHealth(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        workerHealthLevel++;
        UpdateAllWorkerCombat();

        Debug.Log($"[업그레이드] 일꾼 체력 +2! 현재: {GetWorkerMaxHealth()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "강화 외골격",
                "일꾼 최대 체력 +2",
                $"{GetWorkerMaxHealth()} HP"
            );
        }
        
        return true;
    }

    // 4. 일꾼 이동 속도 업그레이드
    public bool UpgradeWorkerSpeed(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        workerSpeedLevel++;
        UpdateAllWorkerSpeed();

        Debug.Log($"[업그레이드] 일꾼 이동 속도 +0.2! 현재: {GetWorkerSpeed()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "빠른 날개",
                "일꾼 이동 속도 +0.2",
                $"{GetWorkerSpeed():F1}"
            );
        }
        
        return true;
    }

    // 5. 하이브 체력 업그레이드
    public bool UpgradeHiveHealth(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        hiveHealthLevel++;
        UpdateAllHiveHealth();

        Debug.Log($"[업그레이드] 하이브 체력 +30! 현재: {GetHiveMaxHealth()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "강화 성벽",
                "하이브 최대 체력 +30",
                $"{GetHiveMaxHealth()} HP"
            );
        }
        
        return true;
    }

    // 6. 최대 일꾼 수 업그레이드
    public bool UpgradeMaxWorkers(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        maxWorkersLevel++;
        UpdateAllHiveMaxWorkers();

        Debug.Log($"[업그레이드] 최대 일꾼 수 +3! 현재: {GetMaxWorkers()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "확장 벌집",
                "최대 일꾼 수 +3",
                $"{GetMaxWorkers()}마리"
            );
        }
        
        return true;
    }

    // 7. 자원 채취량 업그레이드
    public bool UpgradeGatherAmount(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        gatherAmountLevel++;
        UpdateAllWorkerGatherAmount();

        Debug.Log($"[업그레이드] 자원 채취량 +1! 현재: {GetGatherAmount()}");
        
        // UI 표시
        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult(
                "효율적 채집",
                "자원 채취량 +1",
                $"{GetGatherAmount()}"
            );
        }
        
        return true;
    }

    // ========== 업데이트 메서드 ==========

    void UpdateAllWorkerCombat()
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;

            // 하이브 자체는 제외 ?
//             var hive = unit.GetComponent<Hive>();
//             if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
//             if (!unit.gameObject.activeInHierarchy) continue;

            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null)
            {
                // 공격력 업데이트 (이벤트 발생)
                combat.SetAttack(GetWorkerAttack());
                
                // 체력 업데이트 - 최대 체력이 증가한 만큼 현재 체력도 증가
                int oldMaxHealth = combat.maxHealth;
                int newMaxHealth = GetWorkerMaxHealth();
                int healthIncrease = newMaxHealth - oldMaxHealth;
                
                combat.SetMaxHealth(newMaxHealth);
                combat.SetHealth(Mathf.Min(combat.health + healthIncrease, combat.maxHealth));
            }
        }
    }

    void UpdateAllWorkerSpeed()
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;

            // 하이브 자체는 제외 ?
//             var hive = unit.GetComponent<Hive>();
//             if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
//             if (!unit.gameObject.activeInHierarchy) continue;

            var controller = unit.GetComponent<UnitController>();
            if (controller != null)
            {
                controller.moveSpeed = GetWorkerSpeed();
            }
        }
    }

    void UpdateAllHiveHealth()
    {
        foreach (var hive in hives)
        {
            if (hive == null) continue;

            var combat = hive.GetComponent<CombatUnit>();
            if (combat != null)
            {
                int oldMax = combat.maxHealth;
                combat.maxHealth = GetHiveMaxHealth();
                // 현재 체력을 비율로 증가
                if (oldMax > 0)
                {
                    float ratio = (float)combat.health / oldMax;
                    combat.health = Mathf.RoundToInt(combat.maxHealth * ratio);
                }
                else
                {
                    combat.health = combat.maxHealth;
                }
            }
        }
    }

    void UpdateAllHiveMaxWorkers()
    {
        // HiveManager의 maxWorkers 업데이트 ✅
        maxWorkers = GetMaxWorkers();
        
        // 각 하이브의 maxWorkers도 동기화
        foreach (var hive in hives)
        {
            if (hive != null)
            {
                hive.maxWorkers = maxWorkers;
            }
        }
        
        // 최대 일꾼 수 변경 이벤트 발생 ✅
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }
    
    /// <summary>
    /// 일꾼 수 변경 알림 (공개 메서드)
    /// Hive에서 일꾼 생성/죽음 시 호출 (더 이상 사용 안 함 - RegisterWorker/UnregisterWorker 사용) ✅
    /// </summary>
    public void NotifyWorkerCountChanged()
    {
        // 간단하게 현재 상태로 이벤트 발생
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    void UpdateAllWorkerGatherAmount()
    {
        if (TileManager.Instance == null) return;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;

            // 하이브 자체는 제외 ?
//             var hive = unit.GetComponent<Hive>();
//             if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
//             if (!unit.gameObject.activeInHierarchy) continue;

            var behavior = unit.GetComponent<UnitBehaviorController>();
            if (behavior != null)
            {
                behavior.gatherAmount = GetGatherAmount();
            }
        }
    }

    // ========== 스탯 계산 메서드 ==========

    public int GetWorkerAttack()
    {
        return 3 + workerAttackLevel;
    }

    public int GetWorkerMaxHealth()
    {
        return 10 + (workerHealthLevel * 2);
    }

    public float GetWorkerSpeed()
    {
        return 2.0f + (float)workerSpeedLevel / 5f;
    }

    public int GetHiveMaxHealth()
    {
        return 100 + (hiveHealthLevel * 30);
    }

    public int GetMaxWorkers()
    {
        return 5 + (maxWorkersLevel * 3);
    }

    public int GetGatherAmount()
    {
        return 1 + gatherAmountLevel;
    }

    // Reapply scale on the next frame to guard against other initialization resetting it
    private System.Collections.IEnumerator EnsureScaleApplied(UnitAgent worker, float targetScale)
    {
        yield return null; // wait one frame
        if (worker == null) yield break;
        worker.transform.localScale = Vector3.one * targetScale;
        Debug.Log($"[부대] {worker.name} 스케일 강제 적용: {targetScale}");
    }
    /// <summary>
    /// 자원 강탈 연출 (하이브가 파괴될 때 호출)
    /// </summary>
    public void PlayResourceStealEffect(Vector3 fromPos, Vector3 toPos, int amount)
    {
        if (honeyProjectilePrefab == null) return;

        // 코루틴 시작 (HiveManager가 살아있는 한 계속 실행됨)
        StartCoroutine(StealEffectRoutine(fromPos, toPos, amount));
    }

    System.Collections.IEnumerator StealEffectRoutine(Vector3 fromPos, Vector3 toPos, int amount)
    {
        // 1. 생성할 꿀 개수 계산 (10꿀당 1개)
        int projectileCount = amount / 10;
        
        // 너무 적으면 최소 1개, 너무 많으면 최대 30개로 제한 (성능/연출 밸런스)
        projectileCount = Mathf.Clamp(projectileCount, 1, 30);

        // 2. 순차적으로 생성
        for (int i = 0; i < projectileCount; i++)
        {
            // 시작 위치에 약간의 랜덤성 부여 (겹치지 않게)
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
            randomOffset.y = 0; // 높이는 고정

            GameObject honeyObj = Instantiate(honeyProjectilePrefab, fromPos + randomOffset, Quaternion.identity);
            
            var projectile = honeyObj.GetComponent<HoneyProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(fromPos + randomOffset, toPos);
            }

            // 다음 발사까지 아주 짧은 대기 (다다다닥 날아가는 느낌)
            yield return new WaitForSeconds(0.05f);
        }
    }
}
