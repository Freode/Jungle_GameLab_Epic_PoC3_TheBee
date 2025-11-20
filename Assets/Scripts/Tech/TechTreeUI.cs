using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테크 트리 UI 패널
/// - 여러 카테고리(탭) 관리
/// - ScrollView 전환
/// - Line Renderer로 테크트리 연결선 그리기
/// - 명령 UI와 호환 가능
/// </summary>
public class TechTreeUI : MonoBehaviour
{
    public static TechTreeUI Instance { get; private set; }
    
    [Header("UI 패널")]
    [Tooltip("테크트리 전체 패널")]
    public GameObject techTreePanel;
    
    [Tooltip("닫기 버튼")]
    public Button closeButton;
    
    [Header("탭 버튼")]
    [Tooltip("일꾼 탭 버튼")]
    public Button economyTabButton;
    
    [Tooltip("여왕벌 탭 버튼")]
    public Button militaryTabButton;
    
    [Tooltip("벌집 탭 버튼")]
    public Button mobilityTabButton;
    
    [Header("ScrollView")]
    [Tooltip("일꾼 ScrollView")]
    public GameObject economyScrollView;
    
    [Tooltip("여왕벌 ScrollView")]
    public GameObject militaryScrollView;
    
    [Tooltip("벌집 ScrollView")]
    public GameObject mobilityScrollView;
    
    [Header("버튼 프리팹")]
    [Tooltip("테크트리 버튼 프리팹")]
    public GameObject techButtonPrefab;
    
    [Header("Line Renderer")]
    [Tooltip("Line Renderer 프리팹")]
    public GameObject lineRendererPrefab;
    
    [Tooltip("선 색상")]
    public Color lineColor = Color.white;
    
    [Tooltip("선 두께")]
    public float lineWidth = 2f;
    
    [Tooltip("곡선 세그먼트 (부드러움 정도)")]
    [Range(4, 32)]
    public int curveSegments = 16;
    
    [Tooltip("꺾이는 지점까지의 거리 비율 (버튼 크기 대비)")]
    [Range(0.5f, 2f)]
    public float bendDistanceRatio = 1.2f;

    public float bendRadius = 10f;
    
    // 현재 활성화된 카테고리
    private TechCategory currentCategory = TechCategory.Worker;
    
    // 생성된 버튼 목록 (카테고리별)
    private Dictionary<TechCategory, List<TechButton>> categoryButtons = new Dictionary<TechCategory, List<TechButton>>();
    
    // 생성된 Line Renderer 목록
    private List<GameObject> lineRenderers = new List<GameObject>();
    
    // 패널이 열려있는지 여부
    public bool IsOpen => techTreePanel != null && techTreePanel.activeSelf;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 초기 비활성화
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
        }
    }
    
    void Start()
    {
        // 버튼 이벤트 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTechTreePanel);
        }
        
        if (economyTabButton != null)
        {
            economyTabButton.onClick.AddListener(() => SwitchTab(TechCategory.Worker));
        }
        
        if (militaryTabButton != null)
        {
            militaryTabButton.onClick.AddListener(() => SwitchTab(TechCategory.Queen));
        }
        
        if (mobilityTabButton != null)
        {
            mobilityTabButton.onClick.AddListener(() => SwitchTab(TechCategory.Hive));
        }
        
        // 테크트리 버튼 생성
        InitializeTechButtons();
    }
    
    void Update()
    {
        // ESC 키로 패널 닫기
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTechTreePanel();
        }
    }
    
    /// <summary>
    /// 테크트리 패널 열기
    /// </summary>
    public void OpenTechTreePanel()
    {
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(true);
            
            // 첫 번째 탭 활성화
            SwitchTab(TechCategory.Worker);
            
            Debug.Log("[TechTreeUI] 테크 트리 패널 열림");
        }
    }
    
    /// <summary>
    /// 테크트리 패널 닫기
    /// </summary>
    public void CloseTechTreePanel()
    {
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
            Debug.Log("[TechTreeUI] 테크 트리 패널 닫힘");
        }
    }
    
    /// <summary>
    /// 테크트리 패널 토글
    /// </summary>
    public void ToggleTechTreePanel()
    {
        if (IsOpen)
        {
            CloseTechTreePanel();
        }
        else
        {
            OpenTechTreePanel();
        }
    }
    
    /// <summary>
    /// 탭 전환
    /// </summary>
    void SwitchTab(TechCategory category)
    {
        currentCategory = category;
        
        // 모든 ScrollView 비활성화
        if (economyScrollView != null) economyScrollView.SetActive(false);
        if (militaryScrollView != null) militaryScrollView.SetActive(false);
        if (mobilityScrollView != null) mobilityScrollView.SetActive(false);
        
        if(economyTabButton != null) economyTabButton.interactable = true;
        if(militaryTabButton != null) militaryTabButton.interactable = true;
        if(mobilityTabButton != null) mobilityTabButton.interactable = true;

        // 선택된 ScrollView 활성화 및 위치 초기화
        GameObject activeScrollView = null;
        Button uninteractButton = null;
        
        switch (category)
        {
            case TechCategory.Worker:
                activeScrollView = economyScrollView;
                uninteractButton = economyTabButton;
                break;
            case TechCategory.Queen:
                activeScrollView = militaryScrollView;
                uninteractButton = militaryTabButton;
                break;
            case TechCategory.Hive:
                activeScrollView = mobilityScrollView;
                uninteractButton = mobilityTabButton;
                break;
        }
        
        if (activeScrollView != null)
        {
            activeScrollView.SetActive(true);
            uninteractButton.interactable = false;

            // ScrollView를 제일 왼쪽으로 초기화
            ScrollRect scrollRect = activeScrollView.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                // normalizedPosition: (0, 0.5) = 왼쪽 중앙
                // X축: 0 = 제일 왼쪽, 1 = 제일 오른쪽
                // Y축: 0 = 제일 아래, 1 = 제일 위, 0.5 = 중앙
                scrollRect.normalizedPosition = new Vector2(0f, 0.5f);
                
                Debug.Log($"[TechTreeUI] {category} ScrollView를 제일 왼쪽으로 초기화");
            }
        }
    }
    
    /// <summary>
    /// 테크트리 버튼 초기화
    /// </summary>
    void InitializeTechButtons()
    {
        if (TechManager.Instance == null)
        {
            Debug.LogError("[TechTreeUI] TechManager.Instance가 null입니다!");
            return;
        }
        
        if (techButtonPrefab == null)
        {
            Debug.LogError("[TechTreeUI] techButtonPrefab이 null입니다!");
            return;
        }
        
        // 카테고리별 Content 컨테이너
        Dictionary<TechCategory, Transform> categoryContents = new Dictionary<TechCategory, Transform>();
        Dictionary<TechCategory, ScrollRect> categoryScrollRects = new Dictionary<TechCategory, ScrollRect>();
        
        if (economyScrollView != null)
        {
            var content = economyScrollView.transform.Find("Viewport/Content");
            if (content != null)
            {
                categoryContents[TechCategory.Worker] = content;
                var scrollRect = economyScrollView.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    categoryScrollRects[TechCategory.Worker] = scrollRect;
                    // Elastic 효과 비활성화
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }
                Debug.Log("[TechTreeUI] Worker Content 찾음");
            }
            else
            {
                Debug.LogWarning("[TechTreeUI] Worker ScrollView의 Viewport/Content를 찾을 수 없습니다!");
            }
        }
        
        if (militaryScrollView != null)
        {
            var content = militaryScrollView.transform.Find("Viewport/Content");
            if (content != null)
            {
                categoryContents[TechCategory.Queen] = content;
                var scrollRect = militaryScrollView.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    categoryScrollRects[TechCategory.Queen] = scrollRect;
                    // Elastic 효과 비활성화
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }
                Debug.Log("[TechTreeUI] Queen Content 찾음");
            }
            else
            {
                Debug.LogWarning("[TechTreeUI] Queen ScrollView의 Viewport/Content를 찾을 수 없습니다!");
            }
        }
        
        if (mobilityScrollView != null)
        {
            var content = mobilityScrollView.transform.Find("Viewport/Content");
            if (content != null)
            {
                categoryContents[TechCategory.Hive] = content;
                var scrollRect = mobilityScrollView.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    categoryScrollRects[TechCategory.Hive] = scrollRect;
                    // Elastic 효과 비활성화
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }
                Debug.Log("[TechTreeUI] Hive Content 찾음");
            }
            else
            {
                Debug.LogWarning("[TechTreeUI] Hive ScrollView의 Viewport/Content를 찾을 수 없습니다!");
            }
        }
        
        Debug.Log($"[TechTreeUI] 총 {TechManager.Instance.allTechs.Count}개의 테크 데이터 처리 시작");
        
        // 카테고리별 최대 영역 추적
        Dictionary<TechCategory, Rect> categoryBounds = new Dictionary<TechCategory, Rect>();
        
        // 각 테크트리 데이터로 버튼 생성
        int createdCount = 0;
        foreach (var tech in TechManager.Instance.allTechs)
        {
            if (tech == null)
            {
                Debug.LogWarning("[TechTreeUI] null 테크 데이터 발견!");
                continue;
            }
            
            // 해당 카테고리의 Content 가져오기
            if (!categoryContents.TryGetValue(tech.category, out Transform content))
            {
                Debug.LogWarning($"[TechTreeUI] {tech.category} 카테고리의 Content를 찾을 수 없습니다.");
                continue;
            }
            
            // 버튼 생성
            GameObject buttonObj = Instantiate(techButtonPrefab, content);
            buttonObj.name = $"TechButton_{tech.techType}";
            
            TechButton techButton = buttonObj.GetComponent<TechButton>();
            
            if (techButton != null)
            {
                // 1. techData 할당
                techButton.techData = tech;
                
                // 2. RectTransform 위치 설정
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = tech.uiPosition;
                    
                    // 카테고리별 최대 영역 계산
                    if (!categoryBounds.ContainsKey(tech.category))
                    {
                        categoryBounds[tech.category] = new Rect(tech.uiPosition, Vector2.zero);
                    }
                    
                    Rect currentBounds = categoryBounds[tech.category];
                    
                    // 버튼 크기 고려 (기본 150x150)
                    float buttonWidth = rectTransform.rect.width;
                    float buttonHeight = rectTransform.rect.height;
                    
                    // 최소값 업데이트
                    currentBounds.xMin = Mathf.Min(currentBounds.xMin, tech.uiPosition.x - buttonWidth / 2f);
                    currentBounds.yMin = Mathf.Min(currentBounds.yMin, tech.uiPosition.y - buttonHeight / 2f);
                    
                    // 최대값 업데이트
                    currentBounds.xMax = Mathf.Max(currentBounds.xMax, tech.uiPosition.x + buttonWidth / 2f);
                    currentBounds.yMax = Mathf.Max(currentBounds.yMax, tech.uiPosition.y + buttonHeight / 2f);
                    
                    categoryBounds[tech.category] = currentBounds;
                }
                
                // 3. 버튼 초기화 (여기서 명시적으로 호출!)
                techButton.Initialize();
                
                // 4. 카테고리별 버튼 목록에 추가
                if (!categoryButtons.ContainsKey(tech.category))
                {
                    categoryButtons[tech.category] = new List<TechButton>();
                }
                categoryButtons[tech.category].Add(techButton);
                
                createdCount++;
            }
            else
            {
                Debug.LogError($"[TechTreeUI] {tech.techName} 버튼에서 TechButton 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        Debug.Log($"[TechTreeUI] {createdCount}개의 테크 버튼 생성 완료");
        
        // 카테고리별 Content 크기 조정
        foreach (var kvp in categoryBounds)
        {
            TechCategory category = kvp.Key;
            Rect bounds = kvp.Value;
            
            if (categoryContents.TryGetValue(category, out Transform content))
            {
                RectTransform contentRect = content.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    // 여백 추가
                    float padding = 0f;
                    
                    // 버튼들의 실제 경계 계산
                    // bounds는 이미 버튼 크기를 포함한 최소/최대 좌표를 담고 있음
                    float leftMost = bounds.xMin;      // 가장 왼쪽
                    float rightMost = bounds.xMax;     // 가장 오른쪽
                    float bottomMost = bounds.yMin;    // 가장 아래
                    float topMost = bounds.yMax;       // 가장 위
                    
                    // Content 크기 계산: 실제 범위 + 양쪽 여백
                    float contentWidth = ((rightMost - leftMost) + (padding * 2)) / 2f;
                    float contentHeight = ((topMost - bottomMost) + (padding * 2));
                    
                    // Content 크기 설정 (앵커/피벗/위치는 Inspector에서 수동 설정)
                    contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
                    
                    Debug.Log($"[TechTreeUI] {category} Content 설정:");
                    Debug.Log($"  - 경계: Left={leftMost:F1}, Right={rightMost:F1}, Bottom={bottomMost:F1}, Top={topMost:F1}");
                    Debug.Log($"  - 범위: Width={rightMost - leftMost:F1}, Height={topMost - bottomMost:F1}");
                    Debug.Log($"  - Content 크기: {contentWidth:F1} x {contentHeight:F1}");
                }
            }
        }
        
        // 연결선 생성
        CreateTechLines();
        
        // 선행 기술이 없는 테크 버튼들의 초기 상태 업데이트
        UpdateInitialTechButtons();
    }
    
    /// <summary>
    /// 선행 기술이 없는 테크 버튼들의 초기 상태 업데이트
    /// </summary>
    void UpdateInitialTechButtons()
    {
        if (TechManager.Instance == null) return;
        
        int addedCount = 0;
        
        // 모든 버튼을 순회하며 선행 테크가 없는 버튼들을 업데이트 목록에 추가
        foreach (var kvp in categoryButtons)
        {
            foreach (var button in kvp.Value)
            {
                if (button == null || button.techData == null) continue;
                
                // 선행 기술이 없는 테크인지 확인
                if (button.techData.prerequisites == null || button.techData.prerequisites.Count == 0)
                {
                    // TechManager의 업데이트 목록에 추가 (Initialize에서 이미 추가되었지만 확인)
                    TechManager.Instance.AddUpdateableButton(button);
                    addedCount++;
                    
                    Debug.Log($"[TechTreeUI] 초기 테크 업데이트 목록 추가: {button.techData.techName}");
                }
            }
        }
        
        Debug.Log($"[TechTreeUI] {addedCount}개의 초기 테크 버튼을 업데이트 목록에 추가 완료");
        
        // 모든 초기화가 끝난 후, TechManager의 UpdateAllButtons를 호출하여
        // 업데이트 목록에 있는 모든 버튼의 상태를 한 번에 갱신
        TechManager.Instance.UpdateAllButtons();
        
        Debug.Log($"[TechTreeUI] 모든 업데이트 대상 버튼의 초기 상태 갱신 완료");
    }
    
    /// <summary>
    /// 테크트리 연결선 생성
    /// </summary>
    void CreateTechLines()
    {
        if (TechManager.Instance == null || lineRendererPrefab == null) return;
        
        // 기존 Line Renderer 제거
        foreach (var line in lineRenderers)
        {
            if (line != null) Destroy(line);
        }
        lineRenderers.Clear();
        
        // 각 카테고리별로 연결선 생성
        foreach (var kvp in categoryButtons)
        {
            TechCategory category = kvp.Key;
            List<TechButton> buttons = kvp.Value;
            
            // 카테고리의 ScrollView Content 가져오기
            Transform content = GetContentTransform(category);
            if (content == null) continue;
            
            // 각 버튼에 대해 이후 테크트리로 연결선 그리기
            foreach (var button in buttons)
            {
                if (button == null || button.techData == null) continue;
                
                TechData currentTech = button.techData;
                
                // 이후 테크트리가 없으면 패스
                if (currentTech.nextTechs == null || currentTech.nextTechs.Count == 0) continue;
                
                // 각 이후 테크트리로 연결선 생성
                foreach (var nextTech in currentTech.nextTechs)
                {
                    if (nextTech == null) continue;
                    
                    // 이후 테크트리 버튼 찾기
                    TechButton nextButton = buttons.Find(b => b.techData == nextTech);
                    if (nextButton == null) continue;
                    
                    // Line Renderer 생성
                    CreateLine(button, nextButton, content);
                }
            }
        }
    }
    
    /// <summary>
    /// 카테고리의 Content Transform 가져오기
    /// </summary>
    Transform GetContentTransform(TechCategory category)
    {
        GameObject scrollView = null;
        
        switch (category)
        {
            case TechCategory.Worker:
                scrollView = economyScrollView;
                break;
            case TechCategory.Queen:
                scrollView = militaryScrollView;
                break;
            case TechCategory.Hive:
                scrollView = mobilityScrollView;
                break;
        }
        
        if (scrollView != null)
        {
            return scrollView.transform.Find("Viewport/Content");
        }
        
        return null;
    }
    
    /// <summary>
    /// 두 버튼 사이에 연결선 생성
    /// </summary>
    void CreateLine(TechButton startButton, TechButton endButton, Transform parent)
    {
        if (startButton == null || endButton == null || parent == null) return;
        
        // UILineRenderer 오브젝트 생성 (Content의 자식으로)
        GameObject lineObj = new GameObject($"Line_{startButton.techData.techType}_to_{endButton.techData.techType}");
        lineObj.transform.SetParent(parent, false);
        
        // UILineRenderer 컴포넌트 추가
        UILineRenderer uiLine = lineObj.AddComponent<UILineRenderer>();
        
        // RectTransform 설정
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        // Content와 동일한 앵커/피벗 설정 (부모 좌표계 사용)
        RectTransform parentRect = parent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            lineRect.anchorMin = parentRect.anchorMin;
            lineRect.anchorMax = parentRect.anchorMax;
            lineRect.pivot = parentRect.pivot;
        }
        else
        {
            // 기본값: Content와 동일하게 좌측 중앙
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(0f, 0.5f);
            lineRect.pivot = new Vector2(0f, 0.5f);
        }
        
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(5000, 5000); // 충분히 큰 크기
        
        // 선 설정
        uiLine.lineWidth = lineWidth;
        uiLine.lineColor = lineColor;
        uiLine.raycastTarget = false; // 마우스 이벤트 무시
        
        // MaskableGraphic을 상속했으므로 자동으로 Viewport의 Mask에 의해 잘림
        
        // 위치 계산
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        RectTransform endRect = endButton.GetComponent<RectTransform>();
        
        if (startRect == null || endRect == null) return;
        
        // 버튼의 anchoredPosition (버튼 피벗 기준 위치)
        Vector2 startPos = startRect.anchoredPosition;
        Vector2 endPos = endRect.anchoredPosition;
        
        // 버튼의 피벗 확인 (보통 중앙 = 0.5, 0.5)
        Vector2 startPivot = startRect.pivot;
        Vector2 endPivot = endRect.pivot;
        
        // 버튼의 실제 크기
        Vector2 startSize = startRect.rect.size;
        Vector2 endSize = endRect.rect.size;
        
        // 버튼의 실제 중심점 계산
        // anchoredPosition은 피벗 기준이므로, 중심점으로 변환
        Vector2 startCenter = startPos + new Vector2(
            (0.5f - startPivot.x) * startSize.x,
            (0.5f - startPivot.y) * startSize.y
        );
        
        Vector2 endCenter = endPos + new Vector2(
            (0.5f - endPivot.x) * endSize.x,
            (0.5f - endPivot.y) * endSize.y
        );
        
        // 시작점: 버튼 중심에서 오른쪽으로
        Vector2 startPoint = startCenter + new Vector2(startSize.x / 2f, 0f);
        
        // 끝점: 버튼 중심에서 왼쪽으로
        Vector2 endPoint = endCenter - new Vector2(endSize.x / 2f, 0f);
        
        // 직선 경로 생성 (곡선 제거)
        List<Vector2> points2D = new List<Vector2>();
        points2D.Add(startPoint);
        points2D.Add(endPoint);
        
        // UILineRenderer에 점들 설정
        uiLine.SetPoints(points2D);
        
        // 생성된 Line 추가
        lineRenderers.Add(lineObj);
        
        // Z 순서 조정 (버튼 뒤에 표시)
        lineObj.transform.SetAsFirstSibling();
        
        Debug.Log($"[TechTreeUI] UI Line 생성: {startButton.techData.techName} → {endButton.techData.techName}");
        Debug.Log($"  - Start Pos: {startPos}, Start Pivot: {startPivot}, Start Size: {startSize}");
        Debug.Log($"  - Start Center: {startCenter}, Start Point: {startPoint}");
        Debug.Log($"  - End Pos: {endPos}, End Pivot: {endPivot}, End Size: {endSize}");
        Debug.Log($"  - End Center: {endCenter}, End Point: {endPoint}");
    }
}
