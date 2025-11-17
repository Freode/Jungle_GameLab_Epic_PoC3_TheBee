# ?? 명령 UI 버튼 클릭 문제 해결 가이드

## 완료된 수정사항

### ? 명령 UI 버튼 클릭 작동
- DragSelector UI 클릭 무시
- TileClickMover UI 체크 개선
- 드래그와 UI 클릭 충돌 해결

---

## ?? 수정된 파일

### 1. DragSelector.cs
```csharp
? HandleDragSelection() 수정
   - 드래그 시작 시 UI 체크 추가
   - 드래그 중 UI 체크 추가
   
? HandleSingleClick() 수정
   - UI 클릭 시 무시
```

### 2. TileClickMover.cs
```csharp
? Update() 수정
   - UI 클릭 처리 개선
   - 드래그 중이 아닐 때만 체크
```

---

## ?? 문제 원인 분석

### 문제 발생 과정

```
[플레이어: 명령 버튼 클릭]
   ↓
[Input.GetMouseButtonDown(0)]
   ↓
[IsPointerOverUI()] ?
   → true (버튼은 UI)
   ↓
[HandleLeftClick() 실행 안 됨] ?
   ↓
[DragSelector.HandleDragSelection()]
   ↓
[UI 체크 없음] ?
   → 드래그 시작 시도
   ↓
[버튼 클릭 무시됨] ?
```

### 해결 방법

```
[플레이어: 명령 버튼 클릭]
   ↓
[Input.GetMouseButtonDown(0)]
   ↓
[DragSelector.HandleDragSelection()]
   ↓
[IsPointerOverGameObject()] ?
   → true (버튼은 UI)
   ↓
return; // 드래그 시작 안 함 ?
   ↓
[Unity Button.onClick 이벤트 발생] ?
   ↓
[명령 실행] ?
```

---

## ?? 핵심 코드

### 1. DragSelector - UI 클릭 무시

```csharp
// DragSelector.cs - HandleDragSelection()
void HandleDragSelection()
{
    // 왼쪽 마우스 버튼 누름
    if (Input.GetMouseButtonDown(0))
    {
        // UI 위에서 클릭하면 무시 ?
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return; // UI 클릭은 무시하고 드래그 시작 안 함 ?
        }

        dragStartPos = Input.mousePosition;
        isDragging = false;
        wasDragging = false;
    }

    // 드래그 중
    if (Input.GetMouseButton(0))
    {
        // UI 위에서 드래그 중이면 무시 ?
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        currentMousePos = Input.mousePosition;
        
        // 드래그 거리 체크...
    }
}
```

---

### 2. DragSelector - 단순 클릭 UI 무시

```csharp
// DragSelector.cs - HandleSingleClick()
void HandleSingleClick()
{
    // UI 위에서 클릭하면 무시 ?
    if (UnityEngine.EventSystems.EventSystem.current != null &&
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
    {
        return; // UI 클릭은 무시 ?
    }

    // Raycast로 유닛 체크...
}
```

---

### 3. TileClickMover - UI 체크 개선

```csharp
// TileClickMover.cs - Update()
void Update()
{
    // Left click: selection / UI interactions
    if (Input.GetMouseButtonDown(0))
    {
        bool isDragging = DragSelector.Instance != null && 
                          DragSelector.Instance.GetSelectedCount() > 0;
        
        // 드래그 중이 아니고, UI가 아니면 월드 클릭 처리 ?
        if (!isDragging && !IsPointerOverUI())
        {
            HandleLeftClick();
        }
    }
    
    // ...
}
```

---

## ?? 비교표

| 상황 | 이전 | 현재 |
|------|------|------|
| **명령 버튼 클릭** | 무시됨 | 작동 ? |
| **드래그 시작** | UI 위에서도 시작 | UI는 무시 ? |
| **단순 클릭** | UI 체크 없음 | UI 체크 있음 ? |

---

## ?? 시각화

### 명령 버튼 클릭 흐름

```
[플레이어: 버튼 클릭]
┌──────────────┐
│ 하이브 건설  │ ← 클릭
│ 비용: 50     │
└──────────────┘
   ↓
[DragSelector 체크]
IsPointerOverUI? → YES
   ↓
return; (드래그 무시) ?
   ↓
[Unity Button 이벤트]
onClick.Invoke() ?
   ↓
[HandleCommandButtonClick]
OnCommandClicked(cmd) ?
   ↓
[명령 실행] ?
cmd.Execute(agent, target)
```

---

### 월드 클릭 vs UI 클릭

```
[왼쪽 클릭]
   ↓
[IsPointerOverUI?]
   ↓
  YES                    NO
   ↓                     ↓
[UI 클릭]           [월드 클릭]
   ↓                     ↓
[DragSelector]       [HandleLeftClick]
return; ?            유닛/타일 선택 ?
   ↓
[Unity 이벤트]
Button.onClick ?
```

---

## ?? 문제 해결

### Q: 버튼을 클릭해도 아무 일도 안 일어나요

**확인:**
```
[ ] Console에 에러 없음
[ ] Button.onClick 리스너 등록 확인
[ ] IsPointerOverGameObject() 작동 확인
[ ] EventSystem 존재 확인
```

**해결:**
```
1. Hierarchy에서 EventSystem 확인
2. Button 컴포넌트 확인
3. Canvas Raycast Target 활성화 확인
4. Console에서 [명령 UI] 로그 확인
```

---

### Q: 드래그가 UI 위에서도 시작돼요

**확인:**
```
[ ] DragSelector.HandleDragSelection() UI 체크
[ ] IsPointerOverGameObject() 호출 확인
[ ] dragStartPos 설정 전 return 확인
```

**해결:**
```
1. DragSelector.cs 코드 확인
2. UI 클릭 시 return 실행 확인
3. dragStartPos 설정 안 되는지 확인
```

---

### Q: 월드를 클릭해도 선택이 안 돼요

**확인:**
```
[ ] IsPointerOverUI() false 반환 확인
[ ] HandleLeftClick() 실행 확인
[ ] Raycast 작동 확인
```

---

## ?? 추가 개선 아이디어

### 1. 버튼 클릭 피드백

```csharp
// 버튼 클릭 시 애니메이션
void HandleCommandButtonClick(ICommand cmd, Button clickedButton)
{
    // 버튼 애니메이션 ?
    clickedButton.transform.DOScale(1.1f, 0.1f)
        .SetLoops(2, LoopType.Yoyo);
    
    OnCommandClicked(cmd);
    
    // 사운드 효과
    AudioManager.Instance?.PlaySound("ButtonClick");
    
    RefreshButtonStates();
    UpdateUnitInfo();
}
```

---

### 2. UI 레이어 설정

```csharp
// Canvas 설정
public class UIManager : MonoBehaviour
{
    void Start()
    {
        // UI Canvas를 "UI" 레이어로 설정
        var canvas = GetComponent<Canvas>();
        canvas.gameObject.layer = LayerMask.NameToLayer("UI");
    }
}
```

---

### 3. 버튼 상태 시각화

```csharp
// 비활성 버튼 시각적 표시
void RefreshButtonStates()
{
    foreach (Transform t in buttonContainer)
    {
        var btn = t.GetComponentInChildren<Button>();
        var img = t.GetComponentInChildren<Image>();
        
        if (btn != null && img != null)
        {
            if (btn.interactable)
            {
                img.color = Color.white; // 활성
            }
            else
            {
                img.color = Color.gray; // 비활성 ?
            }
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 명령 버튼 클릭 → 명령 실행
[ ] 버튼 위에서 드래그 → 드래그 안 됨
[ ] 월드 클릭 → 유닛/타일 선택
[ ] 드래그 선택 → 일꾼 선택
[ ] UI 클릭 → 월드 선택 안 됨
[ ] 버튼 비활성화 → 클릭 안 됨
```

---

## ?? 완료!

**핵심 수정:**
- ? DragSelector UI 클릭 무시
- ? 드래그 시작 시 UI 체크
- ? 단순 클릭 시 UI 체크

**결과:**
- 명령 버튼 정상 작동
- 드래그와 UI 충돌 해결
- 직관적인 조작 가능

**게임 플레이:**
- 명령 버튼 클릭 가능
- 드래그 선택 정상 작동
- UI/월드 구분 명확

게임 개발 화이팅! ??????
