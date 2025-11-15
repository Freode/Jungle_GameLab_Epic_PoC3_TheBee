using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : MonoBehaviour, IUnitCommandProvider
{
    public int q;
    public int r;

    public GameObject workerPrefab;
    public float spawnInterval = 10f; // seconds
    public int maxWorkers = 10;

    // Deprecated: Use HiveManager.playerStoredResources instead
    // public int storedResources = 0;

    // Relocation state
    public bool isRelocating = false;
    public UnitAgent queenBee; // 여왕벌 참조 (씬에 이미 존재)
    
    private List<UnitAgent> workers = new List<UnitAgent>();
    private Coroutine spawnRoutine;
    private Coroutine relocateRoutine;

    // Commands for hive
    [Header("Hive Commands")]
    public SOCommand[] hiveCommands; // All hive commands (including relocate)

    void OnEnable()
    {
        HiveManager.Instance?.RegisterHive(this);
    }

    void OnDisable()
    {
        HiveManager.Instance?.UnregisterHive(this);
    }

    public void Initialize(int q, int r)
    {
        this.q = q; this.r = r;
        
        // Note: queenBee should be assigned externally when constructing the hive
        // The queen already exists in the scene
        
        // start spawning
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());

        // show boundary using configured hive activity radius from HiveManager
        int radius = 5;
        if (HiveManager.Instance != null) radius = HiveManager.Instance.hiveActivityRadius;
        if (HexBoundaryHighlighter.Instance != null)
        {
            HexBoundaryHighlighter.Instance.ShowBoundary(this, radius);
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (!isRelocating && workers.Count < maxWorkers)
            {
                SpawnWorker();
            }
        }
    }

    void SpawnWorker()
    {
        if (workerPrefab == null) return;
        Vector3 pos = TileHelper.HexToWorld(q, r, 0.5f);
        var go = Instantiate(workerPrefab, pos, Quaternion.identity);
        var agent = go.GetComponent<UnitAgent>();
        if (agent == null) agent = go.AddComponent<UnitAgent>();
        agent.SetPosition(q, r);
        // assign home hive so activity radius checks work
        agent.homeHive = this;
        agent.canMove = true;
        agent.faction = Faction.Player;
        agent.isQueen = false;
        workers.Add(agent);
    }

    // IUnitCommandProvider implementation
    public IEnumerable<ICommand> GetCommands(UnitAgent agent)
    {
        var commands = new List<ICommand>();
        
        // Add all hive commands
        if (hiveCommands != null)
        {
            foreach (var cmd in hiveCommands)
            {
                if (cmd != null)
                {
                    commands.Add(cmd);
                }
            }
        }
        
        return commands;
    }

    // Check if hive can relocate (not already relocating)
    public bool CanRelocate()
    {
        return !isRelocating;
    }

    // Start hive relocation process (resource check done in SOCommand.IsAvailable)
    public void StartRelocation(int resourceCost)
    {
        if (isRelocating)
        {
            Debug.LogWarning("Hive is already relocating");
            return;
        }

        // Deduct resources from HiveManager
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.TrySpendResources(resourceCost);
        }
        
        isRelocating = true;

        // Stop spawning workers
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        Debug.Log($"하이브 이사 준비 시작! 20초 후에 하이브가 제거됩니다.");

        // Start relocation countdown
        if (relocateRoutine != null) StopCoroutine(relocateRoutine);
        relocateRoutine = StartCoroutine(RelocationCountdown());
    }

    IEnumerator RelocationCountdown()
    {
        yield return new WaitForSeconds(20f);

        // Destroy hive
        DestroyHive();
    }

    void DestroyHive()
    {
        Debug.Log("하이브가 제거되었습니다. 일꾼들이 여왕벌을 따라갑니다.");

        // Clear boundary
        if (HexBoundaryHighlighter.Instance != null)
        {
            HexBoundaryHighlighter.Instance.Clear();
        }

        // Stop all workers and make them follow queen
        foreach (var worker in workers)
        {
            if (worker == null) continue;
            
            // Stop current actions
            var ctrl = worker.GetComponent<UnitController>();
            if (ctrl != null)
            {
                ctrl.ClearPath();
            }

            // Mark worker as following queen (no manual order given yet)
            worker.isFollowingQueen = true;
            worker.hasManualOrder = false;
            
            // Clear home hive reference (hive no longer exists)
            worker.homeHive = null;
        }

        // Unregister from HiveManager
        HiveManager.Instance?.UnregisterHive(this);

        // Destroy the hive GameObject
        Destroy(gameObject);
    }

    // Called when a new hive is constructed to reclaim all homeless workers
    public void ReclaimWorkers()
    {
        // This will be called by the new hive when constructed
        // All workers with homeHive pointing to this old hive should move to new location
        foreach (var worker in workers)
        {
            if (worker == null) continue;
            worker.isFollowingQueen = false;
            worker.hasManualOrder = false;
            
            // Move worker to new hive location
            var start = TileManager.Instance.GetTile(worker.q, worker.r);
            var dest = TileManager.Instance.GetTile(q, r);
            if (start != null && dest != null)
            {
                var path = Pathfinder.FindPath(start, dest);
                if (path != null)
                {
                    var ctrl = worker.GetComponent<UnitController>();
                    if (ctrl == null) ctrl = worker.gameObject.AddComponent<UnitController>();
                    ctrl.agent = worker;
                    ctrl.SetPath(path);
                }
            }
        }
    }

    public List<UnitAgent> GetWorkers()
    {
        return new List<UnitAgent>(workers);
    }

    // Commands targeting the hive workers
    public void IssueCommandToWorkers(string commandId, CommandTarget target)
    {
        foreach (var w in workers)
        {
            if (w == null) continue;
            switch (commandId)
            {
                case "hive_explore":
                    var start = TileManager.Instance.GetTile(w.q, w.r);
                    var dest = TileManager.Instance.GetTile(target.q, target.r);
                    var path = Pathfinder.FindPath(start, dest);
                    if (path != null)
                    {
                        var ctrl = w.GetComponent<UnitController>();
                        if (ctrl == null) ctrl = w.gameObject.AddComponent<UnitController>();
                        ctrl.agent = w;
                        ctrl.SetPath(path);
                    }
                    break;
                case "hive_gather":
                    // for now treat gather as move
                    goto case "hive_explore";
                case "hive_attack":
                    // move towards target unit or tile
                    goto case "hive_explore";
            }
        }
    }
}
