using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : MonoBehaviour
{
    public int q;
    public int r;

    public GameObject workerPrefab;
    public float spawnInterval = 10f; // seconds
    public int maxWorkers = 10;

    public int storedResources = 0;

    private List<UnitAgent> workers = new List<UnitAgent>();
    private Coroutine spawnRoutine;

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
            if (workers.Count < maxWorkers)
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
        workers.Add(agent);
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
