# ?? 유닛 정보 표시 시스템 가이드

## 개요
명령 UI에 선택된 유닛의 이름, 체력, 공격력을 표시하는 시스템입니다.

---

## ? 표시되는 정보

### 유닛 이름
```
- 벌집: 하이브
- 여왕벌: 여왕벌
- 일꾼 꿀벌: 일꾼 꿀벌
- 적 유닛: 적 유닛
- 기타: GameObject 이름
```

### 체력 정보
```
HP: 현재체력/최대체력
예: HP: 45/50
```

### 공격력 정보
```
공격력: 숫자
예: 공격력: 10
```

---

## ?? Unity 설정

### 1단계: UI 구조 설정

기존 UnitCommandPanel에 다음 UI 요소를 추가하세요:

```
Canvas
└── UnitCommandPanel
    ├── UnitInfoPanel (Panel) ← 새로 추가
    │   ├── UnitNameText (TextMeshProUGUI)
    │   ├── UnitHealthText (TextMeshProUGUI)
    │   └── UnitAttackText (TextMeshProUGUI)
    └── ButtonContainer
        └── [명령 버튼들]
```

#### 상세 구조 예시
```
UnitCommandPanel (Panel)
├── UnitInfoPanel (Panel)
│   ├── Background (Image) - 반투명 검정색
│   ├── UnitNameText (TMP)
│   │   └── Text: "일꾼 꿀벌"
│   ├── UnitHealthText (TMP)
│   │   └── Text: "HP: 50/50"
│   └── UnitAttackText (TMP)
│       └── Text: "공격력: 10"
└── ButtonContainer
    └── ...
```

### 2단계: Inspector 연결

UnitCommandPanel 컴포넌트 설정:

```yaml
Panel Root: UnitCommandPanel GameObject
Button Container: ButtonContainer RectTransform
Command Button Prefab: [기존 프리팹]

# 새로 추가된 필드
Unit Name Text: UnitNameText (TMP)
Unit Health Text: UnitHealthText (TMP)
Unit Attack Text: UnitAttackText (TMP)
Unit Info Panel: UnitInfoPanel GameObject (선택사항)
```

---

## ?? UI 레이아웃 예시

### 레이아웃 구조
```
┌─────────────────────────────┐
│  유닛 정보                    │
│  ┌─────────────────────────┐│
│  │ 일꾼 꿀벌                 ││
│  │ HP: 45/50                ││
│  │ 공격력: 10                ││
│  └─────────────────────────┘│
│                              │
│  명령 버튼                    │
│  [이동] [채집] [공격] [건설]  │
└─────────────────────────────┘
```

### UI 위치 설정

#### UnitInfoPanel
```
Anchor: Top Center
Pivot: 0.5, 1
Position Y: -10 (상단에서 10 떨어짐)
Width: 250
Height: 80
```

#### UnitNameText
```
Font Size: 18
Alignment: Center
Color: White (255, 255, 255)
Position: Top of Panel
```

#### UnitHealthText
```
Font Size: 14
Alignment: Center
Color: Green (100, 255, 100)
Position: Middle
```

#### UnitAttackText
```
Font Size: 14
Alignment: Center
Color: Red (255, 100, 100)
Position: Bottom
```

---

## ?? 작동 방식

### 유닛 선택 시
```
1. 유닛 클릭
2. UnitCommandPanel.Show(agent) 호출
3. UpdateUnitInfo() 실행
   ↓
4. 유닛 이름 판별 (GetUnitName)
5. CombatUnit 컴포넌트 확인
6. 체력/공격력 가져오기
7. UI 텍스트 업데이트
```

### 실시간 업데이트
```
Update() 메서드에서 매 프레임마다:
- 현재 선택된 유닛의 정보 갱신
- 체력 변화 실시간 반영
```

---

## ?? 유닛 이름 판별 로직

```csharp
string GetUnitName(UnitAgent agent)
{
    // 1. 하이브 체크
    if (agent.GetComponent<Hive>() != null)
        return "벌집";
    
    // 2. 여왕벌 체크
    if (agent.isQueen)
        return "여왕벌";
    
    // 3. 플레이어 일꾼 체크
    if (agent.faction == Faction.Player && !agent.isQueen)
        return "일꾼 꿀벌";
    
    // 4. 적 유닛
    if (agent.faction == Faction.Enemy)
        return "적 유닛";
    
    // 5. 기본 (GameObject 이름)
    return agent.gameObject.name;
}
```

---

## ?? 커스터마이징

### 1. 유닛 이름 커스터마이즈

#### UnitAgent에 이름 필드 추가
```csharp
public class UnitAgent : MonoBehaviour
{
    [Header("유닛 정보")]
    public string unitName = ""; // Inspector에서 설정
    
    // ...existing code...
}
```

#### GetUnitName 수정
```csharp
string GetUnitName(UnitAgent agent)
{
    // 커스텀 이름이 설정되어 있으면 우선 사용
    if (!string.IsNullOrEmpty(agent.unitName))
        return agent.unitName;
    
    // ...기존 로직...
}
```

### 2. 추가 정보 표시

#### 이동 속도 표시
```csharp
[Header("유닛 정보 UI")]
public TextMeshProUGUI unitSpeedText;

void UpdateUnitInfo()
{
    // ...기존 코드...
    
    var controller = currentAgent.GetComponent<UnitController>();
    if (controller != null && unitSpeedText != null)
    {
        unitSpeedText.text = $"속도: {controller.moveSpeed:F1}";
    }
}
```

#### 레벨 표시
```csharp
public TextMeshProUGUI unitLevelText;

void UpdateUnitInfo()
{
    // ...기존 코드...
    
    if (unitLevelText != null)
    {
        int level = GetUnitLevel(currentAgent);
        unitLevelText.text = $"Lv. {level}";
    }
}
```

### 3. 체력 바 추가

```csharp
[Header("유닛 정보 UI")]
public Slider healthBar;

void UpdateUnitInfo()
{
    var combat = currentAgent.GetComponent<CombatUnit>();
    if (combat != null && healthBar != null)
    {
        healthBar.maxValue = combat.maxHealth;
        healthBar.value = combat.health;
    }
}
```

### 4. 색상 코딩

#### 체력에 따른 색상 변경
```csharp
void UpdateUnitInfo()
{
    var combat = currentAgent.GetComponent<CombatUnit>();
    if (combat != null && unitHealthText != null)
    {
        float healthPercent = (float)combat.health / combat.maxHealth;
        
        // 체력에 따라 색상 변경
        if (healthPercent > 0.6f)
            unitHealthText.color = Color.green;
        else if (healthPercent > 0.3f)
            unitHealthText.color = Color.yellow;
        else
            unitHealthText.color = Color.red;
        
        unitHealthText.text = $"HP: {combat.health}/{combat.maxHealth}";
    }
}
```

---

## ?? UI 스타일 예시

### 스타일 1: 심플
```
┌────────────────┐
│ 일꾼 꿀벌       │
│ HP: 50/50      │
│ 공격력: 10      │
└────────────────┘
```

### 스타일 2: 아이콘 포함
```
┌────────────────┐
│ ?? 일꾼 꿀벌    │
│ ?? 50/50       │
│ ?? 10          │
└────────────────┘
```

### 스타일 3: 상세 정보
```
┌─────────────────────────┐
│ [아이콘] 일꾼 꿀벌 Lv.3  │
│ ━━━━━━━━━━ 50/50       │
│ 공격력: 10  속도: 2.0   │
│ 소속: 플레이어 진영      │
└─────────────────────────┘
```

---

## ?? 최적화 팁

### 1. Update 빈도 줄이기

매 프레임 업데이트가 부담스러우면:

```csharp
private float updateInterval = 0.1f; // 0.1초마다 업데이트
private float lastUpdateTime;

void Update()
{
    if (currentAgent != null && panelRoot != null && panelRoot.activeSelf)
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateUnitInfo();
            lastUpdateTime = Time.time;
        }
    }
}
```

### 2. 이벤트 기반 업데이트

체력이 변경될 때만 업데이트:

```csharp
// CombatUnit.cs에 이벤트 추가
public event System.Action OnHealthChanged;

public void TakeDamage(int damage)
{
    health -= damage;
    OnHealthChanged?.Invoke();
}

// UnitCommandPanel.cs
void Show(UnitAgent agent)
{
    // ...기존 코드...
    
    var combat = agent.GetComponent<CombatUnit>();
    if (combat != null)
    {
        combat.OnHealthChanged += UpdateUnitInfo;
    }
}

void Hide()
{
    if (currentAgent != null)
    {
        var combat = currentAgent.GetComponent<CombatUnit>();
        if (combat != null)
        {
            combat.OnHealthChanged -= UpdateUnitInfo;
        }
    }
    
    // ...기존 코드...
}
```

---

## ?? 문제 해결

### 정보가 표시되지 않아요

**확인사항:**
```
? UnitInfoPanel이 활성화되어 있는지
? Text 컴포넌트가 Inspector에 연결되었는지
? Text 컴포넌트가 Canvas 안에 있는지
? Canvas의 Render Mode 확인
```

### "-" 로만 표시되어요

**원인:**
```
CombatUnit 컴포넌트가 없는 유닛입니다.
```

**해결:**
```
1. 유닛 프리팹에 CombatUnit 컴포넌트 추가
2. Inspector에서 attack/maxHealth 값 설정
```

### 체력이 업데이트되지 않아요

**확인사항:**
```
? Update() 메서드가 실행되고 있는지
? currentAgent가 null이 아닌지
? CombatUnit의 health 값이 실제로 변경되는지
```

### 텍스트가 잘려 보여요

**해결:**
```
1. TextMeshProUGUI의 RectTransform 크기 증가
2. Font Size 감소
3. Text Auto-Sizing 활성화
```

---

## ?? 정보 표시 예시

### 일꾼 꿀벌
```
일꾼 꿀벌
HP: 50/50
공격력: 10
```

### 여왕벌
```
여왕벌
HP: 100/100
공격력: 15
```

### 벌집
```
벌집
HP: 200/200
공격력: -
```

### 적 유닛
```
적 유닛
HP: 30/40
공격력: 8
```

---

## ? 체크리스트

### Unity 설정
```
[ ] UnitInfoPanel 생성
[ ] UnitNameText (TMP) 추가
[ ] UnitHealthText (TMP) 추가
[ ] UnitAttackText (TMP) 추가
[ ] Inspector에 연결
```

### 테스트
```
[ ] 일꾼 선택 시 정보 표시 확인
[ ] 여왕벌 선택 시 정보 표시 확인
[ ] 하이브 선택 시 정보 표시 확인
[ ] 체력 변화 시 실시간 업데이트 확인
[ ] 다른 유닛 선택 시 정보 변경 확인
```

---

## ?? 완료!

이제 유닛을 선택하면 명령 UI에 유닛의 이름, 체력, 공격력이 표시됩니다!

**핵심 기능:**
- ? 유닛 이름 자동 판별
- ? 체력/공격력 실시간 표시
- ? 매 프레임 자동 업데이트
- ? 확장 가능한 구조

게임 플레이 화이팅! ???
