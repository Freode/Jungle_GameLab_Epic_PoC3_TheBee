using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 하이브 업그레이드/명령 패널 (Tab키로 토글)
/// - 항상 활성화 상태 (유닛 선택과 무관)
/// - Tab키로 화면 왼쪽 밖에서 안쪽으로 슬라이드
/// - 하이브 관련 명령 실행 (업그레이드, 이사 등)
/// </summary>
public class HiveUpgradePanel : MonoBehaviour
{
    public static HiveUpgradePanel Instance { get; private set; }

    [Header("Panel Settings")]
    public RectTransform panelRect; // 패널 RectTransform
    public float slideSpeed = 5f; // 슬라이드 속도
    public float screenPadding = 10f; // 화면 안쪽 패딩
    public Button toggleButton; // ? 토글 버튼 (Inspector에서 할당)

    [Header("Commands")]
    public RectTransform buttonContainer; // 버튼들이 들어갈 컨테이너
    public GameObject commandButtonPrefab; // 버튼 프리팹
    public SOCommand[] hiveCommands; // 하이브 명령들 (Inspector에서 할당)

    private bool isOpen = false; // 패널 열림 상태
    private float hiddenXPosition; // 화면 밖 X 위치
    private float visibleXPosition; // 화면 안 X 위치
    private float targetXPosition; // 목표 X 위치

    // time control
    private float previousTimeScale = 1f; // 패널 열기 전 저장된 타임스케일

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
        // ? 초기 위치 계산
        CalculatePositions();

        // ? 처음엔 화면 밖에 숨김
        isOpen = false;
        SetPanelPosition(hiddenXPosition);
        targetXPosition = hiddenXPosition;

        // ? 명령 버튼 생성
        RebuildCommands();

        // ? 자원 변경 이벤트 구독
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += RefreshButtonStates;
        }
        
        // ? 토글 버튼 리스너 추가 (요구사항 3)
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(TogglePanel);
            Debug.Log("[업그레이드 패널] 토글 버튼 리스너 추가");
        }
        else
        {
            Debug.LogWarning("[업그레이드 패널] toggleButton이 null입니다!");
        }

        // 초기 타이머 텍스트 설정
        // Timer is handled by separate GameTimer component

        Debug.Log($"[업그레이드 패널] 초기화 완료 - 숨김 위치: {hiddenXPosition}, 보임 위치: {visibleXPosition}");
    }

    void OnDestroy()
    {
        // 자원 변경 이벤트 구독 해제
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged -= RefreshButtonStates;
        }

        // restore timescale if this panel was destroyed while paused
        if (Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        }
    }

    void Update()
    {
        // ? Tab키로 토글
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }

        // ? 부드러운 슬라이드 애니메이션
        float currentX = panelRect.anchoredPosition.x;
        if (Mathf.Abs(currentX - targetXPosition) > 0.1f)
        {
            float newX = Mathf.Lerp(currentX, targetXPosition, Time.unscaledDeltaTime * slideSpeed);
            SetPanelPosition(newX);
        }
        else
        {
            SetPanelPosition(targetXPosition);
        }

        // Timer updating moved to GameTimer component
    }

    /// <summary>
    /// 패널 위치 계산 (화면 밖/안)
    /// </summary>
    void CalculatePositions()
    {
        if (panelRect == null)
        {
            Debug.LogError("[업그레이드 패널] panelRect가 null입니다!");
            return;
        }

        // 패널 너비
        float panelWidth = panelRect.rect.width;

        // 화면 밖 위치 (왼쪽)
        hiddenXPosition = -panelWidth;

        // 화면 안 위치 (왼쪽 경계가 화면 안쪽 + 패딩)
        visibleXPosition = screenPadding;

        Debug.Log($"[업그레이드 패널] 위치 계산 - 패널 너비: {panelWidth}, 숨김: {hiddenXPosition}, 보임: {visibleXPosition}");
    }

    /// <summary>
    /// 패널 위치 설정
    /// </summary>
    void SetPanelPosition(float xPosition)
    {
        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(xPosition, panelRect.anchoredPosition.y);
        }
    }

    /// <summary>
    /// 패널 토글 (열기/닫기)
    /// </summary>
    public void TogglePanel()
    {
        isOpen = !isOpen;
        targetXPosition = isOpen ? visibleXPosition : hiddenXPosition;
        Debug.Log($"[업그레이드 패널] {(isOpen ? "열림" : "닫힘")}");

        ApplyPauseState();
    }

    /// <summary>
    /// 패널 열기
    /// </summary>
    public void OpenPanel()
    {
        isOpen = true;
        targetXPosition = visibleXPosition;
        Debug.Log("[업그레이드 패널] 열림");
        ApplyPauseState();
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void ClosePanel()
    {
        isOpen = false;
        targetXPosition = hiddenXPosition;
        Debug.Log("[업그레이드 패널] 닫힘");
        ApplyPauseState();
    }

    /// <summary>
    /// 패널 열림 상태에 따라 게임 일시정지/재개 적용
    /// </summary>
    void ApplyPauseState()
    {
        if (isOpen)
        {
            // save previous timescale and pause
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            // ensure UI animations use unscaled time (we used unscaledDeltaTime for sliding)
            Debug.Log("[업그레이드 패널] 게임 일시정지");
        }
        else
        {
            // restore previous timescale
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            Debug.Log("[업그레이드 패널] 게임 재개");
        }
    }

    /// <summary>
    /// 명령 버튼 생성
    /// </summary>
    void RebuildCommands()
    {
        // 기존 버튼 제거
        foreach (Transform t in buttonContainer)
        {
            Destroy(t.gameObject);
        }

        if (hiveCommands == null || hiveCommands.Length == 0)
        {
            Debug.LogWarning("[업그레이드 패널] hiveCommands가 비어있습니다!");
            return;
        }

        // 하이브 찾기 (명령 실행 시 필요)
        Hive hive = FindPlayerHive();
        UnitAgent hiveAgent = hive != null ? hive.GetComponent<UnitAgent>() : null;

        // 명령 버튼 생성
        foreach (var cmd in hiveCommands)
        {
            if (cmd == null) continue;

            var command = cmd; // 클로저 캡처 방지
            var btnObj = Instantiate(commandButtonPrefab, buttonContainer);
            btnObj.name = "cmd_" + command.Id;

            var btn = btnObj.GetComponentInChildren<Button>();
            var img = btnObj.GetComponentInChildren<Image>();
            var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            var txt = btnObj.GetComponentInChildren<Text>();

            // 아이콘 설정
            if (img != null) img.sprite = command.Icon;

            // 텍스트 설정
            string buttonText = command.DisplayName;

            // SOUpgradeCommand인 경우 레벨/비용 표시
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
                    buttonText += $"\n꿀: <color=#FFFF00>{currentCost}</color>{levelText}";
                }
                else
                {
                    buttonText += $"\n최대 레벨{levelText}";
                }
            }
            else if (!string.IsNullOrEmpty(cmd.CostText))
            {
                buttonText += $"\n{cmd.CostText}";
            }

            if (tmp != null)
                tmp.text = buttonText;
            else if (txt != null)
                txt.text = buttonText;

            // 버튼 활성화 상태
            bool avail = hiveAgent != null && command.IsAvailable(hiveAgent);
            if (btn != null)
            {
                btn.interactable = avail;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => HandleCommandButtonClick(command));
            }
        }

        Debug.Log($"[업그레이드 패널] 명령 버튼 {hiveCommands.Length}개 생성 완료");
    }

    /// <summary>
    /// 명령 버튼 클릭 처리
    /// </summary>
    void HandleCommandButtonClick(ICommand cmd)
    {
        // 하이브 찾기
        Hive hive = FindPlayerHive();
        if (hive == null)
        {
            Debug.LogWarning("[업그레이드 패널] 플레이어 하이브를 찾을 수 없습니다!");
            return;
        }

        UnitAgent hiveAgent = hive.GetComponent<UnitAgent>();
        if (hiveAgent == null)
        {
            Debug.LogWarning("[업그레이드 패널] 하이브 UnitAgent를 찾을 수 없습니다!");
            return;
        }

        // 명령 실행 가능 여부 확인
        if (!cmd.IsAvailable(hiveAgent))
        {
            Debug.Log($"[업그레이드 패널] 명령 실행 불가: {cmd.DisplayName}");
            return;
        }

        Debug.Log($"[업그레이드 패널] 명령 실행: {cmd.DisplayName}");

        // 명령 실행
        if (cmd.RequiresTarget)
        {
            // 타겟이 필요한 명령 (예: 이사)
            PendingCommandHolder.EnsureInstance();
            if (PendingCommandHolder.Instance != null)
            {
                TileClickMover.Instance?.EnterMoveMode();
                PendingCommandHolder.Instance.SetPendingCommand(cmd, hiveAgent);
            }
        }
        else
        {
            // 즉시 실행 명령 (예: 업그레이드)
            cmd.Execute(hiveAgent, CommandTarget.ForTile(hiveAgent.q, hiveAgent.r));
        }

        // 버튼 상태 새로고침
        RefreshButtonStates();
    }

    /// <summary>
    /// 버튼 상태 새로고침
    /// </summary>
    void RefreshButtonStates()
    {
        // 하이브 찾기
        Hive hive = FindPlayerHive();
        UnitAgent hiveAgent = hive != null ? hive.GetComponent<UnitAgent>() : null;

        if (hiveAgent == null)
        {
            Debug.LogWarning("[업그레이드 패널] 하이브를 찾을 수 없어 버튼 상태를 새로고침할 수 없습니다.");
            
            // 하이브가 없어도 버튼 비활성화는 해야 함
            foreach (Transform t in buttonContainer)
            {
                var btn = t.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.interactable = false;
                }
            }
            return;
        }

        // 모든 버튼 상태 업데이트
        foreach (Transform t in buttonContainer)
        {
            var btn = t.GetComponentInChildren<Button>();
            if (btn == null) continue;

            // 명령 찾기
            string btnName = t.name;
            if (btnName.StartsWith("cmd_"))
            {
                string cmdId = btnName.Substring(4);
                foreach (var cmd in hiveCommands)
                {
                    if (cmd != null && cmd.Id == cmdId)
                    {
                        // 활성화 상태 업데이트
                        btn.interactable = cmd.IsAvailable(hiveAgent);

                        // 텍스트 업데이트
                        var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                        var txt = t.GetComponentInChildren<Text>();

                        string buttonText = cmd.DisplayName;

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
                                buttonText += $"\n꿀: <color=#FFFF00>{currentCost}</color>{levelText}";
                            }
                            else
                            {
                                buttonText += $"\n최대 레벨{levelText}";
                            }
                        }
                        else if (!string.IsNullOrEmpty(cmd.CostText))
                        {
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
        
        Debug.Log("[업그레이드 패널] 버튼 상태 새로고침 완료");
    }

    /// <summary>
    /// 플레이어 하이브 찾기
    /// </summary>
    Hive FindPlayerHive()
    {
        if (HiveManager.Instance == null) return null;

        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive != null)
            {
                var hiveAgent = hive.GetComponent<UnitAgent>();
                if (hiveAgent != null && hiveAgent.faction == Faction.Player)
                {
                    return hive;
                }
            }
        }

        return null;
    }
}
