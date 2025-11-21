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

    [Header("Worker Management")]
    public int currentWorkers = 0; // 현재 일꾼 수 ✅
    public int maxWorkers = 10; // 최대 일꾼 수 (기본값) ✅
    private List<UnitAgent> allWorkers = new List<UnitAgent>(); // 모든 일꾼 목록 ✅

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

        // enable HexBoundaryHighlighter singleton when first hive is registered
        if (hives.Count == 1)
        {
            var bh = HexBoundaryHighlighter.Instance;
            if (bh != null)
            {
                bh.SetEnabledForHives(true);
                bh.debugLogs = false; // disable debug logs
            }
        }
    }

    public void UnregisterHive(Hive hive)
    {
        if (hive == null) return;
        if (hives.Contains(hive)) hives.Remove(hive);

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
        currentWorkers = allWorkers.Count;
        
        Debug.Log($"[HiveManager] 일꾼 등록: {currentWorkers}/{maxWorkers}");
        
        // 일꾼 수 변경 이벤트 발생
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    /// <summary>
    /// 일꾼 해제 (죽음 시 호출) ✅
    /// </summary>
    public void UnregisterWorker(UnitAgent worker)
    {
        if (worker == null || !allWorkers.Contains(worker)) return;
        
        allWorkers.Remove(worker);
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
        hiveActivityRadius = 2 + hiveRangeLevel;

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
            var hive = unit.GetComponent<Hive>();
            if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
            if (!unit.gameObject.activeInHierarchy) continue;

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
            var hive = unit.GetComponent<Hive>();
            if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
            if (!unit.gameObject.activeInHierarchy) continue;

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
            var hive = unit.GetComponent<Hive>();
            if (hive != null) continue;

            // 하이브 안에 있는 여왕벌 제외 (비활성화 상태) ?
            if (!unit.gameObject.activeInHierarchy) continue;

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
        return 200 + (hiveHealthLevel * 30);
    }

    public int GetMaxWorkers()
    {
        return 3 + (maxWorkersLevel * 3);
    }

    public int GetGatherAmount()
    {
        return 1 + gatherAmountLevel;
    }
}
