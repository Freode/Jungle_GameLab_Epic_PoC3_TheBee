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

    public override bool IsAvailable(UnitAgent agent)
    {
        if (agent == null) return false;

        // 하이브에서만 업그레이드 가능
        Hive hive = agent.GetComponent<Hive>();
        if (hive == null) return false;

        // 자원 확인
        if (resourceCost > 0 && HiveManager.Instance != null)
        {
            if (!HiveManager.Instance.HasResources(resourceCost))
            {
                return false;
            }
        }

        return true;
    }

    public override void Execute(UnitAgent agent, CommandTarget target)
    {
        // UpgradeCommandHandler에 위임
        UpgradeCommandHandler.ExecuteUpgrade(upgradeType, resourceCost);
    }
}
