# ?? 3가지 주요 시스템 개선 가이드

## 완료된 개선사항

### 1. ? 일꾼 꿀벌 활동 범위 제한
- 활동 범위 밖 적 자동 감지 제거
- 수동 명령 시 활동 범위 체크
- 전투 중 활동 범위 체크

### 2. ? 말벌집 최대 일꾼 수 6마리로 변경
- Enemy 하이브만 6마리로 제한
- Player 하이브는 기존 설정 유지

### 3. ? 새 하이브에 기존 일꾼 포함
- Initialize 시 기존 일꾼 자동 추가
- workers 리스트에 등록
- homeHive 자동 설정

---

## ?? 수정된 파일

### 1. UnitBehaviorController.cs
```csharp
? FindNearbyEnemy() 수정
   - 활동 범위 밖 적 무시
   
? IssueCommandToTile() 수정
   - 수동 명령 시 활동 범위 체크
```

### 2. Hive.cs
```csharp
? OnEnable() 수정
   - Enemy 하이브 maxWorkers = 6 설정
   
? Initialize() 수정
   - 기존 일꾼 자동 탐색 및 추가
   - workers 리스트에 등록
```

---

## ?? 시스템 작동 방식

### 1. 일꾼 꿀벌 활동 범위 제한

#### 자동 적 감지 시
```
[DetectNearbyEnemies 호출]
   ↓
[FindNearbyEnemy(1) 호출]
   ↓
[적 발견] ?
foreach (unit in AllUnits)
{
    if (적 유닛)
    {
        // 활동 범위 체크 ?
        int distanceToHive = Distance(homeHive, enemy)
        
        if (distanceToHive > activityRadius)
        {
            continue // 무시 ?
        }
        
        return unit // 범위 내 적
    }
}
```

#### 수동 명령 시
```
[IssueCommandToTile 호출]
   ↓
[적 타일 클릭]
   ↓
[활동 범위 체크] ?
if (distanceToHive > activityRadius)
{
    Debug.Log("활동 범위 밖") ?
    return // 명령 무시
}
   ↓
[공격 시작]
MoveAndAttack(enemy)
```

#### 전투 중 체크
```
[WaitAndCombatRoutine 실행]
   ↓
while (전투 중)
{
    // 활동 범위 체크 ?
    if (distanceToHive > activityRadius)
    {
        Debug.Log("범위 이탈, 복귀") ?
        ReturnToHive()
        yield break
    }
    
    // 공격...
}
```

---

### 2. 말벌집 최대 일꾼 수 6마리

```
[Enemy 하이브 OnEnable]
   ↓
[Faction 체크]
if (hiveAgent.faction == Enemy)
{
    // WaspWaveManager 등록
    WaspWaveManager.RegisterEnemyHive(this)
    
    // MaxWorkers 설정 ?
    maxWorkers = 6 ?
}
   ↓
[SpawnLoop 시작]
while (true)
{
    if (workers.Count < maxWorkers) // 6마리 제한 ?
    {
        SpawnWorker()
    }
}
```

---

### 3. 새 하이브에 기존 일꾼 포함

```
[하이브 건설]
Hive.Initialize(q, r)
   ↓
[기존 일꾼 탐색] ?
int existingWorkerCount = 0
foreach (unit in AllUnits)
{
    if (Player && !isQueen)
    {
        existingWorkerCount++
        
        // 리스트 추가 ?
        if (!workers.Contains(unit))
        {
            workers.Add(unit) ?
            unit.homeHive = this ?
        }
    }
}
   ↓
[로그 출력] ?
Debug.Log($"기존 일꾼 수: {existingWorkerCount}")
   ↓
[스폰 루프 시작]
SpawnLoop() // 기존 수 + 새로 생성
```

---

## ?? 핵심 코드

### 1. 활동 범위 제한

```csharp
// UnitBehaviorController.cs

// 자동 적 감지
UnitAgent FindNearbyEnemy(int range)
{
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        // 적 유닛 체크...
        
        // 활동 범위 체크 ?
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r, 
                unit.q, unit.r
            );
            
            if (distanceToHive > activityRadius)
            {
                continue; // 무시 ?
            }
        }
        
        return unit;
    }
}

// 수동 명령
public void IssueCommandToTile(HexTile tile)
{
    var enemy = FindEnemyOnTile(tile);
    if (enemy != null)
    {
        // 활동 범위 체크 ?
        if (agent.homeHive != null)
        {
            int distanceToHive = Pathfinder.AxialDistance(
                agent.homeHive.q, agent.homeHive.r, 
                tile.q, tile.r
            );
            
            if (distanceToHive > activityRadius)
            {
                Debug.Log("활동 범위 밖"); ?
                return;
            }
        }
        
        MoveAndAttack(enemy);
    }
}
```

---

### 2. 말벌집 6마리 제한

```csharp
// Hive.cs - OnEnable()
void OnEnable()
{
    // ...기존 코드...
    
    // Enemy 하이브 체크
    var hiveAgent = GetComponent<UnitAgent>();
    if (hiveAgent != null && hiveAgent.faction == Faction.Enemy)
    {
        // WaspWaveManager 등록
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.RegisterEnemyHive(this);
        }
        
        // maxWorkers를 6으로 설정 ?
        maxWorkers = 6;
    }
    
    spawnRoutine = StartCoroutine(SpawnLoop());
}
```

---

### 3. 기존 일꾼 포함

```csharp
// Hive.cs - Initialize()
public void Initialize(int q, int r)
{
    this.q = q; 
    this.r = r;
    
    // 업그레이드 적용
    if (HiveManager.Instance != null)
    {
        maxWorkers = HiveManager.Instance.GetMaxWorkers();
    }
    
    // 기존 일꾼 수 카운트 ?
    int existingWorkerCount = 0;
    if (TileManager.Instance != null)
    {
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.faction == Faction.Player && !unit.isQueen)
            {
                existingWorkerCount++;
                
                // 기존 일꾼을 workers 리스트에 추가 ?
                if (!workers.Contains(unit))
                {
                    workers.Add(unit);
                    unit.homeHive = this;
                }
            }
        }
    }
    
    if (showDebugLogs)
        Debug.Log($"기존 일꾼 수: {existingWorkerCount}, 최대: {maxWorkers}");
    
    // 스폰 루프 시작
    spawnRoutine = StartCoroutine(SpawnLoop());
    
    // ...나머지 코드...
}
```

---

## ?? 비교표

### 1. 활동 범위 제한

| 상황 | 이전 | 현재 |
|------|------|------|
| **자동 적 감지** | 모든 적 | 범위 내만 ? |
| **수동 명령** | 모든 타일 | 범위 내만 ? |
| **전투 중** | 추격 계속 | 범위 이탈 시 복귀 ? |

### 2. 말벌집 일꾼 수

| 진영 | 이전 | 현재 |
|------|------|------|
| **플레이어** | 10 (기본) | 10 (기본) ? |
| **적 (말벌)** | 10 (기본) | 6 ? |

### 3. 하이브 일꾼 수

| 시점 | 이전 | 현재 |
|------|------|------|
| **건설 직후** | 0 | 기존 일꾼 수 ? |
| **리스트** | 비어있음 | 기존 일꾼 포함 ? |
| **homeHive** | null | 새 하이브 ? |

---

## ?? 시각화

### 1. 활동 범위 제한

```
        하이브
          ??
         / | \
        /  |  \
       /   |   \
      ??  ??  ??  ← 활동 범위 내 (5칸)
      |    |   |
      ?    ?   ?  ← 적 공격 가능
      
      
         ??        ← 활동 범위 밖
         ?         ← 공격 불가 ?
```

### 2. 말벌집 일꾼 수

```
플레이어 하이브:
?? → ???????????????????? (최대 10마리)

말벌집:
??? → ???????????? (최대 6마리) ?
```

### 3. 하이브 일꾼 포함

```
[하이브 건설 전]
?? ?? ?? ?? ??  (기존 5마리)
?? (여왕벌)

      ↓ 하이브 건설

[하이브 건설 후]
      ??
   / | | | \
  ?? ?? ?? ?? ??  (기존 5마리 포함) ?
  
workers.Count = 5 ?
maxWorkers = 10
→ 앞으로 5마리 더 생성 가능
```

---

## ? 테스트 체크리스트

```
[ ] 일꾼이 활동 범위 밖 적 자동 감지 안 함
[ ] 일꾼이 활동 범위 밖 타일 수동 명령 안 됨
[ ] 전투 중 범위 이탈 시 하이브로 복귀
[ ] 말벌집 일꾼 수 최대 6마리
[ ] 플레이어 하이브 일꾼 수 최대 10마리 (기본)
[ ] 새 하이브 건설 시 기존 일꾼 포함
[ ] 새 하이브 workers 리스트에 기존 일꾼 등록
[ ] 기존 일꾼의 homeHive가 새 하이브로 설정
```

---

## ?? 문제 해결

### Q: 일꾼이 여전히 범위 밖 적을 공격해요

**확인:**
```
[ ] agent.homeHive != null
[ ] activityRadius 값 확인 (기본 5)
[ ] Pathfinder.AxialDistance() 작동
[ ] FindNearbyEnemy() 범위 체크 코드
```

**해결:**
```
1. Console에서 "활동 범위 밖" 로그 확인
2. homeHive 설정 확인
3. activityRadius 값 증가 시도
```

### Q: 말벌집이 6마리 이상 생성해요

**확인:**
```
[ ] Hive.OnEnable() 실행 확인
[ ] hiveAgent.faction == Enemy 확인
[ ] maxWorkers = 6 설정 확인
[ ] SpawnLoop의 체크 로직 확인
```

**해결:**
```
1. showDebugLogs = true 설정
2. "최대 일꾼 수 도달: X/6" 로그 확인
3. RemoveAll(w => w == null) 작동 확인
```

### Q: 새 하이브에 기존 일꾼이 안 포함돼요

**확인:**
```
[ ] Initialize() 호출 확인
[ ] TileManager.GetAllUnits() 작동
[ ] unit.faction == Player 확인
[ ] workers.Add(unit) 실행
[ ] unit.homeHive = this 설정
```

**해결:**
```
1. showDebugLogs = true 설정
2. "기존 일꾼 수: X" 로그 확인
3. workers.Count 확인
```

---

## ?? 추가 개선 아이디어

### 1. 활동 범위 시각화

```csharp
// 하이브 선택 시 활동 범위 표시
void OnDrawGizmosSelected()
{
    if (agent.homeHive != null)
    {
        Gizmos.color = Color.green;
        Vector3 hivePos = TileHelper.HexToWorld(
            agent.homeHive.q, agent.homeHive.r, 0.5f
        );
        Gizmos.DrawWireSphere(hivePos, activityRadius * 0.5f);
    }
}
```

### 2. 말벌집 난이도별 일꾼 수

```csharp
// Hive.cs
public enum Difficulty { Easy, Normal, Hard }
public Difficulty difficulty = Normal;

void OnEnable()
{
    if (hiveAgent.faction == Enemy)
    {
        switch (difficulty)
        {
            case Easy: maxWorkers = 4; break;
            case Normal: maxWorkers = 6; break; ?
            case Hard: maxWorkers = 8; break;
        }
    }
}
```

### 3. 하이브 일꾼 마이그레이션 애니메이션

```csharp
// 기존 일꾼이 새 하이브로 이동하는 애니메이션
IEnumerator MigrateWorkers()
{
    foreach (var worker in existingWorkers)
    {
        // 새 하이브로 이동
        MoveWorkerToHive(worker, this);
        yield return new WaitForSeconds(0.5f);
    }
}
```

---

## ?? 완료!

**핵심 수정:**
- ? 일꾼 활동 범위 제한 (자동 + 수동 + 전투)
- ? 말벌집 최대 6마리
- ? 새 하이브에 기존 일꾼 포함

**게임 플레이:**
- 전략적 위치 선정 중요
- 말벌 난이도 조절
- 하이브 이전 시 일꾼 유지

**밸런스:**
- 플레이어: 최대 10마리
- 적 (말벌): 최대 6마리
- 활동 범위: 5칸 (기본)

게임 개발 화이팅! ??????
