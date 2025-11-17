# ?? 6가지 시스템 개선 완료 가이드

## 완료된 개선사항

### 1. ? 하이브 파괴 시 경계선 즉시 제거
- DestroyHive()에서 HexBoundaryHighlighter.Clear() 호출

### 2. ? 꿀벌 전투 시 활동 범위 제한
- MoveAndAttack()에서 범위 체크
- WaitAndCombatRoutine()에서 실시간 범위 체크

### 3. ? 유닛 체력 실시간 반영
- CombatUnit.OnStatsChanged 이벤트 추가
- UnitCommandPanel에서 이벤트 구독

### 4. ? 공격력 실시간 반영
- SetAttack() 메서드로 이벤트 발생
- 업그레이드 시 자동 UI 업데이트

### 5. ? 현재 일꾼 수 표시
- UnitCommandPanel에 workerCountText 추가
- 여왕벌 선택 시 일꾼 수 표시

### 6. ? 적이 하이브 우선 공격
- FindNearestPlayerUnit()에서 하이브 우선 탐지

---

## ?? 수정된 파일

### 1. Hive.cs
```csharp
? DestroyHive() 수정
   - HexBoundaryHighlighter.Clear() 추가
```

### 2. UnitBehaviorController.cs
```csharp
? MoveAndAttack() 수정
   - 활동 범위 체크 추가
   
? WaitAndCombatRoutine() 수정
   - 전투 중 범위 체크
   - 범위 이탈 시 하이브 복귀
   
? ReturnToHive() 추가
   - 하이브로 복귀 메서드
```

### 3. CombatUnit.cs
```csharp
? OnStatsChanged 이벤트 추가
? TakeDamage() 수정 - 이벤트 발생
? SetHealth() 추가
? SetMaxHealth() 추가
? SetAttack() 추가
```

### 4. HiveManager.cs
```csharp
? UpdateAllWorkerCombat() 수정
   - SetAttack(), SetMaxHealth(), SetHealth() 사용
```

### 5. UnitCommandPanel.cs
```csharp
? workerCountText 필드 추가
? SubscribeToEvents() 추가
? UnsubscribeFromEvents() 추가
? UpdateUnitInfo() 수정 - 일꾼 수 표시
```

### 6. EnemyAI.cs
```csharp
? FindNearestPlayerUnit() 수정
   - 하이브 우선 탐지
```

---

## ?? 시스템 작동 방식

### 1. 하이브 파괴 시 경계선 제거

```
[하이브 파괴]
DestroyHive()
   ↓
[경계선 제거] ?
HexBoundaryHighlighter.Clear()
   ↓
[여왕벌 부활]
queenBee.canMove = true
renderer.enabled = true
   ↓
[일꾼 여왕벌 추적]
worker.isFollowingQueen = true
```

### 2. 꿀벌 전투 시 활동 범위 제한

```
[전투 명령]
MoveAndAttack(enemy)
   ↓
[범위 체크] ?
if (distanceToHive > activityRadius)
{
    공격 중단
    return
}
   ↓
[전투 시작]
WaitAndCombatRoutine()
   ↓
[실시간 범위 체크] ?
while (전투 중)
{
    if (distanceToHive > activityRadius)
    {
        전투 중단
        하이브 복귀 ?
    }
}
```

### 3, 4. 체력/공격력 실시간 반영

```
[체력 감소]
combat.TakeDamage(damage)
   ↓
[이벤트 발생] ?
OnStatsChanged?.Invoke()
   ↓
[UI 자동 업데이트] ?
UnitCommandPanel.UpdateUnitInfo()
   ↓
[화면에 즉시 표시]
HP: 45/50 (실시간)
```

### 5. 일꾼 수 표시

```
[여왕벌 선택]
UnitCommandPanel.Show(queen)
   ↓
[일꾼 수 계산] ?
foreach (unit in AllUnits)
{
    if (Player && !isQueen)
        workerCount++
}
   ↓
[UI 표시] ?
workerCountText.text = "일꾼 수: 5"
```

### 6. 적이 하이브 우선 공격

```
[적 AI 타겟 탐색]
FindNearestPlayerUnit()
   ↓
[하이브 우선 탐지] ?
foreach (unit in AllUnits)
{
    if (unit.GetComponent<Hive>() != null)
    {
        nearestHive = unit  // 최우선 ?
    }
    else
    {
        nearest = unit  // 일반 유닛
    }
}
   ↓
[하이브 우선 반환] ?
if (nearestHive != null)
    return nearestHive
else
    return nearest
```

---

## ?? 핵심 코드

### 1. 경계선 즉시 제거

```csharp
// Hive.cs - DestroyHive()
void DestroyHive()
{
    // 경계선 즉시 제거 ?
    if (HexBoundaryHighlighter.Instance != null)
    {
        HexBoundaryHighlighter.Instance.Clear();
    }
    
    // 여왕벌 부활...
}
```

### 2. 활동 범위 제한

```csharp
// UnitBehaviorController.cs

// 전투 시작 전 체크
void MoveAndAttack(UnitAgent enemy)
{
    if (agent.homeHive != null)
    {
        int distanceToHive = Pathfinder.AxialDistance(
            agent.homeHive.q, agent.homeHive.r, 
            enemy.q, enemy.r
        );
        
        if (distanceToHive > activityRadius)
        {
            Debug.Log("적이 활동 범위 밖");
            currentTask = Idle;
            return; ?
        }
    }
    
    // 전투 시작...
}

// 전투 중 체크
IEnumerator WaitAndCombatRoutine(...)
{
    while (전투 중)
    {
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(...);
            
            if (distanceToHive > activityRadius)
            {
                Debug.Log("활동 범위 벗어남, 복귀");
                ReturnToHive(); ?
                yield break;
            }
        }
        
        // 공격...
    }
}
```

### 3. 체력/공격력 이벤트

```csharp
// CombatUnit.cs
public event System.Action OnStatsChanged;

public void TakeDamage(int dmg)
{
    health -= dmg;
    OnStatsChanged?.Invoke(); ?
    
    if (health <= 0) Die();
}

public void SetAttack(int newAttack)
{
    attack = newAttack;
    OnStatsChanged?.Invoke(); ?
}

public void SetMaxHealth(int newMaxHealth)
{
    maxHealth = newMaxHealth;
    OnStatsChanged?.Invoke(); ?
}
```

### 4. UI 이벤트 구독

```csharp
// UnitCommandPanel.cs
void SubscribeToEvents(UnitAgent agent)
{
    var combat = agent.GetComponent<CombatUnit>();
    if (combat != null)
    {
        combat.OnStatsChanged += UpdateUnitInfo; ?
    }
}

void UnsubscribeFromEvents(UnitAgent agent)
{
    var combat = agent.GetComponent<CombatUnit>();
    if (combat != null)
    {
        combat.OnStatsChanged -= UpdateUnitInfo; ?
    }
}
```

### 5. 일꾼 수 표시

```csharp
// UnitCommandPanel.cs - UpdateUnitInfo()
if (currentAgent.faction == Faction.Player && currentAgent.isQueen)
{
    // 여왕벌인 경우, 일꾼 수 카운트 ?
    int workerCount = 0;
    foreach (var unit in FindObjectsOfType<UnitAgent>())
    {
        if (unit.faction == Faction.Player && !unit.isQueen)
            workerCount++;
    }
    
    if (workerCountText != null)
        workerCountText.text = $"일꾼 수: {workerCount}"; ?
}
```

### 6. 하이브 우선 공격

```csharp
// EnemyAI.cs - FindNearestPlayerUnit()
UnitAgent nearestHive = null; // 하이브 ?
UnitAgent nearest = null; // 일반 유닛
int minHiveDistance = int.MaxValue;
int minDistance = int.MaxValue;

foreach (var unit in AllUnits)
{
    // 하이브 체크 ?
    var hive = unit.GetComponent<Hive>();
    if (hive != null)
    {
        if (distance < minHiveDistance)
        {
            minHiveDistance = distance;
            nearestHive = unit; ?
        }
    }
    else
    {
        // 일반 유닛
        if (distance < minDistance)
        {
            minDistance = distance;
            nearest = unit;
        }
    }
}

// 하이브 우선 반환 ?
if (nearestHive != null)
{
    Debug.Log("하이브 발견! 우선 공격");
    return nearestHive;
}

return nearest;
```

---

## ?? Unity 설정 (5번 필수!)

### 일꾼 수 UI 설정

**1단계: TextMeshProUGUI 추가**
```
UnitCommandPanel 하위
→ UnitInfoPanel
   → WorkerCountText (TextMeshProUGUI) 추가
```

**2단계: 속성 설정**
```
Text: "일꾼 수: 0"
Font Size: 14
Color: Yellow (255, 255, 100)
Alignment: Center
```

**3단계: Inspector 연결**
```
UnitCommandPanel 컴포넌트
→ Worker Count Text: WorkerCountText 연결
```

---

## ?? 비교표

| 기능 | 이전 | 현재 |
|------|------|------|
| **경계선 제거** | 수동/남아있음 | 자동 즉시 제거 ? |
| **전투 범위** | 무제한 | 활동 범위 제한 ? |
| **체력 UI** | Update 루프 | 이벤트 기반 ? |
| **공격력 UI** | Update 루프 | 이벤트 기반 ? |
| **일꾼 수** | 표시 없음 | 실시간 표시 ? |
| **적 우선순위** | 일반 유닛 | 하이브 우선 ? |

---

## ? 테스트 체크리스트

```
[ ] 하이브 파괴 시 경계선 즉시 사라짐
[ ] 꿀벌이 범위 밖 적 공격 안 함
[ ] 전투 중 범위 이탈 시 하이브 복귀
[ ] 체력 감소 시 UI 즉시 업데이트
[ ] 공격력 업그레이드 시 UI 즉시 업데이트
[ ] 여왕벌 선택 시 일꾼 수 표시
[ ] 적이 하이브 있으면 하이브 우선 공격
[ ] 하이브 없으면 일반 유닛 공격
```

---

## ?? 문제 해결

### Q: 경계선이 안 사라져요

**확인:**
```
[ ] HexBoundaryHighlighter.Instance != null
[ ] Clear() 호출 확인
[ ] DestroyHive() 실행 확인
```

### Q: 꿀벌이 범위 밖으로 나가요

**확인:**
```
[ ] agent.homeHive != null
[ ] activityRadius 값 확인
[ ] Pathfinder.AxialDistance() 작동
```

### Q: UI가 실시간으로 안 바뀌어요

**확인:**
```
[ ] OnStatsChanged 이벤트 발생
[ ] SubscribeToEvents() 호출
[ ] UpdateUnitInfo() 실행
```

### Q: 일꾼 수가 안 나와요

**확인:**
```
[ ] WorkerCountText 연결 확인
[ ] 여왕벌 선택 확인
[ ] FindObjectsOfType() 작동
```

### Q: 적이 하이브를 안 공격해요

**확인:**
```
[ ] GetComponent<Hive>() 확인
[ ] nearestHive != null 확인
[ ] showDebugLogs = true 로그 확인
```

---

## ?? 추가 개선 아이디어

### 1. 범위 경고 UI

```csharp
// 활동 범위 이탈 경고
if (distanceToHive > activityRadius * 0.8f)
{
    ShowWarning("활동 범위 경계!");
}
```

### 2. 체력 바 색상

```csharp
// 체력에 따라 색상 변경
float healthPercent = (float)health / maxHealth;

if (healthPercent > 0.6f)
    healthBar.color = Color.green;
else if (healthPercent > 0.3f)
    healthBar.color = Color.yellow;
else
    healthBar.color = Color.red;
```

### 3. 일꾼 수 변경 알림

```csharp
// 일꾼 수 변경 시 애니메이션
if (workerCount != previousWorkerCount)
{
    workerCountText.GetComponent<Animator>().SetTrigger("Change");
}
```

### 4. 하이브 공격 우선순위 표시

```csharp
// 적 AI가 하이브 공격 시 이펙트
if (target.GetComponent<Hive>() != null)
{
    ShowTargetIndicator(target, Color.red);
}
```

---

## ?? 완료!

**핵심 수정:**
- ? 하이브 파괴 시 경계선 즉시 제거
- ? 꿀벌 전투 시 활동 범위 제한
- ? 체력 실시간 반영 (이벤트)
- ? 공격력 실시간 반영 (이벤트)
- ? 일꾼 수 실시간 표시
- ? 적이 하이브 우선 공격

**Unity 설정 필수:**
- WorkerCountText 컴포넌트 추가 및 연결

**게임 플레이:**
- 직관적인 시각적 피드백
- 전략적 전투 (범위 제한)
- 실시간 정보 표시
- 적의 지능적 행동

게임 개발 화이팅! ??????
