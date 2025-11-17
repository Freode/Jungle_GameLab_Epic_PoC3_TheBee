# ?? 일꾼 적 탐지 활동 범위 제한 수정 가이드

## 완료된 개선사항

### ? 일꾼이 활동 범위 밖 적 탐지 불가
- DetectNearbyEnemies() 활동 범위 체크 추가
- FindNearbyEnemy() 이미 활동 범위 체크됨
- 같은 타일 적도 활동 범위 체크

### ? EnemyAI.cs 코드 오류 수정
- FindNearestPlayerUnit() 중복 변수 제거
- IsWithinHiveRange() 중복 코드 제거

---

## ?? 수정된 파일

### 1. UnitBehaviorController.cs
```csharp
? DetectNearbyEnemies() 수정
   - 같은 타일 적 활동 범위 체크 추가
   - distanceToHive > activityRadius → 무시
```

### 2. EnemyAI.cs
```csharp
? FindNearestPlayerUnit() 수정
   - 중복 변수 선언 제거
   - nearestHive, nearest 변수 정리
   
? IsWithinHiveRange() 수정
   - 중복 코드 제거
   - 단순화
```

---

## ?? 시스템 작동 방식

### 문제 상황 (이전)

```
[일꾼 위치]
(5, 5)
   ↓
[하이브 위치]
(0, 0)
   ↓
[적 위치]
(10, 10) ← 하이브로부터 거리 14
   ↓
[DetectNearbyEnemies() 호출]
   ↓
[같은 타일 체크]
currentTile에 적 있음?
   NO
   ↓
[FindNearbyEnemy(1) 호출]
   ↓
[활동 범위 체크] ? (이미 있음)
distanceToHive = 14 > activityRadius = 5
   → 무시 ?
   
하지만...

[같은 타일에 적이 있는 경우]
currentTile에 적 있음?
   YES → 즉시 교전 ?
   
[문제]
활동 범위 체크 없음 ?
```

---

### 해결 (현재)

```
[일꾼 위치]
(5, 5)
   ↓
[하이브 위치]
(0, 0)
   ↓
[적 위치]
(5, 5) ← 일꾼과 같은 타일
하이브로부터 거리 7
   ↓
[DetectNearbyEnemies() 호출]
   ↓
[같은 타일 체크]
currentTile에 적 있음?
   YES
   ↓
[활동 범위 체크 추가] ?
if (agent.homeHive != null)
{
    distanceToHive = Distance(homeHive, currentTile)
                   = Distance((0,0), (5,5))
                   = 7
    
    if (distanceToHive > activityRadius) // 7 > 5
    {
        return; // 무시 ?
    }
}
   ↓
[결과]
활동 범위 밖 적은 탐지 안 됨 ?
```

---

## ?? 핵심 코드

### DetectNearbyEnemies() 수정

```csharp
// UnitBehaviorController.cs
void DetectNearbyEnemies()
{
    if (agent == null || combat == null) return;
    
    // 현재 타일의 적 확인
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
                    // 적이 활동 범위 밖이면 무시 ?
                    return;
                }
            }
            
            // 같은 타일에 적 발견 → 즉시 교전
            currentTask = UnitTaskType.Attack;
            targetUnit = enemy;
            StartCoroutine(WaitAndCombatRoutine(currentTile, enemy));
            return;
        }
    }
    
    // 주변 1칸 내 적 탐색 (FindNearbyEnemy는 이미 활동 범위 체크함)
    var nearbyEnemy = FindNearbyEnemy(1);
    if (nearbyEnemy != null)
    {
        // 주변에 적 발견 → 이동 후 교전
        currentTask = UnitTaskType.Attack;
        targetUnit = nearbyEnemy;
        MoveAndAttack(nearbyEnemy);
    }
}
```

---

### FindNearbyEnemy() (이미 수정됨)

```csharp
// UnitBehaviorController.cs
UnitAgent FindNearbyEnemy(int range)
{
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        // ...적 유닛 체크...
        
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
                    continue; // 적이 활동 범위 밖 ?
                }
            }
            
            return unit;
        }
    }
    
    return null;
}
```

---

## ?? 비교표

### 적 탐지

| 상황 | 이전 | 현재 |
|------|------|------|
| **같은 타일 적** | 항상 탐지 | 범위 체크 ? |
| **주변 1칸 적** | 범위 체크 | 범위 체크 ? |
| **활동 범위 밖** | 탐지됨 | 탐지 안 됨 ? |

---

## ?? 시각화

### 활동 범위

```
        하이브
          ??
         /|\
        / | \
       /  |  \
      /   |   \
    ??   ??   ??  (활동 범위 5칸)
    |    |    |
    ??   ??   ??  ← 범위 내 적 (탐지 O)
    
    
         ??        ← 범위 밖 적 (탐지 X) ?
```

---

### 탐지 프로세스

```
[일꾼 Idle 상태]
   ↓
[0.5초마다 적 감지]
DetectNearbyEnemies()
   ↓
[1단계: 같은 타일 체크] ?
currentTile에 적?
   YES → 활동 범위 체크 ?
      IN → 교전
      OUT → 무시 ?
   NO → 2단계
   ↓
[2단계: 주변 1칸 체크] ?
FindNearbyEnemy(1)
   → 이미 활동 범위 체크됨 ?
```

---

## ?? 문제 해결

### Q: 일꾼이 활동 범위 밖 적을 공격해요

**확인:**
```
[ ] DetectNearbyEnemies() 활동 범위 체크 실행
[ ] distanceToHive 계산 정확성
[ ] activityRadius 값 확인 (기본 5)
[ ] agent.homeHive != null 확인
```

**해결:**
```
1. Console에서 거리 계산 로그 확인
2. activityRadius 값 증가 시도
3. homeHive 설정 확인
```

---

### Q: 같은 타일에 적이 있는데 공격 안 해요

**확인:**
```
[ ] 활동 범위 내에 있는지 확인
[ ] distanceToHive <= activityRadius
[ ] DetectNearbyEnemies() 호출 확인
[ ] currentTask가 Idle인지 확인
```

**해결:**
```
1. 하이브와 일꾼 사이 거리 확인
2. activityRadius 값 확인
3. enemyDetectionInterval 확인 (0.5초)
```

---

### Q: EnemyAI.cs 컴파일 에러가 나요

**확인:**
```
[ ] FindNearestPlayerUnit() 변수 중복 제거
[ ] IsWithinHiveRange() 중복 코드 제거
[ ] 빌드 성공 확인
```

**해결:**
```
1. 수정된 코드 확인
2. 빌드 다시 실행
3. Console에서 에러 메시지 확인
```

---

## ?? 추가 개선 아이디어

### 1. 활동 범위 시각화

```csharp
// 일꾼 선택 시 활동 범위 표시
void OnDrawGizmosSelected()
{
    if (agent.homeHive != null)
    {
        Gizmos.color = Color.cyan;
        Vector3 hivePos = TileHelper.HexToWorld(
            agent.homeHive.q, agent.homeHive.r, 0.5f
        );
        Gizmos.DrawWireSphere(hivePos, activityRadius * 0.5f);
    }
}
```

---

### 2. 적 탐지 로그

```csharp
// DetectNearbyEnemies() 수정
void DetectNearbyEnemies()
{
    var enemy = FindEnemyOnTile(currentTile);
    if (enemy != null)
    {
        int distanceToHive = Distance(homeHive, currentTile);
        
        if (distanceToHive > activityRadius)
        {
            Debug.Log($"[적 탐지] {agent.name}: 적이 범위 밖 (거리: {distanceToHive})");
            return;
        }
        
        Debug.Log($"[적 탐지] {agent.name}: 적 발견! 교전 시작");
        // ...
    }
}
```

---

### 3. 범위 경고 UI

```csharp
// 활동 범위 밖 적 클릭 시 경고
public void IssueCommandToTile(HexTile tile)
{
    var enemy = FindEnemyOnTile(tile);
    if (enemy != null)
    {
        int distanceToHive = Distance(homeHive, tile);
        
        if (distanceToHive > activityRadius)
        {
            // UI 경고 표시 ?
            WarningUI.Instance?.ShowWarning(
                "활동 범위를 벗어난 적입니다!\n공격할 수 없습니다."
            );
            return;
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 일꾼이 하이브 근처에 있음
[ ] 적이 활동 범위 내 → 자동 공격
[ ] 적이 활동 범위 밖 → 공격 안 함
[ ] 같은 타일 적도 범위 체크
[ ] 주변 1칸 적도 범위 체크
[ ] 전투 중 범위 이탈 → 복귀
[ ] EnemyAI.cs 빌드 성공
```

---

## ?? 완료!

**핵심 수정:**
- ? DetectNearbyEnemies() 활동 범위 체크
- ? 같은 타일 적도 범위 확인
- ? EnemyAI.cs 코드 오류 수정

**결과:**
- 일꾼이 활동 범위 밖 적 무시
- 전략적 위치 선정 중요
- 하이브 근처 안전 지대

**게임 플레이:**
- 하이브 위치 중요
- 활동 범위 내에서만 전투
- 예측 가능한 AI 행동

게임 개발 화이팅! ??????
