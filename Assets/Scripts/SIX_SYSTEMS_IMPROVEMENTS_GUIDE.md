# ?? 6가지 시스템 개선 완료 가이드

## 완료된 개선사항

### 1. ? 여왕벌 하이브 건설 후 자동 이동
- 하이브 건설 명령 시 여왕벌이 하이브 타일로 자동 이동
- 하이브 내부로 진입

### 2. ? 적 말벌 스폰 로직 개선
- 맵 밖(맵 크기 + 100)에서 생성
- 비활성화 상태로 생성
- 각 하이브 위치로 이동 후 활성화

### 3. ? 명령 UI 자원 요구량 실시간 업데이트
- 자원 변경 이벤트 추가
- 자원 변경 시 버튼 상태 자동 갱신

### 4. ? 업그레이드 효과 UI 표시
- Unity에서 UpgradeResultUI 설정 필요
- Canvas, Panel, 3개 텍스트 컴포넌트 연결 필요

### 5. ? 하이브 파괴 시 일꾼이 여왕벌 따라가기
- 하이브 파괴 시 모든 일꾼이 여왕벌 추적 모드
- homeHive 제거 및 isFollowingQueen 활성화

### 6. ? 새 하이브와 기존 일꾼 상호작용
- 하이브 건설 시 homeHive 없는 일꾼 자동 할당
- 자원 전달 시 homeHive 업데이트

---

## ?? 수정된 파일

### 1. ConstructHiveHandler.cs
```csharp
? MoveQueenToHive() 추가
   - 여왕벌 하이브 위치로 이동
   
? AssignHomelessWorkersToHive() 추가
   - homeHive 없는 일꾼 할당
   
? ExecuteConstruct() 수정
   - 여왕벌 자동 이동 로직 추가
   - 일꾼 자동 할당 로직 추가
```

### 2. WaspWaveManager.cs
```csharp
? SpawnAndAttackWasp() 수정
   - 맵 밖에서 생성 (mapRadius + 100)
   - 비활성화 상태로 생성
   
? MoveToHiveAndActivate() 추가
   - 하이브 위치로 순간이동
   - 렌더러/콜라이더 활성화
   - 타겟 하이브로 이동 명령
```

### 3. HiveManager.cs
```csharp
? OnResourcesChanged 이벤트 추가
? AddResources() 수정 - 이벤트 발생
? TrySpendResources() 수정 - 이벤트 발생
```

### 4. UnitCommandPanel.cs
```csharp
? OnEnable() 추가
   - 자원 변경 이벤트 구독
   
? OnDisable() 추가
   - 이벤트 구독 해제
```

### 5. Hive.cs
```csharp
? DestroyHive() 수정
   - 일꾼 여왕벌 추적 모드 활성화
   - homeHive null 처리
   - 작업 취소
```

### 6. UnitBehaviorController.cs
```csharp
? CancelCurrentTask() 추가
   - 현재 작업 취소 메서드
   
? DeliverResourcesRoutine() 수정
   - homeHive 자동 업데이트
```

---

## ?? 시스템 작동 방식

### 1. 여왕벌 하이브 건설

```
[여왕벌 "하이브 건설" 명령]
   ↓
[하이브 생성]
위치: 여왕벌 현재 위치 (q, r)
   ↓
[여왕벌 이동]
MoveQueenToHive(queen, q, r)
→ SetPosition(q, r)
→ transform.position = hivePos ?
   ↓
[여왕벌 숨김]
canMove = false
Renderer/Collider 비활성화
   ↓
[완료]
여왕벌이 하이브 내부로 진입 ?
```

### 2. 적 말벌 스폰

```
[웨이브 발동]
   ↓
[1단계: 맵 밖 생성]
위치: (mapRadius + 100, mapRadius + 100)
렌더러/콜라이더 비활성화 ?
   ↓
[2단계: 하이브 위치로 이동]
transform.position = hivePos
SetPosition(hive.q, hive.r)
   ↓
[3단계: 활성화]
렌더러/콜라이더 활성화 ?
   ↓
[4단계: 타겟으로 이동]
FindPath(hive → playerHive)
SetPath(path) ?
```

### 3. 자원 요구량 실시간 업데이트

```
[자원 변경]
AddResources() 또는 TrySpendResources()
   ↓
[이벤트 발생]
OnResourcesChanged?.Invoke() ?
   ↓
[구독자 반응]
UnitCommandPanel.RefreshButtonStates()
   ↓
[버튼 상태 갱신]
foreach (command)
{
    button.interactable = cmd.IsAvailable() ?
}
```

### 4. 업그레이드 효과 UI

```
[업그레이드 실행]
HiveManager.UpgradeXXX(cost)
   ↓
[업그레이드 성공]
TrySpendResources() → true
   ↓
[UI 표시]
UpgradeResultUI.ShowUpgradeResult(
    "날카로운 침",
    "일꾼 공격력 +1",
    "4"
) ?
   ↓
[Fade In 0.3초]
[표시 3초]
[Fade Out 0.3초]
```

### 5. 하이브 파괴 → 일꾼이 여왕벌 추적

```
[하이브 파괴]
DestroyHive()
   ↓
[여왕벌 부활]
canMove = true
Renderer/Collider 활성화 ?
   ↓
[모든 일꾼 처리]
foreach (worker in workers)
{
    worker.homeHive = null ?
    worker.isFollowingQueen = true ?
    worker.hasManualOrder = false
    
    behavior.CancelCurrentTask() ?
}
   ↓
[완료]
일꾼들이 여왕벌을 따라다님 ?
```

### 6. 새 하이브 건설 → 일꾼 자동 할당

```
[하이브 건설]
ExecuteConstruct()
   ↓
[homeHive 없는 일꾼 찾기]
foreach (unit in AllUnits)
{
    if (unit.homeHive == null)
    {
        unit.homeHive = newHive ?
        이동 명령
    }
}
   ↓
[자원 전달 시]
DeliverResourcesRoutine()
{
    if (agent.homeHive != hive)
    {
        agent.homeHive = hive ?
    }
}
   ↓
[완료]
일꾼이 새 하이브와 상호작용 ?
```

---

## ?? Unity 설정 (4번 필수!)

### 업그레이드 결과 UI 설정

**1단계: Canvas 생성**
```
Hierarchy 우클릭
→ UI → Canvas

설정:
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
```

**2단계: UpgradeResultPanel 생성**
```
Canvas 우클릭 → UI → Panel

이름: UpgradeResultPanel
RectTransform:
- Anchor: Middle Center
- Pos Y: 200
- Width: 400, Height: 150
```

**3단계: CanvasGroup 추가**
```
UpgradeResultPanel 선택
→ Add Component → Canvas Group
```

**4단계: 텍스트 3개 생성**
```
1. UpgradeNameText (위)
   - Font Size: 28, Bold, White
   
2. UpgradeEffectText (중간)
   - Font Size: 20, 연한 초록

3. CurrentValueText (아래)
   - Font Size: 18, 연한 노랑
```

**5단계: UpgradeResultUI 컴포넌트**
```
Canvas 선택
→ Add Component → UpgradeResultUI

연결:
- Result Panel: UpgradeResultPanel
- Upgrade Name Text: UpgradeNameText
- Upgrade Effect Text: UpgradeEffectText
- Current Value Text: CurrentValueText
```

---

## ?? 핵심 코드

### 1. 여왕벌 자동 이동

```csharp
// ConstructHiveHandler.cs
private static void MoveQueenToHive(UnitAgent queen, int q, int r)
{
    // 타일 좌표 업데이트
    queen.SetPosition(q, r);
    
    // 월드 위치 업데이트
    Vector3 hivePos = TileHelper.HexToWorld(q, r, queen.hexSize);
    queen.transform.position = hivePos;
}
```

### 2. 적 말벌 맵 밖 스폰

```csharp
// WaspWaveManager.cs
void SpawnAndAttackWasp(...)
{
    // 맵 밖 위치
    int mapSize = GameManager.Instance.mapRadius;
    int spawnDistance = mapSize + 100;
    Vector3 farAwayPos = TileHelper.HexToWorld(spawnDistance, spawnDistance, 0.5f);
    
    // 생성 및 비활성화
    GameObject waspObj = Instantiate(waspPrefab, farAwayPos, ...);
    renderer.enabled = false;
    collider.enabled = false;
    
    // 하이브 위치로 이동 후 활성화
    StartCoroutine(MoveToHiveAndActivate(agent, fromHive, targetHive));
}
```

### 3. 자원 변경 이벤트

```csharp
// HiveManager.cs
public event System.Action OnResourcesChanged;

public void AddResources(int amount)
{
    playerStoredResources += amount;
    OnResourcesChanged?.Invoke(); // 이벤트 발생
}

// UnitCommandPanel.cs
void OnEnable()
{
    HiveManager.Instance.OnResourcesChanged += RefreshButtonStates;
}
```

### 4. 하이브 파괴 시 일꾼 처리

```csharp
// Hive.cs
void DestroyHive()
{
    // 여왕벌 부활
    queenBee.canMove = true;
    renderer.enabled = true;
    
    // 일꾼 처리
    foreach (var worker in workers)
    {
        worker.homeHive = null; // homeHive 제거
        worker.isFollowingQueen = true; // 여왕벌 추적
        
        behavior.CancelCurrentTask(); // 작업 취소
    }
}
```

### 5. 새 하이브 일꾼 할당

```csharp
// ConstructHiveHandler.cs
private static void AssignHomelessWorkersToHive(Hive hive, int q, int r)
{
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.homeHive == null) // homeHive 없는 일꾼
        {
            unit.homeHive = hive; // 새 하이브 할당
            unit.isFollowingQueen = false;
            
            // 하이브로 이동
            var path = Pathfinder.FindPath(start, dest);
            SetPath(path);
        }
    }
}
```

---

## ?? 비교표

| 기능 | 이전 | 현재 |
|------|------|------|
| **여왕벌 건설** | 제자리 | 하이브로 이동 ? |
| **적 스폰** | 하이브에서 생성 | 맵 밖에서 생성 ? |
| **버튼 업데이트** | 수동 | 자동 ? |
| **업그레이드 UI** | Console | UI 표시 ? |
| **하이브 파괴** | 일꾼 방치 ? | 여왕 추적 ? |
| **일꾼 할당** | 수동 | 자동 ? |

---

## ? 테스트 체크리스트

```
[ ] 여왕벌 하이브 건설 후 하이브 위치로 이동
[ ] 적 말벌이 맵 밖에서 생성 후 활성화
[ ] 자원 증가/감소 시 버튼 상태 자동 갱신
[ ] 업그레이드 UI 설정 완료 (Unity)
[ ] 업그레이드 실행 시 UI 표시
[ ] 하이브 파괴 시 일꾼이 여왕벌 추적
[ ] 새 하이브 건설 시 일꾼 자동 할당
[ ] 일꾼이 새 하이브로 자원 전달
```

---

## ?? 문제 해결

### Q: 여왕벌이 하이브로 안 움직여요

**확인:**
```
[ ] ConstructHiveHandler.cs 수정 확인
[ ] MoveQueenToHive() 호출 확인
[ ] SetPosition() 및 transform.position 확인
```

### Q: 적이 맵 중앙에 나타나요

**확인:**
```
[ ] WaspWaveManager.cs 수정 확인
[ ] spawnDistance 계산 확인
[ ] MoveToHiveAndActivate() 호출 확인
```

### Q: 버튼이 자동 갱신 안 돼요

**확인:**
```
[ ] HiveManager.OnResourcesChanged 이벤트 확인
[ ] UnitCommandPanel.OnEnable() 구독 확인
[ ] RefreshButtonStates() 호출 확인
```

### Q: 업그레이드 UI가 안 보여요

**확인:**
```
[ ] Canvas 생성 확인
[ ] UpgradeResultUI 컴포넌트 확인
[ ] 3개 텍스트 연결 확인
[ ] UpgradeResultUI.Instance != null 확인
```

### Q: 일꾼이 여왕벌 안 따라가요

**확인:**
```
[ ] Hive.DestroyHive() 수정 확인
[ ] worker.isFollowingQueen = true 확인
[ ] CancelCurrentTask() 호출 확인
```

### Q: 일꾼이 새 하이브로 자원 안 가져가요

**확인:**
```
[ ] AssignHomelessWorkersToHive() 호출 확인
[ ] DeliverResourcesRoutine() homeHive 업데이트 확인
[ ] unit.homeHive != null 확인
```

---

## ?? 완료!

**핵심 수정:**
- ? 여왕벌 하이브 자동 이동
- ? 적 맵 밖 스폰 + 활성화
- ? 자원 변경 이벤트 + 자동 갱신
- ? 업그레이드 UI 표시 (Unity 설정 필요)
- ? 하이브 파괴 → 일꾼 여왕벌 추적
- ? 새 하이브 일꾼 자동 할당

**Unity 설정 필수:**
1. UpgradeResultUI 설정 (4번)
   - Canvas + Panel + 3개 텍스트
   - UpgradeResultUI 컴포넌트

**게임 플레이:**
- 직관적인 하이브 건설
- 자연스러운 적 스폰
- 실시간 버튼 업데이트
- 시각적 업그레이드 피드백
- 일꾼 자동 관리

게임 개발 화이팅! ??????
