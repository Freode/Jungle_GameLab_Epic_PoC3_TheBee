using UnityEngine;

public static class UpgradeCommandHandler
{
    public static bool ExecuteUpgrade(UpgradeType upgradeType, int cost)
    {
        if (HiveManager.Instance == null)
        {
            Debug.LogWarning("[업그레이드] HiveManager를 찾을 수 없습니다!");
            return false;
        }

        bool success = false;

        switch (upgradeType)
        {
            case UpgradeType.HiveRange:
                success = HiveManager.Instance.UpgradeHiveRange(cost);
                break;
            case UpgradeType.WorkerAttack:
                success = HiveManager.Instance.UpgradeWorkerAttack(cost);
                break;
            case UpgradeType.WorkerHealth:
                success = HiveManager.Instance.UpgradeWorkerHealth(cost);
                break;
            case UpgradeType.WorkerSpeed:
                success = HiveManager.Instance.UpgradeWorkerSpeed(cost);
                break;
            case UpgradeType.HiveHealth:
                success = HiveManager.Instance.UpgradeHiveHealth(cost);
                break;
            case UpgradeType.MaxWorkers:
                success = HiveManager.Instance.UpgradeMaxWorkers(cost);
                break;
            case UpgradeType.GatherAmount:
                success = HiveManager.Instance.UpgradeGatherAmount(cost);
                break;
        }

        if (!success)
        {
            Debug.Log($"[업그레이드] 실패! (자원 부족: 필요 {cost})");
        }

        return success;
    }
}

public enum UpgradeType
{
    HiveRange,      // 하이브 활동 범위 +1
    WorkerAttack,   // 일꾼 공격력 +1
    WorkerHealth,   // 일꾼 체력 +5
    WorkerSpeed,    // 일꾼 이동 속도 +1
    HiveHealth,     // 하이브 체력 +30
    MaxWorkers,     // 최대 일꾼 수 +5
    GatherAmount    // 자원 채취량 +2
}
