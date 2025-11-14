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
        if (Input.GetMouseButtonDown(0))
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
                    OnTileClicked(tile);
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
                    if (tile != null) OnTileClicked(tile);
                }
            }
        }

        // Cancel move mode with right click or Escape
        if (moveMode && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            StopMoveMode();
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

    void OnTileClicked(HexTile tile)
    {
        // update debug UI
        if (debugText != null)
        {
            debugText.text = $"Tile: ({tile.q}, {tile.r})";
        }

        if (selectedUnitInstance == null)
        {
            // do not spawn unit on click anymore
            return;
        }
        else
        {
            // move existing unit using Pathfinder
            // if move confirmation required, only move when in moveMode
            if (requireMoveConfirm && !moveMode && !PendingCommandHolder.Instance.HasPending)
            {
                // allow direct click movement if unit canMove
                if (selectedUnitInstance.canMove)
                {
                    // fall through to move handling below
                }
                else
                {
                    if (debugText != null) debugText.text = "Select a command in the command UI to issue";
                    return;
                }
            }

            // if there is a pending command, execute with this tile as target
            if (PendingCommandHolder.Instance.HasPending)
            {
                PendingCommandHolder.Instance.ExecutePending(CommandTarget.ForTile(tile.q, tile.r));
                StopMoveMode();
                return;
            }

            // otherwise default to move if moveMode or unit.canMove
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
}
