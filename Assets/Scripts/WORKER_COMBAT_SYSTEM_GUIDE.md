# ?? 일꾼 전투 시스템 가이드

## 완료된 개선사항

### ? 1. 적 유닛 우클릭 공격 (활동 범위 체크)
- 일꾼 선택 후 적 유닛 우클릭 시 공격
- 활동 범위 내인지 자동 체크
- 범위 밖이면 명령 거부

### ? 2. 자동 방어 시스템
- Idle 상태에서 주기적으로 적 탐지
- 같은 타일 적 진입 시 즉시 교전
- 주변 1칸 내 적 발견 시 자동 추격

---

## ?? 수정된 파일

### 1. TileClickMover.cs
```csharp
? HandleRightClick() 수정
   - 유닛 우클릭 우선 체크
   - 적 유닛 → HandleAttackCommand()
   - 아군 유닛 → SelectUnit()
   
? HandleAttackCommand() 추가
   - 활동 범위 체크
   - behavior.IssueAttackCommand() 호출
```

### 2. UnitBehaviorController.cs
```csharp
? DetectNearbyEnemies() 추가
   - 주기적으로 적 탐지 (0.5초)
   - 같은 타일 적 → 즉시 교전
   - 주변 1칸 적 → 이동 후 교전
   
? IssueAttackCommand() 추가
   - 수동 공격 명령 처리
   - 활동 범위 체크
   - MoveAndAttack() 호출
   
? Update() 수정
   - 적 감지 주기 추가
```

---

## ?? 시스템 작동 방식

### 1. 적 유닛 우클릭 공격

#### 플레이어 행동
```
[일꾼 선택]
   ↓
[적 유닛 우클릭]
   ↓
[HandleRightClick()]
{
    // 유닛 체크 (우선)
    var unit = hit.GetComponent<UnitAgent>()
    
    if (unit.faction == Enemy) ?
    {
        HandleAttackCommand(unit)
    }
}
   ↓
[HandleAttackCommand(enemy)]
{
    // 1. 활동 범위 체크 ?
    int hiveDistance = AxialDistance(
        homeHive.q, homeHive.r,
        enemy.q, enemy.r
    )
    
    if (hiveDistance > activityRadius) ?
    {
        debugText = "적이 활동 범위 밖입니다"
        return
    }
    
    // 2. 공격 명령 실행 ?
    behavior.IssueAttackCommand(enemy)
}
   ↓
[IssueAttackCommand(enemy)]
{
    // 활동 범위 재체크
    if (distanceToHive > activityRadius)
    {
        return
    }
    
    // 공격 명령
    currentTask = Attack
    targetUnit = enemy
    MoveAndAttack(enemy) ?
}
   ↓
[MoveAndAttack(enemy)]
{
    // 경로 탐색
    path = FindPath(start, enemy.q, enemy.r)
    mover.SetPath(path)
    
    // 전투 루틴 시작
    StartCoroutine(WaitAndCombatRoutine(dest, enemy))
}
   ↓
[결과]
일꾼이 적에게 이동 → 전투! ?
```

---

### 2. 자동 방어 시스템

#### Idle 상태 주기적 탐지
```
[Update()]
{
    if (currentTask == Idle) ?
    {
        if (Time.time - lastEnemyDetection > 0.5f) ?
        {
            lastEnemyDetection = Time.time
            DetectNearbyEnemies() ?
        }
    }
}
   ↓
[DetectNearbyEnemies()]
{
    // 1. 현재 타일 적 체크 ?
    var currentTile = GetTile(agent.q, agent.r)
    var enemy = FindEnemyOnTile(currentTile)
    
    if (enemy != null) ?
    {
        // 활동 범위 체크
        if (distanceToHive <= activityRadius)
        {
            // 즉시 교전! ?
            currentTask = Attack
            targetUnit = enemy
            StartCoroutine(WaitAndCombatRoutine(currentTile, enemy))
            return
        }
    }
    
    // 2. 주변 1칸 적 탐색 ?
    var nearbyEnemy = FindNearbyEnemy(1)
    
    if (nearbyEnemy != null) ?
    {
        // 이동 후 교전 ?
        currentTask = Attack
        targetUnit = nearbyEnemy
        MoveAndAttack(nearbyEnemy)
    }
}
   ↓
[FindNearbyEnemy(1)]
{
    foreach (var unit in TileManager.GetAllUnits())
    {
        // 적 유닛만
        if (unit.faction != agent.faction)
        {
            // 거리 체크
            int distance = AxialDistance(agent, unit)
            
            if (distance <= 1) ?
            {
                // 활동 범위 체크 ?
                int distanceToHive = AxialDistance(homeHive, unit)
                
                if (distanceToHive <= activityRadius) ?
                {
                    return unit ?
                }
            }
        }
    }
}
   ↓
[결과]
적이 접근하면 자동으로 전투! ?
```

---

## ?? 핵심 코드

### 1. TileClickMover - 적 우클릭 처리

```csharp
// TileClickMover.cs
void HandleRightClick()
{
    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
    {
        // 먼저 유닛 체크 (타일보다 우선) ?
        var unit = hit.collider.GetComponentInParent<UnitAgent>();
        if (unit != null)
        {
            // 적 유닛 우클릭 시 공격 명령 ?
            if (unit.faction == Faction.Enemy && 
                selectedUnitInstance != null && 
                selectedUnitInstance.faction == Faction.Player)
            {
                HandleAttackCommand(unit); ?
                return;
            }
            // 아군 유닛은 선택
            else if (unit.faction == Faction.Player)
            {
                SelectUnit(unit);
                return;
            }
        }

        var tile = hit.collider.GetComponentInParent<HexTile>();
        if (tile != null)
        {
            OnTileCommand(tile);
            return;
        }
    }
}

/// <summary>
/// 적 유닛 공격 명령 처리 (활동 범위 체크) ?
/// </summary>
void HandleAttackCommand(UnitAgent enemy)
{
    if (selectedUnitInstance == null || enemy == null) return;
    
    // 플레이어 유닛만 공격 가능
    if (selectedUnitInstance.faction != Faction.Player) return;
    
    // 활동 범위 체크 ?
    var behavior = selectedUnitInstance.GetComponent<UnitBehaviorController>();
    if (behavior != null)
    {
        // 적이 하이브로부터 활동 범위 내인지 확인 ?
        if (selectedUnitInstance.homeHive != null)
        {
            int hiveDistance = Pathfinder.AxialDistance(
                selectedUnitInstance.homeHive.q, 
                selectedUnitInstance.homeHive.r,
                enemy.q, enemy.r
            );
            
            if (hiveDistance > behavior.activityRadius)
            {
                if (debugText != null)
                    debugText.text = $"적이 활동 범위 밖입니다 ({hiveDistance}/{behavior.activityRadius})";
                return;
            }
        }
        
        // 공격 명령 실행 ?
        behavior.IssueAttackCommand(enemy);
        
        if (debugText != null)
            debugText.text = $"적 유닛 공격 명령: ({enemy.q}, {enemy.r})";
    }
}
```

---

### 2. UnitBehaviorController - 자동 방어

```csharp
// UnitBehaviorController.cs
void Update()
{
    // ...existing code...
    
    // Idle 상태에서 주기적으로 적 감지 ?
    if (currentTask == UnitTaskType.Idle && 
        Time.time - lastEnemyDetection > enemyDetectionInterval)
    {
        lastEnemyDetection = Time.time;
        DetectNearbyEnemies(); ?
    }
}

/// <summary>
/// Surrounding enemy detection and automatic combat ?
/// </summary>
void DetectNearbyEnemies()
{
    if (agent == null || combat == null) return;
    
    // 현재 타일의 적 확인 ?
    var currentTile = TileManager.Instance?.GetTile(agent.q, agent.r);
    if (currentTile != null)
    {
        var enemy = FindEnemyOnTile(currentTile);
        if (enemy != null)
        {
            // 활동 범위 체크 ?
            if (agent.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(
                    agent.homeHive.q, agent.homeHive.r, 
                    currentTile.q, currentTile.r
                );
                
                if (distanceToHive > activityRadius)
                {
                    // 적이 활동 범위 밖이면 무시
                    return;
                }
            }
            
            // 같은 타일에 적 발견 → 즉시 교전 ?
            currentTask = UnitTaskType.Attack;
            targetUnit = enemy;
            StartCoroutine(WaitAndCombatRoutine(currentTile, enemy));
            return;
        }
    }
    
    // 주변 1칸 내 적 탐색 ?
    var nearbyEnemy = FindNearbyEnemy(1);
    if (nearbyEnemy != null)
    {
        // 주변에 적 발견 → 이동 후 교전 ?
        currentTask = UnitTaskType.Attack;
        targetUnit = nearbyEnemy;
        MoveAndAttack(nearbyEnemy);
    }
}

/// <summary>
/// 주변 범위 내 적 유닛 찾기 ?
/// </summary>
UnitAgent FindNearbyEnemy(int range)
{
    if (TileManager.Instance == null) return null;
    
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit == null || unit == agent) continue;
        
        // 적 유닛만
        if (unit.faction == agent.faction) continue;
        if (unit.faction == Faction.Neutral) continue;
        
        // 거리 체크 ?
        int distance = Pathfinder.AxialDistance(agent.q, agent.r, unit.q, unit.r);
        if (distance <= range)
        {
            // 하이브가 있는 경우 활동 범위 체크 ?
            if (agent.homeHive != null)
            {
                int distanceToHive = Pathfinder.AxialDistance(
                    agent.homeHive.q, agent.homeHive.r, 
                    unit.q, unit.r
                );
                
                if (distanceToHive > activityRadius)
                {
                    // 적이 활동 범위 밖
                    continue;
                }
            }
            
            return unit;
        }
    }
    
    return null;
}

/// <summary>
/// 적 유닛 공격 명령 (수동 명령) ?
/// </summary>
public void IssueAttackCommand(UnitAgent enemy)
{
    if (enemy == null) return;
    
    // Mark that this worker received a manual order
    if (agent.isFollowingQueen)
    {
        agent.hasManualOrder = true;
        agent.isFollowingQueen = false;
    }
    
    // 활동 범위 체크 ?
    if (agent.homeHive != null)
    {
        int distanceToHive = Pathfinder.AxialDistance(
            agent.homeHive.q, agent.homeHive.r,
            enemy.q, enemy.r
        );
        
        if (distanceToHive > activityRadius)
        {
            Debug.Log($"[전투] {agent.name}: 적이 활동 범위 밖입니다.");
            return;
        }
    }
    
    // 공격 명령 실행 ?
    currentTask = UnitTaskType.Attack;
    targetUnit = enemy;
    MoveAndAttack(enemy);
    
    Debug.Log($"[전투] {agent.name}: 적 유닛 공격 명령 받음 → {enemy.name}");
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **적 우클릭** | 선택만 ? | 공격 명령 ? |
| **활동 범위 체크** | 없음 ? | 자동 체크 ? |
| **자동 방어** | 없음 ? | 주기적 탐지 ? |
| **적 진입 감지** | 없음 ? | 즉시 교전 ? |

---

## ?? 시각화

### 적 우클릭 공격 흐름

```
[플레이어]
일꾼 선택
   ↓
적 유닛 우클릭
   ↓
[HandleAttackCommand]
활동 범위 체크
   ↓
하이브로부터 거리 = 4
활동 범위 = 5
   ↓
범위 내! ?
   ↓
[IssueAttackCommand]
공격 명령 실행
   ↓
[MoveAndAttack]
적에게 이동
   ↓
[WaitAndCombatRoutine]
도착 → 전투 시작!
```

### 자동 방어 흐름

```
[Idle 상태]
자원 채취 완료
currentTask = Idle
   ↓
[Update() - 0.5초마다]
DetectNearbyEnemies()
   ↓
[같은 타일 체크]
var enemy = FindEnemyOnTile(currentTile)
   ↓
적 발견! ?
   ↓
활동 범위 체크
distanceToHive = 3
activityRadius = 5
   ↓
범위 내! ?
   ↓
즉시 교전!
StartCoroutine(WaitAndCombatRoutine)
   ↓
[전투 시작]
일꾼 vs 적 말벌!
```

---

## ?? 문제 해결

### Q: 적 우클릭해도 공격 안 해요

**확인:**
```
[ ] 일꾼 선택됨
[ ] 적이 활동 범위 내
[ ] Console에 "적 유닛 공격 명령"
```

**해결:**
```
1. 일꾼 선택 확인
   - selectedUnitInstance != null
   - faction == Player

2. 활동 범위 확인
   - homeHive != null
   - hiveDistance <= activityRadius

3. Console 로그 확인
   - "[전투] 적 유닛 공격 명령 받음"
```

---

### Q: 자동 방어가 작동 안 해요

**확인:**
```
[ ] currentTask == Idle
[ ] enemyDetectionInterval = 0.5초
[ ] DetectNearbyEnemies() 호출
```

**해결:**
```
1. Update() 확인
   - Idle 상태인지
   - 0.5초마다 호출되는지

2. DetectNearbyEnemies() 확인
   - 같은 타일 적 체크
   - 주변 1칸 적 체크

3. 활동 범위 확인
   - 적이 범위 내에 있는지
```

---

### Q: 적이 진입해도 가만히 있어요

**확인:**
```
[ ] Idle 상태
[ ] 적이 같은 타일
[ ] FindEnemyOnTile() 작동
```

**해결:**
```
1. currentTask 확인
   - Idle 상태여야 탐지 작동

2. 적 위치 확인
   - agent.q == enemy.q
   - agent.r == enemy.r

3. 활동 범위 확인
   - distanceToHive <= activityRadius
```

---

## ?? 추가 개선 아이디어

### 1. 공격 명령 시각 피드백

```csharp
// TileClickMover.cs
void HandleAttackCommand(UnitAgent enemy)
{
    // ...existing code...
    
    // 공격 대상 하이라이트 ?
    if (TileHighlighter.Instance != null)
    {
        TileHighlighter.Instance.HighlightTarget(enemy, Color.red, 1f);
    }
    
    // 공격 라인 표시 ?
    if (LineRenderer.Instance != null)
    {
        Vector3 from = selectedUnitInstance.transform.position;
        Vector3 to = enemy.transform.position;
        LineRenderer.Instance.DrawAttackLine(from, to, 1f);
    }
    
    behavior.IssueAttackCommand(enemy);
}
```

---

### 2. 자동 방어 알림

```csharp
// UnitBehaviorController.cs
void DetectNearbyEnemies()
{
    // ...existing code...
    
    if (enemy != null)
    {
        // 알림 표시 ?
        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowNotification(
                $"{agent.name}: 적 발견! 교전 시작",
                2f,
                agent.transform.position
            );
        }
        
        // 사운드 재생 ?
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("CombatStart");
        }
        
        // 즉시 교전
        StartCoroutine(WaitAndCombatRoutine(currentTile, enemy));
    }
}
```

---

### 3. 그룹 공격 명령

```csharp
// TileClickMover.cs
void HandleRightClickForMultipleUnits(List<UnitAgent> units)
{
    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    UnitAgent targetEnemy = null;

    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
    {
        targetEnemy = hit.collider.GetComponentInParent<UnitAgent>();
    }

    // 적 유닛 우클릭 시 그룹 공격 ?
    if (targetEnemy != null && targetEnemy.faction == Faction.Enemy)
    {
        int attackCount = 0;
        
        foreach (var unit in units)
        {
            if (unit == null || unit.faction != Faction.Player) continue;

            var behavior = unit.GetComponent<UnitBehaviorController>();
            if (behavior != null)
            {
                // 활동 범위 체크
                if (unit.homeHive != null)
                {
                    int hiveDistance = Pathfinder.AxialDistance(
                        unit.homeHive.q, unit.homeHive.r,
                        targetEnemy.q, targetEnemy.r
                    );
                    
                    if (hiveDistance <= behavior.activityRadius)
                    {
                        behavior.IssueAttackCommand(targetEnemy);
                        attackCount++;
                    }
                }
            }
        }
        
        if (debugText != null)
        {
            debugText.text = $"{attackCount}개 유닛이 적 공격!";
        }
        
        return;
    }
    
    // ...existing code (타일 이동)...
}
```

---

## ? 테스트 체크리스트

```
[적 우클릭 공격]
[ ] 일꾼 선택
[ ] 적 유닛 우클릭
[ ] Console: "적 유닛 공격 명령" ?
[ ] 일꾼이 적에게 이동 ?
[ ] 도착 후 전투 시작 ?
[ ] 활동 범위 밖 적 → "범위 밖입니다" ?

[자동 방어]
[ ] 일꾼 Idle 상태
[ ] 적이 같은 타일 진입
[ ] Console: "적 발견" ?
[ ] 즉시 교전 시작 ?
[ ] 주변 1칸 적 → 이동 후 교전 ?
[ ] 활동 범위 밖 적 → 무시 ?
```

---

## ?? 완료!

**핵심 기능:**
- ? 적 우클릭 공격 명령
- ? 활동 범위 자동 체크
- ? 자동 방어 시스템
- ? 적 진입 즉시 교전

**결과:**
- 직관적인 공격 명령
- 안전한 활동 범위 제한
- 자동 방어로 안전성 향상
- 전략적 게임 플레이

**게임 플레이:**
- 일꾼 우클릭으로 적 공격
- 자원 채취 중 적 진입 시 자동 방어
- 활동 범위 내에서만 교전
- 자연스러운 전투 흐름

게임 개발 화이팅! ???????
