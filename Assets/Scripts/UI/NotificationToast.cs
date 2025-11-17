using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 간단한 알림 메시지 표시
/// </summary>
public class NotificationToast : MonoBehaviour
{
    public static NotificationToast Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI messageText;

    [Header("설정")]
    public float fadeDuration = 0.3f;
    public float displayDuration = 2f;

    private Coroutine currentToast;
    private Color originalTextColor;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 원본 텍스트 색상 저장
        if (messageText != null)
        {
            originalTextColor = messageText.color;
        }

        // 초기 상태: 투명
        HideImmediate();
    }

    /// <summary>
    /// 알림 메시지 표시
    /// </summary>
    public void ShowMessage(string message, float duration = -1f)
    {
        if (currentToast != null)
        {
            StopCoroutine(currentToast);
        }

        currentToast = StartCoroutine(ShowToastRoutine(message, duration > 0 ? duration : displayDuration));
    }

    IEnumerator ShowToastRoutine(string message, float duration)
    {
        gameObject.SetActive(true);

        if (messageText != null)
        {
            messageText.text = message;
        }

        // Fade In
        if (messageText != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                messageText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
                yield return null;
            }
            messageText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 1f);
        }

        // 표시 대기
        yield return new WaitForSeconds(duration);

        // Fade Out
        if (messageText != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                messageText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
                yield return null;
            }
            messageText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        }

        // gameObject.SetActive(false);
        currentToast = null;
    }

    /// <summary>
    /// 현재 토스트 즉시 숨김
    /// </summary>
    public void Hide()
    {
        if (currentToast != null)
        {
            StopCoroutine(currentToast);
            currentToast = null;
        }

        HideImmediate();
    }

    /// <summary>
    /// 즉시 투명하게
    /// </summary>
    void HideImmediate()
    {
        if (messageText != null)
        {
            messageText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        }

        // gameObject.SetActive(false);
    }
}
