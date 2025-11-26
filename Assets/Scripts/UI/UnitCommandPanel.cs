using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UnitCommandPanel : MonoBehaviour
{
    public static UnitCommandPanel Instance { get; private set; }

    public GameObject panelRoot;
    public RectTransform buttonContainer;
    public GameObject commandButtonPrefab; // prefab with Button + Image + Text

    [Header("유닛 정보 UI")]
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitHealthText;
    public TextMeshProUGUI unitAttackText;
    public TextMeshProUGUI workerCountText; // 일꾼 수 표시 ?

    private UnitAgent currentAgent;

    // a list of default SOCommands, assign in inspector
    public SOCommand[] defaultCommands;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // ✅ 여왕벌을 찾을 때까지 반복 탐색 코루틴 시작
        StartCoroutine(FindQueenAndShowCommandsRoutine());
        
        // 자원 변경 이벤트 구독
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += RefreshButtonStates;
        }
    }
    
    /// <summary>
    /// 여왕벌을 찾을 때까지 반복 탐색하는 코루틴
    /// </summary>
    System.Collections.IEnumerator FindQueenAndShowCommandsRoutine()
    {
        Debug.Log("[명령 UI] 여왕벌 탐색 시작...");
        
        UnitAgent queenAgent = null;
        int attemptCount = 0;
        
        // 여왕벌을 찾을 때까지 반복 (최대 30초)
        while (queenAgent == null && attemptCount < 300)
        {
            attemptCount++;
            
            // TileManager가 있으면 여왕벌 찾기
            if (TileManager.Instance != null)
            {
                foreach (var unit in TileManager.Instance.GetAllUnits())
                {
                    if (unit != null && unit.isQueen && unit.faction == Faction.Player)
                    {
                        queenAgent = unit;
                        Debug.Log($"[명령 UI] 여왕벌 발견! (시도 횟수: {attemptCount})");
                        break;
                    }
                }
            }
            
            // 못 찾았으면 0.1초 대기 후 재시도
            if (queenAgent == null)
            {
                if (attemptCount % 10 == 0) // 1초마다 로그
                {
                    Debug.Log($"[명령 UI] 여왕벌 탐색 중... (시도 {attemptCount}/300)");
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // 여왕벌을 찾았으면 명령 패널 표시
        if (queenAgent != null)
        {
            ShowQueenCommands(queenAgent);
            Debug.Log("[명령 UI] 여왕벌 명령 패널 표시 완료!");
        }
        else
        {
            Debug.LogWarning("[명령 UI] 여왕벌을 찾지 못했습니다! (30초 타임아웃)");
            // 패널은 숨김 상태 유지
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
    
    /// <summary>
    /// 여왕벌 명령 패널 표시
    /// </summary>
    void ShowQueenCommands(UnitAgent queenAgent)
    {
        if (queenAgent == null)
        {
            Debug.LogWarning("[명령 UI] 여왕벌이 null입니다!");
            return;
        }
        
        // 이전 agent의 이벤트 구독 해제
        if (currentAgent != null)
        {
            UnsubscribeFromEvents(currentAgent);
        }
        
        currentAgent = queenAgent;
        if (panelRoot != null) panelRoot.SetActive(true);
        
        // 이벤트 구독
        SubscribeToEvents(currentAgent);
        
        // 유닛 정보 표시
        UpdateUnitInfo();
        
        RebuildCommands();
        
        Debug.Log($"[명령 UI] 여왕벌 명령 패널 활성화: {queenAgent.name}");
    }

    void OnDestroy()
    {
        // 자원 변경 이벤트 구독 해제
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged -= RefreshButtonStates;
        }
    }

    // Show the panel for the given unit
    public void Show(UnitAgent agent)
    {
        // ✅ 항상 여왕벌 명령만 표시 (요구사항 2)
        // 다른 유닛을 선택해도 명령 패널은 여왕벌 명령 유지
        // (아무 작업도 하지 않음)
    }

    public void Hide()
    {
        // ✅ 명령 패널은 절대 숨기지 않음 (요구사항 2)
        // (아무 작업도 하지 않음)
    }

    /// <summary>
    /// 유닛 이벤트 구독
    /// </summary>
    void SubscribeToEvents(UnitAgent agent)
    {
        if (agent == null) return;

        var combat = agent.GetComponent<CombatUnit>();
        if (combat != null)
        {
            combat.OnStatsChanged += UpdateUnitInfo;
        }

        var hive = agent.GetComponent<Hive>();
        if (hive != null)
        {
            var hiveCombat = hive.GetComponent<CombatUnit>();
            if (hiveCombat != null)
            {
                hiveCombat.OnStatsChanged += UpdateUnitInfo;
            }
        }
    }

    /// <summary>
    /// 유닛 이벤트 구독 해제
    /// </summary>
    void UnsubscribeFromEvents(UnitAgent agent)
    {
        if (agent == null) return;

        var combat = agent.GetComponent<CombatUnit>();
        if (combat != null)
        {
            combat.OnStatsChanged -= UpdateUnitInfo;
        }

        var hive = agent.GetComponent<Hive>();
        if (hive != null)
        {
            var hiveCombat = hive.GetComponent<CombatUnit>();
            if (hiveCombat != null)
            {
                hiveCombat.OnStatsChanged -= UpdateUnitInfo;
            }
        }
    }

    void UpdateUnitInfo()
    {
        if (currentAgent == null)
        {
            // 정보 지움
            if (unitNameText != null) unitNameText.text = "";
            if (unitHealthText != null) unitHealthText.text = "";
            if (unitAttackText != null) unitAttackText.text = "";
            if (workerCountText != null) workerCountText.text = "";
            return;
        }

        // 유닛 이름
        string unitName = GetUnitName(currentAgent);
        if (unitNameText != null)
            unitNameText.text = unitName;

        // 하이브인지 확인
        var hive = currentAgent.GetComponent<Hive>();
        CombatUnit combat = null;
        
        if (hive != null)
        {
            // 하이브인 경우: 하이브 GameObject의 CombatUnit만 표시
            combat = hive.GetComponent<CombatUnit>();
            
            // 체력 표시
            if (combat != null && unitHealthText != null)
            {
                unitHealthText.text = $"HP: {combat.health}/{combat.maxHealth}";
            }
            
            // 하이브는 공격력 표시 안 함
            if (unitAttackText != null)
            {
                unitAttackText.text = "";
            }
        }
        else
        {
            // 일반 유닛인 경우: 유닛의 CombatUnit 표시
            combat = currentAgent.GetComponent<CombatUnit>();
            
            // 체력 표시
            if (combat != null)
            {
                if (unitHealthText != null)
                    unitHealthText.text = $"HP: {combat.health}/{combat.maxHealth}";
                
                // 공격력 표시 (0보다 클 때만)
                if (unitAttackText != null)
                {
                    if (combat.attack > 0)
                        unitAttackText.text = $"공격력: {combat.attack}";
                    else
                        unitAttackText.text = "";
                }
            }
            else
            {
                // CombatUnit이 없으면 숨김
                if (unitHealthText != null)
                    unitHealthText.text = "";
                
                if (unitAttackText != null)
                    unitAttackText.text = "";
            }
        }

        // 일꾼 수 정보 업데이트
        if (workerCountText != null)
        {
            // 플레이어 하이브인 경우 - HiveManager의 값 사용 ✅
            if (hive != null && currentAgent.faction == Faction.Player)
            {
                if (HiveManager.Instance != null)
                {
                    workerCountText.text = $"일꾼 수: {HiveManager.Instance.currentWorkers}/{HiveManager.Instance.maxWorkers}";
                }
                else
                {
                    workerCountText.text = $"일꾼 수: {hive.GetWorkers().Count}/{hive.maxWorkers}";
                }
            }
            // 여왕벌인 경우 (전체 일꾼 수) - HiveManager의 값 사용 ✅
            else if (currentAgent.faction == Faction.Player && currentAgent.isQueen)
            {
                if (HiveManager.Instance != null)
                {
                    workerCountText.text = $"일꾼 수: {HiveManager.Instance.currentWorkers}/{HiveManager.Instance.maxWorkers}";
                }
                else
                {
                    int workerCount = 0;
                    foreach (var unit in FindObjectsOfType<UnitAgent>())
                    {
                        if (unit.faction == Faction.Player && !unit.isQueen)
                            workerCount++;
                    }
                    workerCountText.text = $"일꾼 수: {workerCount}";
                }
            }
            else
            {
                workerCountText.text = ""; // 일반 유닛은 숨김
            }
        }
    }

    string GetUnitName(UnitAgent agent)
    {
        if (agent == null) return "알 수 없음";

        // 하이브 체크 (플레이어 진영)
        var hive = agent.GetComponent<Hive>();
        if (hive != null)
        {
            if (agent.faction == Faction.Player)
                return "꿀벌집";
            else if (agent.faction == Faction.Enemy && agent.gameObject.name.Contains("Elite"))
                return "장수말벌집";
            else if (agent.faction == Faction.Enemy)
                return "말벌집";
        }

        // 적 하이브 체크 (상대 진영)
        var enemyHive = agent.GetComponent<EnemyHive>();
        if(enemyHive != null)
        {
            if (agent.gameObject.name.Contains("Elite"))
                return "장수말벌집";
            else if(agent.gameObject.name.Contains("Normal"))
                return "말벌집";
        }

        // 여왕벌/말벌 여왕 체크
        if (agent.isQueen)
        {
            if (agent.faction == Faction.Player)
                return "여왕벌";
            else if (agent.faction == Faction.Enemy)
                return "말벌 여왕";
        }

        // 일꾼 체크
        if (agent.faction == Faction.Player && !agent.isQueen)
            return "일꾼 꿀벌";

        // 적 유닛 (말벌) - EliteWasp 구별 ?
        if (agent.faction == Faction.Enemy && !agent.isQueen)
        {
            // GameObject 이름으로 EliteWasp 체크 ?
            if (agent.gameObject.name.Contains("Lv2"))
            {
                return "장수말벌";
            }
            return "말벌";
        }

        // 중립 유닛
        if (agent.faction == Faction.Neutral)
            return "중립 유닛";

        // 기본 (GameObject 이름)
        return agent.gameObject.name;
    }

    void RebuildCommands()
    {
        // clear existing
        foreach (Transform t in buttonContainer) Destroy(t.gameObject);

        if (currentAgent == null) return;

        // Enemy 유닛은 명령 버튼 표시 안 함 (정보만 표시)
        if (currentAgent.faction == Faction.Enemy)
        {
            Debug.Log("[명령 UI] 적 유닛은 명령을 내릴 수 없습니다.");
            return;
        }

        // get commands from provider (IUnitCommandProvider) if available
        List<ICommand> commands = new List<ICommand>();
        var provider = currentAgent.GetComponent<IUnitCommandProvider>();
        if (provider != null)
        {
            foreach (var c in provider.GetCommands(currentAgent))
            {
                if (c != null) commands.Add(c);
            }
        }

        // fallback to defaultCommands if none provided
        if (commands.Count == 0 && defaultCommands != null)
        {
            foreach (var c in defaultCommands)
            {
                commands.Add(c);
            }
        }

        // create buttons (left-to-right via HorizontalLayoutGroup on buttonContainer)
        foreach (var cmd in commands)
        {
            var command = cmd; // local copy to avoid closure capture issues
            var btnObj = Instantiate(commandButtonPrefab, buttonContainer);
            btnObj.name = "cmd_" + command.Id;
            var btn = btnObj.GetComponentInChildren<Button>();
            var img = btnObj.GetComponentInChildren<UnityEngine.UI.Image>();
            var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            var txt = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
            
            // 아이콘 설정
            if (img != null) img.sprite = command.Icon;
            
            // 텍스트 설정 (이름 + 비용)
            string buttonText = command.DisplayName;
            // ✅ SOUpgradeCommand인 경우 현재 비용과 레벨 표시
            if (cmd is SOUpgradeCommand upgradeCmd)
            {
                int currentCost = upgradeCmd.GetCurrentCost();
                int currentLevel = upgradeCmd.GetCurrentLevel();
                int maxLv = upgradeCmd.maxLevel;

                string levelText = maxLv > 0
                    ? $" (Lv.<color=#00FF00>{currentLevel}</color>/{maxLv})"
                    : $" (Lv.<color=#00FF00>{currentLevel}</color>)";

                if (currentCost > 0)
                {
                    buttonText += $"\n꿀: <color=#00FF00>{currentCost}</color>{levelText}";
                }
                else
                {
                    buttonText += $"\n{levelText}";
                }
            }
            else if (!string.IsNullOrEmpty(cmd.CostText))
            {
                // 일반 명령은 기본 CostText 사용
                buttonText += $"\n{cmd.CostText}";
            }

            if (tmp != null) 
                tmp.text = buttonText;
            else if (txt != null) 
                txt.text = buttonText;

            // disable if not available
            bool avail = command.IsAvailable(currentAgent);
            if (btn != null)
            {
                btn.interactable = avail;
                btn.onClick.RemoveAllListeners();
                // use local copy of btn for closure
                var localBtn = btn;
                btn.onClick.AddListener(() => HandleCommandButtonClick(command, localBtn));
            }
        }
    }

    void HandleCommandButtonClick(ICommand cmd, Button clickedButton)
    {
        OnCommandClicked(cmd);

        // After command execution, refresh button states and unit info
        RefreshButtonStates();
        UpdateUnitInfo(); // 체력 등이 변경될 수 있으므로 업데이트
    }

    void OnCommandClicked(ICommand cmd)
    {
        if (currentAgent == null) return;
        if (!cmd.IsAvailable(currentAgent)) return;

        // Special-case: construct_hive and relocate_hive should execute immediately at the agent's current tile
        if (cmd.Id == "construct_hive" || cmd.Id == "relocate_hive")
        {
            cmd.Execute(currentAgent, CommandTarget.ForTile(currentAgent.q, currentAgent.r));
            
            // Hide panel if command requests it
            if (cmd.HidePanelOnClick)
            {
                Hide();
            }
            return;
        }

        if (cmd.RequiresTarget)
        {
            // ensure PendingCommandHolder exists
            PendingCommandHolder.EnsureInstance();
            if (PendingCommandHolder.Instance == null)
            {
                Debug.LogWarning("PendingCommandHolder not found in scene. Command will not be queued.");
                return;
            }

            // enter target selection mode
            TileClickMover.Instance?.EnterMoveMode();
            PendingCommandHolder.Instance?.SetPendingCommand(cmd, currentAgent);
            
            // Hide panel if command requests it
            if (cmd.HidePanelOnClick)
            {
                Hide();
            }
        }
        else
        {
            cmd.Execute(currentAgent, new CommandTarget { type = CommandTargetType.None });
            
            // Hide panel if command requests it
            if (cmd.HidePanelOnClick)
            {
                Hide();
            }
        }
    }

    // Refresh button states to reflect current availability
    void RefreshButtonStates()
    {
        if (currentAgent == null) return;

        // Get commands from provider
        List<ICommand> commands = new List<ICommand>();
        var provider = currentAgent.GetComponent<IUnitCommandProvider>();
        if (provider != null)
        {
            foreach (var c in provider.GetCommands(currentAgent))
            {
                if (c != null) commands.Add(c);
            }
        }

        // fallback to defaultCommands if none provided
        if (commands.Count == 0 && defaultCommands != null)
        {
            foreach (var c in defaultCommands)
            {
                commands.Add(c);
            }
        }

        // Update button states and text ✅
        foreach (Transform t in buttonContainer)
        {
            var btn = t.GetComponentInChildren<Button>();
            if (btn == null) continue;

            // Find corresponding command
            string btnName = t.name;
            if (btnName.StartsWith("cmd_"))
            {
                string cmdId = btnName.Substring(4);
                foreach (var cmd in commands)
                {
                    if (cmd.Id == cmdId)
                    {
                        // ✅ 버튼 활성화 상태 업데이트
                        btn.interactable = cmd.IsAvailable(currentAgent);
                        
                        // ✅ 버튼 텍스트 업데이트 (비용 변경 반영)
                        var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                        var txt = t.GetComponentInChildren<UnityEngine.UI.Text>();
                        
                        string buttonText = cmd.DisplayName;
                        
                        // ✅ SOUpgradeCommand인 경우 현재 비용과 레벨 표시
                        if (cmd is SOUpgradeCommand upgradeCmd)
                        {
                            int currentCost = upgradeCmd.GetCurrentCost();
                            int currentLevel = upgradeCmd.GetCurrentLevel();
                            int maxLv = upgradeCmd.maxLevel;
                            
                            string levelText = maxLv > 0 
                                ? $" (Lv.<color=#00FF00>{currentLevel}</color>/{maxLv})" 
                                : $" (Lv.<color=#00FF00>{currentLevel}</color>)";

                            if (currentCost > 0)
                            {
                                buttonText += $"\n꿀: <color=#00FF00>{currentCost}</color>{levelText}";
                            }
                            else
                            {
                                buttonText += $"\n{levelText}";
                            }
                        }
                        else if (!string.IsNullOrEmpty(cmd.CostText))
                        {
                            // 일반 명령은 기본 CostText 사용
                            buttonText += $"\n{cmd.CostText}";
                        }
                        
                        if (tmp != null)
                            tmp.text = buttonText;
                        else if (txt != null)
                            txt.text = buttonText;
                        
                        break;
                    }
                }
            }
        }
    }

    // 외부에서 유닛 정보 강제 업데이트 (체력 변경 시 등)
    public void ForceUpdateUnitInfo()
    {
        UpdateUnitInfo();
    }

    void Update()
    {
        // 매 프레임마다 체력 업데이트 (실시간 반영)
        if (currentAgent != null && panelRoot != null && panelRoot.activeSelf)
        {
            UpdateUnitInfo();
        }
    }
}
