using UnityEngine;
using System.Text;
using TMPro;

/// <summary>
/// 테크트리 툴팁 UI
/// - 테크트리 상세 정보 표시
/// - 호버 시 나타남
/// </summary>
public class TechTooltip : MonoBehaviour
{
    public static TechTooltip Instance { get; private set; }
    
    [Header("UI 컴포넌트")]
    [Tooltip("툴팁 패널")]
    public GameObject tooltipPanel;
    
    [Tooltip("테크트리 이름 텍스트")]
    public TextMeshProUGUI titleText;
    
    [Tooltip("테크트리 설명 텍스트")]
    public TextMeshProUGUI descriptionText;
    
    [Tooltip("효과 목록 텍스트")]
    public TextMeshProUGUI effectsText;
    
    [Header("위치 설정")]
    [Tooltip("툴팁 오프셋")]
    public Vector2 tooltipOffset = new Vector2(100f, 0f);
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 초기 비활성화
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 툴팁 표시
    /// </summary>
    public void ShowTooltip(TechData tech, Vector3 buttonPosition)
    {
        if (tech == null || tooltipPanel == null) return;
        
        // 툴팁 활성화
        tooltipPanel.SetActive(true);
        
        // 제목 설정
        if (titleText != null)
        {
            titleText.text = tech.techName;
        }
        
        // 설명 설정
        if (descriptionText != null)
        {
            descriptionText.text = tech.description;
        }
        
        // 효과 목록 설정
        if (effectsText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>효과:</b>");
            
            if (tech.effects != null && tech.effects.Count > 0)
            {
                foreach (var effect in tech.effects)
                {
                    sb.AppendLine($"? {effect.effectName}: {effect.currentValue} → <color=green>{effect.upgradeValue}</color>");
                }
            }
            else
            {
                sb.AppendLine("효과 정보 없음");
            }
            
            effectsText.text = sb.ToString();
        }
        
        // 툴팁 위치 설정
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, buttonPosition);
        tooltipPanel.transform.position = screenPos + tooltipOffset;
    }
    
    /// <summary>
    /// 툴팁 숨김
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
