using System.Collections.Generic;
using UnityEngine;

public enum UnitTaskType { Idle, Move, Attack, Gather, ReturnToHive, FollowQueen }

/// <summary>
/// 기본 유닛 행동 컨트롤러 (부모 클래스)
/// - QueenBehaviorController, WorkerBehaviorController가 상속
/// </summary>
public class UnitBehaviorController : MonoBehaviour
{
    [Header("Components")]
    public UnitAgent agent;
    public CombatUnit combat;
    public UnitController mover;
    public RoleAssigner role;

    [Header("Gather Settings")]
    public int gatherAmount = 1;
    public float gatherCooldown = 1.0f;
    public Color gatherColor = Color.yellow;

    [Header("Activity Settings")]
    public int activityRadius = 5;

    [Header("Current Task")]
    public UnitTaskType currentTask = UnitTaskType.Idle;
    public string currentTaskString = string.Empty;
    public HexTile targetTile;
    public UnitAgent targetUnit;

    // 작업 변경 이벤트 ✅
    public event System.Action OnTaskChanged;

    /// <summary>
    /// 작업 설정 (이벤트 발생) ✅
    /// </summary>
    public void SetCurrentTask(UnitTaskType newTask)
    {
        if (currentTask != newTask)
        {
            currentTask = newTask;
            OnTaskChanged?.Invoke();
        }
    }

    protected virtual void Start()
    {
        if (agent == null) agent = GetComponent<UnitAgent>();
        if (combat == null) combat = GetComponent<CombatUnit>();
        if (mover == null) mover = GetComponent<UnitController>();
        if (role == null) role = GetComponent<RoleAssigner>();

        // Read default activity radius from HiveManager if available
        if (HiveManager.Instance != null)
        {
            activityRadius = HiveManager.Instance.hiveActivityRadius;
        }
    }

    /// <summary>
    /// 주변 범위 내 적 유닛 찾기
    /// </summary>
    protected UnitAgent FindNearbyEnemy(int range)
    {
        if (TileManager.Instance == null) return null;
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null || unit == agent) continue;
            
            // 적 유닛만
            if (unit.faction == agent.faction) continue;
            if (unit.faction == Faction.Neutral) continue;
            
            // 무적 유닛은 제외
            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null && combat.isInvincible)
            {
                continue;
            }
            
            // 거리 체크
            int distance = Pathfinder.AxialDistance(agent.q, agent.r, unit.q, unit.r);
            if (distance <= range)
            {
                // 하이브가 있는 경우 활동 범위 체크
                if (agent.homeHive != null)
                {
                    int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, unit.q, unit.r);
                    if (distanceToHive > activityRadius)
                    {
                        continue;
                    }
                }
                // 하이브가 없는 경우 (여왕벌 모드)
                else if (agent.isQueen)
                {
                    if (distance > 1)
                    {
                        continue;
                    }
                }
                // 하이브도 없고 여왕벌도 아닌 경우 (일반 일꾼)
                else
                {
                    UnitAgent queen = FindQueenInScene();
                    if (queen != null)
                    {
                        int distanceToQueen = Pathfinder.AxialDistance(queen.q, queen.r, unit.q, unit.r);
                        if (distanceToQueen > 1)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                
                return unit;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 여왕벌 찾기
    /// </summary>
    protected UnitAgent FindQueenInScene()
    {
        var allUnits = TileManager.Instance.GetAllUnits();
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.isQueen && unit.faction == agent.faction)
            {
                return unit;
            }
        }
        return null;
    }

    /// <summary>
    /// 타일에서 적 찾기
    /// </summary>
    protected UnitAgent FindEnemyOnTile(HexTile tile)
    {
        foreach (var u in TileManager.Instance.GetAllUnits())
        {
            if (u == agent) continue;
            if (u.faction == agent.faction) continue;
            if (u.q == tile.q && u.r == tile.r)
            {
                var combat = u.GetComponent<CombatUnit>();
                if (combat != null && combat.isInvincible)
                {
                    continue;
                }
                
                return u;
            }
        }
        return null;
    }

    /// <summary>
    /// 타일로 이동 명령 (virtual - 오버라이드 가능)
    /// </summary>
    public virtual void IssueCommandToTile(HexTile tile)
    {
        // 기본 구현: 타일로 이동만
        if (tile == null) return;
        
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var path = Pathfinder.FindPath(start, tile);
        if (path != null)
        {
            mover.SetPath(path);
            currentTask = UnitTaskType.Move;
            targetTile = tile;
        }
    }

    /// <summary>
    /// 적 공격 명령 (virtual - 오버라이드 가능)
    /// </summary>
    public virtual void IssueAttackCommand(UnitAgent enemy)
    {
        // 기본 구현: 적에게 이동 후 공격
        if (enemy == null) return;
        
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = TileManager.Instance.GetTile(enemy.q, enemy.r);
        var path = Pathfinder.FindPath(start, dest);
        
        if (path != null)
        {
            mover.SetPath(path);
            currentTask = UnitTaskType.Attack;
            targetUnit = enemy;
        }
    }

    /// <summary>
    /// 현재 작업 취소 (virtual - 오버라이드 가능)
    /// </summary>
    public virtual void CancelCurrentTask()
    {
        currentTask = UnitTaskType.Idle;
        targetTile = null;
        targetUnit = null;
        
        if (mover != null)
        {
            mover.ClearPath();
        }
        
        Debug.Log($"[행동] {agent.name}의 작업이 취소되었습니다.");
    }
}
