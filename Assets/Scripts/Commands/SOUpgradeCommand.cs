using UnityEngine;

/// <summary>
/// 업그레이드 전용 명령 - SOCommand를 상속받아 업그레이드 타입을 지정할 수 있습니다.
/// Inspector에서 upgradeType을 선택하면 해당 업그레이드가 실행됩니다.
/// </summary>
[CreateAssetMenu(menuName = "Commands/UpgradeCommand", fileName = "UpgradeCommand")]
public class SOUpgradeCommand : SOCommand
{
    [Header("업그레이드 설정")]
    [Tooltip("업그레이드 종류를 선택하세요")]
    public UpgradeType upgradeType;

    // New: target role for role-specific upgrades (e.g., WorkerSpeed)
    [Tooltip("역할별 업그레이드 대상 (일꾼 속도 등)")]
    public RoleType targetRole = RoleType.Gatherer;

    [Header("비용 설정")]
    [Tooltip("초기 업그레이드 비용 (매 업그레이드마다 이 값만큼 증가)")]
    public int baseCost = 10; // 초기 비용

    [Tooltip("최대 업그레이드 레벨 (0 = 무제한)")]
    public int maxLevel = 0; // 최대 레벨

    public override bool IsAvailable(UnitAgent agent)
    {
        if (agent == null) return false;

        // 하이브에서만 업그레이드 가능
        Hive hive = agent.GetComponent<Hive>();
        if (hive == null) return false;

        // 최대 레벨 체크
        if (maxLevel > 0 && HiveManager.Instance != null)
        {
            int currentLevel = GetCurrentLevel(upgradeType);
            if (currentLevel >= maxLevel)
            {
                return false; // 최대 레벨 도달
            }
        }

        // 현재 비용 계산
        int currentCost = GetCurrentCost();

        // 자원 확인
        if (currentCost > 0 && HiveManager.Instance != null)
        {
            if (!HiveManager.Instance.HasResources(currentCost))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 현재 비용 계산 (레벨에 따라 증가)
    /// </summary>
    public int GetCurrentCost()
    {
        if (HiveManager.Instance == null) return baseCost;

        int currentLevel = GetCurrentLevel(upgradeType);
        return baseCost + (baseCost * currentLevel);
    }

    /// <summary>
    /// 현재 레벨 가져오기
    /// </summary>
    int GetCurrentLevel(UpgradeType type)
    {
        if (HiveManager.Instance == null) return 0;

        switch (type)
        {
            case UpgradeType.HiveRange:
                return HiveManager.Instance.hiveRangeLevel;
            case UpgradeType.WorkerAttack:
                return HiveManager.Instance.workerAttackLevel;
            case UpgradeType.WorkerHealth:
                return HiveManager.Instance.workerHealthLevel;
            case UpgradeType.WorkerSpeed:
                // role-specific: return level for targetRole
                switch (targetRole)
                {
                    case RoleType.Attacker: return HiveManager.Instance.workerSpeedLevelAttacker;
                    case RoleType.Gatherer: return HiveManager.Instance.workerSpeedLevelGatherer;
                    case RoleType.Tank: return HiveManager.Instance.workerSpeedLevelTank;
                    default: return HiveManager.Instance.workerSpeedLevelGatherer;
                }
            case UpgradeType.HiveHealth:
                return HiveManager.Instance.hiveHealthLevel;
            case UpgradeType.MaxWorkers:
                return HiveManager.Instance.maxWorkersLevel;
            case UpgradeType.GatherAmount:
                return HiveManager.Instance.gatherAmountLevel;
            default:
                return 0;
        }
    }
    
    // ✅ ICommand.GetCurrentLevel() 구현 (public으로 오버라이드)
    public override int GetCurrentLevel()
    {
        return GetCurrentLevel(upgradeType);
    }

    public override void Execute(UnitAgent agent, CommandTarget target)
    {
        // 현재 비용으로 업그레이드 실행
        int currentCost = GetCurrentCost();

        if (upgradeType == UpgradeType.WorkerSpeed)
        {
            // role-specific path
            if (HiveManager.Instance != null)
            {
                HiveManager.Instance.UpgradeWorkerSpeedForRole(targetRole, currentCost);
            }
            else
            {
                Debug.LogWarning("[업그레이드] HiveManager 없음: UpgradeWorkerSpeedForRole 호출 불가");
            }
            return;
        }

        // fallback to existing handler for other upgrade types
        UpgradeCommandHandler.ExecuteUpgrade(upgradeType, currentCost);
    }
    
    // CostText 오버라이드 (현재 비용과 레벨 표시) ✅
    public new string CostText
    {
        get
        {
            int currentCost = GetCurrentCost();
            int currentLevel = HiveManager.Instance != null ? GetCurrentLevel(upgradeType) : 0;
            
            string levelText = maxLevel > 0 ? $" (Lv.{currentLevel}/{maxLevel})" : $" (Lv.{currentLevel})";
            
            if (currentCost <= 0) return levelText;
            return $"꿀: <color=#00FF00>{currentCost}</color>{levelText}";
        }
    }
}
