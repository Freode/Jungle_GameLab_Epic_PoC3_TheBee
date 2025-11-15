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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterHive(Hive hive)
    {
        if (hive == null) return;
        if (!hives.Contains(hive)) hives.Add(hive);

        // enable HexBoundaryHighlighter singleton when first hive is registered
        if (hives.Count == 1)
        {
            var bh = HexBoundaryHighlighter.Instance;
            if (bh != null)
            {
                bh.SetEnabledForHives(true);
                bh.debugLogs = false; // disable debug logs
            }
        }
    }

    public void UnregisterHive(Hive hive)
    {
        if (hive == null) return;
        if (hives.Contains(hive)) hives.Remove(hive);

        if (hives.Count == 0)
        {
            // disable boundary highlighter when no hives remain
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
        Hive found = null;
        int best = int.MaxValue;
        foreach (var h in hives)
        {
            int d = Pathfinder.AxialDistance(q, r, h.q, h.r);
            if (d < best)
            {
                best = d; found = h;
            }
        }
        return found;
    }

    // Add resources to player's storage
    public void AddResources(int amount)
    {
        playerStoredResources += amount;
        Debug.Log($"자원 추가: +{amount}, 총 자원: {playerStoredResources}");
    }

    // Try to spend resources, returns true if successful
    public bool TrySpendResources(int amount)
    {
        if (playerStoredResources >= amount)
        {
            playerStoredResources -= amount;
            Debug.Log($"자원 사용: -{amount}, 남은 자원: {playerStoredResources}");
            return true;
        }
        return false;
    }

    // Check if player has enough resources
    public bool HasResources(int amount)
    {
        return playerStoredResources >= amount;
    }
}
