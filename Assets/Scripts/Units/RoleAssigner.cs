using UnityEngine;
using System.Collections.Generic;

public enum UnitRole
{ 
    None, 
    Worker, 
    Scout, 
    Guard,
    Queen,
    Hive,
    Normal,
}

// RoleType enum moved to Assets/Scripts/Units/RoleType.cs to be shared with ScriptableObjects

[System.Serializable]
public class RoleStats
{
    public int bonusMaxHealth = 0;
    public int bonusAttack = 0;
    public float attackCooldownMultiplier = 1f;

    public int gatherAmountBonus = 0;
    public float gatherCooldownMultiplier = 1f;

    public int activityRadiusBonus = 0;

    // movement speed multiplier (1 = normal, <1 slower, >1 faster)
    public float moveSpeedMultiplier = 1f;
}

public class RoleAssigner : MonoBehaviour, IUnitCommandProvider
{
    public UnitAgent agent;
    public UnitRole currentRole = UnitRole.None;

    // New: role-based stats
    [Header("Role System")]
    public RoleType role = RoleType.Gatherer;
    public RoleStats attackerStats = new RoleStats { bonusMaxHealth = 0, bonusAttack = 3, attackCooldownMultiplier = 1f, gatherAmountBonus = 0, gatherCooldownMultiplier = 1f, activityRadiusBonus = 1, moveSpeedMultiplier = 1f };
    public RoleStats gathererStats = new RoleStats { bonusMaxHealth = 0, bonusAttack = 0, attackCooldownMultiplier = 1f, gatherAmountBonus = 1, gatherCooldownMultiplier = 0.9f, activityRadiusBonus = 0, moveSpeedMultiplier = 1f };
    public RoleStats tankStats = new RoleStats { bonusMaxHealth = 20, bonusAttack = 0, attackCooldownMultiplier = 1.1f, gatherAmountBonus = 0, gatherCooldownMultiplier = 1f, activityRadiusBonus = 1, moveSpeedMultiplier = 0.8f };

    // cached components
    private CombatUnit combat;
    private UnitBehaviorController behavior;
    private UnitController controller;

    // cached base values (captured once in Awake)
    private int baseMaxHealth;
    private int baseAttack;
    private float baseAttackCooldown;

    private int baseGatherAmount;
    private float baseGatherCooldown;
    private int baseActivityRadius;

    private float baseMoveSpeed = 2f;

    private bool baseInitialized = false;

    // assignable commands for this role (inspector)
    public SOCommand[] roleCommands;

    void Awake()
    {
        if (agent == null) agent = GetComponent<UnitAgent>();

        // cache components
        combat = GetComponent<CombatUnit>();
        behavior = GetComponent<UnitBehaviorController>();
        controller = GetComponent<UnitController>();

        if (controller != null)
        {
            baseMoveSpeed = controller.moveSpeed;
        }

        if (combat != null)
        {
            baseMaxHealth = combat.maxHealth;
            baseAttack = combat.attack;
            baseAttackCooldown = combat.GetBaseAttackCooldown();
        }
        else
        {
            baseMaxHealth = 0;
            baseAttack = 0;
            baseAttackCooldown = 1f;
        }

        if (behavior != null)
        {
            baseGatherAmount = behavior.gatherAmount;
            baseGatherCooldown = behavior.gatherCooldown;
            baseActivityRadius = behavior.activityRadius;
        }
        else
        {
            baseGatherAmount = 0;
            baseGatherCooldown = 1f;
            baseActivityRadius = 0;
        }

        baseInitialized = true;
    }

    void Start()
    {
        // apply initial role stats
        ApplyRole(role);
    }

    /// <summary>
    /// Keep older API for UnitRole - limited behavior changes
    /// </summary>
    public void AssignRole(UnitRole role)
    {
        currentRole = role;
        // placeholder: you can add behavior changes here based on role
        switch (role)
        {
            case UnitRole.Worker:
                agent.visionRange = 0;
                if (agent != null) agent.visionRange = 0;
                FogOfWarManager.Instance?.RecalculateVisibility();
                // e.g., set gather behavior
                break;
            case UnitRole.Scout:
                // increase vision
                agent.visionRange = 1;
                if (agent != null) agent.visionRange = 1;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.Guard:
                // defensive behavior
                break;
            case UnitRole.Queen:
                agent.visionRange = 1;
                if (agent != null) agent.visionRange = 1;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.Hive:
                agent.visionRange = 2;
                if (agent != null) agent.visionRange = 2;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.Normal:
                agent.visionRange = 0;
                if (agent != null) agent.visionRange = 0;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.None:
            default:
                agent.visionRange = 1;
                if (agent != null) agent.visionRange = 1;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
        }
    }

    // New API: set RoleType and apply stats
    public void SetRole(RoleType newRole)
    {
        role = newRole;
        ApplyRole(newRole);
    }

    // expose refresh so external systems (HiveManager) can reapply role-based stats including global upgrades
    public void RefreshRole()
    {
        ApplyRole(role);
    }

    void ApplyRole(RoleType r)
    {
        if (!baseInitialized)
        {
            // ensure bases are cached
            Awake();
        }

        RoleStats stats = GetStatsForRole(r);

        if (combat != null)
        {
            // compute max health including role stat and global upgrades (health upgrades only apply to Tank role)
            int newMax = baseMaxHealth + stats.bonusMaxHealth;
            if (r == RoleType.Tank && HiveManager.Instance != null)
            {
                newMax += HiveManager.Instance.workerHealthLevel * 2; // workerHealthLevel increases max health by 2 per level
            }
            if (newMax < 1) newMax = 1;

            // capture previous max/health to allow increasing current health when max increases
            int prevMax = combat.maxHealth;
            int prevHealth = combat.health;

            combat.SetMaxHealth(newMax);

            // If max increased, also increase current health by the same delta (clamped to new max)
            int delta = newMax - prevMax;
            if (delta > 0)
            {
                combat.SetHealth(Mathf.Min(prevHealth + delta, newMax));
            }

            int newAttack = baseAttack + stats.bonusAttack;
            // apply global attack upgrades only to Attacker role
            if (r == RoleType.Attacker && HiveManager.Instance != null)
            {
                newAttack += HiveManager.Instance.workerAttackLevel; // each level gives +1 attack
            }
            if (newAttack < 0) newAttack = 0;
            combat.SetAttack(newAttack);

            // use CombatUnit API to apply cooldown multiplier
            combat.SetAttackCooldownMultiplier(stats.attackCooldownMultiplier);
        }

        if (behavior != null)
        {
            int effectiveGather = Mathf.Max(0, baseGatherAmount + stats.gatherAmountBonus);
            // apply global gather upgrades only to Gatherer role
            if (r == RoleType.Gatherer && HiveManager.Instance != null)
            {
                effectiveGather += HiveManager.Instance.gatherAmountLevel; // each level increases gather amount by 1
            }
            behavior.gatherAmount = effectiveGather;

            behavior.gatherCooldown = Mathf.Max(0.01f, baseGatherCooldown * stats.gatherCooldownMultiplier);
            behavior.activityRadius = Mathf.Max(0, baseActivityRadius + stats.activityRadiusBonus);
        }

        // apply movement speed multiplier if UnitController exists
        if (controller != null)
        {
            float levelBonus = 0f;
            if (HiveManager.Instance != null)
            {
                levelBonus = HiveManager.Instance.GetWorkerSpeedBonusValue(r);
            }
            controller.moveSpeed = (baseMoveSpeed * stats.moveSpeedMultiplier) + levelBonus;
        }

        Debug.Log($"[RoleAssigner] {gameObject.name} role applied: {r}");
    }

    RoleStats GetStatsForRole(RoleType r)
    {
        switch (r)
        {
            case RoleType.Attacker: return attackerStats;
            case RoleType.Gatherer: return gathererStats;
            case RoleType.Tank: return tankStats;
            default: return gathererStats;
        }
    }

    // IUnitCommandProvider
    public IEnumerable<ICommand> GetCommands(UnitAgent a)
    {
        if (roleCommands == null) yield break;
        foreach (var c in roleCommands)
        {
            yield return c;
        }
    }
}
