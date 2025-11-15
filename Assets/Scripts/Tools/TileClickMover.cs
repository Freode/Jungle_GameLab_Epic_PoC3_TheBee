using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// Debug tool: click on a tile to move the selected unit there or register a new unit
public class TileClickMover : MonoBehaviour
{
    public static TileClickMover Instance { get; private set; }

    public Camera mainCamera;
    public UnitAgent selectedUnitPrefab;
    private UnitAgent selectedUnitInstance;
    public TextMeshProUGUI debugText; // UI text to show coordinates

    // If true, moving requires pressing Move in UI first, then clicking a target tile
    public bool requireMoveConfirm = true;
    private bool moveMode = false;

    public TileHighlighter highlighter;
    public HexBoundaryHighlighter boundaryHighlighter;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Left click: selection / UI interactions
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        // Right click: issue command/move to clicked tile
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }

        // Cancel move mode with Escape
        if (moveMode && Input.GetKeyDown(KeyCode.Escape))
        {
            StopMoveMode();
        }

        // show radius when unit selected
        if (selectedUnitInstance != null)
        {
            var behavior = selectedUnitInstance.GetComponent<UnitBehaviorController>();

            // Do NOT call boundaryHighlighter.ShowBoundary here ? showing boundary on unit selection caused confusion.
            // BoundaryHighlighter is enabled by HiveManager when hives exist and should be triggered when a hive is constructed or explicitly requested.
            // Only show the circle/sprite radius via TileHighlighter for the selected unit.
            if (highlighter != null)
                highlighter.ShowRadius(selectedUnitInstance.homeHive, behavior != null ? behavior.activityRadius : 0);
        }
        else
        {
            boundaryHighlighter?.Clear();
            highlighter?.HideRadius();
        }
    }

    void HandleLeftClick()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var unit = hit.collider.GetComponentInParent<UnitAgent>();
            if (unit != null)
            {
                // select existing unit
                SelectUnit(unit);
                return;
            }

            var tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                // just show tile info
                if (debugText != null) debugText.text = $"Tile: ({tile.q}, {tile.r})";
            }
        }
        else
        {
            // Try 2D
            Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit2 = Physics2D.Raycast(wp, Vector2.zero);
            if (hit2.collider != null)
            {
                var unit = hit2.collider.GetComponentInParent<UnitAgent>();
                if (unit != null)
                {
                    SelectUnit(unit);
                    return;
                }
                var tile = hit2.collider.GetComponentInParent<HexTile>();
                if (tile != null)
                {
                    if (debugText != null) debugText.text = $"Tile: ({tile.q}, {tile.r})";
                }
            }
        }
    }

    void HandleRightClick()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                OnTileCommand(tile);
            }

            var unit = hit.collider.GetComponentInParent<UnitAgent>();
            if (unit != null)
            {
                // right-click on unit could be used for attack-targeting etc; for now select
                SelectUnit(unit);
            }
        }
        else
        {
            // Try 2D
            Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit2 = Physics2D.Raycast(wp, Vector2.zero);
            if (hit2.collider != null)
            {
                var tile = hit2.collider.GetComponentInParent<HexTile>();
                if (tile != null) OnTileCommand(tile);
                var unit = hit2.collider.GetComponentInParent<UnitAgent>();
                if (unit != null) SelectUnit(unit);
            }
        }
    }

    void SelectUnit(UnitAgent unit)
    {
        if (selectedUnitInstance != null) selectedUnitInstance.SetSelected(false);
        selectedUnitInstance = unit;
        selectedUnitInstance.SetSelected(true);
        if (debugText != null) debugText.text = $"Selected unit at ({unit.q},{unit.r})";

        // show command UI for this unit
        UnitCommandPanel.Instance?.Show(selectedUnitInstance);
    }

    public void EnterMoveMode()
    {
        moveMode = true;
        if (debugText != null) debugText.text = "Select target for command";
    }

    public void StopMoveMode()
    {
        moveMode = false;
        if (debugText != null) debugText.text = "";
        PendingCommandHolder.Instance?.Clear();
    }

    void OnTileCommand(HexTile tile)
    {
        // update debug UI
        if (debugText != null)
        {
            debugText.text = $"Tile: ({tile.q}, {tile.r})";
        }

        if (selectedUnitInstance == null)
        {
            // no unit selected to command
            return;
        }

        // if there is a pending command, execute with this tile as target
        if (PendingCommandHolder.Instance.HasPending)
        {
            PendingCommandHolder.Instance.ExecutePending(CommandTarget.ForTile(tile.q, tile.r));
            StopMoveMode();
            return;
        }

        // If unit has behavior controller, dispatch to its decision logic (priority attack/gather/idle)
        var behavior = selectedUnitInstance.GetComponent<UnitBehaviorController>();
        if (behavior != null && selectedUnitInstance.canMove)
        {
            behavior.IssueCommandToTile(tile);
            StopMoveMode();
            return;
        }

        // if move confirmation required, only move when in moveMode unless unit allows direct click
        if (requireMoveConfirm && !moveMode && !selectedUnitInstance.canMove)
        {
            if (debugText != null) debugText.text = "Select a command in the command UI to issue";
            return;
        }

        // otherwise perform move if allowed
        if (moveMode || selectedUnitInstance.canMove)
        {
            var startTile = TileManager.Instance.GetTile(selectedUnitInstance.q, selectedUnitInstance.r);
            var path = Pathfinder.FindPath(startTile, tile);
            if (path != null && path.Count > 0)
            {
                var ctrl = selectedUnitInstance.GetComponent<UnitController>();
                if (ctrl == null) ctrl = selectedUnitInstance.gameObject.AddComponent<UnitController>();
                ctrl.agent = selectedUnitInstance;
                ctrl.SetPath(path);
            }
            StopMoveMode();
        }
    }
}
