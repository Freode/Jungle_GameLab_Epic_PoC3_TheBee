using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 유닛/타일 선택 시 정보 표시 UI
/// </summary>
public class SelectionInfoUI : MonoBehaviour
{
    public static SelectionInfoUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private RectTransform panelRect;

    [Header("Settings")]
    [SerializeField] private Vector2 fixedPosition = new Vector2(10f, 10f);

    private Camera mainCamera;
    private UnitAgent selectedUnit; // 단일 선택된 유닛
    private HexTile selectedTile; // 선택된 타일
    private List<UnitAgent> dragSelectedUnits = new List<UnitAgent>(); // 드래그 선택된 유닛들

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            
            if (panelRect != null)
            {
                panelRect.anchoredPosition = fixedPosition;
            }
        }
    }

    void FixedUpdate()
    {
        UpdateInfo();
    }

    void UpdateInfo()
    {
        // 1순위: 드래그 선택된 유닛들
        if (DragSelector.Instance != null)
        {
            var dragUnits = DragSelector.Instance.GetSelectedUnits();
            if (dragUnits.Count > 0)
            {
                if (!AreListsEqual(dragUnits, dragSelectedUnits))
                {
                    dragSelectedUnits.Clear();
                    dragSelectedUnits.AddRange(dragUnits);
                    selectedUnit = null;
                    selectedTile = null;
                    ShowDragSelectedInfo();
                }
                else
                {
                    UpdateDragSelectedInfo();
                }
                return;
            }
            else if (dragSelectedUnits.Count > 0)
            {
                // 드래그 선택 해제됨
                dragSelectedUnits.Clear();
            }
        }

        // 2순위: 단일 선택된 유닛 (TileClickMover)
        if (TileClickMover.Instance != null)
        {
            var unit = TileClickMover.Instance.GetSelectedUnit();
            if (unit != null)
            {
                if (selectedUnit != unit)
                {
                    selectedUnit = unit;
                    selectedTile = null;
                    ShowUnitInfo(unit);
                }
                else
                {
                    UpdateUnitInfo(unit);
                }
                return;
            }
        }

        // 3순위: 선택된 타일 (외부에서 SelectTile 호출됨)
        if (selectedTile != null)
        {
            UpdateTileInfo(selectedTile);
            return;
        }

        // 선택 해제됨
        if (selectedUnit != null || infoPanel.activeSelf)
        {
            selectedUnit = null;
            HideInfo();
        }
    }

    bool AreListsEqual(List<UnitAgent> list1, List<UnitAgent> list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    void ShowDragSelectedInfo()
    {
        if (infoPanel == null || titleText == null || detailsText == null) return;

        if (!infoPanel.activeSelf)
        {
            infoPanel.SetActive(true);
        }

        titleText.text = $"선택된 꿀벌 : {dragSelectedUnits.Count}마리";
        UpdateDragSelectedInfo();
    }

    void UpdateDragSelectedInfo()
    {
        if (detailsText == null || dragSelectedUnits.Count == 0) return;

        // 평균 능력치 계산
        int totalHealth = 0;
        int totalMaxHealth = 0;
        int totalAttack = 0;
        int validCount = 0;

        foreach (var unit in dragSelectedUnits)
        {
            if (unit == null) continue;

            var combat = unit.GetComponent<CombatUnit>();
            if (combat != null)
            {
                totalHealth += combat.health;
                totalMaxHealth += combat.maxHealth;
                totalAttack += combat.attack;
                validCount++;
            }
        }

        string details = "";
        
        if (validCount > 0)
        {
            float avgHealth = (float)totalHealth / validCount;
            float avgMaxHealth = (float)totalMaxHealth / validCount;
            float avgAttack = (float)totalAttack / validCount;

            details += $"===평균 능력치===\n";
            details += $"체력: {avgHealth:F1}/{avgMaxHealth:F1}\n";
            details += $"공격력: {avgAttack:F1}\n";
            
            details += $"===총합===\n";
            details += $"체력: {totalHealth}/{totalMaxHealth}\n";
            details += $"공격력: {totalAttack}\n";
        }

        detailsText.text = details.TrimEnd('\n');
    }

    void ShowUnitInfo(UnitAgent unit)
    {
        if (infoPanel == null || titleText == null || detailsText == null) return;

        if (!infoPanel.activeSelf)
        {
            infoPanel.SetActive(true);
        }

        // ? UnitAgent의 unitName 사용
        string displayName = GetUnitDisplayName(unit);
        titleText.text = displayName;

        UpdateUnitInfo(unit);
    }

    void UpdateUnitInfo(UnitAgent unit)
    {
        if (detailsText == null || unit == null) return;

        string details = "";

        details += $"위치: ({unit.q}, {unit.r})\n";

        var combat = unit.GetComponent<CombatUnit>();
        if (combat != null)
        {
            details += $"체력: {combat.health}/{combat.maxHealth}\n";
            details += $"공격력: {combat.attack}\n";
        }

        var behavior = unit.GetComponent<UnitBehaviorController>();
        if (behavior != null)
        {
            details += $"현재 작업: {GetTaskString(behavior.currentTask)}\n";
        }

        detailsText.text = details.TrimEnd('\n');
    }

    void ShowTileInfo(HexTile tile)
    {
        if (infoPanel == null || titleText == null || detailsText == null) return;

        if (!infoPanel.activeSelf)
        {
            infoPanel.SetActive(true);
        }

        // ? 타일 이름 표시
        string tileName = GetTileDisplayName(tile);
        titleText.text = tileName;

        UpdateTileInfo(tile);
    }

    void UpdateTileInfo(HexTile tile)
    {
        if (detailsText == null || tile == null) return;

        string details = "";

        if (tile.fogState != HexTile.FogState.Hidden)
        {
            //if (tile.terrain != null)
            //{
            //    details += $"지형: {tile.terrain.terrainName}\n";
            //}

            details += $"위치: ({tile.q}, {tile.r})\n";

            if (tile.resourceAmount > 0)
            {
                details += $"자원량: {tile.resourceAmount}\n";
            }
            else if (tile.terrain != null && tile.terrain.resourceYield > 0)
            {
                details += "자원: 고갈됨\n";
            }

            if (tile.enemyHive != null)
            {
                details += $"적 하이브 위치\n";
                var hiveCombat = tile.enemyHive.GetComponent<CombatUnit>();
                if (hiveCombat != null)
                {
                    details += $"하이브 체력: {hiveCombat.health}/{hiveCombat.maxHealth}\n";
                }
            }
        }
        else
        {
            details += $"시야 상태: {GetFogStateString(tile.fogState)}\n";
        }

        detailsText.text = details.TrimEnd('\n');
    }

    void HideInfo()
    {
        if (infoPanel != null && infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 타일 선택 (외부 호출용)
    /// </summary>
    public void SelectTile(HexTile tile)
    {
        if (tile == null || tile.fogState == HexTile.FogState.Hidden)
        {
            selectedTile = null;
            HideInfo();
            return;
        }

        selectedUnit = null;
        dragSelectedUnits.Clear();
        selectedTile = tile;
        ShowTileInfo(tile);
    }

    /// <summary>
    /// 선택 해제 (외부 호출용)
    /// </summary>
    public void ClearSelection()
    {
        selectedUnit = null;
        selectedTile = null;
        dragSelectedUnits.Clear();
        HideInfo();
    }

    /// <summary>
    /// ? 유닛의 표시 이름 가져오기
    /// </summary>
    string GetUnitDisplayName(UnitAgent unit)
    {
        // 1. UnitAgent의 unitName이 있으면 사용
        if (!string.IsNullOrEmpty(unit.unitName))
        {
            return unit.unitName;
        }

        // 2. 기본 타입 이름 생성
        return GetUnitTypeString(unit);
    }

    /// <summary>
    /// ? 타일의 표시 이름 가져오기
    /// </summary>
    string GetTileDisplayName(HexTile tile)
    {
        // 타일에 UnitAgent가 있으면 (하이브 등)
        var unit = tile.GetComponent<UnitAgent>();
        if (unit != null)
        {
            return GetUnitDisplayName(unit);
        }

        // 타일의 지형 이름 사용
        if (tile.terrain != null && !string.IsNullOrEmpty(tile.terrain.terrainName))
        {
            return $"{tile.terrain.terrainName}";
        }

        // 기본값
        return $"타일 ({tile.q}, {tile.r})";
    }

    string GetUnitTypeString(UnitAgent unit)
    {
        // ? 하이브 체크
        var hive = unit.GetComponent<Hive>();
        if (hive != null)
        {
            return "꿀벌집";
        }

        // ? 여왕벌 체크
        if (unit.isQueen)
        {
            return "여왕벌";
        }
        
        // ? 진영별 구분
        if (unit.faction == Faction.Player)
        {
            return "일꾼 꿀벌";
        }
        else if (unit.faction == Faction.Enemy)
        {
            return "말벌";
        }
        else
        {
            return "중립 유닛";
        }
    }

    string GetTaskString(UnitTaskType task)
    {
        switch (task)
        {
            case UnitTaskType.Idle: return "대기";
            case UnitTaskType.Move: return "이동";
            case UnitTaskType.Attack: return "공격";
            case UnitTaskType.Gather: return "자원 채취";
            case UnitTaskType.ReturnToHive: return "하이브 복귀";
            case UnitTaskType.FollowQueen: return "여왕벌 추적";
            default: return "알 수 없음";
        }
    }

    string GetFogStateString(HexTile.FogState state)
    {
        switch (state)
        {
            case HexTile.FogState.Hidden: return "미탐색";
            case HexTile.FogState.Revealed: return "탐색됨";
            case HexTile.FogState.Visible: return "시야 내";
            default: return "알 수 없음";
        }
    }
}
