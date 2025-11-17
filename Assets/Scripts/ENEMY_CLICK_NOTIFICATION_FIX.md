# ?? 적 유닛 클릭 & 이사 알림 수정 가이드

## 완료된 개선사항

### ? 1. 적 유닛 우클릭 가능
- EnemyVisibilityController에 컬라이더 활성화/비활성화 추가
- 적이 보일 때 컬라이더도 활성화
- 적이 안 보이면 컬라이더도 비활성화

### ? 2. 이사 준비 알림 토스트
- NotificationToast 컴포넌트 생성
- 이사 시작 시 "이사 준비. 10초 후 벌집이 파괴됩니다." 표시
- 3초간 표시 후 자동 사라짐

---

## ?? 수정/생성된 파일

### 1. EnemyVisibilityController.cs
```csharp
? SetUnitVisibility() 수정
   - Collider2D 활성화/비활성화 추가
   - Collider3D 활성화/비활성화 추가
   - visible = true → 클릭 가능
   - visible = false → 클릭 불가능
```

### 2. NotificationToast.cs (신규)
```csharp
? ShowMessage(string message, float duration)
   - Fade In/Out 애니메이션
   - 지정된 시간 동안 표시
   - Singleton 패턴
```

### 3. Hive.cs
```csharp
? StartRelocation() 수정
   - NotificationToast.ShowMessage() 호출
   - "이사 준비. 10초 후 벌집이 파괴됩니다."
   - 3초간 표시
```

---

## ?? 시스템 작동 방식

### 1. 적 유닛 클릭 가능

#### 이전 문제
```
[적 유닛 발견]
EnemyVisibilityController.UpdateEnemyVisibility()
{
    SetUnitVisibility(enemy, true)
    {
        renderer.enabled = true ?
        sprite.enabled = true ?
        
        // 컬라이더 처리 없음 ?
    }
}
   ↓
[플레이어]
적 유닛 우클릭
   ↓
Physics.Raycast()
   ↓
[결과]
컬라이더 비활성화 상태 ?
hit.collider == null ?
우클릭 감지 안 됨 ?
```

---

#### 수정 후
```
[적 유닛 발견]
EnemyVisibilityController.UpdateEnemyVisibility()
{
    SetUnitVisibility(enemy, true)
    {
        renderer.enabled = true ?
        sprite.enabled = true ?
        
        // 컬라이더 활성화 ?
        collider2D.enabled = true ?
        collider3D.enabled = true ?
    }
}
   ↓
[플레이어]
적 유닛 우클릭
   ↓
Physics.Raycast()
   ↓
hit.collider != null ?
   ↓
[HandleAttackCommand]
활동 범위 체크
   ↓
behavior.IssueAttackCommand(enemy)
   ↓
[결과]
일꾼이 적 공격! ?
```

---

### 2. 이사 준비 알림 토스트

#### 실행 흐름
```
[플레이어]
하이브 선택 → 이사 명령 클릭
   ↓
[SOCommand]
StartRelocation(resourceCost)
   ↓
[Hive.StartRelocation()]
{
    // 자원 차감
    HiveManager.TrySpendResources(resourceCost)
    
    // 알림 표시 ?
    NotificationToast.ShowMessage(
        "이사 준비. 10초 후 벌집이 파괴됩니다.",
        3f
    )
    
    // 카운트다운 시작
    StartCoroutine(RelocationCountdown())
}
   ↓
[NotificationToast]
{
    gameObject.SetActive(true)
    
    // Fade In (0.3초)
    canvasGroup.alpha: 0 → 1
    
    // 표시 (3초)
    yield WaitForSeconds(3f)
    
    // Fade Out (0.3초)
    canvasGroup.alpha: 1 → 0
    
    gameObject.SetActive(false)
}
   ↓
[결과]
알림이 3초간 표시됨 ?
부드러운 페이드 효과 ?
자동으로 사라짐 ?
```

---

## ?? 핵심 코드

### 1. EnemyVisibilityController - 컬라이더 활성화

```csharp
// EnemyVisibilityController.cs
void SetUnitVisibility(UnitAgent unit, bool visible)
{
    if (unit == null) return;

    // ...existing code (렌더러 처리)...
    
    // 6. 컬라이더 활성화/비활성화 (클릭 가능/불가능) ?
    var collider2D = targetObject.GetComponent<Collider2D>();
    if (collider2D != null)
    {
        collider2D.enabled = visible; ?
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] Collider2D 설정: {collider2D.name}, enabled={visible}");
    }
    
    var collider3D = targetObject.GetComponent<Collider>();
    if (collider3D != null)
    {
        collider3D.enabled = visible; ?
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] Collider3D 설정: {collider3D.name}, enabled={visible}");
    }
}
```

---

### 2. NotificationToast - 알림 토스트

```csharp
// NotificationToast.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationToast : MonoBehaviour
{
    public static NotificationToast Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;

    [Header("설정")]
    public float fadeDuration = 0.3f;
    public float displayDuration = 2f;

    private Coroutine currentToast;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 초기 상태: 투명
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 알림 메시지 표시 ?
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
            messageText.text = message; ?
        }

        // Fade In ?
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // 표시 대기 ?
        yield return new WaitForSeconds(duration);

        // Fade Out ?
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
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

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }
}
```

---

### 3. Hive - 이사 알림

```csharp
// Hive.cs
public void StartRelocation(int resourceCost)
{
    if (isRelocating)
    {
        Debug.LogWarning("이미 이사 준비 중입니다.");
        return;
    }

    // 자원 차감
    if (HiveManager.Instance != null)
    {
        if (!HiveManager.Instance.TrySpendResources(resourceCost))
        {
            Debug.LogWarning($"[하이브 이사] 자원 차감 실패: {resourceCost}");
            return;
        }
        Debug.Log($"[하이브 이사] 자원 차감 성공: {resourceCost}");
    }

    isRelocating = true;

    // 일꾼 생성 중지
    if (spawnRoutine != null)
    {
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    // 알림 토스트 표시 ?
    if (NotificationToast.Instance != null)
    {
        NotificationToast.Instance.ShowMessage(
            "이사 준비. 10초 후 벌집이 파괴됩니다.", 
            3f
        ); ?
    }

    // 10초 카운트다운 시작
    if (relocateRoutine != null)
    {
        StopCoroutine(relocateRoutine);
    }
    relocateRoutine = StartCoroutine(RelocationCountdown());

    Debug.Log("[하이브 이사] 이사 준비 시작! 10초 후 하이브가 파괴됩니다.");
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **적 컬라이더** | 비활성화 ? | visible 따라감 ? |
| **적 우클릭** | 안 됨 ? | 작동 ? |
| **이사 알림** | 없음 ? | 토스트 표시 ? |
| **페이드 효과** | 없음 ? | 부드러움 ? |

---

## ?? 시각화

### 적 유닛 클릭 흐름

```
[적 발견]
   ↓
SetUnitVisibility(enemy, true)
   ↓
renderer.enabled = true
sprite.enabled = true
collider2D.enabled = true ?
collider3D.enabled = true ?
   ↓
[플레이어 우클릭]
   ↓
Physics.Raycast()
   ↓
hit.collider = enemy.collider ?
   ↓
HandleAttackCommand(enemy)
   ↓
공격 명령 실행! ?
```

### 이사 알림 타임라인

```
T=0.0s: 이사 명령
   ↓
T=0.0s: NotificationToast.ShowMessage()
   ↓
T=0.0~0.3s: Fade In (alpha: 0 → 1)
   ↓
T=0.3~3.3s: 메시지 표시
   "이사 준비. 10초 후 벌집이 파괴됩니다."
   ↓
T=3.3~3.6s: Fade Out (alpha: 1 → 0)
   ↓
T=3.6s: 토스트 숨김
```

---

## ?? 문제 해결

### Q: 적 우클릭해도 공격 안 해요

**확인:**
```
[ ] 적이 보임
[ ] 적 컬라이더 enabled = true
[ ] Console: "적 유닛 공격 명령"
```

**해결:**
```
1. 적이 보이는지 확인
   - EnemyVisibilityController.IsPositionVisible()

2. 컬라이더 확인
   - Inspector에서 Collider enabled = true

3. 활동 범위 확인
   - 적이 하이브 범위 내에 있는지
```

---

### Q: 알림 토스트가 안 보여요

**확인:**
```
[ ] NotificationToast GameObject 존재
[ ] Canvas 설정
[ ] messageText 연결
[ ] canvasGroup 연결
```

**해결:**
```
1. Unity 에디터에서:
   - Hierarchy → UI → Canvas
   - NotificationToast GameObject 생성
   - TextMeshProUGUI 추가
   - CanvasGroup 추가

2. NotificationToast 스크립트 연결
   - messageText 드래그앤드롭
   - canvasGroup 드래그앤드롭

3. Canvas 설정
   - Render Mode = Screen Space - Overlay
```

---

### Q: 토스트가 너무 빨리/늦게 사라져요

**해결:**
```csharp
// NotificationToast.cs - Inspector에서 설정
fadeDuration = 0.3f; // Fade 시간
displayDuration = 2f; // 표시 시간

// 또는 코드에서 직접 지정
NotificationToast.Instance.ShowMessage(message, 5f); // 5초
```

---

## ?? Unity 설정 가이드

### NotificationToast UI 생성

```
[Hierarchy]
Canvas
  └─ NotificationToast (GameObject)
      ├─ Component: CanvasGroup
      ├─ Component: NotificationToast
      └─ MessageText (TextMeshProUGUI)
          └─ Text: ""
```

### Inspector 설정

```
[NotificationToast GameObject]
RectTransform:
  - Anchor: Middle Center
  - Width: 600
  - Height: 100
  - Pos Y: 300 (화면 중상단)

CanvasGroup:
  - Alpha: 0
  - Interactable: false
  - Block Raycasts: false

NotificationToast (Script):
  - Message Text: [드래그앤드롭 MessageText]
  - Canvas Group: [드래그앤드롭 CanvasGroup]
  - Fade Duration: 0.3
  - Display Duration: 2

[MessageText]
TextMeshProUGUI:
  - Font Size: 32
  - Alignment: Center
  - Color: White
  - Outline: Black (2)
```

---

## ? 테스트 체크리스트

```
[적 유닛 클릭]
[ ] 적 발견
[ ] 일꾼 선택
[ ] 적 우클릭
[ ] 공격 명령 실행 ?
[ ] Console: "적 유닛 공격 명령" ?

[이사 알림]
[ ] 하이브 선택
[ ] 이사 명령 클릭
[ ] 알림 토스트 표시 ?
[ ] "이사 준비. 10초 후 벌집이 파괴됩니다." ?
[ ] Fade In 애니메이션 ?
[ ] 3초 표시 ?
[ ] Fade Out 애니메이션 ?
[ ] 자동 사라짐 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? 적 컬라이더 활성화
- ? 적 우클릭 공격 가능
- ? 이사 알림 토스트
- ? 부드러운 페이드 효과

**결과:**
- 적 유닛 클릭 가능
- 직관적인 공격 명령
- 이사 시 명확한 알림
- 자연스러운 UX

**게임 플레이:**
- 적 발견 → 우클릭 → 공격!
- 이사 명령 → 알림 표시 → 10초 대기
- 명확한 피드백
- 완벽한 사용자 경험

게임 개발 화이팅! ???????
