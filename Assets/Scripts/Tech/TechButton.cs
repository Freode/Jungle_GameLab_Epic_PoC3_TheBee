using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 테크트리 버튼 상태
/// </summary>
public enum TechButtonState
{
    Locked,         // 회색 - 선행 테크트리 미완료
    Insufficient,   // 빨간색 - 꿀 부족
    Available,      // 노란색 - 연구 가능
    Researched      // 초록색 - 연구 완료
}

/// <summary>
/// 테크트리 버튼 UI
/// - 버튼 상태에 따라 색상 변경
/// - 연구 가능 여부 확인
/// - 호버 시 툴팁 표시
/// </summary>
[RequireComponent(typeof(Button))]
public class TechButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("테크트리 데이터")]
    [Tooltip("이 버튼이 나타내는 테크트리 데이터")]
    public TechData techData;
    
    [Header("UI 컴포넌트")]
    [Tooltip("버튼 컴포넌트")]
    public Button button;
    
    [Tooltip("버튼 이미지 (색상 변경용)")]
    public Image buttonImage;
    
    [Tooltip("테크트리 이름 텍스트")]
    public TextMeshProUGUI nameText;
    
    [Tooltip("필요 꿀 양 텍스트")]
    public TextMeshProUGUI costText;
    
    [Header("버튼 색상")]
    [Tooltip("잠김 상태 색상 (회색)")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Tooltip("자원 부족 색상 (빨간색)")]
    public Color insufficientColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    [Tooltip("연구 가능 색상 (노란색)")]
    public Color availableColor = new Color(1f, 0.9f, 0.2f, 1f);
    
    [Tooltip("연구 완료 색상 (초록색)")]
    public Color researchedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    // 현재 버튼 상태
    private TechButtonState currentState = TechButtonState.Locked;
    
    // 초기화 완료 여부
    private bool isInitialized = false;
    
    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        // 버튼 클릭 이벤트 연결
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    void OnDestroy()
    {
        // TechManager에서 제거
        if (TechManager.Instance != null)
        {
            TechManager.Instance.RemoveUpdateableButton(this);
        }
    }
    
    /// <summary>
    /// 버튼 초기화 (TechTreeUI에서 호출)
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[TechButton] {techData?.techName}이(가) 이미 초기화되었습니다.");
            return;
        }
        
        if (techData == null)
        {
            Debug.LogError("[TechButton] techData가 null입니다! 초기화 실패.");
            return;
        }
        
        // 초기 상태 업데이트
        UpdateButtonState();
        
        // TechManager에 이 버튼 등록 (업데이트 대상 목록)
        if (TechManager.Instance != null && currentState != TechButtonState.Researched)
        {
            TechManager.Instance.AddUpdateableButton(this);
        }
        
        isInitialized = true;
        Debug.Log($"[TechButton] {techData.techName} 초기화 완료");
    }
    
    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    public void UpdateButtonState()
    {
        if (techData == null)
        {
            Debug.LogWarning("[TechButton] techData가 null입니다! 상태 업데이트 불가.");
            return;
        }
        
        if (TechManager.Instance == null)
        {
            Debug.LogWarning("[TechButton] TechManager.Instance가 null입니다!");
            return;
        }
        
        // 이미 연구된 경우
        if (TechManager.Instance.IsResearched(techData.techType))
        {
            SetState(TechButtonState.Researched);
            return;
        }
        
        // 선행 테크트리 확인
        if (!TechManager.Instance.CheckPrerequisites(techData))
        {
            SetState(TechButtonState.Locked);
            return;
        }
        
        // 자원 확인
        if (HiveManager.Instance != null)
        {
            if (HiveManager.Instance.HasResources(techData.honeyResearchCost))
            {
                SetState(TechButtonState.Available);
            }
            else
            {
                SetState(TechButtonState.Insufficient);
            }
        }
    }
    
    /// <summary>
    /// 버튼 상태 설정
    /// </summary>
    void SetState(TechButtonState newState)
    {
        currentState = newState;
        
        // 버튼 색상 변경
        if (buttonImage != null)
        {
            switch (currentState)
            {
                case TechButtonState.Locked:
                    buttonImage.color = lockedColor;
                    break;
                case TechButtonState.Insufficient:
                    buttonImage.color = insufficientColor;
                    break;
                case TechButtonState.Available:
                    buttonImage.color = availableColor;
                    break;
                case TechButtonState.Researched:
                    buttonImage.color = researchedColor;
                    break;
            }
        }
        
        // 버튼 활성화/비활성화
        if (button != null)
        {
            button.interactable = (currentState == TechButtonState.Available);
        }
        
        // UI 텍스트 업데이트
        UpdateUI();
    }
    
    /// <summary>
    /// UI 텍스트 업데이트
    /// </summary>
    void UpdateUI()
    {
        if (techData == null) return;
        
        // 이름 텍스트
        if (nameText != null)
        {
            nameText.text = techData.techName;
        }
        
        // 비용 텍스트
        if (costText != null)
        {
            if (currentState == TechButtonState.Researched)
            {
                costText.text = "연구 완료";
            }
            else
            {
                costText.text = $"꿀 {techData.honeyResearchCost}";
            }
        }
    }
    
    /// <summary>
    /// 버튼 클릭 이벤트
    /// </summary>
    void OnButtonClick()
    {
        if (techData == null || TechManager.Instance == null) return;
        
        // 연구 시도
        if (TechManager.Instance.TryResearchTech(techData))
        {
            // 연구 성공 - 상태 업데이트
            SetState(TechButtonState.Researched);
            
            // 업데이트 대상 목록에서 제거
            TechManager.Instance.RemoveUpdateableButton(this);
            
            // 이후 테크트리 버튼들을 업데이트 대상 목록에 추가
            AddNextTechsToUpdateList();
            
            // 툴팁 숨김
            if (TechTooltip.Instance != null)
            {
                TechTooltip.Instance.HideTooltip();
            }
        }
    }
    
    /// <summary>
    /// 이후 테크트리 버튼들을 업데이트 대상 목록에 추가
    /// </summary>
    void AddNextTechsToUpdateList()
    {
        if (techData == null || techData.nextTechs == null) return;
        
        // 모든 TechButton 찾기
        var allButtons = FindObjectsOfType<TechButton>();
        
        foreach (var nextTech in techData.nextTechs)
        {
            if (nextTech == null) continue;
            
            // 해당 테크트리의 버튼 찾기
            foreach (var btn in allButtons)
            {
                if (btn.techData == nextTech)
                {
                    // 업데이트 대상 목록에 추가
                    if (TechManager.Instance != null)
                    {
                        TechManager.Instance.AddUpdateableButton(btn);
                    }
                    
                    // 즉시 상태 업데이트
                    btn.UpdateButtonState();
                }
            }
        }
    }
    
    /// <summary>
    /// 마우스 호버 시작
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (techData != null && TechTooltip.Instance != null)
        {
            TechTooltip.Instance.ShowTooltip(techData, transform.position);
        }
    }
    
    /// <summary>
    /// 마우스 호버 종료
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TechTooltip.Instance != null)
        {
            TechTooltip.Instance.HideTooltip();
        }
    }
}
