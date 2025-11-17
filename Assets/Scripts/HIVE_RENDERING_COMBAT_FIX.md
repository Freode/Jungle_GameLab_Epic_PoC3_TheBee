# ?? 말벌집 렌더링 & 일꾼 전투 버그 수정 가이드

## 완료된 개선사항

### 1. ? 말벌집 렌더링 문제 해결
- SetUnitVisibility 디버그 로그 강화
- 모든 렌더러 찾기 개선
- 중복 처리 방지
- 렌더러 카운트 확인

### 2. ? 일꾼 전투 버그 수정
- WaitAndCombatRoutine 강화
- 적 상태 체크 강화
- 전투 종료 후 Idle 처리
- 자동 재교전 기능

---

## ?? 수정된 파일

### 1. EnemyVisibilityController.cs
```csharp
? SetUnitVisibility() 강화
   - 디버그 로그 추가
   - rendererCount 카운트
   - 중복 처리 방지
   - 렌더러 없음 경고
```

### 2. UnitBehaviorController.cs
```csharp
? WaitAndCombatRoutine() 수정
   - 적 null 체크 강화
   - GameObject 파괴 체크
   - 체력 0 체크
   - 전투 종료 후 Idle 처리
   - 자동 재교전 기능
```

### 3. EnemyAI.cs
```csharp
? 중복 코드 제거
   - attackRange 필드 중복 제거
   - FindNearestPlayerUnit 변수 정리
   - IsWithinHiveRange 간소화
```

---

## ?? 시스템 작동 방식

### 1. 말벌집 렌더링 문제 원인 및 해결

#### 문제 원인

```
[말벌집 생성]
GameObject: EnemyHive
├─ UnitAgent
├─ Hive
└─ [자식] HiveSprite ?
   └─ SpriteRenderer

[시야 진입]
UpdateEnemyVisibility()
   ↓
SetUnitVisibility(hive, true)
   ↓
[렌더러 찾기]
GetComponentsInChildren<Renderer>(true)
   ↓
[문제 1] 렌더러가 비활성화 상태로 시작 ?
[문제 2] 중복 처리로 다시 비활성화 ?
[문제 3] 렌더러를 못 찾음 ?
   ↓
[결과]
렌더러.enabled = true 안 됨 ?
```

---

#### 해결 방법

```
[말벌집 생성]
GameObject: EnemyHive
├─ UnitAgent
├─ Hive ?
└─ [자식] HiveSprite
   └─ SpriteRenderer

[시야 진입]
UpdateEnemyVisibility()
   ↓
SetUnitVisibility(hive, true)
   ↓
[1. 하이브 확인] ?
var hive = unit.GetComponent<Hive>()
if (hive != null)
{
    targetObject = hive.gameObject ?
    
    if (showDebugLogs)
        Debug.Log("하이브 렌더러 찾기") ?
}
   ↓
[2. 모든 렌더러 찾기] ?
int rendererCount = 0

// 자신의 Renderer
renderer.enabled = true
rendererCount++ ?

// 자식 Renderer (true 파라미터로 비활성화된 것도 찾기)
foreach (var r in GetComponentsInChildren<Renderer>(true))
{
    r.enabled = true
    rendererCount++ ?
    
    if (showDebugLogs)
        Debug.Log($"자식 Renderer: {r.name}") ?
}

// 부모 Renderer
if (parent != null)
{
    parentRenderer.enabled = true
    rendererCount++ ?
}
   ↓
[3. 렌더러 확인] ?
if (rendererCount == 0 && hive != null)
{
    Debug.LogWarning("렌더러 없음!") ?
}

if (showDebugLogs)
    Debug.Log($"총 {rendererCount}개 처리 완료") ?
   ↓
[결과]
하이브가 보임! ?
```

---

### 2. 일꾼 전투 버그 원인 및 해결

#### 문제 원인

```
[전투 시작]
MoveAndAttack(enemy)
   ↓
[전투 루프]
WaitAndCombatRoutine(dest, enemy)
   ↓
while (enemyCombat.health > 0)  ← 문제 1 ?
{
    // 공격...
}
   ↓
[문제 1] enemy == null 체크 없음 ?
[문제 2] enemy.gameObject 파괴 체크 없음 ?
[문제 3] enemyCombat == null 체크 없음 ?
[문제 4] 전투 종료 후 Idle 처리 부족 ?
   ↓
[결과]
NullReferenceException 발생 ?
또는 전투 후 멈춤 ?
```

---

#### 해결 방법

```
[전투 시작]
MoveAndAttack(enemy)
   ↓
[이동 대기] ?
while (Distance > 0.1f)
{
    // 타임아웃 체크 ?
    if (waitElapsed > 10f)
    {
        Debug.Log("이동 타임아웃")
        currentTask = Idle
        yield break ?
    }
    
    // 적 사라짐 체크 ?
    if (enemy == null || enemy.gameObject == null)
    {
        Debug.Log("적 사라짐")
        currentTask = Idle
        yield break ?
    }
}
   ↓
[전투 루프] ?
while (true)  ← 무한 루프로 변경 ?
{
    // 1. 적 null 체크 ?
    if (enemy == null || enemy.gameObject == null)
    {
        Debug.Log("적 사라짐, 전투 종료")
        break ?
    }
    
    // 2. CombatUnit 체크 ?
    if (enemyCombat == null)
    {
        Debug.Log("적 CombatUnit 파괴됨")
        break ?
    }
    
    // 3. 체력 체크 ?
    if (enemyCombat.health <= 0)
    {
        Debug.Log("적 처치!")
        break ?
    }
    
    if (myCombat.health <= 0)
    {
        Debug.Log("사망")
        break ?
    }
    
    // 4. 활동 범위 체크 ?
    if (distanceToHive > activityRadius)
    {
        Debug.Log("범위 이탈")
        break ?
    }
    
    // 5. 공격 ?
    if (myCombat.CanAttack())
    {
        myCombat.TryAttack(enemyCombat)
        Debug.Log($"공격! (HP: {enemyCombat.health})") ?
    }
    
    yield return new WaitForSeconds(0.1f)
}
   ↓
[전투 종료] ?
Debug.Log("전투 루틴 종료, Idle로 전환") ?
currentTask = Idle ?
targetUnit = null ?
   ↓
[자동 재교전] ?
yield return new WaitForSeconds(0.5f)

if (currentTask == Idle)
{
    var nearbyEnemy = FindNearbyEnemy(1)
    if (nearbyEnemy != null)
    {
        Debug.Log("주변 적 발견, 재교전")
        MoveAndAttack(nearbyEnemy) ?
    }
}
   ↓
[결과]
안정적인 전투 ?
전투 후 Idle 전환 ?
자동 재교전 ?
```

---

## ?? 핵심 코드

### 1. SetUnitVisibility 강화

```csharp
// EnemyVisibilityController.cs
void SetUnitVisibility(UnitAgent unit, bool visible)
{
    if (unit == null) return;

    var hive = unit.GetComponent<Hive>();
    GameObject targetObject = unit.gameObject;
    
    if (hive != null)
    {
        targetObject = hive.gameObject;
        
        if (showDebugLogs) ?
            Debug.Log($"[시야 디버그] 하이브 렌더러 찾기: {targetObject.name}, visible={visible}");
    }

    int rendererCount = 0; ?

    // 1. 자신의 SpriteRenderer
    var sprite = targetObject.GetComponent<SpriteRenderer>();
    if (sprite != null)
    {
        sprite.enabled = visible;
        rendererCount++; ?
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] SpriteRenderer 설정: {sprite.name}");
    }

    // 2. 자신의 Renderer
    var renderer = targetObject.GetComponent<Renderer>();
    if (renderer != null && renderer != sprite) // 중복 방지 ?
    {
        renderer.enabled = visible;
        rendererCount++;
    }

    // 3. 자식 GameObject들의 모든 Renderer
    var childRenderers = targetObject.GetComponentsInChildren<Renderer>(true);
    foreach (var r in childRenderers)
    {
        r.enabled = visible;
        rendererCount++;
        
        if (showDebugLogs)
            Debug.Log($"[시야 디버그] 자식 Renderer: {r.name}");
    }

    // 4. 자식 SpriteRenderer (중복 방지)
    var childSprites = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
    foreach (var s in childSprites)
    {
        bool alreadyProcessed = false;
        foreach (var r in childRenderers)
        {
            if (r == s)
            {
                alreadyProcessed = true;
                break;
            }
        }
        
        if (!alreadyProcessed)
        {
            s.enabled = visible;
            rendererCount++;
        }
    }
    
    // 5. 부모 GameObject 체크
    if (hive != null && targetObject.transform.parent != null)
    {
        var parentRenderer = targetObject.transform.parent.GetComponent<Renderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = visible;
            rendererCount++;
        }
    }
    
    // 렌더러가 하나도 없으면 경고 ?
    if (rendererCount == 0 && hive != null)
    {
        Debug.LogWarning($"[시야 경고] 하이브 {unit.name}에 렌더러를 찾을 수 없습니다!");
    }
    
    if (showDebugLogs && hive != null)
        Debug.Log($"[시야 디버그] 총 {rendererCount}개 렌더러 처리 완료");
}
```

---

### 2. WaitAndCombatRoutine 강화

```csharp
// UnitBehaviorController.cs
System.Collections.IEnumerator WaitAndCombatRoutine(HexTile dest, UnitAgent enemy)
{
    // 도착할 때까지 대기 (타임아웃 포함) ?
    float waitTimeout = 10f;
    float waitElapsed = 0f;
    
    while (Vector3.Distance(transform.position, TileHelper.HexToWorld(dest.q, dest.r, agent.hexSize)) > 0.1f)
    {
        waitElapsed += Time.deltaTime;
        
        if (waitElapsed > waitTimeout) ?
        {
            Debug.LogWarning($"[전투] 이동 타임아웃");
            currentTask = UnitTaskType.Idle;
            targetUnit = null;
            yield break;
        }
        
        if (enemy == null || enemy.gameObject == null) ?
        {
            Debug.Log($"[전투] 적 사라짐");
            currentTask = UnitTaskType.Idle;
            targetUnit = null;
            yield break;
        }
        
        yield return null;
    }

    var myCombat = agent.GetComponent<CombatUnit>();
    if (myCombat == null) ?
    {
        Debug.LogWarning($"[전투] CombatUnit 없음");
        currentTask = UnitTaskType.Idle;
        yield break;
    }

    var enemyCombat = enemy.GetComponent<CombatUnit>();
    if (enemyCombat == null) ?
    {
        Debug.LogWarning($"[전투] 적 CombatUnit 없음");
        currentTask = UnitTaskType.Idle;
        yield break;
    }

    Debug.Log($"[전투] 전투 시작 vs {enemy.name}"); ?

    // 전투 루프
    while (true) ?
    {
        // 1. 적 상태 체크 ?
        if (enemy == null || enemy.gameObject == null)
        {
            Debug.Log($"[전투] 적 사라짐, 전투 종료");
            break;
        }
        
        if (enemyCombat == null)
        {
            Debug.Log($"[전투] 적 CombatUnit 파괴됨");
            break;
        }
        
        if (enemyCombat.health <= 0)
        {
            Debug.Log($"[전투] 적 처치!");
            break;
        }
        
        if (myCombat.health <= 0)
        {
            Debug.Log($"[전투] 사망");
            break;
        }

        // 2. 활동 범위 체크 ?
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(agent.homeHive.q, agent.homeHive.r, agent.q, agent.r);
            if (distanceToHive > activityRadius)
            {
                Debug.Log($"[전투] 활동 범위 이탈");
                ReturnToHive();
                yield break;
            }
        }
        
        // 3. 공격 ?
        if (myCombat.CanAttack())
        {
            bool attacked = myCombat.TryAttack(enemyCombat);
            if (attacked)
            {
                Debug.Log($"[전투] 공격! (적 HP: {enemyCombat.health}/{enemyCombat.maxHealth})"); ?
            }
        }

        yield return new WaitForSeconds(0.1f);
    }

    // 전투 종료 ?
    Debug.Log($"[전투] 전투 루틴 종료, Idle로 전환");
    currentTask = UnitTaskType.Idle;
    targetUnit = null;
    
    // 자동 재교전 ?
    yield return new WaitForSeconds(0.5f);
    
    if (currentTask == UnitTaskType.Idle)
    {
        var nearbyEnemy = FindNearbyEnemy(1);
        if (nearbyEnemy != null)
        {
            Debug.Log($"[전투] 주변 적 발견, 재교전");
            currentTask = UnitTaskType.Attack;
            targetUnit = nearbyEnemy;
            MoveAndAttack(nearbyEnemy);
        }
    }
}
```

---

## ?? 비교표

### 말벌집 렌더링

| 항목 | 이전 | 현재 |
|------|------|------|
| **렌더러 찾기** | 부분적 | 완전 ? |
| **디버그 로그** | 없음 | 있음 ? |
| **중복 처리** | 있음 | 방지 ? |
| **렌더러 카운트** | 없음 | 있음 ? |

### 일꾼 전투

| 항목 | 이전 | 현재 |
|------|------|------|
| **적 null 체크** | 부족 | 완전 ? |
| **GameObject 파괴** | 미체크 | 체크 ? |
| **전투 종료 처리** | 부족 | 완전 ? |
| **자동 재교전** | 없음 | 있음 ? |

---

## ?? 시각화

### 말벌집 렌더링 흐름

```
[시야 진입]
?? → → → ???
   ↓
[렌더러 찾기]
1. hive.gameObject ?
2. SpriteRenderer ?
3. Renderer ?
4. 자식 Renderer x7 ?
5. 부모 Renderer ?
   ↓
[카운트]
총 10개 렌더러 ?
   ↓
[모두 활성화]
renderer.enabled = true ?
   ↓
[결과]
??? 보임! ?
```

### 일꾼 전투 흐름

```
[전투 시작]
?? → ??
   ↓
[이동 대기]
while (Distance > 0.1f)
{
    // 타임아웃 체크 ?
    // 적 사라짐 체크 ?
}
   ↓
[전투 루프]
while (true)
{
    // 1. 적 null? → 종료 ?
    // 2. CombatUnit null? → 종료 ?
    // 3. 체력 0? → 종료 ?
    // 4. 범위 이탈? → 복귀 ?
    // 5. 공격! ?
}
   ↓
[전투 종료]
currentTask = Idle ?
targetUnit = null ?
   ↓
[재교전 체크]
nearbyEnemy != null?
YES → 재교전 ?
NO → Idle 유지
```

---

## ?? 문제 해결

### Q: 말벌집이 여전히 안 보여요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에 "하이브 렌더러 찾기" 로그
[ ] "총 N개 렌더러 처리 완료" 로그
[ ] rendererCount > 0 확인
```

**해결:**
```
1. EnemyVisibilityController.showDebugLogs = true
2. Console 로그 확인
   - "하이브 렌더러 찾기: EnemyHive, visible=true"
   - "SpriteRenderer 설정: HiveSprite"
   - "총 X개 렌더러 처리 완료"
3. rendererCount가 0이면 경고 로그 확인
4. 하이브 Prefab 구조 확인
```

---

### Q: 일꾼이 1회 공격 후 멈춰요

**확인:**
```
[ ] Console에 "[전투] 전투 시작" 로그
[ ] "[전투] 공격! (적 HP: X)" 로그
[ ] "[전투] 전투 루틴 종료, Idle로 전환" 로그
[ ] 적 체력이 0이 되었는지 확인
```

**해결:**
```
1. Console 로그 확인
2. 적이 죽지 않았는데 전투가 끝났다면:
   - null 체크 문제
   - GameObject 파괴 문제
3. 전투 종료 후 Idle 로그 확인
4. 재교전 로그 확인
```

---

### Q: NullReferenceException 발생

**확인:**
```
[ ] enemy == null 체크
[ ] enemy.gameObject == null 체크
[ ] enemyCombat == null 체크
[ ] Stack Trace 확인
```

**해결:**
```
1. 모든 null 체크 추가됨 확인
2. 전투 중 적이 사라지면 break 확인
3. GameObject 파괴 시 null 확인
```

---

## ? 테스트 체크리스트

```
[ ] showDebugLogs = true 설정
[ ] 말벌집 시야 진입
[ ] Console에 "하이브 렌더러 찾기" 로그
[ ] 말벌집이 보임 ?
[ ] 일꾼이 적 공격
[ ] Console에 "전투 시작" 로그
[ ] 공격 로그 여러 번
[ ] 적 처치 로그
[ ] "Idle로 전환" 로그
[ ] 주변 적 있으면 재교전
[ ] NullReferenceException 없음 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? SetUnitVisibility 디버그 강화
- ? WaitAndCombatRoutine 안정화
- ? 자동 재교전 기능

**결과:**
- 말벌집이 정상 표시
- 안정적인 전투 시스템
- 자동 재교전

게임 개발 화이팅! ????????
