using System.Collections.Generic;
using UnityEngine;

public class HiveManager : MonoBehaviour
{
    public static HiveManager Instance { get; private set; }

    private List<Hive> hives = new List<Hive>();

    [Header("Hive Settings")]
    public int hiveActivityRadius = 5;

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
                bh.debugLogs = true; // enable logs to assist debugging
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
}
