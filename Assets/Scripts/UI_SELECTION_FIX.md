# ?? 3가지 UI/선택 시스템 개선 가이드

## 완료된 개선사항

### 1. ? 드래그 선택 시 하이브 클릭 해제
- 드래그 시작 시 TileClickMover 선택 해제
- 하이브 선택 자동 해제

### 2. ? 하이브 클릭 시 하이브 능력치 표시
- Hive 우선순위 체크
- 하이브 자체 정보 표시

### 3. ? 새 하이브 생성 후 일꾼 수동 명령 초기화
- hasManualOrder 플래그 초기화
- isFollowingQueen 플래그 초기화
- UnitBehaviorController 작업 취소

---

## ?? 수정된 파일

### 1. DragSelector.cs
```csharp
? HandleDragSelection() 수정
   - 드래그 시작 시 DeselectAll() 호출
   - TileClickMover.Instance.DeselectUnit() 호출
```

### 2. TileClickMover.cs
```csharp
? HandleLeftClick() 수정
   - Hive 우선순위 체크
   - UnitAgent보다 먼저 Hive 검색
```

### 3. Hive.cs
```csharp
? Initialize() 수정
   - hasManualOrder = false
   - isFollowingQueen = false
   - behavior.CancelCurrentTask()
```

---

## ?? 시스템 작동 방식

### 1. 드래그 선택 시 하이브 해제

#### 문제 상황 (이전)
```
[하이브 선택됨]
selectedUnitInstance = HiveAgent
   ↓
[드래그 시작]
   ↓
[일꾼 3명 선택]
selectedUnits = [Worker1, Worker2, Worker3]
   ↓
? 하이브가 여전히 선택됨
selectedUnitInstance = HiveAgent (유지)
```

#### 해결 (현재)
```
[하이브 선택됨]
selectedUnitInstance = HiveAgent
   ↓
[드래그 시작]
   ↓
[모든 선택 해제] ?
DeselectAll() → 일꾼 선택 해제
TileClickMover.DeselectUnit() → 하이브 선택 해제 ?
   ↓
[일꾼 3명 선택]
selectedUnits = [Worker1, Worker2, Worker3]
selectedUnitInstance = null ?
```

---

### 2. 하이브 클릭 시 하이브 정보 표시

#### 문제 상황 (이전)
```
[하이브 클릭]
   ↓
[Raycast Hit]
   ↓
[UnitAgent 먼저 체크] ?
var unit = GetComponentInParent<UnitAgent>()
→ 하이브 위의 일꾼을 선택
   ↓
[일꾼 정보 표시] ?
HP: 50/50
공격력: 10
```

#### 해결 (현재)
```
[하이브 클릭]
   ↓
[Raycast Hit]
   ↓
[Hive 먼저 체크] ?
var hive = GetComponentInParent<Hive>()
→ 하이브 자체를 선택
   ↓
[하이브 정보 표시] ?
HP: 200/200
일꾼 수: 5/10
```

---

### 3. 새 하이브 생성 후 일꾼 초기화

#### 문제 상황 (이전)
```
[하이브 이사 전]
Worker1: hasManualOrder = true (수동 명령 받음)
Worker2: hasManualOrder = false
   ↓
[새 하이브 생성]
Initialize(q, r)
   ↓
[일꾼 추가]
workers.Add(Worker1)
workers.Add(Worker2)
   ↓
? 플래그 유지
Worker1: hasManualOrder = true (여전히 수동 모드)
Worker2: hasManualOrder = false
   ↓
? 문제 발생
Worker1은 활동 범위 제한 무시
```

#### 해결 (현재)
```
[하이브 이사 전]
Worker1: hasManualOrder = true
Worker2: hasManualOrder = false
   ↓
[새 하이브 생성]
Initialize(q, r)
   ↓
[일꾼 추가 + 초기화] ?
workers.Add(Worker1)
Worker1.hasManualOrder = false ?
Worker1.isFollowingQueen = false ?
behavior.CancelCurrentTask() ?
   ↓
workers.Add(Worker2)
Worker2.hasManualOrder = false
Worker2.isFollowingQueen = false
   ↓
? 모든 일꾼 초기화됨
Worker1: hasManualOrder = false
Worker2: hasManualOrder = false
   ↓
? 정상 작동
모든 일꾼이 활동 범위 제한 적용
```

---

## ?? 핵심 코드

### 1. 드래그 시 하이브 해제

```csharp
// DragSelector.cs - HandleDragSelection()
if (!isDragging && dragDistance > minDragDistance)
{
    isDragging = true;
    wasDragging = true;
    
    // 드래그 시작 시 모든 선택 해제
    DeselectAll(); // 일꾼 선택 해제
    
    // TileClickMover의 선택도 해제 ?
    if (TileClickMover.Instance != null)
    {
        TileClickMover.Instance.DeselectUnit(); // 하이브 선택 해제 ?
    }
}
```

---

### 2. 하이브 우선순위 체크

```csharp
// TileClickMover.cs - HandleLeftClick()
void HandleLeftClick()
{
    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    
    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
    {
        // 먼저 Hive 체크 (우선순위) ?
        var hive = hit.collider.GetComponentInParent<Hive>();
        if (hive != null)
        {
            var hiveAgent = hive.GetComponent<UnitAgent>();
            if (hiveAgent != null)
            {
                SelectUnit(hiveAgent); // 하이브 선택 ?
                return;
            }
        }

        // 그 다음 UnitAgent 체크
        var unit = hit.collider.GetComponentInParent<UnitAgent>();
        if (unit != null)
        {
            SelectUnit(unit); // 일꾼 선택
            return;
        }
        
        // ...
    }
}
```

---

### 3. 일꾼 초기화

```csharp
// Hive.cs - Initialize()
public void Initialize(int q, int r)
{
    // 기존 일꾼 수 카운트
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit != null && unit.faction == Faction.Player && !unit.isQueen)
        {
            if (!workers.Contains(unit))
            {
                workers.Add(unit);
                unit.homeHive = this;
                
                // 수동 명령 플래그 초기화 ?
                unit.hasManualOrder = false;
                unit.isFollowingQueen = false;
                
                // UnitBehaviorController 초기화 ?
                var behavior = unit.GetComponent<UnitBehaviorController>();
                if (behavior != null)
                {
                    behavior.CancelCurrentTask(); ?
                }
            }
        }
    }
}
```

---

## ?? 비교표

### 1. 드래그 선택

| 상황 | 이전 | 현재 |
|------|------|------|
| **하이브 선택 후 드래그** | 하이브 유지 | 하이브 해제 ? |
| **일꾼 선택** | 정상 | 정상 ? |
| **드래그 완료** | 중복 선택 | 단일 선택 ? |

### 2. 하이브 클릭

| 클릭 대상 | 이전 | 현재 |
|----------|------|------|
| **하이브** | 일꾼 정보 | 하이브 정보 ? |
| **일꾼** | 일꾼 정보 | 일꾼 정보 ? |
| **우선순위** | UnitAgent | Hive ? |

### 3. 새 하이브 생성

| 항목 | 이전 | 현재 |
|------|------|------|
| **hasManualOrder** | 유지 | 초기화 ? |
| **isFollowingQueen** | 유지 | 초기화 ? |
| **작업 상태** | 유지 | 취소 ? |
| **활동 범위** | 무시됨 | 정상 적용 ? |

---

## ?? 시각화

### 1. 드래그 선택 흐름

```
[하이브 선택]
?? (선택됨)
   ↓
[드래그 시작]
   ↓
[자동 해제] ?
?? (선택 해제)
   ↓
[일꾼 드래그 선택]
┌─────────────┐
│  ??  ??  ?? │  (드래그 영역)
└─────────────┘
   ↓
[일꾼만 선택됨] ?
?? ?? ?? (선택됨)
?? (선택 안 됨)
```

---

### 2. 클릭 우선순위

```
[마우스 클릭]
   ↓
[Raycast]
   ↓
1순위: Hive? ?
   YES → 하이브 선택
   NO → 2순위
   ↓
2순위: UnitAgent?
   YES → 일꾼 선택
   NO → 3순위
   ↓
3순위: HexTile?
   YES → 타일 클릭
   NO → 무시
```

---

### 3. 일꾼 초기화

```
[하이브 이사 전]
Worker1: 
├─ hasManualOrder: true ?
├─ task: Attack
└─ 활동 범위 무시

Worker2:
├─ hasManualOrder: false
├─ task: Gather
└─ 활동 범위 적용

         ↓
[새 하이브 생성]
         ↓
[Initialize 실행] ?
         ↓

[일꾼 초기화 후]
Worker1:
├─ hasManualOrder: false ?
├─ task: Idle ?
└─ 활동 범위 적용 ?

Worker2:
├─ hasManualOrder: false
├─ task: Idle ?
└─ 활동 범위 적용
```

---

## ?? 문제 해결

### Q: 드래그 선택해도 하이브가 안 해제돼요

**확인:**
```
[ ] TileClickMover.Instance != null
[ ] DeselectUnit() 호출 확인
[ ] selectedUnitInstance = null 확인
```

**해결:**
```
1. DragSelector.HandleDragSelection() 확인
2. TileClickMover.Instance.DeselectUnit() 호출 확인
3. Console에서 에러 확인
```

---

### Q: 하이브 클릭 시 일꾼 정보가 나와요

**확인:**
```
[ ] HandleLeftClick()에서 Hive 먼저 체크
[ ] GetComponentInParent<Hive>() 작동
[ ] return 문 확인
```

**해결:**
```
1. TileClickMover.HandleLeftClick() 순서 확인
2. Hive 컴포넌트 존재 확인
3. UnitAgent보다 먼저 체크되는지 확인
```

---

### Q: 새 하이브 생성 후 일꾼이 활동 범위 무시해요

**확인:**
```
[ ] hasManualOrder = false 설정
[ ] isFollowingQueen = false 설정
[ ] CancelCurrentTask() 호출
```

**해결:**
```
1. Hive.Initialize() 코드 확인
2. unit.hasManualOrder 값 확인 (디버그)
3. behavior.CancelCurrentTask() 실행 확인
```

---

## ?? 추가 개선 아이디어

### 1. 드래그 선택 시 시각적 피드백

```csharp
// 하이브 해제 시 애니메이션
public void DeselectUnit()
{
    if (selectedUnitInstance != null)
    {
        // 해제 애니메이션
        selectedUnitInstance.GetComponent<Animator>()?.SetTrigger("Deselect");
        
        selectedUnitInstance.SetSelected(false);
        selectedUnitInstance = null;
    }
}
```

---

### 2. 하이브/일꾼 구분 UI

```csharp
// 선택된 유닛 타입 표시
void UpdateSelectionUI()
{
    if (selectedUnitInstance != null)
    {
        var hive = selectedUnitInstance.GetComponent<Hive>();
        if (hive != null)
        {
            selectionTypeText.text = "?? 하이브";
            selectionTypeText.color = Color.yellow;
        }
        else
        {
            selectionTypeText.text = "?? 일꾼";
            selectionTypeText.color = Color.white;
        }
    }
}
```

---

### 3. 일꾼 초기화 로그

```csharp
// Hive.Initialize()
if (showDebugLogs)
{
    Debug.Log($"[일꾼 초기화] {unit.name}");
    Debug.Log($"  - hasManualOrder: {unit.hasManualOrder}");
    Debug.Log($"  - isFollowingQueen: {unit.isFollowingQueen}");
    Debug.Log($"  - task: {behavior.currentTask}");
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 선택 후 드래그 → 하이브 해제
[ ] 드래그로 일꾼 3명 선택 → 정상 선택
[ ] 하이브 클릭 → 하이브 정보 표시
[ ] 일꾼 클릭 → 일꾼 정보 표시
[ ] 새 하이브 생성 → 일꾼 초기화
[ ] 초기화 후 활동 범위 적용 확인
[ ] 수동 명령 플래그 초기화 확인
```

---

## ?? 완료!

**핵심 수정:**
- ? 드래그 시 하이브 자동 해제
- ? 하이브 우선순위 클릭 체크
- ? 새 하이브 생성 시 일꾼 초기화

**게임 플레이:**
- 직관적인 선택 시스템
- 명확한 하이브/일꾼 구분
- 정상적인 활동 범위 적용

**사용자 경험:**
- 혼란 감소
- 정확한 정보 표시
- 예측 가능한 동작

게임 개발 화이팅! ??????
