using UnityEngine;

public interface ICommand
{
    string Id { get; }
    string DisplayName { get; }
    Sprite Icon { get; }
    bool RequiresTarget { get; }
    bool HidePanelOnClick { get; }
    
    // 명령 비용 정보
    int ResourceCost { get; }
    string CostText { get; }

    bool IsAvailable(UnitAgent agent);
    void Execute(UnitAgent agent, CommandTarget target);

    public string LevelCostText(int level);
    
    // ✅ 현재 레벨 가져오기 (업그레이드 명령에서만 사용)
    int GetCurrentLevel();
}
