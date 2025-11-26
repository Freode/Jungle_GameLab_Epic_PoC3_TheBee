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

        // ✅ 키보드 단축키 (요구사항 2, 3, 5)
        HandleKeyboardShortcuts();

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
    
    /// <summary>
    /// 키보드 단축키 처리 (요구사항 2, 3, 5)
    /// </summary>
    void HandleKeyboardShortcuts()
    {
        // ✅ 1번: 여왕벌 선택 (요구사항 2)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectQueen();
        }
        
        // ✅ 2번: 꿀벌집 선택 (요구사항 3)
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectPlayerHive();
        }
        
        // ✅ Q/W/E: 부대 페르몬 명령 (요구사항 5)
        // Allow Q/W/E to work even if another unit is selected by finding the queen if needed
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExecutePheromoneCommand(WorkerSquad.Squad1);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            ExecutePheromoneCommand(WorkerSquad.Squad2);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            ExecutePheromoneCommand(WorkerSquad.Squad3);
        }
    }
    
    /// <summary>
    /// 여왕벌 선택 (요구사항 2)
    /// </summary>
    void SelectQueen()
    {
        if (TileManager.Instance == null) return;
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.isQueen && unit.faction == Faction.Player)
            {
                SelectUnit(unit);
                Debug.Log("[단축키] 여왕벌 선택");
                return;
            }
        }
        
        Debug.Log("[단축키] 여왕벌을 찾을 수 없습니다.");
    }
    
    /// <summary>
    /// 플레이어 꿀벌집 선택 (요구사항 3)
    /// </summary>
    void SelectPlayerHive()
    {
        if (HiveManager.Instance == null) return;
        
        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive != null)
            {
                var hiveAgent = hive.GetComponent<UnitAgent>();
                if (hiveAgent != null && hiveAgent.faction == Faction.Player)
                {
                    SelectUnit(hiveAgent);
                    Debug.Log("[단축키] 꿀벌집 선택");
                    return;
                }
            }
        }
        
        Debug.Log("[단축키] 꿀벌집을 찾을 수 없습니다.");
    }
    
    /// <summary>
    /// 페르몬 명령 실행 (요구사항 5)
    /// - 선택 여부와 무관하게 동작: 선택된 유닛이 여왕벌이면 그것을 사용하고,
    ///   아니면 씬에서 플레이어 여왕벌을 찾아 사용합니다.
    /// </summary>
    void ExecutePheromoneCommand(WorkerSquad squad)
    {
        UnitAgent queen = null;

        // prefer currently selected unit if it's the queen
        if (selectedUnitInstance != null && selectedUnitInstance.isQueen && selectedUnitInstance.faction == Faction.Player)
        {
            queen = selectedUnitInstance;
        }
        else
        {
            // find queen in scene
            if (TileManager.Instance != null)
            {
                foreach (var unit in TileManager.Instance.GetAllUnits())
                {
                    if (unit != null && unit.isQueen && unit.faction == Faction.Player)
                    {
                        queen = unit;
                        break;
                    }
                }
            }
        }

        if (queen == null)
        {
            Debug.Log("[페르몬] 여왕벌을 찾을 수 없어 페르몬 명령을 실행할 수 없습니다.");
            return;
        }

        // 페르몬 명령 실행
        QueenPheromoneCommandHandler.ExecutePheromone(
            queen,
            CommandTarget.ForTile(queen.q, queen.r),
            squad
        );
        
        string squadName = squad == WorkerSquad.Squad1 ? "1번" :
                          squad == WorkerSquad.Squad2 ? "2번" : "3번";
        Debug.Log($"[단축키] {squadName} 부대 페르몬 명령 (queen: {queen.name})");
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
                        
                        // ✅ 타일 정보를 SelectionInfoUI에 전달
                        if (SelectionInfoUI.Instance != null)
                        {
                            SelectionInfoUI.Instance.SelectTile(tile);
                        }
                        
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

        // Try 2D - 모든 히트 수집 ?
        Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        RaycastHit2D[] hits2D = Physics2D.RaycastAll(wp, Vector2.zero);
            
        if (hits2D.Length > 0)
        {   
            // 바로 타일에 대한 명령
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

    void HandleRightClickForMultipleUnits(List<UnitAgent> units)
    {
        // ✅ 다중 유닛 우클릭 이동 명령 주석 처리 (요구사항 4)
        /*
        HexTile targetTile = null;
        Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        RaycastHit2D[] hits2D = Physics2D.RaycastAll(wp, Vector2.zero);

        if (hits2D.Length > 0)
        {
            // 타일 찾기
            foreach (var hit2 in hits2D)
            {
                var tile = hit2.collider.GetComponentInParent<HexTile>();
                if (tile != null)
                {
                    targetTile = tile;
                    break;
                }
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
        */
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
        
        // ✅ SelectionInfoUI 선택 해제
        if (SelectionInfoUI.Instance != null)
        {
            SelectionInfoUI.Instance.ClearSelection();
        }
    }

    /// <summary>
    /// 현재 선택된 유닛 가져오기 (SelectionInfoUI에서 사용)
    /// </summary>
    public UnitAgent GetSelectedUnit()
    {
        return selectedUnitInstance;
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

        // if there is a pending command, execute with this tile as target
        if (PendingCommandHolder.Instance != null && PendingCommandHolder.Instance.HasPending)
        {
            PendingCommandHolder.Instance.ExecutePending(CommandTarget.ForTile(tile.q, tile.r));
            StopMoveMode();
            return;
        }

        // If selectedUnitInstance is a player queen that can move -> command it
        if (selectedUnitInstance != null && selectedUnitInstance.isQueen && selectedUnitInstance.canMove && selectedUnitInstance.faction == Faction.Player)
        {
            // ✅ 꿀벌집이 있으면 활동 범위 체크 (요구사항 4)
            if (selectedUnitInstance.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(
                    selectedUnitInstance.homeHive.q, selectedUnitInstance.homeHive.r,
                    tile.q, tile.r
                );

                int activityRadius = 5; // 기본값
                if (HiveManager.Instance != null)
                {
                    activityRadius = HiveManager.Instance.hiveActivityRadius;
                }

                if (distanceToHive > activityRadius)
                {
                    if (debugText != null)
                    {
                        debugText.text = $"꿀벌집 활동 범위를 벗어났습니다 ({distanceToHive}/{activityRadius})";
                    }
                    Debug.Log($"[여왕벌] 이동 불가: 꿀벌집으로부터 {distanceToHive}칸 (최대 {activityRadius}칸)");
                    return;
                }
            }

            // 여왕벌 전용 이동 명령
            var queenBehavior = selectedUnitInstance.GetComponent<QueenBehaviorController>();
            if (queenBehavior != null)
            {
                queenBehavior.IssueCommandToTile(tile);
                StopMoveMode();
                return;
            }

            // QueenBehaviorController가 없으면 기본 이동
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
            return;
        }

        // If selected unit is null or an enemy or a non-moving unit, command the queen without changing selection
        UnitAgent queenAgent = null;
        if (TileManager.Instance != null)
        {
            foreach (var unit in TileManager.Instance.GetAllUnits())
            {
                if (unit != null && unit.isQueen && unit.faction == Faction.Player)
                {
                    queenAgent = unit;
                    break;
                }
            }
        }

        if (queenAgent != null && queenAgent.canMove)
        {
            // Activity radius check using queen's homeHive
            if (queenAgent.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(
                    queenAgent.homeHive.q, queenAgent.homeHive.r,
                    tile.q, tile.r
                );

                int activityRadius = 5;
                if (HiveManager.Instance != null) activityRadius = HiveManager.Instance.hiveActivityRadius;

                if (distanceToHive > activityRadius)
                {
                    if (debugText != null)
                    {
                        debugText.text = $"꿀벌집 활동 범위를 벗어났습니다 ({distanceToHive}/{activityRadius})";
                    }
                    Debug.Log($"[여왕벌] 이동 불가(queen): 꿀벌집으로부터 {distanceToHive}칸 (최대 {activityRadius}칸)");
                    return;
                }
            }

            // Issue move to queen without changing selectedUnitInstance
            var queenBehavior = queenAgent.GetComponent<QueenBehaviorController>();
            if (queenBehavior != null)
            {
                queenBehavior.IssueCommandToTile(tile);
                StopMoveMode();
                return;
            }

            // Fallback to UnitController movement
            var start = TileManager.Instance.GetTile(queenAgent.q, queenAgent.r);
            var path2 = Pathfinder.FindPath(start, tile);
            if (path2 != null && path2.Count > 0)
            {
                var ctrl = queenAgent.GetComponent<UnitController>();
                if (ctrl == null) ctrl = queenAgent.gameObject.AddComponent<UnitController>();
                ctrl.agent = queenAgent;
                ctrl.SetPath(path2);
            }
            StopMoveMode();
            return;
        }

        // No valid mover found
        if (debugText != null)
        {
            debugText.text = "이동할 수 있는 유닛이 없습니다.";
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
