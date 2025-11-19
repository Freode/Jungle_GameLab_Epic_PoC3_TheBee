using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
            // 드래그 선택된 유닛이 있으면 모두 선택 해제 ?
            if (DragSelector.Instance != null && DragSelector.Instance.GetSelectedCount() > 0)
            {
                DragSelector.Instance.DeselectAll();
            }
            
            // 단일 선택된 유닛이 있으면 선택 해제 ?
            if (selectedUnitInstance != null && !IsPointerOverUI())
            {
                DeselectUnit();
            }
            
            // UI가 아니면 월드 클릭 처리 ?
            if (!IsPointerOverUI())
            {
                HandleLeftClick();
            }
        }

        // Right click: issue command/move to clicked tile
        if (Input.GetMouseButtonDown(1))
        {
            // Check if clicking over UI - if so, ignore
            if (!IsPointerOverUI())
            {
                HandleRightClick();
            }
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

            if (highlighter != null)
                highlighter.ShowRadius(selectedUnitInstance.homeHive, behavior != null ? behavior.activityRadius : 0);
        }
        else
        {
            highlighter?.HideRadius();
        }
    }

    // Check if mouse is over UI element
    bool IsPointerOverUI()
    {
        // Check if EventSystem exists
        if (EventSystem.current == null)
            return false;

        // Check if pointer is over UI
        return EventSystem.current.IsPointerOverGameObject();
    }

    //void OnDrawGizmos()
    //{
    //    Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    pos.z = 0f;
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawSphere(pos, 0.1f);
    //}

    void HandleLeftClick()
    {
        // Try 2D - 모든 히트 수집 ?
        Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        RaycastHit2D[] hits2D = Physics2D.RaycastAll(wp, Vector2.zero);
            
        if (hits2D.Length > 0)
        {
            // 렌더링 순서로 정렬 (sortingOrder + UnitAgent ID) ?
            System.Array.Sort(hits2D, (a, b) => {
                var spriteA = a.collider.GetComponentInChildren<SpriteRenderer>();
                var spriteB = b.collider.GetComponentInChildren<SpriteRenderer>();
                    
                if (spriteA != null && spriteB != null)
                {
                    // 1. sortingOrder 비교 (높은 값이 위 = 먼저 선택) ?
                    if (spriteA.sortingOrder != spriteB.sortingOrder)
                    {
                        return spriteB.sortingOrder.CompareTo(spriteA.sortingOrder);
                    }
                        
                    // 2. 같은 sortingOrder면 UnitAgent ID로 비교 (낮은 ID = 나중 생성 = 먼저 선택) ?
                    var unitA = a.collider.GetComponent<UnitAgent>();
                    var unitB = b.collider.GetComponent<UnitAgent>();
                        
                    if (unitA != null && unitB != null)
                    {
                        // 낮은 ID가 먼저 (나중에 생성 = 위에 있음) ?
                        return unitA.id.CompareTo(unitB.id);
                    }
                }
                    
                return 0;
            });
                
            // 가장 위에 있는 UnitAgent 찾기 (Hive 포함) ?
            UnitAgent topUnit = null;
            foreach (var hit2 in hits2D)
            {              
                // UnitAgent 체크
                var unit = hit2.collider.GetComponentInParent<UnitAgent>();
                if (unit != null)
                {
                    topUnit = unit;
                    break;
                }
            }
                
            // UnitAgent가 있으면 선택, 없으면 타일 선택 ?
            if (topUnit != null)
            {
                SelectUnit(topUnit);
                return;
            }
            else
            {
                // UnitAgent가 없으면 타일 선택
                foreach (var hit2 in hits2D)
                {
                    var tile = hit2.collider.GetComponentInParent<HexTile>();
                    if (tile != null)
                    {
                        DeselectUnit();
                        if (debugText != null) debugText.text = $"Tile: ({tile.q}, {tile.r})";
                        return;
                    }
                }
            }
        }
        
    }

    void HandleRightClick()
    {
        // 드래그로 선택된 유닛들이 있으면 그룹 이동
        if (DragSelector.Instance != null)
        {
            var dragSelected = DragSelector.Instance.GetSelectedUnits();
            if (dragSelected.Count > 0)
            {
                HandleRightClickForMultipleUnits(dragSelected);
                return;
            }
        }

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // 모든 히트를 가져와서 렌더링 순서대로 정렬 ?
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        
        if (hits.Length > 0)
        {
            // 렌더링 순서 기준으로 정렬 ?
            System.Array.Sort(hits, (a, b) => {
                var spriteA = a.collider.GetComponentInParent<SpriteRenderer>();
                var spriteB = b.collider.GetComponentInParent<SpriteRenderer>();
                
                if (spriteA != null && spriteB != null)
                {
                    // 1. sortingOrder 비교 (높은 값이 위 = 먼저 선택) ?
                    if (spriteA.sortingOrder != spriteB.sortingOrder)
                    {
                        return spriteB.sortingOrder.CompareTo(spriteA.sortingOrder);
                    }
                    
                    // 2. 같은 sortingOrder면 UnitAgent ID로 비교 (낮은 ID = 나중 생성 = 먼저 선택) ?
                    var unitA = a.collider.GetComponentInParent<UnitAgent>();
                    var unitB = b.collider.GetComponentInParent<UnitAgent>();
                    
                    if (unitA != null && unitB != null)
                    {
                        // 낮은 ID가 먼저 (나중에 생성 = 위에 있음) ?
                        return unitA.id.CompareTo(unitB.id);
                    }
                }
                
                // 3. 카메라 거리로 최종 정렬
                return a.distance.CompareTo(b.distance);
            });
            
            // 가장 가까운 것부터 순서대로 체크 ?
            foreach (var hit in hits)
            {
                // 유닛 우선 체크 (타일보다 우선)
                var unit = hit.collider.GetComponentInParent<UnitAgent>();
                if (unit != null)
                {
                    // 적 유닛 우클릭 시 공격 명령
                    if (unit.faction == Faction.Enemy && selectedUnitInstance != null && selectedUnitInstance.faction == Faction.Player)
                    {
                        HandleAttackCommand(unit);
                        return;
                    }
                    // 아군 유닛은 선택
                    else if (unit.faction == Faction.Player)
                    {
                        SelectUnit(unit);
                        return;
                    }
                }
                
                // Hive 체크
                var hive = hit.collider.GetComponentInParent<Hive>();
                if (hive != null)
                {
                    var hiveAgent = hive.GetComponent<UnitAgent>();
                    if (hiveAgent != null)
                    {
                        SelectUnit(hiveAgent);
                        return;
                    }
                }
            }
            
            // 유닛이 없으면 타일 명령 (가장 가까운 타일)
            foreach (var hit in hits)
            {
                var tile = hit.collider.GetComponentInParent<HexTile>();
                if (tile != null)
                {
                    OnTileCommand(tile);
                    return;
                }
            }
        }
        else
        {
            // Try 2D - 모든 히트 수집 ?
            Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits2D = Physics2D.RaycastAll(wp, Vector2.zero);
            
            if (hits2D.Length > 0)
            {
                // 렌더링 순서로 정렬 (sortingOrder + UnitAgent ID) ?
                System.Array.Sort(hits2D, (a, b) => {
                    var spriteA = a.collider.GetComponentInParent<SpriteRenderer>();
                    var spriteB = b.collider.GetComponentInParent<SpriteRenderer>();
                    
                    if (spriteA != null && spriteB != null)
                    {
                        // 1. sortingOrder 비교 (높은 값이 위 = 먼저 선택) ?
                        if (spriteA.sortingOrder != spriteB.sortingOrder)
                        {
                            return spriteB.sortingOrder.CompareTo(spriteA.sortingOrder);
                        }
                        
                        // 2. 같은 sortingOrder면 UnitAgent ID로 비교 (낮은 ID = 나중 생성 = 먼저 선택) ?
                        var unitA = a.collider.GetComponentInParent<UnitAgent>();
                        var unitB = b.collider.GetComponentInParent<UnitAgent>();
                        
                        if (unitA != null && unitB != null)
                        {
                            // 낮은 ID가 먼저 (나중에 생성 = 위에 있음) ?
                            return unitA.id.CompareTo(unitB.id);
                        }
                    }
                    
                    return 0;
                });
                
                // 가장 위에 있는 것부터 순서대로 체크 ?
                foreach (var hit2 in hits2D)
                {
                    // 유닛 우선 체크
                    var unit = hit2.collider.GetComponentInParent<UnitAgent>();
                    if (unit != null)
                    {
                        // 적 유닛 우클릭 시 공격 명령
                        if (unit.faction == Faction.Enemy && selectedUnitInstance != null && selectedUnitInstance.faction == Faction.Player)
                        {
                            HandleAttackCommand(unit);
                            return;
                        }
                        // 아군 유닛은 선택
                        else if (unit.faction == Faction.Player)
                        {
                            SelectUnit(unit);
                            return;
                        }
                    }
                    
                    // Hive 체크
                    var hive = hit2.collider.GetComponentInParent<Hive>();
                    if (hive != null)
                    {
                        var hiveAgent = hive.GetComponent<UnitAgent>();
                        if (hiveAgent != null)
                        {
                            SelectUnit(hiveAgent);
                            return;
                        }
                    }
                }
                
                // 유닛이 없으면 타일 명령 (가장 위에 있는 타일)
                foreach (var hit2 in hits2D)
                {
                    var tile = hit2.collider.GetComponentInParent<HexTile>();
                    if (tile != null) 
                    {
                        OnTileCommand(tile);
                        return;
                    }
                }
            }
        }
    }

    void HandleRightClickForMultipleUnits(List<UnitAgent> units)
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        HexTile targetTile = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            targetTile = hit.collider.GetComponentInParent<HexTile>();
        }
        else
        {
            Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit2 = Physics2D.Raycast(wp, Vector2.zero);
            if (hit2.collider != null)
            {
                targetTile = hit2.collider.GetComponentInParent<HexTile>();
            }
        }

        if (targetTile != null)
        {
            // 모든 선택된 유닛을 목표 타일로 이동
            foreach (var unit in units)
            {
                if (unit == null || !unit.canMove) continue;

                var behavior = unit.GetComponent<UnitBehaviorController>();
                if (behavior != null)
                {
                    behavior.IssueCommandToTile(targetTile);
                }
            }

            if (debugText != null)
            {
                debugText.text = $"{units.Count}개 유닛 이동: ({targetTile.q}, {targetTile.r})";
            }
        }
    }

    public void SelectUnit(UnitAgent unit)
    {
        // canMove가 false이고 여왕벌이면 하이브 안에 있는 것이므로 선택 불가
        if (unit.isQueen && !unit.canMove)
        {
            Debug.Log("[선택] 여왕벌이 하이브 안에 있어 선택할 수 없습니다.");
            return;
        }

        if (selectedUnitInstance != null) selectedUnitInstance.SetSelected(false);
        selectedUnitInstance = unit;
        selectedUnitInstance.SetSelected(true);
        if (debugText != null) debugText.text = $"Selected unit at ({unit.q},{unit.r})";

        // show command UI for this unit
        UnitCommandPanel.Instance?.Show(selectedUnitInstance);
    }

    public void DeselectUnit()
    {
        if (selectedUnitInstance != null)
        {
            selectedUnitInstance.SetSelected(false);
            selectedUnitInstance = null;
        }
        
        // Hide command UI
        UnitCommandPanel.Instance?.Hide();
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

        // Enemy 유닛은 명령을 내릴 수 없음
        if (selectedUnitInstance.faction == Faction.Enemy)
        {
            if (debugText != null)
            {
                debugText.text = "적 유닛은 명령을 내릴 수 없습니다.";
            }
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

    /// <summary>
    /// 적 유닛 공격 명령 처리 (활동 범위 체크) ?
    /// </summary>
    void HandleAttackCommand(UnitAgent enemy)
    {
        if (selectedUnitInstance == null || enemy == null) return;
        
        // 플레이어 유닛만 공격 가능
        if (selectedUnitInstance.faction != Faction.Player)
        {
            if (debugText != null)
                debugText.text = "플레이어 유닛만 공격 명령을 내릴 수 있습니다.";
            return;
        }
        
        // 활동 범위 체크 ?
        var behavior = selectedUnitInstance.GetComponent<UnitBehaviorController>();
        if (behavior != null)
        {
            int distance = Pathfinder.AxialDistance(
                selectedUnitInstance.q, selectedUnitInstance.r,
                enemy.q, enemy.r
            );
            
            // 활동 범위 내인지 확인 ?
            if (selectedUnitInstance.homeHive != null)
            {
                int hiveDistance = Pathfinder.AxialDistance(
                    selectedUnitInstance.homeHive.q, selectedUnitInstance.homeHive.r,
                    enemy.q, enemy.r
                );
                
                if (hiveDistance > behavior.activityRadius)
                {
                    if (debugText != null)
                        debugText.text = $"적이 활동 범위 밖입니다 (하이브로부터 {hiveDistance}/{behavior.activityRadius})";
                    return;
                }
            }
            
            // 공격 명령 실행 ?
            behavior.IssueAttackCommand(enemy);
            
            if (debugText != null)
                debugText.text = $"적 유닛 공격 명령: ({enemy.q}, {enemy.r})";
        }
        else
        {
            if (debugText != null)
                debugText.text = "공격 명령을 내릴 수 없습니다.";
        }
    }
}
