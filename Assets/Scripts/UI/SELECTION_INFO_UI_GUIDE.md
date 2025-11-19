# SelectionInfoUI 설정 가이드

## ? 선택 기반 정보 UI 구현 완료!

---

## ?? 주요 변경 사항

### **HoverInfoUI → SelectionInfoUI**

- ? **호버 방식**: 마우스를 올리면 정보 표시 ?
- ? **선택 방식**: 유닛/타일을 선택하면 정보 표시 ?

---

## ?? 작동 방식

### **1. 단일 유닛 선택**

```
[일꾼 꿀벌 클릭]
   ↓
TileClickMover.SelectUnit() 호출
   ↓
SelectionInfoUI.UpdateInfo()
   ↓
┌─────────────────────┐
│   일꾼 꿀벌         │
├─────────────────────┤
│ 위치: (5, 3)        │
│ 진영: 플레이어      │
│ 체력: 10/10         │
│ 공격력: 3           │
│ 소속 하이브: (0, 0) │
│ 현재 작업: 자원 채취│
└─────────────────────┘
```

---

### **2. 드래그 다중 선택 (신규!)**

```
[일꾼 3마리 드래그 선택]
   ↓
DragSelector.SelectUnitsInDragArea()
   ↓
SelectionInfoUI.ShowDragSelectedInfo()
   ↓
┌─────────────────────┐
│ 일꾼 선택: 3마리    │ ?
├─────────────────────┤
│ 선택된 일꾼: 3마리  │
│                     │
│ 평균 능력치:        │ ?
│ 체력: 9.3/10.0      │
│ 공격력: 3.0         │
│                     │
│ 총합:               │ ?
│ 체력: 28/30         │
│ 공격력: 9           │
└─────────────────────┘
```

---

### **3. 타일 선택**

```
[빈 타일 클릭]
   ↓
SelectionInfoUI.SelectTile(tile)
   ↓
┌─────────────────────┐
│   타일 (5, 3)       │
├─────────────────────┤
│ 지형: Forest        │
│ 자원량: 350         │
│ 시야 상태: 시야 내  │
└─────────────────────┘
```

---

## ?? 우선순위

### **정보 표시 우선순위**

```
1순위: 드래그 선택된 유닛들 ?
   ↓
2순위: 단일 선택된 유닛 ?
   ↓
3순위: 선택된 타일 ?
   ↓
선택 없음: UI 숨김
```

---

## ?? Unity 설정 방법

### **1. SelectionInfoUI GameObject 생성**

```
Canvas
   └─ SelectionInfoUI (Empty GameObject)
      └─ Component: SelectionInfoUI (Script)
```

**중요!** 기존 HoverInfoUI GameObject가 있다면:
1. 이름을 `SelectionInfoUI`로 변경
2. HoverInfoUI 스크립트 제거
3. SelectionInfoUI 스크립트 추가

---

### **2. InfoPanel 구조 (동일)**

```
SelectionInfoUI
   └─ InfoPanel (Panel)
      ├─ Image (배경) ← 검은색 반투명
      ├─ TitleText (TextMeshProUGUI)
      └─ DetailsText (TextMeshProUGUI)
```

---

### **3. SelectionInfoUI 스크립트 설정**

```
SelectionInfoUI (Script)
   ├─ Info Panel: [InfoPanel GameObject 드래그]
   ├─ Title Text: [TitleText 드래그]
   ├─ Details Text: [DetailsText 드래그]
   ├─ Panel Rect: [InfoPanel RectTransform 드래그]
   └─ Settings
      └─ Fixed Position: (10, 10) ← 왼쪽 위 고정
```

---

## ?? UI 레이아웃 설정

### **InfoPanel (Panel)**

```
RectTransform:
├─ Width: 250
├─ Height: 200 ← 드래그 선택 정보가 더 길어서 증가
├─ Pivot: (0, 1) ← 왼쪽 위
└─ Anchor: Top Left

Image:
├─ Color: (0, 0, 0, 200) ← 반투명 검은색
└─ Raycast Target: false
```

---

### **TitleText (TextMeshProUGUI)**

```
RectTransform:
├─ Anchors: Stretch Top
├─ Left: 10, Right: -10
├─ Top: -10, Height: 30

Text:
├─ Font Size: 18
├─ Color: White
├─ Alignment: Center Middle
└─ Overflow: Ellipsis
```

---

### **DetailsText (TextMeshProUGUI)**

```
RectTransform:
├─ Anchors: Stretch
├─ Left: 10, Right: -10
├─ Top: -50, Bottom: 10

Text:
├─ Font Size: 14
├─ Color: White (200, 200, 200)
├─ Alignment: Top Left
├─ Overflow: Overflow
└─ Wrapping: Enabled
```

---

## ?? 주요 기능

### **1. 단일 선택 정보**

```csharp
void ShowUnitInfo(UnitAgent unit)
{
    // 유닛 정보 표시
    titleText.text = "일꾼 꿀벌";
    
    string details = "";
    details += $"위치: ({unit.q}, {unit.r})\n";
    details += $"진영: 플레이어\n";
    details += $"체력: {combat.health}/{combat.maxHealth}\n";
    details += $"공격력: {combat.attack}\n";
    details += $"현재 작업: 자원 채취\n";
}
```

---

### **2. 드래그 다중 선택 정보 (신규!)**

```csharp
void ShowDragSelectedInfo()
{
    titleText.text = $"일꾼 선택: {dragSelectedUnits.Count}마리";
    
    // 평균 능력치 계산 ?
    int totalHealth = 0;
    int totalMaxHealth = 0;
    int totalAttack = 0;
    
    foreach (var unit in dragSelectedUnits)
    {
        var combat = unit.GetComponent<CombatUnit>();
        totalHealth += combat.health;
        totalMaxHealth += combat.maxHealth;
        totalAttack += combat.attack;
    }
    
    float avgHealth = (float)totalHealth / validCount;
    float avgAttack = (float)totalAttack / validCount;
    
    string details = $"선택된 일꾼: {dragSelectedUnits.Count}마리\n\n";
    details += $"평균 능력치:\n";
    details += $"체력: {avgHealth:F1}/{avgMaxHealth:F1}\n";
    details += $"공격력: {avgAttack:F1}\n\n";
    details += $"총합:\n";
    details += $"체력: {totalHealth}/{totalMaxHealth}\n";
    details += $"공격력: {totalAttack}\n";
}
```

**표시 정보:**
- ? 선택된 일꾼 수
- ? 평균 체력 (소수점 1자리)
- ? 평균 공격력 (소수점 1자리)
- ? 총 체력 합계
- ? 총 공격력 합계

---

## ?? 시나리오 예시

### **시나리오 1: 일꾼 3마리 드래그 선택**

```
[일꾼 A]
체력: 10/10
공격력: 3

[일꾼 B]
체력: 8/10
공격력: 3

[일꾼 C]
체력: 10/10
공격력: 3

[드래그 선택 시 표시]
┌─────────────────────┐
│ 일꾼 선택: 3마리    │
├─────────────────────┤
│ 선택된 일꾼: 3마리  │
│                     │
│ 평균 능력치:        │
│ 체력: 9.3/10.0      │ ← (10+8+10)/3 = 9.3
│ 공격력: 3.0         │ ← (3+3+3)/3 = 3.0
│                     │
│ 총합:               │
│ 체력: 28/30         │ ← 10+8+10 = 28
│ 공격력: 9           │ ← 3+3+3 = 9
└─────────────────────┘
```

---

### **시나리오 2: 단일 선택 → 드래그 선택**

```
[일꾼 A 클릭]
   ↓
┌─────────────────────┐
│   일꾼 꿀벌         │ ← 단일 선택 정보
├─────────────────────┤
│ 위치: (5, 3)        │
│ 체력: 10/10         │
│ 공격력: 3           │
└─────────────────────┘
   ↓
[일꾼 3마리 드래그]
   ↓
┌─────────────────────┐
│ 일꾼 선택: 3마리    │ ← 드래그 선택 정보
├─────────────────────┤
│ 평균 능력치:        │
│ 체력: 9.3/10.0      │
│ 공격력: 3.0         │
└─────────────────────┘
```

---

### **시나리오 3: 드래그 선택 해제**

```
[일꾼 3마리 선택 중]
   ↓
[빈 곳 클릭]
   ↓
DragSelector.DeselectAll() 호출
   ↓
SelectionInfoUI.HideInfo() 호출
   ↓
[UI 비활성화] ?
```

---

## ?? 업데이트 주기

### **FixedUpdate 사용**

```csharp
void FixedUpdate()
{
    UpdateInfo(); // 매 물리 프레임마다 실행
}

void UpdateInfo()
{
    // 1순위: 드래그 선택
    if (dragUnits.Count > 0)
    {
        UpdateDragSelectedInfo(); // 실시간 업데이트
    }
    // 2순위: 단일 선택
    else if (selectedUnit != null)
    {
        UpdateUnitInfo(selectedUnit); // 실시간 업데이트
    }
}
```

**장점:**
- ? 체력 변화 실시간 반영
- ? 작업 상태 실시간 업데이트
- ? 드래그 선택 수 변화 즉시 반영

---

## ? 체크리스트

```
[단일 선택]
[ ] 일꾼 클릭 시 정보 표시 ?
[ ] 여왕벌 클릭 시 정보 표시 ?
[ ] 타일 클릭 시 정보 표시 ?
[ ] 미탐색 타일은 정보 숨김 ?
[ ] 빈 곳 클릭 시 UI 숨김 ?

[드래그 선택]
[ ] 일꾼 3마리 선택 시 정보 표시 ?
[ ] 선택된 일꾼 수 표시 ?
[ ] 평균 능력치 표시 ?
[ ] 총합 능력치 표시 ?
[ ] 드래그 해제 시 UI 숨김 ?

[우선순위]
[ ] 드래그 선택 > 단일 선택 ?
[ ] 단일 선택 > 타일 선택 ?

[업데이트]
[ ] FixedUpdate로 실시간 갱신 ?
[ ] 체력 변화 즉시 반영 ?

[빌드]
[ ] 컴파일 성공 ?
```

---

## ?? 완료!

**주요 기능:**
- ? **선택 기반 정보 표시**: 호버 → 선택
- ? **드래그 다중 선택 지원**: 일꾼 여러 마리 선택
- ? **평균/총합 능력치**: 드래그 선택 시 표시
- ? **우선순위 시스템**: 드래그 > 단일 > 타일
- ? **실시간 업데이트**: FixedUpdate

**새로운 파일:**
- `Assets\Scripts\UI\SelectionInfoUI.cs` ← 새로 생성

**수정된 파일:**
- `Assets\Scripts\Tools\TileClickMover.cs` ← GetSelectedUnit() 추가

**삭제된 파일:**
- `Assets\Scripts\UI\HoverInfoUI.cs` ← 제거됨

**Unity 설정:**
1. 기존 HoverInfoUI GameObject 이름 변경: `SelectionInfoUI`
2. HoverInfoUI 스크립트 제거
3. SelectionInfoUI 스크립트 추가
4. InfoPanel, TitleText, DetailsText 연결
5. Fixed Position: (10, 10)

**드래그 선택 표시:**
```
일꾼 선택: 3마리

선택된 일꾼: 3마리

평균 능력치:
체력: 9.3/10.0
공격력: 3.0

총합:
체력: 28/30
공격력: 9
```

게임 개발 화이팅! ???????
