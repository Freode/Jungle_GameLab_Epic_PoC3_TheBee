# ??? 드래그 선택 시스템 가이드

## 개요
마우스 드래그로 여러 일꾼 꿀벌을 동시에 선택할 수 있는 RTS 스타일의 선택 시스템입니다.

---

## ? 기능

### 드래그 선택
- **마우스 왼쪽 버튼 드래그**: 영역 내의 모든 일꾼 꿀벌 선택
- **실시간 선택 박스**: 드래그 중 반투명 초록색 박스 표시
- **자동 필터링**: 플레이어 소속 일꾼만 선택 (여왕벌 제외)

### 다중 명령
- **우클릭 이동**: 선택된 모든 유닛을 해당 위치로 이동
- **단체 행동**: 자원 채집, 공격 등 동시 수행

---

## ?? 사용 방법

### 1. 기본 선택

#### 단일 선택 (기존 방식)
```
1. 유닛 클릭 → 1개 선택
```

#### 드래그 선택 (새로운 방식)
```
1. 마우스 왼쪽 버튼 누른 상태 유지
2. 원하는 영역으로 드래그
3. 버튼을 놓으면 영역 내 모든 일꾼 선택됨
```

### 2. 선택된 유닛 명령

```
1. 일꾼들을 드래그로 선택
2. 우클릭으로 목표 지점 지정
3. 모든 선택된 유닛이 해당 위치로 이동
```

---

## ?? Unity 설정

### 1단계: DragSelector 추가

```
1. Hierarchy에서 빈 GameObject 생성
2. 이름을 "DragSelector"로 변경
3. DragSelector.cs 컴포넌트 추가
```

### 2단계: Inspector 설정

#### 기본 설정
```yaml
Enable Drag Selection: true (체크)
Min Drag Distance: 10 (픽셀)
```

#### 비주얼 설정
```yaml
Selection Box Color: 
  R: 0, G: 255, B: 0, A: 50 (반투명 초록색)

Selection Box Border Color:
  R: 0, G: 255, B: 0, A: 255 (진한 초록색)

Border Width: 2 (픽셀)
```

---

## ?? 커스터마이징

### 선택 박스 색상 변경

```csharp
// Inspector에서 변경 가능
Selection Box Color: 반투명 색상 (알파값 낮게)
Selection Box Border Color: 진한 색상 (알파값 높게)
```

### 최소 드래그 거리 조정

```csharp
// Inspector에서 변경
Min Drag Distance: 10
  - 작을수록 민감함 (실수로 드래그 선택 쉬움)
  - 클수록 의도적인 드래그만 선택
```

---

## ?? 작동 원리

### 선택 과정

```
1. 마우스 다운 → 시작 위치 기록
2. 드래그 중 → 최소 거리 확인
3. 최소 거리 초과 → 드래그 모드 활성화
4. 선택 박스 표시 (OnGUI)
5. 마우스 업 → 박스 내 유닛 검사
6. 일꾼 필터링 → 선택 표시
```

### 유닛 필터링

```csharp
? 선택 가능:
   - faction == Player
   - isQueen == false
   - 화면에 보이는 유닛

? 선택 불가:
   - 적 유닛
   - 여왕벌
   - 화면 밖 유닛
   - 카메라 뒤쪽 유닛
```

---

## ?? 시스템 통합

### TileClickMover와 통합

```csharp
// 드래그 선택된 유닛 확인
var dragSelected = DragSelector.Instance.GetSelectedUnits();

// 다중 유닛 명령
if (dragSelected.Count > 0)
{
    HandleRightClickForMultipleUnits(dragSelected);
}
```

### UnitAgent 선택 표시

```csharp
// 각 유닛에 선택 표시
foreach (var unit in selectedUnits)
{
    unit.SetSelected(true);
}
```

---

## ?? 고급 기능

### 선택 정보 가져오기

```csharp
// 현재 선택된 유닛들
List<UnitAgent> units = DragSelector.Instance.GetSelectedUnits();

// 선택된 유닛 수
int count = DragSelector.Instance.GetSelectedCount();

// 특정 유닛 선택 여부
bool isSelected = DragSelector.Instance.IsUnitSelected(unit);
```

### 프로그램적으로 선택 해제

```csharp
// DragSelector.cs에 추가할 메서드
public void ClearSelection()
{
    DeselectAll();
}
```

---

## ?? UI/UX 개선 아이디어

### 선택 카운터 표시

```csharp
// UI Text 추가
public TextMeshProUGUI selectionCountText;

void Update()
{
    if (selectionCountText != null)
    {
        int count = DragSelector.Instance.GetSelectedCount();
        selectionCountText.text = count > 0 ? $"{count} 유닛 선택됨" : "";
    }
}
```

### 선택 사운드 효과

```csharp
public AudioClip selectionSound;
private AudioSource audioSource;

void SelectUnitsInDragArea()
{
    // ... 선택 로직 ...
    
    if (unitsInRect.Count > 0 && audioSource != null)
    {
        audioSource.PlayOneShot(selectionSound);
    }
}
```

### 유닛 초상화 표시

```csharp
// 선택된 유닛들의 초상화를 UI에 표시
public Transform portraitContainer;
public GameObject portraitPrefab;

void UpdatePortraits()
{
    // 기존 초상화 제거
    foreach (Transform child in portraitContainer)
        Destroy(child.gameObject);
    
    // 새 초상화 생성
    foreach (var unit in selectedUnits)
    {
        var portrait = Instantiate(portraitPrefab, portraitContainer);
        // ... 유닛 정보로 초상화 설정 ...
    }
}
```

---

## ?? 문제 해결

### 드래그가 작동하지 않아요

**확인사항:**
```
? DragSelector GameObject가 씬에 있는지
? DragSelector.cs가 활성화되어 있는지
? Enable Drag Selection이 체크되어 있는지
? EventSystem이 씬에 있는지
```

### 선택 박스가 안 보여요

**확인사항:**
```
? Selection Box Color의 Alpha 값이 0이 아닌지
? Min Drag Distance를 충분히 드래그했는지
? Canvas가 화면을 가리고 있지 않은지
```

### UI 위에서 드래그할 때 선택되어요

**해결 방법:**
```csharp
// DragSelector.cs의 Update()에서
if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
{
    return; // UI 위에서는 무시
}
```

### 여왕벌도 선택되어요

**확인사항:**
```csharp
// SelectUnitsInDragArea()에서
if (unit.isQueen) continue; // 여왕벌 제외
```

---

## ?? 최적화 팁

### 프레임 드롭 방지

```csharp
// 매 프레임마다 모든 유닛 확인하지 않기
private float lastCheckTime = 0f;
public float checkInterval = 0.1f; // 0.1초마다 확인

void Update()
{
    if (Time.time - lastCheckTime < checkInterval)
        return;
    
    lastCheckTime = Time.time;
    // ... 드래그 체크 로직 ...
}
```

### 공간 분할 사용

```csharp
// 화면에 보이는 유닛만 확인
Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

foreach (var unit in TileManager.Instance.GetAllUnits())
{
    if (!GeometryUtility.TestPlanesAABB(planes, unit.GetComponent<Collider>().bounds))
        continue;
    
    // ... 선택 체크 ...
}
```

---

## ?? 키 바인딩 확장

### Shift 키로 추가 선택

```csharp
void SelectUnitsInDragArea()
{
    // Shift 키를 누르고 있으면 기존 선택 유지
    if (!Input.GetKey(KeyCode.LeftShift))
    {
        DeselectAll();
    }
    
    // 새 유닛들 추가
    selectedUnits.AddRange(unitsInRect);
}
```

### Ctrl+A로 전체 선택

```csharp
void Update()
{
    // Ctrl+A로 모든 일꾼 선택
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
    {
        SelectAllWorkers();
    }
}

void SelectAllWorkers()
{
    DeselectAll();
    
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction == Faction.Player && !unit.isQueen)
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }
    }
}
```

---

## ?? 확장 아이디어

### 컨트롤 그룹

```csharp
private Dictionary<KeyCode, List<UnitAgent>> controlGroups = new Dictionary<KeyCode, List<UnitAgent>>();

void Update()
{
    // Ctrl+숫자: 그룹 저장
    if (Input.GetKey(KeyCode.LeftControl))
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SaveControlGroup(KeyCode.Alpha1);
    }
    // 숫자: 그룹 선택
    else if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        SelectControlGroup(KeyCode.Alpha1);
    }
}
```

### 더블클릭으로 같은 타입 선택

```csharp
private float lastClickTime = 0f;
private UnitAgent lastClickedUnit = null;

void HandleLeftClick()
{
    // 더블클릭 감지
    if (Time.time - lastClickTime < 0.3f && lastClickedUnit == unit)
    {
        SelectAllOfType(unit);
    }
    
    lastClickTime = Time.time;
    lastClickedUnit = unit;
}

void SelectAllOfType(UnitAgent template)
{
    // 화면에 보이는 같은 타입의 모든 유닛 선택
    // 예: 모든 일꾼, 모든 전투 유닛 등
}
```

---

## ? 체크리스트

설정 완료 확인:

```
[ ] DragSelector GameObject 생성
[ ] DragSelector.cs 컴포넌트 추가
[ ] Enable Drag Selection 체크
[ ] Min Drag Distance 설정 (기본 10)
[ ] Selection Box Color 설정
[ ] 게임 실행하여 드래그 테스트
[ ] 선택 박스 표시 확인
[ ] 다중 유닛 선택 확인
[ ] 우클릭 이동 테스트
```

---

## ?? 완료!

이제 RTS 게임처럼 마우스 드래그로 여러 일꾼을 동시에 선택하고 명령을 내릴 수 있습니다!

**주요 기능:**
- ? 드래그로 영역 선택
- ? 실시간 선택 박스 표시
- ? 자동 유닛 필터링
- ? 다중 유닛 명령
- ? 기존 시스템과 완벽 통합

즐거운 게임 플레이 되세요! ???
