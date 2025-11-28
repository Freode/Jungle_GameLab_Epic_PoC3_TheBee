using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 여왕벌 전용 행동 컨트롤러
/// - 우클릭으로 이동만 가능
/// - 이동 중 다른 타일 클릭 시 즉시 경로 재계산
/// - 여왕벌 전용 명령 제공 (건설, 이사, 페르몬)
/// </summary>
public class QueenBehaviorController : UnitBehaviorController, IUnitCommandProvider
{
    [Header("여왕벌 명령")]
    public SOCommand[] queenCommands; // 여왕벌 전용 명령 (Inspector에서 할당)
    
    [Header("여왕벌 하이브 위 회복")]
    [Tooltip("여왕이 하이브 위에 있을 때 회복 주기(초)")]
    public float queenHealInterval = 1.0f;
    [Tooltip("여왕이 하이브 위에 있을 때 주기당 회복량")]
    public int queenHealAmount = 1;

    private float healTimer = 0f;
    
    void Update()
    {
        // 기본 Update는 실행하지 않음 (Idle 이동, 적 감지 등 불필요)
        HandleHealingOnHive();
    }

    /// <summary>
    /// 여왕벌 이동 명령 (우클릭)
    /// </summary>
    public void MoveToTile(HexTile targetTile)
    {
        if (targetTile == null || agent == null || mover == null) return;

        // 현재 위치에서 목표 타일까지 경로 찾기
        var currentTile = TileManager.Instance.GetTile(agent.q, agent.r);
        if (currentTile == null) return;

        // 경로 계산
        var path = Pathfinder.FindPath(currentTile, targetTile);
        if (path != null && path.Count > 0)
        {
            // 이동 중이어도 즉시 새 경로로 갱신 ?
            mover.SetPath(path);
            
            currentTask = UnitTaskType.Move;
            this.targetTile = targetTile;
            
            Debug.Log($"[여왕벌] {agent.name} 이동 명령: ({currentTile.q}, {currentTile.r}) → ({targetTile.q}, {targetTile.r})");
        }
        else
        {
            Debug.LogWarning($"[여왕벌] 경로를 찾을 수 없습니다: ({currentTile.q}, {currentTile.r}) → ({targetTile.q}, {targetTile.r})");
        }
    }

    /// <summary>
    /// 타일 명령 (우클릭) - 이동만 수행
    /// </summary>
    public override void IssueCommandToTile(HexTile tile)
    {
        MoveToTile(tile);
    }
    
    /// <summary>
    /// IUnitCommandProvider 구현 - 여왕벌 명령 제공
    /// </summary>
    public IEnumerable<ICommand> GetCommands(UnitAgent agent)
    {
        var commands = new List<ICommand>();
        
        if (queenCommands != null)
        {
            foreach (var cmd in queenCommands)
            {
                if (cmd != null)
                {
                    commands.Add(cmd);
                }
            }
        }
        
        return commands;
    }

    /// <summary>
    /// 여왕이 하이브 위에 있을 때 주기적으로 체력 회복
    /// </summary>
    private void HandleHealingOnHive()
    {
        if (agent == null) return;

        // homeHive가 비어 있으면 같은 타일의 하이브를 찾아 자동 설정
        if (agent.homeHive == null && TileManager.Instance != null)
        {
            // Tile 구조에 하이브 참조가 없다면 그대로 유지
        }

        if (agent.homeHive == null) return;

        // 같은 타일에 있는지 체크
        if (agent.q == agent.homeHive.q && agent.r == agent.homeHive.r)
        {
            healTimer += Time.deltaTime;
            if (healTimer >= queenHealInterval)
            {
                var combat = agent.GetComponent<CombatUnit>();
                if (combat != null)
                {
                    combat.SetHealth(combat.health + queenHealAmount);
                }
                healTimer = 0f;
            }
        }
        else
        {
            healTimer = 0f;
        }
    }
}
