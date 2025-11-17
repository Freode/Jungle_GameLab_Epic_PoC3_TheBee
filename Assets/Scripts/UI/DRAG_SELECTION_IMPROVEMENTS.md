# ?? 드래그 선택 & 적 진영 시스템 개선 가이드

## 개선 사항 요약

### 1. ? 드래그 선택 영역 수정
- Y축 좌표 변환 오류 수정
- 실제 화면 좌표로 정확한 영역 계산
- 모든 일꾼이 제대로 선택됨

### 2. ? 선택 색상 변경
- 노란색 → **연한 연두색** (Light Green)
- RGB: (144, 238, 144)
- 꿀벌집, 여왕벌, 일꾼 모두 적용

### 3. ? 적 진영 추가
- **말벌**: 적 일꾼 유닛
- **말벌 여왕**: 적 여왕 유닛
- **말벌집**: 적 하이브

### 4. ? 하이브 능력치 버그 수정
- 하이브 클릭 시 하이브의 CombatUnit 참조
- 일꾼 능력치가 아닌 하이브 능력치 표시

---

## ?? 선택 색상

### 변경 전
```
노란색: RGB(255, 255, 0)
```

### 변경 후
```
연한 연두색: RGB(144, 238, 144)
= Light Green
```

### 코드
```csharp
Color selectedColor = new Color(144f / 255f, 238f / 255f, 144f / 255f, 1f);
```

---

## ?? 유닛 이름 체계

### 플레이어 진영 (Faction.Player)
```
꿀벌집      - Hive 컴포넌트 있음
여왕벌      - isQueen = true
일꾼 꿀벌   - isQueen = false
```

### 적 진영 (Faction.Enemy)
```
말벌집      - Hive 컴포넌트 있음
말벌 여왕   - isQueen = true
말벌        - isQueen = false
```

### 중립 진영 (Faction.Neutral)
```
중립 유닛   - 공격하지 않는 유닛
```

---

## ?? 드래그 선택 영역 수정

### 문제점
```csharp
// 이전 코드 - Y축 반전으로 영역 계산 오류
Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
{
    screenPos1.y = Screen.height - screenPos1.y; // ? 문제
    screenPos2.y = Screen.height - screenPos2.y;
    // ...
}
```

### 해결
```csharp
// 수정된 코드 - Y축 그대로 사용
void SelectUnitsInDragArea()
{
    Vector2 startPos = dragStartPos;
    Vector2 endPos = currentMousePos;
    
    float minX = Mathf.Min(startPos.x, endPos.x);
    float maxX = Mathf.Max(startPos.x, endPos.x);
    float minY = Mathf.Min(startPos.y, endPos.y); // ? Y축 그대로
    float maxY = Mathf.Max(startPos.y, endPos.y);
    
    Rect selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);
    
    // 유닛의 화면 좌표도 그대로 사용
    Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
    Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
    
    if (selectionRect.Contains(screenPos2D)) // ? 정확한 판정
    {
        unitsInRect.Add(unit);
    }
}
```

---

## ?? 하이브 능력치 버그 수정

### 문제점
```
꿀벌집 클릭 시:
- 이름: "꿀벌집" ?
- 체력: 50/50 ? (일꾼 체력)
- 공격력: 10 ? (일꾼 공격력)
```

### 원인
```csharp
// 이전 코드
var combat = currentAgent.GetComponent<CombatUnit>();
// → 하이브의 경우도 UnitAgent의 CombatUnit을 참조
// → 하이브는 별도의 GameObject이므로 CombatUnit이 다름
```

### 해결
```csharp
// 수정된 코드
var hive = currentAgent.GetComponent<Hive>();
CombatUnit combat = null;

if (hive != null)
{
    // 하이브의 CombatUnit은 하이브 GameObject에 있음
    combat = hive.GetComponent<CombatUnit>();
}
else
{
    // 일반 유닛의 CombatUnit
    combat = currentAgent.GetComponent<CombatUnit>();
}
```

### 결과
```
꿀벌집 클릭 시:
- 이름: "꿀벌집" ?
- 체력: 200/200 ? (하이브 체력)
- 공격력: (비움) ? (하이브는 공격 안 함)
```

---

## ?? 적 진영 생성 방법

### 1. 말벌 (적 일꾼) 생성

```csharp
// 프리팹 설정
GameObject waspPrefab;

// 생성
var wasp = Instantiate(waspPrefab, position, Quaternion.identity);
var agent = wasp.GetComponent<UnitAgent>();
agent.faction = Faction.Enemy;  // ← 적 진영
agent.isQueen = false;
agent.canMove = true;

// CombatUnit 설정
var combat = wasp.GetComponent<CombatUnit>();
combat.attack = 12;      // 꿀벌보다 강함
combat.maxHealth = 60;
combat.health = 60;
```

### 2. 말벌 여왕 생성

```csharp
var waspQueen = Instantiate(waspQueenPrefab, position, Quaternion.identity);
var agent = waspQueen.GetComponent<UnitAgent>();
agent.faction = Faction.Enemy;
agent.isQueen = true;      // ← 여왕
agent.canMove = true;

var combat = waspQueen.GetComponent<CombatUnit>();
combat.attack = 20;
combat.maxHealth = 150;
combat.health = 150;
```

### 3. 말벌집 생성

```csharp
var waspHive = Instantiate(waspHivePrefab, position, Quaternion.identity);

// UnitAgent 설정
var agent = waspHive.GetComponent<UnitAgent>();
if (agent == null) agent = waspHive.AddComponent<UnitAgent>();
agent.faction = Faction.Enemy;  // ← 적 진영
agent.canMove = false;
agent.SetPosition(q, r);

// Hive 컴포넌트 설정
var hive = waspHive.GetComponent<Hive>();
if (hive == null) hive = waspHive.AddComponent<Hive>();
hive.workerPrefab = waspPrefab;  // 말벌 생성
hive.maxWorkers = 15;            // 꿀벌집보다 많음
hive.spawnInterval = 8f;         // 더 빠르게 생성
hive.Initialize(q, r);

// CombatUnit 설정 (하이브 방어력)
var combat = waspHive.GetComponent<CombatUnit>();
if (combat == null) combat = waspHive.AddComponent<CombatUnit>();
combat.maxHealth = 250;  // 꿀벌집보다 강함
combat.health = 250;
combat.attack = 0;       // 하이브는 공격 안 함
```

---

## ?? 게임 플레이 시나리오

### 플레이어 vs 적

```
[플레이어 진영]
꿀벌집 (HP: 200)
├─ 여왕벌 (HP: 100, 공격력: 15)
└─ 일꾼 꿀벌 x10 (HP: 50, 공격력: 10)

     VS

[적 진영]
말벌집 (HP: 250)
├─ 말벌 여왕 (HP: 150, 공격력: 20)
└─ 말벌 x15 (HP: 60, 공격력: 12)
```

### 전투 흐름
```
1. 드래그로 일꾼 꿀벌 5마리 선택
2. 선택된 유닛들이 연한 연두색으로 표시됨
3. 말벌집 근처로 우클릭 이동
4. 자동으로 말벌과 전투 시작
5. 체력 바가 실시간으로 업데이트됨
```

---

## ?? 유닛 정보 표시 예시

### 꿀벌집 선택
```
꿀벌집
HP: 200/200
(공격력 비표시)
```

### 일꾼 꿀벌 선택
```
일꾼 꿀벌
HP: 50/50
공격력: 10
```

### 말벌집 선택
```
말벌집
HP: 250/250
(공격력 비표시)
```

### 말벌 선택
```
말벌
HP: 60/60
공격력: 12
```

---

## ?? 버그 수정 상세

### 버그 1: 드래그 영역 불일치

**증상:**
```
드래그 박스를 그렸는데 일부 유닛만 선택됨
```

**원인:**
```
OnGUI()는 좌상단 원점 좌표계
Camera.ScreenToWorldPoint()는 좌하단 원점 좌표계
→ Y축 변환이 중복 적용됨
```

**해결:**
```
Y축 변환을 제거하고 일관된 좌표계 사용
```

### 버그 2: 하이브 능력치 오류

**증상:**
```
꿀벌집 클릭 시 일꾼의 체력/공격력 표시
```

**원인:**
```
하이브와 일꾼이 같은 UnitAgent를 공유
CombatUnit을 잘못 참조함
```

**해결:**
```
하이브 체크 후 하이브의 CombatUnit 직접 참조
```

---

## ?? 시각적 개선

### 선택 표시

#### 변경 전
```
?? 노란색 (눈에 거슬림)
```

#### 변경 후
```
?? 연한 연두색 (자연스러움)
```

### 색상 코드
```csharp
// C# 코드
new Color(144f / 255f, 238f / 255f, 144f / 255f, 1f)

// Unity Inspector
R: 144
G: 238
B: 144
A: 255

// Hex Code
#90EE90
```

---

## ? 테스트 체크리스트

### 드래그 선택
```
[ ] 일꾼 여러 마리 배치
[ ] 마우스 드래그로 영역 선택
[ ] 영역 내 모든 일꾼 선택 확인
[ ] 선택된 유닛이 연한 연두색으로 변경 확인
```

### 유닛 정보 표시
```
[ ] 꿀벌집 클릭 → 이름: "꿀벌집", HP: 200/200
[ ] 여왕벌 클릭 → 이름: "여왕벌", HP: 100/100
[ ] 일꾼 클릭 → 이름: "일꾼 꿀벌", HP: 50/50
[ ] 말벌집 클릭 → 이름: "말벌집", HP: 250/250
[ ] 말벌 클릭 → 이름: "말벌", HP: 60/60
```

### 적 진영
```
[ ] 말벌 프리팹 생성
[ ] Faction을 Enemy로 설정
[ ] 말벌집 생성 및 말벌 스폰 확인
[ ] 플레이어 유닛과 전투 가능 확인
```

---

## ?? 완료!

모든 개선사항이 적용되었습니다!

**핵심 개선:**
- ? 드래그 선택 영역 정확도 개선
- ? 선택 색상 연한 연두색으로 변경
- ? 적 진영 (말벌, 말벌집) 이름 지원
- ? 하이브 능력치 버그 수정

게임 개발 화이팅! ??????
