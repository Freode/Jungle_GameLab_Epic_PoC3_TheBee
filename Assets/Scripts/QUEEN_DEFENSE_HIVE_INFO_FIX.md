# ?? 여왕벌 방어 & 하이브 정보 표시 수정 가이드

## 완료된 개선사항

### 1. ? 하이브 없을 때 여왕벌 기준 적 공격
- 여왕벌 1칸 이내 적 자동 공격
- 일꾼도 여왕벌 1칸 이내 적 공격
- 하이브 없는 상황 대응

### 2. ? 하이브 클릭 시 하이브 정보 표시
- Initialize() 하이브 자신 제외
- Hive 컴포넌트 체크 추가
- 올바른 정보 표시

---

## ?? 수정된 파일

### 1. UnitBehaviorController.cs
```csharp
? FindNearbyEnemy() 수정
   - 하이브 없을 때 여왕벌 기준 체크
   - 여왕벌 isQueen 플래그 활용
   - 일꾼도 여왕벌 1칸 이내만 공격
```

### 2. Hive.cs
```csharp
? Initialize() 수정
   - 하이브 자신의 UnitAgent 제외
   - Hive 컴포넌트 체크
   - workers 리스트 정확성 개선
```

---

## ?? 시스템 작동 방식

### 1. 여왕벌 기준 적 공격

#### 시나리오 A: 하이브 있음
```
[하이브 존재]
??
 |
 ↓
[활동 범위 체크]
5칸 이내 → 공격 가능 ?
5칸 밖 → 공격 불가
```

#### 시나리오 B: 하이브 없음 + 여왕벌
```
[하이브 없음]
homeHive == null
   ↓
[여왕벌 체크]
agent.isQueen? → YES
   ↓
[여왕벌 기준 1칸 체크] ?
distance <= 1 → 공격 가능 ?
distance > 1 → 공격 불가
```

#### 시나리오 C: 하이브 없음 + 일꾼
```
[하이브 없음]
homeHive == null
   ↓
[여왕벌 아님]
agent.isQueen? → NO
   ↓
[여왕벌 찾기] ?
FindQueenInScene()
   ↓
[여왕벌 기준 1칸 체크] ?
distanceToQueen <= 1 → 공격 가능 ?
distanceToQueen > 1 → 공격 불가
```

---

### 2. 하이브 정보 표시

#### 문제 상황 (이전)
```
[Hive.Initialize()]
   ↓
[기존 일꾼 탐색]
foreach (unit in AllUnits)
{
    if (unit.faction == Player && !isQueen)
    {
        workers.Add(unit) ?
    }
}
   ↓
[문제 발생]
workers = [Hive 자신, Worker1, Worker2] ?
   ↓
[하이브 클릭]
UnitCommandPanel.Show(hive)
   ↓
[workers[0] = Hive] ?
→ 하이브가 일꾼으로 취급됨
→ 일꾼 정보 표시됨
```

#### 해결 (현재)
```
[Hive.Initialize()]
   ↓
[하이브 자신 확인] ?
var hiveAgent = GetComponent<UnitAgent>()
   ↓
[기존 일꾼 탐색]
foreach (unit in AllUnits)
{
    // 하이브 자신 제외 ?
    if (unit == hiveAgent) continue;
    
    // Hive 컴포넌트 체크 ?
    if (unit.GetComponent<Hive>() != null) continue;
    
    if (unit.faction == Player && !isQueen)
    {
        workers.Add(unit) ?
    }
}
   ↓
[정상 상태]
workers = [Worker1, Worker2] ?
   ↓
[하이브 클릭]
UnitCommandPanel.Show(hive)
   ↓
[하이브 정보 표시] ?
HP: 200/200
일꾼 수: 2/10
```

---

## ?? 핵심 코드

### 1. 여왕벌 기준 적 공격

```csharp
// UnitBehaviorController.cs - FindNearbyEnemy()
UnitAgent FindNearbyEnemy(int range)
{
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        // 적 유닛 체크...
        int distance = Pathfinder.AxialDistance(agent.q, agent.r, unit.q, unit.r);
        
        if (distance <= range)
        {
            // 하이브가 있는 경우
            if (agent.homeHive != null)
            {
                // 활동 범위 체크 (기존 로직)
                int distanceToHive = Pathfinder.AxialDistance(
                    agent.homeHive.q, agent.homeHive.r, 
                    unit.q, unit.r
                );
                if (distanceToHive > activityRadius)
                {
                    continue;
                }
            }
            // 하이브가 없고 여왕벌인 경우 ?
            else if (agent.isQueen)
            {
                // 여왕벌 기준 1칸 이내만 공격 ?
                if (distance > 1)
                {
                    continue;
                }
            }
            // 하이브 없고 일꾼인 경우 ?
            else
            {
                // 여왕벌 위치 기준 1칸 이내 체크 ?
                UnitAgent queen = FindQueenInScene();
                if (queen != null)
                {
                    int distanceToQueen = Pathfinder.AxialDistance(
                        queen.q, queen.r, 
                        unit.q, unit.r
                    );
                    if (distanceToQueen > 1)
                    {
                        continue;
                    }
                }
                else
                {
                    // 여왕벌도 없으면 공격 안 함
                    continue;
                }
            }
            
            return unit;
        }
    }
    
    return null;
}
```

---

### 2. 하이브 정보 표시

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
    
    // 기존 일꾼 수 카운트
    int existingWorkerCount = 0;
    if (TileManager.Instance != null)
    {
        // 하이브 자신의 UnitAgent ?
        var hiveAgent = GetComponent<UnitAgent>();
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.faction == Faction.Player && !unit.isQueen)
            {
                // 하이브 자신은 제외 ?
                if (hiveAgent != null && unit == hiveAgent)
                {
                    continue;
                }
                
                // Hive 컴포넌트가 있는 유닛은 제외 ?
                var unitHive = unit.GetComponent<Hive>();
                if (unitHive != null)
                {
                    continue;
                }
                
                existingWorkerCount++;
                
                // 기존 일꾼을 workers 리스트에 추가
                if (!workers.Contains(unit))
                {
                    workers.Add(unit);
                    unit.homeHive = this;
                    
                    // 플래그 초기화
                    unit.hasManualOrder = false;
                    unit.isFollowingQueen = false;
                    
                    // 작업 취소
                    var behavior = unit.GetComponent<UnitBehaviorController>();
                    if (behavior != null)
                    {
                        behavior.CancelCurrentTask();
                    }
                }
            }
        }
    }
    
    if (showDebugLogs)
        Debug.Log($"[하이브 초기화] 기존 일꾼 수: {existingWorkerCount}, 최대: {maxWorkers}");
    
    // ...나머지 코드...
}
```

---

## ?? 비교표

### 1. 적 공격 범위

| 상황 | 이전 | 현재 |
|------|------|------|
| **하이브 있음** | 5칸 범위 | 5칸 범위 ? |
| **하이브 없음 + 여왕벌** | 공격 안 함 | 1칸 범위 ? |
| **하이브 없음 + 일꾼** | 공격 안 함 | 여왕벌 1칸 범위 ? |

### 2. 하이브 정보 표시

| 클릭 대상 | 이전 | 현재 |
|----------|------|------|
| **하이브** | 일꾼 정보 | 하이브 정보 ? |
| **일꾼** | 일꾼 정보 | 일꾼 정보 ? |
| **workers 리스트** | [Hive, Worker1] | [Worker1] ? |

---

## ?? 시각화

### 1. 여왕벌 방어 시스템

```
[하이브 있음]
      ??
     /|\
    / | \
   ?? ?? ??  (5칸 범위)
   |  |  |
   ?  ?  ?  공격 가능

[하이브 없음]
      ??
     /|\
    / | \
   ?? ?? ??  (1칸 범위) ?
   |  |  |
   ?  ?  ?  공격 가능
```

### 2. 공격 범위 비교

```
하이브 모드:
    ? ? ? ? ?
    ? ? ? ? ?
    ? ? ?? ? ?  ← 5칸 범위
    ? ? ? ? ?
    ? ? ? ? ?

여왕벌 모드:
        ?
      ? ?? ?    ← 1칸 범위 ?
        ?
```

### 3. 하이브 정보 표시

```
[이전]
workers = [Hive, Worker1, Worker2] ?
   ↓
[하이브 클릭]
   ↓
[일꾼 정보 표시] ?

[현재]
workers = [Worker1, Worker2] ?
   ↓
[하이브 클릭]
   ↓
[하이브 정보 표시] ?
```

---

## ?? 문제 해결

### Q: 여왕벌이 적을 공격 안 해요

**확인:**
```
[ ] agent.isQueen = true 설정
[ ] homeHive == null 확인
[ ] 적이 1칸 이내에 있는지 확인
[ ] FindNearbyEnemy() 실행 확인
```

**해결:**
```
1. Console에서 "적 발견" 로그 확인
2. agent.isQueen 값 확인 (디버그)
3. distance <= 1 체크 확인
```

---

### Q: 일꾼이 여왕벌 근처에서 공격 안 해요

**확인:**
```
[ ] FindQueenInScene() 작동
[ ] distanceToQueen <= 1 체크
[ ] 여왕벌이 씬에 존재하는지 확인
```

**해결:**
```
1. FindQueenInScene() 반환값 확인
2. 여왕벌 위치 확인
3. 거리 계산 확인
```

---

### Q: 하이브 클릭 시 여전히 일꾼 정보 나와요

**확인:**
```
[ ] Initialize() 실행 확인
[ ] hiveAgent != unit 체크
[ ] GetComponent<Hive>() 체크
[ ] workers 리스트 내용 확인
```

**해결:**
```
1. Debug.Log로 workers 리스트 출력
2. 하이브 자신이 포함되는지 확인
3. continue 문 실행 확인
```

---

## ?? 추가 개선 아이디어

### 1. 여왕벌 방어 범위 시각화

```csharp
// 여왕벌 선택 시 1칸 범위 표시
void OnDrawGizmosSelected()
{
    if (agent.isQueen && agent.homeHive == null)
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = TileHelper.HexToWorld(agent.q, agent.r, 0.5f);
        Gizmos.DrawWireSphere(pos, 1 * 0.5f); // 1칸 범위
    }
}
```

---

### 2. 여왕벌 경고 UI

```csharp
// 적이 1칸 내 침입 시 경고
if (agent.isQueen && FindNearbyEnemy(1) != null)
{
    WarningUI.Instance?.ShowWarning("적이 여왕벌 근처에 접근!");
}
```

---

### 3. 일꾼 자동 방어

```csharp
// 여왕벌 근처 일꾼 자동 방어 모드
public bool autoDefendQueen = true;

void Update()
{
    if (autoDefendQueen && agent.homeHive == null && !agent.isQueen)
    {
        var nearbyEnemy = FindNearbyEnemy(1);
        if (nearbyEnemy != null)
        {
            // 자동 공격
            MoveAndAttack(nearbyEnemy);
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 있음 → 5칸 범위 공격
[ ] 하이브 없음 + 여왕벌 → 1칸 범위 공격
[ ] 하이브 없음 + 일꾼 → 여왕벌 1칸 범위 공격
[ ] 하이브 클릭 → 하이브 정보 표시
[ ] 일꾼 클릭 → 일꾼 정보 표시
[ ] workers 리스트에 하이브 없음
```

---

## ?? 완료!

**핵심 수정:**
- ? 여왕벌 기준 1칸 방어
- ? 일꾼도 여왕벌 근처 방어
- ? 하이브 정보 정확 표시

**게임 플레이:**
- 하이브 없어도 방어 가능
- 여왕벌 근처 안전 지대
- 명확한 정보 표시

**밸런스:**
- 하이브 있음: 5칸 범위
- 하이브 없음: 1칸 범위
- 여왕벌 보호 강화

게임 개발 화이팅! ??????
