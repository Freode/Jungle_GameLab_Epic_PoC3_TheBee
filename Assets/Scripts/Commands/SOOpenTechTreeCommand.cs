using UnityEngine;

/// <summary>
/// 테크 트리 UI를 여는 명령
/// 타겟이 필요없고, 클릭 시 패널을 숨기지 않음
/// </summary>
[CreateAssetMenu(menuName = "Commands/OpenTechTreeCommand", fileName = "OpenTechTreeCommand")]
public class SOOpenTechTreeCommand : SOCommand
{
    void OnEnable()
    {
        // 테크 트리 명령은 타겟이 필요없음
        requiresTarget = false;
        
        // 클릭해도 명령 패널을 숨기지 않음
        hidePanelOnClick = false;
        
        // 비용 없음
        resourceCost = 0;
    }

    public override bool IsAvailable(UnitAgent agent)
    {
        // 하이브에서만 테크 트리 열기 가능
        if (agent == null) return false;
        
        Hive hive = agent.GetComponent<Hive>();
        if (hive == null) return false;
        
        // 아군 하이브에서만 가능 (UnitAgent의 faction 확인)
        return agent.faction == Faction.Player;
    }

    public override void Execute(UnitAgent agent, CommandTarget target)
    {
        // TechTreeUI 열기
        if (TechTreeUI.Instance != null)
        {
            TechTreeUI.Instance.OpenTechTreePanel();
            Debug.Log("[OpenTechTreeCommand] 테크 트리 UI 열림");
        }
        else
        {
            Debug.LogWarning("[OpenTechTreeCommand] TechTreeUI.Instance를 찾을 수 없습니다!");
        }
    }
}
