using System.Collections.Generic;
using UnityEngine;

public enum UnitTaskType { Idle, Move, Attack, Gather, ReturnToHive }

public class UnitBehaviorController : MonoBehaviour
{
    public UnitAgent agent;
    public CombatUnit combat;
    public UnitController mover;
    public RoleAssigner role;

    // gather settings
    public int gatherAmount = 1; // amount taken per gather action
    public float gatherCooldown = 1.0f; // seconds to wait before next gather cycle

    // activity radius from home hive
    public int activityRadius = 5;

    // Current assigned task
    public UnitTaskType currentTask = UnitTaskType.Idle;
    public HexTile targetTile;
    public UnitAgent targetUnit;

    void Start()
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

    public void IssueCommandToTile(HexTile tile)
    {
        // enforce activity radius: must be within homeHive radius
        if (agent.homeHive != null)
        {
            int d = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, tile.q, tile.r);
            if (d > activityRadius)
            {
                // out of range: ignore or optionally clamp; here we ignore the command
                Debug.Log("Target out of home hive activity radius");
                return;
            }
        }

        // Priority logic on click: 1) if enemy present -> attack, 2) else if resource available -> gather, 3) else idle
        if (tile == null) return;

        var enemy = FindEnemyOnTile(tile);
        if (enemy != null)
        {
            // attack
            currentTask = UnitTaskType.Attack;
            targetUnit = enemy;
            MoveAndAttack(enemy);
            return;
        }

        if (tile.resourceAmount > 0)
        {
            // gather
            currentTask = UnitTaskType.Gather;
            targetTile = tile;
            MoveAndGather(tile);
            return;
        }

        // nothing to do
        currentTask = UnitTaskType.Idle;
        targetTile = tile;
    }

    UnitAgent FindEnemyOnTile(HexTile tile)
    {
        // search for any UnitAgent of enemy faction on the tile using TileManager
        foreach (var u in TileManager.Instance.GetAllUnits())
        {
            if (u == agent) continue;
            if (u.faction == agent.faction) continue;
            if (u.q == tile.q && u.r == tile.r)
            {
                return u;
            }
        }
        return null;
    }

    void MoveAndAttack(UnitAgent enemy)
    {
        if (enemy == null) return;
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = TileManager.Instance.GetTile(enemy.q, enemy.r);
        var path = Pathfinder.FindPath(start, dest);
        if (path == null) return;
        // go to enemy tile
        mover.SetPath(path);
        // on arrival, naive: directly apply damage if within range
        StartCoroutine(WaitAndDealDamageRoutine(dest, enemy));
    }

    System.Collections.IEnumerator WaitAndDealDamageRoutine(HexTile dest, UnitAgent enemy)
    {
        // wait until we reach dest (approx)
        while (Vector3.Distance(transform.position, TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize)) > 0.1f)
        {
            yield return null;
        }
        // apply damage
        var c = enemy.GetComponent<CombatUnit>();
        if (c != null && agent.GetComponent<CombatUnit>() != null)
        {
            c.TakeDamage(agent.GetComponent<CombatUnit>().attack);
        }
    }

    void MoveAndGather(HexTile tile)
    {
        if (tile == null) return;
        var start = TileManager.Instance.GetTile(agent.q, agent.r);
        var dest = tile;
        var path = Pathfinder.FindPath(start, dest);
        if (path == null) return;
        mover.SetPath(path);
        // remember targetTile so repeat behavior uses same location
        targetTile = dest;
        StartCoroutine(WaitAndGatherRoutine(dest));
    }

    System.Collections.IEnumerator WaitAndGatherRoutine(HexTile dest)
    {
        // wait to arrive
        while (Vector3.Distance(transform.position, TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize)) > 0.1f)
        {
            yield return null;
        }
        // gather some amount
        int taken = dest.TakeResource(gatherAmount);
        // now return to hive (find nearest hive)
        var hive = HiveManager.Instance.FindNearestHive(dest.q, dest.r);
        if (hive != null)
        {
            var hiveTile = TileManager.Instance.GetTile(hive.q, hive.r);
            var pathBack = Pathfinder.FindPath(dest, hiveTile);
            if (pathBack != null)
            {
                mover.SetPath(pathBack);
                StartCoroutine(DeliverResourcesRoutine(hive, taken));
            }
            else
            {
                // couldn't find path back -> go idle
                currentTask = UnitTaskType.Idle;
            }
        }
        else
        {
            // no hive -> go idle
            currentTask = UnitTaskType.Idle;
        }
    }

    System.Collections.IEnumerator DeliverResourcesRoutine(Hive hive, int amount)
    {
        // wait to arrive at hive
        while (Vector3.Distance(transform.position, TileHelper.HexToWorld(hive.q, hive.r, agent.hexSize)) > 0.1f)
        {
            yield return null;
        }
        hive.storedResources += amount;

        // After delivering, if the original target tile still has resources, go back and repeat after cooldown.
        if (targetTile != null && targetTile.resourceAmount > 0)
        {
            // repeat gather cycle after cooldown
            currentTask = UnitTaskType.Gather;
            yield return new WaitForSeconds(gatherCooldown);
            MoveAndGather(targetTile);
        }
        else
        {
            currentTask = UnitTaskType.Idle;
        }
    }

    Hive FindNearestHive(int q, int r)
    {
        return HiveManager.Instance.FindNearestHive(q, r);
    }
}
