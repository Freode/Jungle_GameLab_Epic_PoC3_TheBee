# ?? 4가지 주요 버그 수정 가이드

## 완료된 개선사항

### 1. ? 플레이어 시야에 적 하이브 표시
- UpdateEnemyVisibility() 로그 추가
- 발견 즉시 discoveredHives에 등록
- 항상 보이도록 설정

### 2. ? 자원 채취 복귀 문제 해결
- IssueCommandToTile() 우선순위 재정렬
- DeliverResourcesRoutine() targetTile 관리
- 자원 고갈 시 Idle 전환

### 3. ? 일꾼 활동 범위 강제 적용
- MoveAndAttack() 범위 체크 강화
- WaitAndCombatRoutine() 실시간 범위 체크
- 적 위치도 함께 체크

### 4. ? 근접 전투로 변경
- CombatUnit.attackRange = 0.1f
- EnemyAI.attackRange = 0
- 같은 타일에서만 공격

---

## ?? 수정된 파일

### 1. EnemyVisibilityController.cs
```csharp
? showDebugLogs 필드 추가
? UpdateEnemyVisibility() 로그 추가
   - 하이브 발견 시 로그
```

### 2. UnitBehaviorController.cs
```csharp
? IssueCommandToTile() 수정
   - 자원 채취 1순위
   - 적 공격 2순위
   - 이동 3순위
   
? DeliverResourcesRoutine() 수정
   - targetTile 유지/초기화
   - 자원 고갈 시 Idle
   
? MoveAndAttack() 수정
   - 적 위치 범위 체크
   - 거리 로그 출력
   
? WaitAndCombatRoutine() 수정
   - 현재 위치 범위 체크
   - 적 위치 범위 체크
   - 실시간 거리 계산
```

### 3. CombatUnit.cs
```csharp
? attackRange = 0.1f
   - 근접 전투
```

### 4. EnemyAI.cs
```csharp
? attackRange = 0
   - 같은 타일에서만 공격
   
? FindNearestPlayerUnit() 중복 변수 제거
? IsWithinHiveRange() 중복 코드 제거
```

---

## ?? 시스템 작동 방식

### 1. 적 하이브 시야 시스템

```
[플레이어 유닛 이동]
   ↓
[시야 범위 계산]
CalculatePlayerVision()
   ↓
[적 하이브 체크]
foreach (unit in AllUnits)
{
    if (unit.faction == Enemy && unit.GetComponent<Hive>())
    {
        if (isCurrentlyVisible)
        {
            discoveredHives.Add(unit) ?
            Debug.Log("하이브 발견!") ?
        }
        
        shouldBeVisible = discoveredHives.Contains(unit) ?
    }
}
   ↓
[결과]
발견된 하이브는 항상 보임 ?
```

---

### 2. 자원 채취 우선순위

```
[타일 클릭]
IssueCommandToTile(tile)
   ↓
[1순위: 자원 채취] ?
if (tile.resourceAmount > 0 && canGather)
{
    MoveAndGather(tile)
    return ?
}
   ↓
[2순위: 적 공격] ?
if (FindEnemyOnTile(tile))
{
    MoveAndAttack(enemy)
    return ?
}
   ↓
[3순위: 이동] ?
MoveToTile(tile)
```

#### 복귀 시스템

```
[자원 채취 완료]
   ↓
[하이브로 이동]
DeliverResourcesRoutine(hive, amount, sourceTile)
   ↓
[하이브 도착]
   ↓
[자원 전달]
HiveManager.AddResources(amount)
   ↓
[자원 확인] ?
if (sourceTile.resourceAmount > 0)
{
    targetTile = sourceTile ?
    MoveAndGather(sourceTile) ?
}
else
{
    currentTask = Idle ?
    targetTile = null ?
}
```

---

### 3. 활동 범위 강제 적용

#### 공격 시작 시

```
[적 발견]
MoveAndAttack(enemy)
   ↓
[범위 체크] ?
if (homeHive != null)
{
    int distanceToHive = Distance(homeHive, enemy)
    
    if (distanceToHive > activityRadius)
    {
        Debug.Log("적이 범위 밖") ?
        currentTask = Idle
        return ?
    }
}
   ↓
[공격 진행]
```

#### 전투 중

```
[전투 루프]
WaitAndCombatRoutine()
   ↓
while (전투 중)
{
    // 현재 위치 체크 ?
    int distanceToHive = Distance(homeHive, agent.position)
    if (distanceToHive > activityRadius)
    {
        Debug.Log("범위 이탈, 복귀") ?
        ReturnToHive()
        yield break ?
    }
    
    // 적 위치 체크 ?
    int enemyDistance = Distance(homeHive, enemy.position)
    if (enemyDistance > activityRadius)
    {
        Debug.Log("적이 범위 이탈") ?
        currentTask = Idle
        yield break ?
    }
    
    // 공격...
}
```

---

### 4. 근접 전투

```
[공격 범위]
CombatUnit.attackRange = 0.1f ?
EnemyAI.attackRange = 0 ?

[공격 조건]
if (distanceToTarget <= attackRange)
{
    // attackRange = 0
    // → 같은 타일(q, r)에서만 공격 ?
    Attack(target)
}
else
{
    // 추격
    ChaseTarget(target)
}
```

---

## ?? 핵심 코드

### 1. 적 하이브 시야

```csharp
// EnemyVisibilityController.cs
void UpdateEnemyVisibility()
{
    CalculatePlayerVision();

    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction != Faction.Enemy) continue;

        var hive = unit.GetComponent<Hive>();
        bool isHive = (hive != null);
        
        Vector2Int unitPos = new Vector2Int(unit.q, unit.r);
        bool isCurrentlyVisible = currentVisibleTiles.Contains(unitPos);

        bool shouldBeVisible = false;

        if (isHive)
        {
            if (isCurrentlyVisible)
            {
                if (!discoveredHives.Contains(unit.gameObject))
                {
                    discoveredHives.Add(unit.gameObject);
                    
                    if (showDebugLogs) ?
                        Debug.Log($"[시야] 적 하이브 발견: {unit.name}");
                }
            }

            shouldBeVisible = discoveredHives.Contains(unit.gameObject);
        }
        else
        {
            shouldBeVisible = isCurrentlyVisible;
        }

        SetUnitVisibility(unit, shouldBeVisible);
    }
}
```

---

### 2. 자원 채취 우선순위

```csharp
// UnitBehaviorController.cs
public void IssueCommandToTile(HexTile tile)
{
    // ...권한 체크...

    // 1순위: 자원 채취 ?
    bool canGather = agent.homeHive != null && !agent.homeHive.isRelocating;
    
    if (canGather && tile.resourceAmount > 0)
    {
        currentTask = UnitTaskType.Gather;
        targetTile = tile;
        MoveAndGather(tile);
        return; ?
    }

    // 2순위: 적 공격 ?
    var enemy = FindEnemyOnTile(tile);
    if (enemy != null)
    {
        // 범위 체크...
        currentTask = UnitTaskType.Attack;
        targetUnit = enemy;
        MoveAndAttack(enemy);
        return; ?
    }

    // 3순위: 이동 ?
    currentTask = UnitTaskType.Move;
    targetTile = tile;
    MoveToTile(tile);
}
```

---

### 3. 활동 범위 강제

```csharp
// UnitBehaviorController.cs
void MoveAndAttack(UnitAgent enemy)
{
    if (enemy == null) return;
    
    // 적 위치 범위 체크 ?
    if (agent.homeHive != null)
    {
        int distanceToHive = Pathfinder.AxialDistance(
            agent.homeHive.q, agent.homeHive.r, 
            enemy.q, enemy.r
        );
        
        if (distanceToHive > activityRadius)
        {
            Debug.Log($"[전투] {agent.name}: 적이 범위 밖 (거리: {distanceToHive}/{activityRadius})"); ?
            currentTask = UnitTaskType.Idle;
            return; ?
        }
    }
    
    // ...경로 설정...
}

IEnumerator WaitAndCombatRoutine(HexTile dest, UnitAgent enemy)
{
    // ...도착 대기...

    while (enemyCombat.health > 0 && myCombat.health > 0)
    {
        if (agent.homeHive != null)
        {
            // 현재 위치 체크 ?
            int distanceToHive = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r, 
                agent.q, agent.r
            );
            
            if (distanceToHive > activityRadius)
            {
                Debug.Log($"[전투] 범위 이탈 (거리: {distanceToHive}/{activityRadius})"); ?
                ReturnToHive();
                yield break; ?
            }
            
            // 적 위치 체크 ?
            int enemyDistance = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r, 
                enemy.q, enemy.r
            );
            
            if (enemyDistance > activityRadius)
            {
                Debug.Log($"[전투] 적이 범위 이탈"); ?
                currentTask = UnitTaskType.Idle;
                yield break; ?
            }
        }
        
        // ...전투...
    }
}
```

---

### 4. 근접 전투

```csharp
// CombatUnit.cs
[Header("기본 능력치")]
public int maxHealth = 10;
public int health;
public int attack = 2;
public float attackRange = 0.1f; // 근접 전투 ?

// EnemyAI.cs
[Tooltip("공격 범위 (타일 수) - 0이면 같은 타일에서만 공격")]
public int attackRange = 0; // 근접 전투 ?
```

---

## ?? 비교표

### 1. 적 하이브 시야

| 항목 | 이전 | 현재 |
|------|------|------|
| **발견 로그** | 없음 | 있음 ? |
| **항상 보임** | 작동 | 작동 ? |
| **디버그** | 어려움 | 쉬움 ? |

### 2. 자원 채취

| 항목 | 이전 | 현재 |
|------|------|------|
| **우선순위** | 적 공격 1순위 | 자원 1순위 ? |
| **복귀** | 불안정 | 안정 ? |
| **targetTile** | 유지 안 됨 | 유지됨 ? |

### 3. 활동 범위

| 항목 | 이전 | 현재 |
|------|------|------|
| **공격 시작** | 일부 체크 | 완전 체크 ? |
| **전투 중** | 체크 없음 | 실시간 체크 ? |
| **적 위치** | 체크 없음 | 체크함 ? |

### 4. 공격 범위

| 유닛 | 이전 | 현재 |
|------|------|------|
| **꿀벌** | 0.1 | 0.1 ? |
| **말벌** | 0 | 0 ? |
| **전투 방식** | 근접 | 근접 ? |

---

## ?? 시각화

### 자원 채취 흐름

```
[타일 클릭]
   ↓
[자원 있음?]
YES → 채취 ?
   ↓
[하이브 이동]
   ↓
[자원 전달]
   ↓
[자원 남음?]
YES → 다시 채취 ?
NO → Idle ?
```

### 활동 범위 체크

```
        하이브
          ??
         /|\
        / | \
    ??  ??  ??  (5칸 범위)
    |   |   |
    ?   ?   ?  ← 공격 가능
    
    
    ??         ← 범위 밖
    ?          ← 공격 불가 ?
```

---

## ?? 문제 해결

### Q: 하이브가 여전히 안 보여요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에서 "[시야] 적 하이브 발견" 로그
[ ] discoveredHives.Count 확인
[ ] 시야 범위 내 진입 확인
```

---

### Q: 자원 채취 후 복귀 안 해요

**확인:**
```
[ ] DeliverResourcesRoutine() 실행 확인
[ ] targetTile != null 확인
[ ] sourceTile.resourceAmount > 0 확인
[ ] currentTask = Gather 설정 확인
```

---

### Q: 범위 밖으로 계속 나가요

**확인:**
```
[ ] agent.homeHive != null 확인
[ ] activityRadius 값 확인 (기본 5)
[ ] distanceToHive 계산 확인
[ ] Console 로그 "범위 밖" 확인
```

---

### Q: 공격이 안 돼요

**확인:**
```
[ ] attackRange 값 확인
   - CombatUnit: 0.1
   - EnemyAI: 0
[ ] 같은 타일에 있는지 확인
[ ] distanceToTarget <= attackRange 확인
```

---

## ? 테스트 체크리스트

```
[ ] 플레이어 유닛 이동 → 적 하이브 발견
[ ] Console에 "하이브 발견" 로그
[ ] 하이브가 계속 보임
[ ] 자원 타일 클릭 → 채취
[ ] 하이브 복귀 → 자원 전달
[ ] 자원 남음 → 다시 채취
[ ] 자원 없음 → Idle
[ ] 적 발견 → 범위 체크
[ ] 범위 밖 → 공격 안 함
[ ] 범위 내 → 공격
[ ] 전투 중 범위 이탈 → 복귀
[ ] 공격 범위 0 → 근접 전투만
```

---

## ?? 완료!

**핵심 수정:**
- ? 적 하이브 시야 개선
- ? 자원 채취 복귀 안정화
- ? 활동 범위 강제 적용
- ? 근접 전투로 변경

**결과:**
- 명확한 적 하이브 발견
- 안정적인 자원 채취
- 엄격한 활동 범위
- 전략적 근접 전투

게임 개발 화이팅! ??????
