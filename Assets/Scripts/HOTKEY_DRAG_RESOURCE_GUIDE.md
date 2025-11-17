# ?? 핫키, 드래그 선택, 자원 채취 개선 가이드

## 완료된 개선사항

### 1. ? 1번 키로 하이브/여왕벌 선택
- 1번 키 누르면 하이브 자동 선택
- 하이브 없으면 여왕벌 선택
- 빠른 거점 접근

### 2. ? 드래그 선택 해제 개선
- 좌클릭으로 다른 유닛 클릭 시 드래그 선택 해제
- 빈 곳 클릭 시도 드래그 선택 해제
- 직관적인 선택 관리

### 3. ? 자원 채취 로직 수정
- 이동 속도 영향 받도록 수정
- 하이브 도착 후 자원 전달
- 타임아웃 처리 추가

### 4. ? Y축 좌표 사용
- X, Y 평면 사용 (2D)
- Z축 0으로 고정
- 올바른 2D 좌표계

---

## ?? 생성/수정된 파일

### 1. HotkeyManager.cs (신규)
```csharp
? SelectHiveOrQueen() - 1번 키 처리
? FindPlayerHive() - 플레이어 하이브 찾기
? FindPlayerQueen() - 플레이어 여왕벌 찾기
? SelectHive() - 하이브 선택
? SelectQueen() - 여왕벌 선택
```

### 2. DragSelector.cs
```csharp
? HandleSingleClick() 수정
   - 다른 유닛 클릭 시 드래그 선택 해제
   
? DeselectAll() public으로 변경
   - 외부 접근 가능
```

### 3. TileClickMover.cs
```csharp
? SelectUnit() public으로 변경
? DeselectUnit() public으로 변경
```

### 4. UnitBehaviorController.cs
```csharp
? WaitAndGatherRoutine() 수정
   - 자원 채취 후 즉시 경로 설정
   
? DeliverResourcesRoutine() 수정
   - 하이브 도착 후 자원 전달
   - 타임아웃 처리 (10초)
   - 이동 중단 감지
```

### 5. TileHelper.cs
```csharp
? GetRandomPositionInTile() 수정
   - randomY 사용 (Z 대신)
   - 2D 평면 (X, Y, 0)
   
? IsPositionInTile() 수정
   - Vector2 거리 계산
```

---

## ?? 시스템 작동 방식

### 1. 1번 키 선택

```
[1번 키 입력]
   ↓
하이브 있음?
   ├─ Yes → 하이브 선택 ?
   │         - UI 패널 표시
   │         - 경계선 표시
   └─ No  → 여왕벌 찾기
             ├─ 있음 → 여왕벌 선택 ?
             └─ 없음 → 알림
```

### 2. 드래그 선택 해제

```
[드래그로 일꾼 3마리 선택]
   ↓
좌클릭:
   ├─ 일꾼 클릭 → 유지 ?
   ├─ 하이브 클릭 → 모두 해제 ?
   ├─ 여왕벌 클릭 → 모두 해제 ?
   └─ 빈 곳 클릭 → 모두 해제 ?
```

### 3. 자원 채취 수정

#### 이전 (문제)
```
[자원 타일 도착]
   ↓
자원 채취
   ↓
MoveWithinCurrentTile() ← 빠른 이동!
   ↓
[하이브로 즉시 순간이동?]
   ↓
자원 전달 안 됨 ?
```

#### 현재 (수정)
```
[자원 타일 도착]
   ↓
타일 내부 이동 (0.3초)
   ↓
자원 채취
   ↓
SetPath(pathBack) ← 정상 이동 ?
   ↓
[하이브 도착 대기]
while (거리 > 0.15f)
   ↓
[하이브 도착]
   ↓
자원 전달 ?
```

### 4. Y축 좌표 사용

```
[이전: Z축 사용]
Vector3(x, y, z)
       ↑  ↑  ↑
     위치 높이 깊이

[현재: Y축 사용 (2D)]
Vector3(x, y, 0)
       ↑  ↑  ↑
     좌우 상하 고정

2D 게임에 적합 ?
```

---

## ?? Unity 설정 (필수!)

### 1. HotkeyManager 생성

```
Hierarchy:
1. 빈 GameObject 생성
2. 이름: "HotkeyManager"
3. HotkeyManager 컴포넌트 추가
```

### 2. 테스트

```
[게임 실행]
   ↓
1번 키 누르기
   ↓
하이브 선택 확인 ?
또는
여왕벌 선택 확인 ?
```

---

## ?? 핵심 코드

### 1번 키 선택

```csharp
// HotkeyManager.cs
void Update()
{
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        SelectHiveOrQueen();
    }
}

void SelectHiveOrQueen()
{
    Hive playerHive = FindPlayerHive();
    
    if (playerHive != null)
    {
        SelectHive(playerHive); // 하이브 우선
    }
    else
    {
        UnitAgent queen = FindPlayerQueen();
        if (queen != null)
        {
            SelectQueen(queen); // 하이브 없으면 여왕벌
        }
    }
}
```

### 드래그 선택 해제

```csharp
// DragSelector.cs
void HandleSingleClick()
{
    bool clickedOnUnit = false;
    
    // Raycast로 유닛 클릭 확인
    var unit = hit.collider.GetComponentInParent<UnitAgent>();
    if (unit != null && unit.faction == Faction.Player && !unit.isQueen)
    {
        clickedOnUnit = true; // 일꾼 클릭
    }
    
    // 일꾼 아닌 것 클릭하면 드래그 선택 해제
    if (selectedUnits.Count > 0 && !clickedOnUnit)
    {
        DeselectAll(); ?
    }
}
```

### 자원 채취 수정

```csharp
// UnitBehaviorController.cs
IEnumerator WaitAndGatherRoutine(HexTile dest)
{
    // 도착 대기
    while (Distance > 0.1f)
        yield return null;
    
    // 타일 내부 이동
    mover.MoveWithinCurrentTile();
    yield return new WaitForSeconds(0.3f);
    
    // 자원 채취
    int taken = dest.TakeResource(gatherAmount);
    
    // 경로 설정 (정상 이동)
    var pathBack = Pathfinder.FindPath(dest, hiveTile);
    mover.SetPath(pathBack); ?
    
    // 자원 전달 루틴
    StartCoroutine(DeliverResourcesRoutine(hive, taken));
}

IEnumerator DeliverResourcesRoutine(Hive hive, int amount)
{
    Vector3 hivePos = TileHelper.HexToWorld(hive.q, hive.r, hexSize);
    
    // 하이브 도착 대기 (타임아웃 10초)
    float timeout = 0f;
    while (Distance > 0.15f && timeout < 10f)
    {
        timeout += Time.deltaTime;
        yield return null;
    }
    
    // 자원 전달
    HiveManager.Instance.AddResources(amount); ?
    
    // 다시 채취 or Idle
    if (targetTile.resourceAmount > 0)
    {
        yield return new WaitForSeconds(gatherCooldown);
        MoveAndGather(targetTile);
    }
}
```

### Y축 좌표 사용

```csharp
// TileHelper.cs
public static Vector3 GetRandomPositionInTile(int q, int r, float hexSize, float margin)
{
    Vector3 center = HexToWorld(q, r, hexSize);
    
    // 각도 및 거리 계산
    float angle = ...;
    float distance = ...;
    
    // X, Y 평면 계산 (2D)
    float randomX = Mathf.Cos(angle) * distance;
    float randomY = Mathf.Sin(angle) * distance; ?
    
    // Z는 0으로 고정
    return new Vector3(center.x + randomX, center.y + randomY, 0f); ?
}
```

---

## ?? 비교표

### 하이브/여왕벌 선택

| 방법 | 이전 | 현재 |
|------|------|------|
| **선택** | 클릭 찾기 | 1번 키 ? |
| **속도** | 느림 | 빠름 ? |
| **우선순위** | 없음 | 하이브 > 여왕벌 ? |

### 드래그 선택

| 상황 | 이전 | 현재 |
|------|------|------|
| **다른 유닛 클릭** | 유지 | 해제 ? |
| **빈 곳 클릭** | 유지 | 해제 ? |
| **일꾼 클릭** | 유지 | 유지 ? |

### 자원 채취

| 요소 | 이전 | 현재 |
|------|------|------|
| **이동 속도** | 영향 없음 ? | 영향 있음 ? |
| **자원 전달** | 즉시 (버그) | 도착 후 ? |
| **타임아웃** | 없음 | 10초 ? |

### 좌표계

| 축 | 이전 | 현재 |
|------|------|------|
| **X축** | 좌우 | 좌우 ? |
| **Y축** | 높이 | 상하 ? |
| **Z축** | 깊이 (사용) | 고정 0 ? |

---

## ?? 문제 해결

### Q: 1번 키가 안 눌려요

**확인:**
```
[ ] HotkeyManager GameObject 존재
[ ] HotkeyManager 컴포넌트 활성화
[ ] HiveManager.Instance != null
[ ] TileManager.Instance != null
```

### Q: 드래그 선택이 해제 안 돼요

**확인:**
```
[ ] DragSelector.Instance != null
[ ] DeselectAll() public 확인
[ ] HandleSingleClick() 호출 확인
```

### Q: 자원이 여전히 안 들어와요

**확인:**
```
[ ] Console에서 "[자원 채취]" 로그 확인
[ ] "[자원 전달]" 로그 확인
[ ] HiveManager.playerStoredResources 값 확인
[ ] 타임아웃 10초 내 도착 확인
```

### Q: 유닛이 여전히 Z축으로 움직여요

**확인:**
```
[ ] TileHelper.GetRandomPositionInTile() 확인
[ ] return new Vector3(x, y, 0f) 확인
[ ] randomY 사용 확인 (randomZ 아님)
```

---

## ? 테스트 체크리스트

```
[ ] HotkeyManager GameObject 생성
[ ] 1번 키 눌러서 하이브 선택
[ ] 하이브 파괴 후 1번 키로 여왕벌 선택
[ ] 드래그로 일꾼 3마리 선택
[ ] 좌클릭으로 하이브 클릭 → 드래그 선택 해제
[ ] 좌클릭으로 빈 곳 클릭 → 드래그 선택 해제
[ ] 일꾼이 자원 채취
[ ] 일꾼이 정상 속도로 하이브 이동
[ ] 하이브 도착 후 자원 증가 확인
[ ] 유닛이 X, Y 평면에서 이동 (Z=0)
```

---

## ?? 완료!

**핵심 기능:**
- ? 1번 키로 하이브/여왕벌 선택
- ? 드래그 선택 해제 개선
- ? 자원 채취 로직 수정
- ? Y축 좌표 사용 (2D)

**Unity 설정 필수:**
1. HotkeyManager GameObject 생성
2. HotkeyManager 컴포넌트 추가

**게임 플레이:**
- 빠른 하이브 접근 (1번 키)
- 직관적인 선택 관리
- 정확한 자원 전달
- 올바른 2D 좌표계

게임 개발 화이팅! ??????
