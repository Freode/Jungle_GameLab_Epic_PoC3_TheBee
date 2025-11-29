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

    // 일꾼 수 변경 이벤트 (현재, 최대)
    public event System.Action<int, int> OnWorkerCountChanged;

    // 하이브 건설/파괴 이벤트
    public event System.Action OnPlayerHiveConstructed;
    public event System.Action OnPlayerHiveDestroyed;

    [Header("Worker Management")]
    public int currentWorkers = 0; // 현재 일꾼 수
    public int maxWorkers = 0; // 합계 최대 (role별 합으로 계산)
    private List<UnitAgent> allWorkers = new List<UnitAgent>();

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
    public int hiveRangeLevel = 0;
    public int workerAttackLevel = 0;
    public int workerHealthLevel = 0;

    // per-role speed levels
    public int workerSpeedLevelGatherer = 0;
    public int workerSpeedLevelAttacker = 0;
    public int workerSpeedLevelTank = 0;

    public int hiveHealthLevel = 0;

    // per-role max workers levels (each level increases that role's capacity by +2)
    public int maxWorkersLevelGatherer = 0;
    public int maxWorkersLevelAttacker = 0;
    public int maxWorkersLevelTank = 0;

    public int gatherAmountLevel = 0;

    [Header("Per-role base max workers")]
    // 초기 기본값: 채취형 2, 공격형 2, 탱커형 1 (요구사항)
    public int baseMaxWorkersGatherer = 2;
    public int baseMaxWorkersAttacker = 2;
    public int baseMaxWorkersTank = 1;

    // Flag indicating player-initiated hive construction in progress
    [HideInInspector]
    public bool isConstructingHive = false;

    // Event fired when construction state changes (true = constructing in progress)
    public event System.Action<bool> OnHiveConstructionStateChanged;

    // Set constructing state and notify listeners
    public void SetConstructingState(bool constructing)
    {
        isConstructingHive = constructing;
        OnHiveConstructionStateChanged?.Invoke(constructing);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // sync initial maxWorkers
        UpdateAllHiveMaxWorkers();
    }

    // Hive registration
    public void RegisterHive(Hive hive)
    {
        if (hive == null) return;
        if (!hives.Contains(hive)) hives.Add(hive);

        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null && hiveAgent.faction == Faction.Player)
        {
            OnPlayerHiveConstructed?.Invoke();
        }

        if (hives.Count == 1)
        {
            var bh = HexBoundaryHighlighter.Instance;
            if (bh != null)
            {
                bh.SetEnabledForHives(true);
                bh.debugLogs = false;
            }

            if (WaspWaveManager.Instance != null)
                WaspWaveManager.Instance.StartNormalWaveRoutine();
        }
    }

    public void UnregisterHive(Hive hive)
    {
        if (hive == null) return;
        if (hives.Contains(hive)) hives.Remove(hive);

        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null && hiveAgent.faction == Faction.Player)
            OnPlayerHiveDestroyed?.Invoke();

        if (hives.Count == 0)
        {
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
        Hive found = null; int best = int.MaxValue;
        foreach (var h in hives)
        {
            if (h == null) continue;
            int d = Pathfinder.AxialDistance(q, r, h.q, h.r);
            if (d < best) { best = d; found = h; }
        }
        return found;
    }

    // Resources
    public void AddResources(int amount)
    {
        playerStoredResources += amount;
        OnResourcesChanged?.Invoke();
    }

    public bool TrySpendResources(int amount)
    {
        if (playerStoredResources >= amount)
        {
            playerStoredResources -= amount;
            OnResourcesChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool HasResources(int amount) => playerStoredResources >= amount;

    // Store original vision ranges for units when temporarily reduced during hive relocation
    private Dictionary<int, int> savedVisionRanges = new Dictionary<int, int>();

    // Reduce vision for hive and queen to 1 during relocation and update FogOfWar
    public void ReduceVisionForRelocation(Hive hive)
    {
        if (hive == null) return;

        // Queen
        if (hive.queenBee != null)
        {
            SaveAndApplyVision(hive.queenBee, 1);
        }

        // Hive agent (the hive itself may have a UnitAgent)
        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null)
        {
            SaveAndApplyVision(hiveAgent, 1);
        }
    }

    // Restore vision for hive and queen after landing
    public void RestoreVisionAfterLanding(Hive hive)
    {
        if (hive == null) return;

        if (hive.queenBee != null)
        {
            RestoreVision(hive.queenBee);
        }

        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null)
        {
            RestoreVision(hiveAgent);
        }
    }

    // Restore vision if hive is destroyed while relocating
    public void RestoreVisionOnHiveDestroyed(Hive hive)
    {
        if (hive == null) return;
        if (hive.queenBee != null)
        {
            RestoreVision(hive.queenBee);
        }
        var hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent != null)
        {
            RestoreVision(hiveAgent);
        }
    }

    private void SaveAndApplyVision(UnitAgent agent, int newVision)
    {
        if (agent == null) return;
        if (!savedVisionRanges.ContainsKey(agent.id))
        {
            savedVisionRanges[agent.id] = agent.visionRange;
        }
        agent.visionRange = newVision;
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.UpdateUnitPosition(agent.id, agent.q, agent.r, agent.visionRange);
        }
    }

    private void RestoreVision(UnitAgent agent)
    {
        if (agent == null) return;
        if (savedVisionRanges.TryGetValue(agent.id, out int original))
        {
            agent.visionRange = original;
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UpdateUnitPosition(agent.id, agent.q, agent.r, agent.visionRange);
            }
            savedVisionRanges.Remove(agent.id);
        }
    }

    // Register worker and assign to smallest squad with capacity.
    public void RegisterWorker(UnitAgent worker, WorkerSquad? desiredSquad = null)
    {
        if (worker == null) return;
        if (allWorkers.Contains(worker)) return;

        // candidate list: desired first then squads ordered by size
        List<WorkerSquad> candidates = new List<WorkerSquad>();
        if (desiredSquad.HasValue) candidates.Add(desiredSquad.Value);
        candidates.AddRange(GetSquadsOrderedBySize());

        WorkerSquad assign = WorkerSquad.Squad1; bool assigned = false;
        foreach (var sq in candidates)
        {
            RoleType role = SquadToRole(sq);
            if (GetSquadCount(sq) < GetMaxWorkersForRole(role))
            {
                assign = sq; assigned = true; break;
            }
        }

        if (!assigned)
        {
            Debug.LogWarning("[HiveManager] 모든 부대가 최대 인원에 도달했습니다. 일꾼 등록 불가.");
            return;
        }

        allWorkers.Add(worker);
        AssignWorkerToSquad(worker, assign);
        currentWorkers = allWorkers.Count;
        UpdateAllHiveMaxWorkers();
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);

        Debug.Log($"[HiveManager] 일꾼 등록: {currentWorkers}/{maxWorkers}, 부대: {assign}");
    }

    private List<WorkerSquad> GetSquadsOrderedBySize()
    {
        var list = new List<WorkerSquad> { WorkerSquad.Squad1, WorkerSquad.Squad2, WorkerSquad.Squad3 };
        list.Sort((a, b) => GetSquadCount(a).CompareTo(GetSquadCount(b)));
        return list;
    }

    private WorkerSquad GetSmallestSquad()
    {
        foreach (var sq in GetSquadsOrderedBySize())
        {
            if (GetSquadCount(sq) < GetMaxWorkersForRole(SquadToRole(sq))) return sq;
        }
        return WorkerSquad.Squad1;
    }

    private WorkerSquad GetSmallestSquadForAssignment(WorkerSquad preferred)
    {
        if (!squadWorkers.ContainsKey(preferred)) return WorkerSquad.Squad1;
        return preferred;
    }

    private RoleType SquadToRole(WorkerSquad sq)
    {
        switch (sq)
        {
            case WorkerSquad.Squad1: return RoleType.Gatherer;
            case WorkerSquad.Squad2: return RoleType.Attacker;
            case WorkerSquad.Squad3: return RoleType.Tank;
            default: return RoleType.Gatherer;
        }
    }

    private void AssignWorkerToSquad(UnitAgent worker, WorkerSquad squad)
    {
        if (worker == null) return;

        // set role
        var roleAssigner = worker.GetComponent<RoleAssigner>();
        if (roleAssigner == null) roleAssigner = worker.gameObject.AddComponent<RoleAssigner>();
        roleAssigner.SetRole(SquadToRole(squad));

        // color
        Color col = GetSquadColor(squad);
        worker.SetOriginalColor(col);
        var sprite = worker.GetComponent<SpriteRenderer>(); if (sprite != null) sprite.color = col;
        var renderer = worker.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", col);
            mpb.SetColor("_BaseColor", col);
            renderer.SetPropertyBlock(mpb);
        }

        if (!squadWorkers[squad].Contains(worker)) squadWorkers[squad].Add(worker);

        float targetScale = squad == WorkerSquad.Squad3 ? 1.35f : 1f;
        worker.transform.localScale = Vector3.one * targetScale;
        StartCoroutine(EnsureScaleApplied(worker, targetScale));

        // apply pheromone command if exists
        if (PheromoneManager.Instance != null)
        {
            var pherList = PheromoneManager.Instance.GetPheromonePositionsOrdered(squad);
            if (pherList != null && pherList.Count > 0)
            {
                // 균등 분배를 위한 타겟 선택
                Vector2Int target = ChooseBalancedPheromoneTarget(squad, pherList);
                StartCoroutine(ApplyPheromoneToWorkerDelayed(worker, squad, target));
            }
        }
    }

    private System.Collections.IEnumerator ApplyPheromoneToWorkerDelayed(UnitAgent worker, WorkerSquad squad, Vector2Int pheromonePos)
    {
        yield return null;
        if (worker == null) yield break;
        var targetTile = TileManager.Instance?.GetTile(pheromonePos.x, pheromonePos.y);
        if (targetTile == null) yield break;
        var behavior = worker.GetComponent<WorkerBehaviorController>();
        if (behavior != null)
        {
            behavior.CancelCurrentTask();
            worker.hasManualOrder = true;
            worker.hasManualTarget = true;
            worker.manualTargetCoord = pheromonePos;
            behavior.IssueCommandToTile(targetTile);
        }
    }

    /// <summary>
    /// 페로몬 목록 중 가장 균등한 타겟 좌표 선택 (현재 부대 배치 기반)
    /// </summary>
    private Vector2Int ChooseBalancedPheromoneTarget(WorkerSquad squad, List<Vector2Int> pheromonePositions)
    {
        if (pheromonePositions == null || pheromonePositions.Count == 0)
        {
            return pheromonePosFallback();
        }

        Dictionary<Vector2Int, int> counts = new Dictionary<Vector2Int, int>();
        foreach (var pos in pheromonePositions)
        {
            if (!counts.ContainsKey(pos)) counts[pos] = 0;
        }

        // 기존 부대원의 목표 카운트
        var squadWorkers = GetSquadWorkers(squad);
        foreach (var w in squadWorkers)
        {
            if (w == null) continue;
            if (w.hasManualTarget && counts.ContainsKey(w.manualTargetCoord))
            {
                counts[w.manualTargetCoord]++;
            }
        }

        int minCount = int.MaxValue;
        foreach (var val in counts.Values)
        {
            if (val < minCount) minCount = val;
        }

        foreach (var pos in pheromonePositions)
        {
            if (counts[pos] == minCount)
            {
                return pos;
            }
        }

        return pheromonePosFallback();

        Vector2Int pheromonePosFallback()
        {
            return pheromonePositions != null && pheromonePositions.Count > 0 ? pheromonePositions[0] : Vector2Int.zero;
        }
    }

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

    private void ApplySquadColor(UnitAgent worker, Color color)
    {
        if (worker == null) return;
        worker.SetOriginalColor(color);
        var sprite = worker.GetComponent<SpriteRenderer>(); if (sprite != null) sprite.color = color;
    }

    // Public accessors used by UI
    public List<UnitAgent> GetAllWorkers()
    {
        allWorkers.RemoveAll(w => w == null);
        currentWorkers = allWorkers.Count;
        return new List<UnitAgent>(allWorkers);
    }

    public List<UnitAgent> GetSquadWorkers(WorkerSquad squad)
    {
        if (!squadWorkers.ContainsKey(squad)) return new List<UnitAgent>();
        squadWorkers[squad].RemoveAll(w => w == null);
        return new List<UnitAgent>(squadWorkers[squad]);
    }

    public int GetSquadCount(WorkerSquad squad)
    {
        if (!squadWorkers.ContainsKey(squad)) return 0;
        squadWorkers[squad].RemoveAll(w => w == null);
        return squadWorkers[squad].Count;
    }

    public int GetCurrentWorkers()
    {
        allWorkers.RemoveAll(w => w == null);
        currentWorkers = allWorkers.Count;
        return currentWorkers;
    }

    public void UnregisterWorker(UnitAgent worker)
    {
        if (worker == null) return;
        allWorkers.Remove(worker);
        foreach (var kv in squadWorkers) if (kv.Value.Contains(worker)) kv.Value.Remove(worker);
        allWorkers.RemoveAll(w => w == null);
        currentWorkers = allWorkers.Count;
        UpdateAllHiveMaxWorkers();
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    // Per-role capacity getters
    public int GetMaxWorkersForRole(RoleType role)
    {
        switch (role)
        {
            case RoleType.Attacker: return baseMaxWorkersAttacker + (maxWorkersLevelAttacker * 2);
            case RoleType.Gatherer: return baseMaxWorkersGatherer + (maxWorkersLevelGatherer * 2);
            case RoleType.Tank: return baseMaxWorkersTank + (maxWorkersLevelTank * 2);
            default: return baseMaxWorkersGatherer + (maxWorkersLevelGatherer * 2);
        }
    }

    public int GetMaxWorkers()
    {
        return GetMaxWorkersForRole(RoleType.Gatherer)
            + GetMaxWorkersForRole(RoleType.Attacker)
            + GetMaxWorkersForRole(RoleType.Tank);
    }

    // Helper: role display label (short form used in parentheses)
    private string GetRoleDisplayName(RoleType role)
    {
        switch (role)
        {
            case RoleType.Attacker: return "공격형";
            case RoleType.Gatherer: return "채취형";
            case RoleType.Tank: return "탱커형";
            default: return "일벌";
        }
    }

    // Event fired when any upgrade is applied (so UIs can refresh)
    public event System.Action OnUpgradeApplied;

    // Role-specific max upgrade (+2 per level)
    public bool UpgradeMaxWorkersForRole(RoleType role, int cost)
    {
        if (!TrySpendResources(cost)) return false;
        switch (role)
        {
            case RoleType.Attacker: maxWorkersLevelAttacker++; break;
            case RoleType.Gatherer: maxWorkersLevelGatherer++; break;
            case RoleType.Tank: maxWorkersLevelTank++; break;
            default: maxWorkersLevelGatherer++; break;
        }
        UpdateAllHiveMaxWorkers();

        string roleShort = GetRoleDisplayName(role);
        if (UpgradeResultUI.Instance != null)
            UpgradeResultUI.Instance.ShowUpgradeResult("확장 군집", $"일벌({roleShort}) 최대 일꾼 +2", $"<color=#00FF00>{GetMaxWorkersForRole(role)}마리</color>");

        // notify listeners about upgrade
        OnUpgradeApplied?.Invoke();
        return true;
    }

    public bool UpgradeWorkerSpeedForRole(RoleType role, int cost)
    {
        if (!TrySpendResources(cost)) return false;
        switch (role)
        {
            case RoleType.Attacker: workerSpeedLevelAttacker++; RefreshRoleFor(RoleType.Attacker); break;
            case RoleType.Gatherer: workerSpeedLevelGatherer++; RefreshRoleFor(RoleType.Gatherer); break;
            case RoleType.Tank: workerSpeedLevelTank++; RefreshRoleFor(RoleType.Tank); break;
            default: workerSpeedLevelGatherer++; RefreshRoleFor(RoleType.Gatherer); break;
        }

        // apply updates to units
        UpdateAllWorkerSpeed();

        // show UI result with localized role name and new speed
        string roleName = GetRoleDisplayName(role);
        // prefer actual unit controller value if available
        float displaySpeed = GetWorkerSpeed(role);
        var sample = FindPlayerUnitWithRole(role);
        if (sample != null)
        {
            var ctrl = sample.GetComponent<UnitController>();
            if (ctrl != null) displaySpeed = ctrl.moveSpeed;
        }

        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult("빠른 날개", $"일벌({roleName}) 이동 속도 증가", $"<color=#00FF00>{displaySpeed:F2} 속도</color>");
        }

        // notify listeners about upgrade
        OnUpgradeApplied?.Invoke();
        
        return true;
    }

    // Simplified other upgrades
    public bool UpgradeHiveRange(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        hiveRangeLevel++;
        hiveActivityRadius = 4 + hiveRangeLevel;

        foreach (var h in hives)
            if (h != null && HexBoundaryHighlighter.Instance != null)
                HexBoundaryHighlighter.Instance.ShowBoundary(h, hiveActivityRadius);

        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult("활동 범위 확장", "일벌 활동 범위 +1", $"<color=#00FF00>{hiveActivityRadius}칸</color>");
        }

        OnUpgradeApplied?.Invoke();
        return true;
    }

    public bool UpgradeWorkerAttack(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        workerAttackLevel++;
        // Apply to attacker-role units
        RefreshRoleFor(RoleType.Attacker);
        UpdateAllWorkerCombat();

        if (UpgradeResultUI.Instance != null)
        {
            // show actual attacker unit attack if present
            int displayAttack = GetWorkerAttack();
            var sample = FindPlayerUnitWithRole(RoleType.Attacker);
            if (sample != null)
            {
                var combat = sample.GetComponent<CombatUnit>();
                if (combat != null) displayAttack = combat.attack;
            }
            UpgradeResultUI.Instance.ShowUpgradeResult("날카로운 침", "일벌(공격형) 공격력 +1", $"<color=#00FF00>{displayAttack}</color>");
        }

        OnUpgradeApplied?.Invoke();
        return true;
    }

    public bool UpgradeWorkerHealth(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        workerHealthLevel++;
        // Apply to tank-role units
        RefreshRoleFor(RoleType.Tank);
        UpdateAllWorkerCombat();

        if (UpgradeResultUI.Instance != null)
        {
            // show actual tank unit max health if present
            int displayMax = GetWorkerMaxHealth();
            var sample = FindPlayerUnitWithRole(RoleType.Tank);
            if (sample != null)
            {
                var combat = sample.GetComponent<CombatUnit>();
                if (combat != null) displayMax = combat.maxHealth;
            }
            UpgradeResultUI.Instance.ShowUpgradeResult("강화 외골격", "일벌(탱커형) 최대 체력 +2", $"<color=#00FF00>{displayMax} HP</color>");
        }

        OnUpgradeApplied?.Invoke();
        return true;
    }

    public bool UpgradeHiveHealth(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        hiveHealthLevel++;
        UpdateAllHiveHealth();

        if (UpgradeResultUI.Instance != null)
        {
            UpgradeResultUI.Instance.ShowUpgradeResult("강화 성벽", "꿀벌집 체력 +30", $"<color=#00FF00>{GetHiveMaxHealth()} HP</color>");
        }

        OnUpgradeApplied?.Invoke();
        return true;
    }

    public bool UpgradeGatherAmount(int cost)
    {
        if (!TrySpendResources(cost)) return false;

        gatherAmountLevel++;
        UpdateAllWorkerGatherAmount();

        if (UpgradeResultUI.Instance != null)
        {
            int displayGather = GetGatherAmount();
            var sample = FindPlayerUnitWithRole(RoleType.Gatherer);
            if (sample != null)
            {
                var behavior = sample.GetComponent<UnitBehaviorController>();
                if (behavior != null) displayGather = behavior.gatherAmount;
            }
            UpgradeResultUI.Instance.ShowUpgradeResult("효율적 채집", "일벌(채취형) 채집량 +1", $"<color=#00FF00>{displayGather}</color>");
        }

        OnUpgradeApplied?.Invoke();
        return true;
    }

    void UpdateAllWorkerCombat()
    {
        if (TileManager.Instance == null) return;
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;
            var ra = unit.GetComponent<RoleAssigner>();
            if (ra != null)
            {
                // Let RoleAssigner reapply role-specific stats (includes global upgrades when applicable)
                ra.RefreshRole();
                continue;
            }

            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null)
            {
                combat.SetAttack(GetWorkerAttack());
                int newMax = GetWorkerMaxHealth();
                combat.SetMaxHealth(newMax);
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
                int newMax = GetHiveMaxHealth();

                float ratio = 1f;
                if (oldMax > 0) ratio = (float)combat.health / oldMax;

                // Use CombatUnit API to ensure events are fired and health clamped correctly
                combat.SetMaxHealth(newMax);

                if (oldMax > 0)
                {
                    int adjustedHealth = Mathf.RoundToInt(newMax * ratio);
                    combat.SetHealth(Mathf.Clamp(adjustedHealth, 0, newMax));
                }
                else
                {
                    combat.SetHealth(newMax);
                }
            }
        }
    }

    void UpdateAllHiveMaxWorkers()
    {
        maxWorkers = GetMaxWorkers();
        foreach (var hive in hives) if (hive != null) hive.maxWorkers = maxWorkers;
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    public void NotifyWorkerCountChanged()
    {
        OnWorkerCountChanged?.Invoke(currentWorkers, maxWorkers);
    }

    void UpdateAllWorkerGatherAmount()
    {
        if (TileManager.Instance == null) return;
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;
            var roleAssigner = unit.GetComponent<RoleAssigner>();
            // only apply to gatherer role units; refresh role to let RoleAssigner handle bonuses
            if (roleAssigner != null && roleAssigner.role == RoleType.Gatherer)
            {
                roleAssigner.RefreshRole();
            }
        }
    }

    // Stats
    public int GetWorkerAttack() => 3 + workerAttackLevel;
    public int GetWorkerMaxHealth() => 10 + (workerHealthLevel * 2);
    public float GetWorkerSpeed(RoleType role)
    {
        float baseSpeed = 2.0f; int level = 0;
        switch (role)
        {
            case RoleType.Attacker: level = workerSpeedLevelAttacker; break;
            case RoleType.Gatherer: level = workerSpeedLevelGatherer; break;
            case RoleType.Tank: level = workerSpeedLevelTank; break;
        }
        return baseSpeed + (float)level / 5f;
    }
    public float GetWorkerSpeedBonusForRole(RoleType role) => (GetWorkerSpeed(role) - 2.0f);
    public int GetHiveMaxHealth() => 100 + (hiveHealthLevel * 30);
    public int GetGatherAmount() => 1 + gatherAmountLevel;

    private System.Collections.IEnumerator EnsureScaleApplied(UnitAgent worker, float targetScale)
    {
        yield return null;
        if (worker == null) yield break;
        worker.transform.localScale = Vector3.one * targetScale;
    }

    void RefreshRoleFor(RoleType role)
    {
        if (TileManager.Instance == null) return;
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;
            var ra = unit.GetComponent<RoleAssigner>();
            if (ra != null && ra.role == role) ra.RefreshRole();
        }
    }

    public void PlayResourceStealEffect(Vector3 fromPos, Vector3 toPos, int amount)
    {
        if (honeyProjectilePrefab == null) return;
        StartCoroutine(StealEffectRoutine(fromPos, toPos, amount));
    }

    System.Collections.IEnumerator StealEffectRoutine(Vector3 fromPos, Vector3 toPos, int amount)
    {
        int projectileCount = Mathf.Clamp(amount / 10, 1, 30);
        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f; randomOffset.y = 0;
            GameObject honeyObj = Instantiate(honeyProjectilePrefab, fromPos + randomOffset, Quaternion.identity);
            var projectile = honeyObj.GetComponent<HoneyProjectile>(); if (projectile != null) projectile.Initialize(fromPos + randomOffset, toPos);
            yield return new WaitForSeconds(0.05f);
        }
    }
    public bool HasPlayerHive()
    {
        foreach (var hive in GetAllHives())
        {
            // 하이브의 UnitAgent를 가져와서 팩션 확인
            var agent = hive.GetComponent<UnitAgent>();
            if (agent != null && agent.faction == Faction.Player)
            {
                return true;
            }
        }
        return false;
    }
    private Coroutine constructionRoutine;

    public void StartConstructionCasting(UnitAgent builder, float duration, System.Action onComplete)
    {
        if (constructionRoutine != null)
        {
            if (NotificationToast.Instance != null) NotificationToast.Instance.ShowMessage("이미 건설 작업 중입니다.", 1.5f);
            return;
        }

        constructionRoutine = StartCoroutine(ConstructionCasting(builder, duration, onComplete));
    }

    private System.Collections.IEnumerator ConstructionCasting(UnitAgent builder, float duration, System.Action onComplete)
    {
        float timer = duration;
        Vector3 startPos = builder.transform.position; // 건설자(여왕벌)의 시작 위치

        // 시작 알림
        if (NotificationToast.Instance != null)
            NotificationToast.Instance.ShowMessage($"{duration}초 뒤 건설합니다. 움직이면 취소됩니다.", 2f);
        
        Debug.Log($"[HiveManager] 건설 준비... ({duration}초)");

        while (timer > 0)
        {
            // 1. 건설자 사망 체크
            if (builder == null)
            {
                constructionRoutine = null;
                yield break;
            }

            // 2. 움직임 체크 (0.1f 이상 이동 시 취소)
            if (Vector3.Distance(builder.transform.position, startPos) > 0.1f)
            {
                if (NotificationToast.Instance != null)
                    NotificationToast.Instance.ShowMessage("이동하여 건설이 취소되었습니다.", 1.5f);
                
                Debug.Log("[HiveManager] 건설자가 움직여서 취소됨.");
                constructionRoutine = null;
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        // 완료
        if (NotificationToast.Instance != null)
            NotificationToast.Instance.ShowMessage("건설 완료!", 1.5f);

        constructionRoutine = null;
        
        // 실제 건설 로직 실행
        onComplete?.Invoke();
    }

    // Backwards-compatible wrappers for legacy callers
    public bool UpgradeWorkerSpeed(int cost)
    {
        return UpgradeWorkerSpeedForRole(RoleType.Gatherer, cost);
    }

    public bool UpgradeMaxWorkers(int cost)
    {
        return UpgradeMaxWorkersForRole(RoleType.Gatherer, cost);
    }

    // Ensure worker speed updates are applied to UnitController components
    void UpdateAllWorkerSpeed()
    {
        if (TileManager.Instance == null) return;
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.isQueen || unit.faction != Faction.Player) continue;
            var controller = unit.GetComponent<UnitController>();
            if (controller != null)
            {
                var ra = unit.GetComponent<RoleAssigner>();
                controller.moveSpeed = ra != null ? GetWorkerSpeed(ra.role) : GetWorkerSpeed(RoleType.Gatherer);
            }
        }
    }

    // Helper: find a player unit with given role to sample runtime-applied stats
    private UnitAgent FindPlayerUnitWithRole(RoleType role)
    {
        if (TileManager.Instance == null) return null;
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit.faction != Faction.Player || unit.isQueen) continue;
            var ra = unit.GetComponent<RoleAssigner>();
            if (ra != null && ra.role == role) return unit;
        }
        return null;
    }
}
