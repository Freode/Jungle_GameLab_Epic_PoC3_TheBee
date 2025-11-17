using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 업그레이드 결과를 화면에 표시하는 UI
/// </summary>
public class UpgradeResultUI : MonoBehaviour
{
    public static UpgradeResultUI Instance { get; private set; }

    [Header("UI 요소")]
    public GameObject resultPanel;
    public TextMeshProUGUI upgradeNameText;
    public TextMeshProUGUI upgradeEffectText;
    public TextMeshProUGUI currentValueText;

    [Header("애니메이션 설정")]
    public float displayDuration = 3f; // 표시 시간
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Coroutine displayRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // CanvasGroup 초기화
        if (resultPanel != null)
        {
            canvasGroup = resultPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = resultPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    void Start()
    {
        Hide();
    }

    /// <summary>
    /// 업그레이드 결과 표시
    /// </summary>
    public void ShowUpgradeResult(string upgradeName, string effect, string currentValue)
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(DisplayUpgradeRoutine(upgradeName, effect, currentValue));
    }

    /// <summary>
    /// 업그레이드 표시 루틴
    /// </summary>
    IEnumerator DisplayUpgradeRoutine(string upgradeName, string effect, string currentValue)
    {
        // 텍스트 설정
        if (upgradeNameText != null)
            upgradeNameText.text = $"<color=#FFD700>?</color> {upgradeName}";

        if (upgradeEffectText != null)
            upgradeEffectText.text = effect;

        if (currentValueText != null)
            currentValueText.text = $"현재: {currentValue}";

        // 패널 활성화
        if (resultPanel != null)
            resultPanel.SetActive(true);

        // Fade In
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));

        // 표시 유지
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));

        // 패널 비활성화
        Hide();
    }

    /// <summary>
    /// CanvasGroup Fade 애니메이션
    /// </summary>
    IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 빠른 표시 (디버그/테스트용)
    /// </summary>
    public void ShowQuick(string message)
    {
        ShowUpgradeResult("알림", message, "");
    }
}
