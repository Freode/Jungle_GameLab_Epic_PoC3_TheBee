using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

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
    [SerializeField] private float verticalOffset = 10f; // 추가 여유 공간
    [SerializeField] private float minPanelHeight = 100f; // 최소 패널 높이
    [SerializeField] private float maxPanelHeight = 500f; // 최대 패널 높이
    [SerializeField] private float fixedPanelWidth = 500f; // 고정 패널 너비 ?

    private Camera mainCamera;
    private UnitAgent selectedUnit; // 단일 선택된 유닛
    private HexTile selectedTile; // 선택된 타일
    private List<UnitAgent> dragSelectedUnits = new List<UnitAgent>(); // 드래그 선택된 유닛들
    
    private ContentSizeFitter sizeFitter; // 자동 크기 조절
    private LayoutElement layoutElement; // 크기 제한용
    
    // 이벤트 구독 관리 ?
    private CombatUnit subscribedCombat; // 현재 구독 중인 CombatUnit
    private UnitBehaviorController subscribedBehavior; // 현재 구독 중인 UnitBehaviorController ?
    private bool hiveManagerSubscribed = false;

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
            
            // VerticalLayoutGroup 확인/추가 (텍스트들을 자동 배치)
            var layoutGroup = infoPanel.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = infoPanel.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.spacing = 5f;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                Debug.Log("[SelectionInfoUI] VerticalLayoutGroup 자동 추가됨");
            }
            
            // ContentSizeFitter 확인/추가
            sizeFitter = infoPanel.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = infoPanel.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로 크기 고정 ?
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;   // 세로 크기 자동
                Debug.Log("[SelectionInfoUI] ContentSizeFitter 자동 추가됨");
            }
            
            // LayoutElement 확인/추가 (크기 제한용)
            layoutElement = infoPanel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = infoPanel.AddComponent<LayoutElement>();
                Debug.Log("[SelectionInfoUI] LayoutElement 자동 추가됨");
            }
            
            // 크기 제한 설정
            layoutElement.minHeight = minPanelHeight;
            layoutElement.preferredHeight = -1; // 자동
            layoutElement.flexibleHeight = 0; // 고정
            layoutElement.preferredWidth = fixedPanelWidth; // 가로 크기 고정 ?
            
            // RectTransform 설정 (가로 크기 고정)
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(fixedPanelWidth, panelRect.sizeDelta.y);
                // 앵커를 Bottom-Left로 설정 ?
                panelRect.anchorMin = new Vector2(0, 0);
                panelRect.anchorMax = new Vector2(0, 0);
                panelRect.pivot = new Vector2(0, 0); // 좌하단 기준
            }
            
            // 텍스트 컴포넌트 설정
            if (titleText != null)
            {
                SetupTextComponent(titleText, "titleText");
            }
            
            if (detailsText != null)
            {
                SetupTextComponent(detailsText, "detailsText");
            }
            
            // 초기 위치 설정은 AdjustPanelPosition에서 처리
        }

        // Subscribe to HiveManager upgrade/resource events so UI updates when stats change
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += OnHiveManagerChangedSimple;
            HiveManager.Instance.OnWorkerCountChanged += OnHiveManagerWorkerCountChanged;
            HiveManager.Instance.OnUpgradeApplied += OnHiveManagerUpgradeApplied;
            hiveManagerSubscribed = true;
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe hive manager if subscribed
        if (hiveManagerSubscribed && HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged -= OnHiveManagerChangedSimple;
            HiveManager.Instance.OnWorkerCountChanged -= OnHiveManagerWorkerCountChanged;
            HiveManager.Instance.OnUpgradeApplied -= OnHiveManagerUpgradeApplied;
            hiveManagerSubscribed = false;
        }

        // 이벤트 구독 해제 (스크립트 비활성화 시) ?
        UnsubscribeFromCombat();
        UnsubscribeFromBehavior(); // ?
    }

    /// <summary>
    /// TextMeshProUGUI 컴포넌트 설정
    /// </summary>
    void SetupTextComponent(TextMeshProUGUI textComponent, string name)
    {
        if (textComponent == null) return;
        
        // 텍스트 설정
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.enableWordWrapping = true;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        
        // LayoutElement 확인/추가
        var textLayout = textComponent.GetComponent<LayoutElement>();
        if (textLayout == null)
        {
            textLayout = textComponent.gameObject.AddComponent<LayoutElement>();
            Debug.Log($"[SelectionInfoUI] {name} LayoutElement 추가됨");
        }
        
        // 텍스트 자동 크기 조절
        textLayout.preferredHeight = -1; // 자동
        textLayout.flexibleHeight = 1; // 유연하게
        textLayout.minHeight = 20f; // 최소 높이
        
        Debug.Log($"[SelectionInfoUI] {name} 설정 완료");
    }

    void OnHiveManagerUpgradeApplied()
    {
        if (selectedUnit != null) UpdateUnitInfo(selectedUnit);
    }

    void EnsureHiveManagerSubscription()
    {
        if (!hiveManagerSubscribed && HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += OnHiveManagerChangedSimple;
            HiveManager.Instance.OnWorkerCountChanged += OnHiveManagerWorkerCountChanged;
            HiveManager.Instance.OnUpgradeApplied += OnHiveManagerUpgradeApplied;
            hiveManagerSubscribed = true;
        }
    }
    
    void FixedUpdate()
    {
        EnsureHiveManagerSubscription();
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

        titleText.text = $"선택된 꿀벌 : <color=#00FF00>{dragSelectedUnits.Count}마리</color>";
        UpdateDragSelectedInfo();
        AdjustPanelPosition();
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
            details += $"체력: <color=#00FF00>{avgHealth:F1}</color>/{avgMaxHealth:F1}\n";
            details += $"공격력: <color=#00FF00>{avgAttack:F1}</color>\n";
            
            details += $"===총합===\n";
            details += $"체력: <color=#00FF00>{totalHealth}</color>/{totalMaxHealth}\n";
            details += $"공격력: <color=#00FF00>{totalAttack}</color>\n";
        }

        detailsText.text = details.TrimEnd('\n');
        AdjustPanelPosition();
    }

    void ShowUnitInfo(UnitAgent unit)
    {
        if (infoPanel == null || titleText == null || detailsText == null) return;

        if (!infoPanel.activeSelf)
        {
            infoPanel.SetActive(true);
        }

        // 이전 구독 해제 ?
        UnsubscribeFromCombat();
        UnsubscribeFromBehavior(); // ?

        // ? UnitAgent의 unitName 사용
        string displayName = GetUnitDisplayName(unit);
        titleText.text = displayName;

        // CombatUnit 이벤트 구독 ?
        SubscribeToCombat(unit);
        
        // UnitBehaviorController 이벤트 구독 ?
        SubscribeToBehavior(unit);

        UpdateUnitInfo(unit);
        AdjustPanelPosition();
    }
    
    /// <summary>
    /// CombatUnit 이벤트 구독 ?
    /// </summary>
    void SubscribeToCombat(UnitAgent unit)
    {
        if (unit == null) return;
        
        // 하이브의 경우 Hive의 CombatUnit 구독
        var hive = unit.GetComponent<Hive>();
        if (hive != null)
        {
            subscribedCombat = hive.GetComponent<CombatUnit>();
        }
        else
        {
            subscribedCombat = unit.GetComponent<CombatUnit>();
        }
        
        if (subscribedCombat != null)
        {
            subscribedCombat.OnStatsChanged += OnCombatStatsChanged;
            Debug.Log($"[SelectionInfoUI] CombatUnit 이벤트 구독: {unit.name}");
        }
    }

    /// <summary>
    /// UnitBehaviorController 이벤트 구독 ?
    /// </summary>
    void SubscribeToBehavior(UnitAgent unit)
    {
        if (unit == null) return;
        
        subscribedBehavior = unit.GetComponent<UnitBehaviorController>();
        
        if (subscribedBehavior != null)
        {
            subscribedBehavior.OnTaskChanged += OnTaskChanged;
            Debug.Log($"[SelectionInfoUI] UnitBehaviorController 이벤트 구독: {unit.name}");
        }
    }
    
    /// <summary>
    /// CombatUnit 이벤트 구독 해제 ?
    /// </summary>
    void UnsubscribeFromCombat()
    {
        if (subscribedCombat != null)
        {
            subscribedCombat.OnStatsChanged -= OnCombatStatsChanged;
            Debug.Log($"[SelectionInfoUI] CombatUnit 이벤트 구독 해제");
            subscribedCombat = null;
        }
    }

    /// <summary>
    /// UnitBehaviorController 이벤트 구독 해제 ?
    /// </summary>
    void UnsubscribeFromBehavior()
    {
        if (subscribedBehavior != null)
        {
            subscribedBehavior.OnTaskChanged -= OnTaskChanged;
            Debug.Log($"[SelectionInfoUI] UnitBehaviorController 이벤트 구독 해제");
            subscribedBehavior = null;
        }
    }
    
    /// <summary>
    /// CombatUnit 이벤트 핸들러 ?
    /// </summary>
    void OnCombatStatsChanged()
    {
        // 선택된 유닛이 있으면 정보 업데이트
        if (selectedUnit != null)
        {
            UpdateUnitInfo(selectedUnit);
        }
    }
    
    /// <summary>
    /// UnitBehaviorController 이벤트 핸들러
    /// </summary>
    void OnTaskChanged()
    {
        // 선택된 유닛이 있으면 정보 업데이트
        if (selectedUnit != null)
        {
            UpdateUnitInfo(selectedUnit);
        }
    }
    
    private void OnHiveManagerChangedSimple()
    {
        if (selectedUnit != null) UpdateUnitInfo(selectedUnit);
    }
    
    private void OnHiveManagerWorkerCountChanged(int cur, int max)
    {
        if (selectedUnit != null) UpdateUnitInfo(selectedUnit);
    }
    
    void UpdateUnitInfo(UnitAgent unit)
    {
        if (detailsText == null || unit == null) return;

        string details = "";

        details += $"위치: ({unit.q}, {unit.r})\n";

        var combat = unit.GetComponent<CombatUnit>();
        if (combat != null)
        {
            details += $"체력: <color=#00FF00>{combat.health}</color>/{combat.maxHealth}\n";
            details += $"공격력: <color=#00FF00>{combat.attack}</color>\n";
        }

        var behavior = unit.GetComponent<UnitBehaviorController>();
        if (behavior != null)
        {
            details += $"현재 작업: <color=#00FF00>{behavior.currentTaskString}</color>\n";
        }

        //// Show global/role upgrades from HiveManager when available
        //if (HiveManager.Instance != null)
        //{
        //    details += "\n=== 업그레이드 ===\n";

        //    // 일벌 채취 (Gatherer)
        //    int gatherAmount = HiveManager.Instance.GetGatherAmount();
        //    details += $"자원 채취량 (채취형 일벌): <color=#00FF00>+{gatherAmount}</color>\n";

        //    // 일벌 공격력 (Attacker)
        //    int attack = HiveManager.Instance.GetWorkerAttack();
        //    details += $"일벌 공격력 (공격형 일벌): <color=#00FF00>+{attack}</color>\n";

        //    // 일벌 체력 (Tank role is primary for health upgrades)
        //    int wHealth = HiveManager.Instance.GetWorkerMaxHealth();
        //    details += $"일벌 최대 체력 (탱커형 일벌 기준): <color=#00FF00>{wHealth}</color>\n";

        //    // 벌집 체력
        //    int hiveHealth = HiveManager.Instance.GetHiveMaxHealth();
        //    details += $"벌집 최대 체력: <color=#00FF00>{hiveHealth} HP</color>\n";

        //    // 활동 범위
        //    int range = HiveManager.Instance.hiveActivityRadius;
        //    details += $"하이브 활동 범위: <color=#00FF00>{range}</color>칸\n";
        //}

        detailsText.text = details.TrimEnd('\n');
        AdjustPanelPosition();
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
        AdjustPanelPosition();
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
                details += $"남은 꿀: <color=#FFFF00>{tile.resourceAmount}</color>\n";
            }
            else if (tile.terrain != null && tile.terrain.resourceYield > 0)
            {
                details += "남은 꿀: 고갈됨\n";
            }

            if (tile.enemyHive != null)
            {
                details += $"적 하이브 위치\n";
                var hiveCombat = tile.enemyHive.GetComponent<CombatUnit>();
                if (hiveCombat != null)
                {
                    details += $"하이브 체력: <color=#00FF00>{hiveCombat.health}</color>/{hiveCombat.maxHealth}\n";
                }
            }
        }
        else
        {
            details += $"위치: ({tile.q}, {tile.r})\n";
            details += $"시야 상태: {GetFogStateString(tile.fogState)}\n";
        }

        detailsText.text = details.TrimEnd('\n');
        AdjustPanelPosition();
    }

    /// <summary>
    /// 텍스트 길이에 따라 패널 위치를 위로 조정
    /// 화면 하단 기준: padding + 패널 높이의 절반
    /// </summary>
    void AdjustPanelPosition()
    {
        if (panelRect == null) return;

        // 1. 텍스트 메쉬 강제 업데이트
        if (titleText != null)
        {
            titleText.ForceMeshUpdate();
        }
        if (detailsText != null)
        {
            detailsText.ForceMeshUpdate();
        }
        
        // 2. 레이아웃 강제 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        
        // 3. ContentSizeFitter 강제 업데이트
        if (sizeFitter != null)
        {
            sizeFitter.SetLayoutVertical();
        }
        
        // 4. Canvas 강제 업데이트
        Canvas.ForceUpdateCanvases();
        
        // 5. 다시 한 번 레이아웃 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        
        // 6. 패널의 현재 높이 가져오기
        float panelHeight = panelRect.rect.height;
        
        // 7. 최대 높이 제한 적용
        if (panelHeight > maxPanelHeight)
        {
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = maxPanelHeight;
            }
            panelHeight = maxPanelHeight;
            
            // 스크롤 가능하도록 detailsText 설정
            if (detailsText != null)
            {
                detailsText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
        else
        {
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = -1; // 자동
            }
            
            if (detailsText != null)
            {
                detailsText.overflowMode = TextOverflowModes.Overflow;
            }
        }
        
        // 8. 최소 높이 보장
        if (panelHeight < minPanelHeight)
        {
            panelHeight = minPanelHeight;
        }
        
        // 9. 위치 계산: 화면 하단 기준
        // X: 왼쪽에서 fixedPosition.x 만큼
        // Y: 하단에서 padding + 패널 높이의 절반 ?
        float xPos = fixedPosition.x;
        float yPos = verticalOffset; // padding + 패널 높이의 절반
        
        Vector2 adjustedPosition = new Vector2(xPos, yPos);
        panelRect.anchoredPosition = adjustedPosition;
        
        // 디버그 로그
        Debug.Log($"[SelectionInfoUI] 패널 크기: {fixedPanelWidth} x {panelHeight}");
        Debug.Log($"[SelectionInfoUI] 패널 위치: {adjustedPosition} (하단 기준)");
    }

    void HideInfo()
    {
        if (infoPanel != null && infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }
        
        // 이벤트 구독 해제 ?
        UnsubscribeFromCombat();
        UnsubscribeFromBehavior(); // ?
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
        
        // 이벤트 구독 해제 (타일 선택 시) ?
        UnsubscribeFromCombat();
        UnsubscribeFromBehavior(); // ?
        
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
        
        // 이벤트 구독 해제 ?
        UnsubscribeFromCombat();
        UnsubscribeFromBehavior(); // ?
        
        HideInfo();
    }

    /// <summary>
    /// ? 유닛의 표시 이름 가져오기
    /// </summary>
    string GetUnitDisplayName(UnitAgent unit)
    {
        // 1. UnitAgent의 unitName이 설정되어 있는 경우
        if (!string.IsNullOrEmpty(unit.unitName))
        {
            string name = "";

            // Determine role suffix for player workers if applicable
            string roleSuffix = "";
            if (unit.faction == Faction.Player && !unit.isQueen)
            {
                var roleAssigner = unit.GetComponent<RoleAssigner>();
                if (roleAssigner != null)
                {
                    switch (roleAssigner.role)
                    {
                        case RoleType.Gatherer: roleSuffix = "(채취형)"; break;
                        case RoleType.Attacker: roleSuffix = "(공격형)"; break;
                        case RoleType.Tank: roleSuffix = "(방어형)"; break;
                        default: roleSuffix = ""; break;
                    }
                }
            }

            switch(unit.faction)
            {
                case Faction.Player:
                    name = $"<color=#00FF00>{unit.unitName}{roleSuffix}</color>";
                    break;
                case Faction.Enemy:
                    name = $"<color=#FF0000>{unit.unitName}</color>";
                    break;
                case Faction.Neutral:
                    name = $"<color=#FFFF00>{unit.unitName}</color>";
                    break;
                default:
                    name = unit.unitName;
                    break;
            }

            return name;
        }

        // 2. 기본 유형 이름 반환
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
            // If player worker, include role if available
            var roleAssigner = unit.GetComponent<RoleAssigner>();
            if (roleAssigner != null)
            {
                switch (roleAssigner.role)
                {
                    case RoleType.Gatherer:
                        return "일벌(채취형)";
                    case RoleType.Attacker:
                        return "일벌(공격형)";
                    case RoleType.Tank:
                        return "일벌(탱커형)";
                    default:
                        return "일벌";
                }
            }

            return "일벌";
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
